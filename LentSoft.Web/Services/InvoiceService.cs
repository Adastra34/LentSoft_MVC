using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LentSoft.Web.Data;
using LentSoft.Web.Models;
using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public class InvoiceService : IInvoiceService
{
    private readonly LentSoftDbContext _context;
    private readonly DianSettings _dianSettings;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(
        LentSoftDbContext context,
        IOptions<DianSettings> dianOptions,
        ILogger<InvoiceService> logger)
    {
        _context = context;
        _dianSettings = dianOptions?.Value ?? new DianSettings();
        _logger = logger;
    }

    public async Task<(List<Invoice> Items, int TotalCount)> GetAllAsync(string? searchTerm, int page, int pageSize)
    {
        var query = _context.Invoices
            .Where(i => i.Activo)
            .Include(i => i.Order)
                .ThenInclude(o => o!.User)
            .Include(i => i.Order)
                .ThenInclude(o => o!.Pagos)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(i =>
                i.NumeroFactura.ToLower().Contains(term) ||
                (i.Order != null && i.Order.User != null && (
                    i.Order.User.Nombre.ToLower().Contains(term) ||
                    i.Order.User.Apellido.ToLower().Contains(term) ||
                    (i.Order.User.Nombre + " " + i.Order.User.Apellido).ToLower().Contains(term)
                ))
            );
        }

        var totalCount = await query.CountAsync();

        if (pageSize < 1) pageSize = 5;
        if (page < 1) page = 1;

        var items = await query
            .OrderByDescending(i => i.FechaEmision)
            .ThenByDescending(i => i.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Invoice?> GetByIdAsync(int id)
    {
        return await _context.Invoices
            .Include(i => i.Order)
                .ThenInclude(o => o!.User)
            .Include(i => i.Order)
                .ThenInclude(o => o!.OrderItems)
                    .ThenInclude(oi => oi.Product)
            .Include(i => i.Order)
                .ThenInclude(o => o!.Pagos)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<Invoice> CreateAsync(Invoice invoice)
    {
        // 1. Evitar doble facturación (validación anti-doble factura)
        var facturaExistente = await _context.Invoices
            .FirstOrDefaultAsync(i => i.OrderId == invoice.OrderId && i.Activo);

        if (facturaExistente != null)
        {
            throw new InvalidOperationException($"El pedido #ORD-{invoice.OrderId:D4} ya cuenta con una factura activa ({facturaExistente.NumeroFactura}).");
        }

        // 2. Cargar pedido asociado
        var order = await _context.Orders
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .Include(o => o.Pagos)
            .FirstOrDefaultAsync(o => o.Id == invoice.OrderId);

        if (order == null)
        {
            throw new KeyNotFoundException($"No se encontró la orden #{invoice.OrderId} para la factura.");
        }

        // 3. Fijar método de pago desde la venta original
        if (!string.IsNullOrWhiteSpace(order.MetodoPagoSimulado))
        {
            invoice.MetodoPago = order.MetodoPagoSimulado;
        }

        // 4. Calcular estado real automáticamente según el saldo del pedido (Requisito 2)
        invoice.Estado = CalcularEstadoSegunPedido(order, invoice.Estado);

        // 5. Garantizar número de factura único predeterminado (DIAN)
        var year = DateTime.UtcNow.Year;
        var count = (await _context.Invoices.CountAsync()) + 1;
        var candidate = $"FAC-{year}-{count:D4}";

        while (await _context.Invoices.AnyAsync(i => i.NumeroFactura == candidate))
        {
            count++;
            candidate = $"FAC-{year}-{count:D4}";
        }
        invoice.NumeroFactura = candidate;

        // Auto-calcular montos (Subtotal e IVA discriminado por ítem)
        if (invoice.Total == 0) invoice.Total = order.Total;

        if (invoice.Subtotal == 0 || invoice.Impuestos == 0)
        {
            decimal calcSubtotal = 0;
            decimal calcImpuestos = 0;

            if (order.OrderItems != null && order.OrderItems.Any())
            {
                foreach (var item in order.OrderItems)
                {
                    var rate = item.Product != null && item.Product.PorcentajeIva >= 0 ? item.Product.PorcentajeIva : 19.00m;
                    var baseUnit = rate > 0 ? (item.PrecioUnitario / (1m + (rate / 100m))) : item.PrecioUnitario;
                    var itemBaseTotal = baseUnit * item.Cantidad;
                    var itemIvaTotal = item.Subtotal - itemBaseTotal;

                    calcSubtotal += itemBaseTotal;
                    calcImpuestos += itemIvaTotal;
                }
            }
            else
            {
                calcSubtotal = Math.Round(invoice.Total / 1.19m, 2);
                calcImpuestos = invoice.Total - calcSubtotal;
            }

            if (invoice.Subtotal == 0) invoice.Subtotal = Math.Round(calcSubtotal, 2);
            if (invoice.Impuestos == 0) invoice.Impuestos = Math.Round(calcImpuestos, 2);
        }

        if (string.IsNullOrWhiteSpace(invoice.ResolucionDianNumero))
        {
            invoice.ResolucionDianNumero = _dianSettings.ResolucionNumero;
        }
        if (string.IsNullOrWhiteSpace(invoice.RangoAutorizadoDesde))
        {
            invoice.RangoAutorizadoDesde = _dianSettings.RangoDesde;
        }
        if (string.IsNullOrWhiteSpace(invoice.RangoAutorizadoHasta))
        {
            invoice.RangoAutorizadoHasta = _dianSettings.RangoHasta;
        }

        if (invoice.FechaEmision == default)
        {
            invoice.FechaEmision = DateTime.UtcNow;
        }

        // Generar CUFE determinístico DIAN
        var cleanNit = _dianSettings.EmpresaNit.Replace("-", "").Replace(" ", "");
        var rawCufe = $"{invoice.NumeroFactura}{invoice.FechaEmision:yyyyMMddHHmmss}{invoice.Total:F2}{invoice.Impuestos:F2}{cleanNit}";
        using (var sha = System.Security.Cryptography.SHA256.Create())
        {
            var hashBytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawCufe));
            invoice.CUFE = Convert.ToHexString(hashBytes).ToLower();
        }

        if (invoice.Estado == "pagada" && invoice.FechaPago == null)
        {
            invoice.FechaPago = DateTime.UtcNow;
        }

        try
        {
            _context.Invoices.Add(invoice);

            var auditoria = new AuditoriaVenta
            {
                TipoEntidad = "Factura",
                EntidadId = invoice.OrderId,
                EstadoNuevo = invoice.Estado,
                Accion = "CreacionFactura",
                Usuario = "Sistema/Ventas",
                Fecha = DateTime.UtcNow,
                Detalles = $"Emitida Factura {invoice.NumeroFactura} para orden #ORD-{invoice.OrderId:D4} por {invoice.Total:C}. CUFE: {invoice.CUFE}"
            };
            _context.AuditoriasVentas.Add(auditoria);

            await _context.SaveChangesAsync();
            _logger.LogInformation("Factura {NumeroFactura} creada exitosamente para pedido {OrderId}. Estado: {Estado}", invoice.NumeroFactura, invoice.OrderId, invoice.Estado);
            return invoice;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error al crear factura para pedido {OrderId}", invoice.OrderId);
            var innerMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            throw new InvalidOperationException($"No se pudo crear la factura debido a una restricción de datos en la base de datos: {innerMsg}", ex);
        }
    }

    public async Task<Invoice?> UpdateAsync(Invoice invoice)
    {
        var existing = await _context.Invoices
            .Include(i => i.Order).ThenInclude(o => o!.Pagos)
            .FirstOrDefaultAsync(i => i.Id == invoice.Id);

        if (existing == null) return null;

        var estadoAnterior = existing.Estado;

        // Calcular estado automáticamente según el saldo contable real del pedido
        if (existing.Order != null)
        {
            existing.Estado = CalcularEstadoSegunPedido(existing.Order, invoice.Estado);
        }
        else
        {
            existing.Estado = invoice.Estado;
        }

        if (!string.IsNullOrWhiteSpace(existing.Order?.MetodoPagoSimulado))
        {
            existing.MetodoPago = existing.Order.MetodoPagoSimulado;
        }
        else if (!string.IsNullOrWhiteSpace(invoice.MetodoPago))
        {
            existing.MetodoPago = invoice.MetodoPago;
        }
        
        if (invoice.Subtotal > 0) existing.Subtotal = invoice.Subtotal;
        if (invoice.Impuestos >= 0) existing.Impuestos = invoice.Impuestos;
        if (invoice.Total > 0) existing.Total = invoice.Total;

        if (existing.Estado == "pagada" && existing.FechaPago == null)
        {
            existing.FechaPago = DateTime.UtcNow;
        }
        else if (existing.Estado != "pagada")
        {
            existing.FechaPago = null;
        }

        try
        {
            if (estadoAnterior != existing.Estado)
            {
                var audit = new AuditoriaVenta
                {
                    TipoEntidad = "Factura",
                    EntidadId = existing.Id,
                    EstadoAnterior = estadoAnterior,
                    EstadoNuevo = existing.Estado,
                    Accion = "CambioEstadoFactura",
                    Usuario = "Sistema/Ventas",
                    Fecha = DateTime.UtcNow,
                    Detalles = $"Factura {existing.NumeroFactura} cambió estado de {estadoAnterior} a {existing.Estado}"
                };
                _context.AuditoriasVentas.Add(audit);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Factura {NumeroFactura} actualizada correctamente. Nuevo estado: {Estado}", existing.NumeroFactura, existing.Estado);
            return existing;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error al actualizar factura {Id}", invoice.Id);
            throw new InvalidOperationException("No se pudo actualizar la factura debido a una restricción de datos.", ex);
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var existing = await _context.Invoices.FindAsync(id);
        if (existing == null) return false;

        var estadoPrevio = existing.Estado;
        existing.Activo = false;
        existing.Estado = "cancelada";
        _context.Invoices.Update(existing);

        var audit = new AuditoriaVenta
        {
            TipoEntidad = "Factura",
            EntidadId = existing.Id,
            EstadoAnterior = estadoPrevio,
            EstadoNuevo = "cancelada",
            Accion = "CancelacionFactura",
            Usuario = "Sistema/Ventas",
            Fecha = DateTime.UtcNow,
            Detalles = $"Factura {existing.NumeroFactura} dada de baja/cancelada."
        };
        _context.AuditoriasVentas.Add(audit);

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Factura {NumeroFactura} anulada/cancelada.", existing.NumeroFactura);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cancelar factura {Id}", id);
            return false;
        }
    }

    public async Task<List<Order>> GetOrdersAvailableForInvoicingAsync()
    {
        return await _context.Orders
            .Where(o => o.Activo)
            .Include(o => o.User)
            .OrderByDescending(o => o.FechaPedido)
            .ToListAsync();
    }

    private static string CalcularEstadoSegunPedido(Order order, string? estadoSolicitado = null)
    {
        if (order == null) return "pendiente";
        if (string.Equals(order.Estado, "cancelado", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(estadoSolicitado, "cancelada", StringComparison.OrdinalIgnoreCase))
        {
            return "cancelada";
        }

        var total = order.Total;
        var saldo = order.SaldoPendiente;

        if (saldo <= 0)
        {
            return "pagada";
        }
        else if (saldo >= total)
        {
            return "pendiente";
        }
        else
        {
            return "parcial";
        }
    }
}
