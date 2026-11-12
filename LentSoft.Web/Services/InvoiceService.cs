using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public class InvoiceService : IInvoiceService
{
    private readonly LentSoftDbContext _context;
    private readonly IConfiguration? _configuration;

    public InvoiceService(LentSoftDbContext context, IConfiguration? configuration = null)
    {
        _context = context;
        _configuration = configuration;
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
        // 1. Evitar doble facturación (validación anti-doble factura con factura activa)
        var facturaExistente = await _context.Invoices
            .FirstOrDefaultAsync(i => i.OrderId == invoice.OrderId && i.Activo && i.Estado != "cancelada");

        if (facturaExistente != null)
        {
            throw new InvalidOperationException($"El pedido #ORD-{invoice.OrderId:D4} ya cuenta con una factura activa ({facturaExistente.NumeroFactura}).");
        }

        // 2. Cargar pedido asociado y validar su estado
        var order = await _context.Orders
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .Include(o => o.Pagos)
            .FirstOrDefaultAsync(o => o.Id == invoice.OrderId);

        if (order == null || !order.Activo)
        {
            throw new KeyNotFoundException($"No se encontró la orden activa #{invoice.OrderId} para la factura.");
        }

        if (string.Equals(order.Estado, "cancelado", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"No se puede emitir una factura sobre el pedido #ORD-{order.Id:D4} porque se encuentra cancelado.");
        }

        // 3. Fijar método de pago desde la venta original
        if (!string.IsNullOrWhiteSpace(order.MetodoPagoSimulado))
        {
            invoice.MetodoPago = order.MetodoPagoSimulado;
        }

        // 4. Calcular estado real automáticamente según el saldo del pedido
        invoice.Estado = CalcularEstadoSegunPedido(order, invoice.Estado);

        // 5. Lectura de configuración DIAN desde IConfiguration (sección Facturacion) con respaldo
        var resolucionDian = _configuration?["Facturacion:ResolucionDianNumero"] ?? "18764028920000";
        var rangoDesde = _configuration?["Facturacion:RangoAutorizadoDesde"] ?? "FAC-2026-0001";
        var rangoHasta = _configuration?["Facturacion:RangoAutorizadoHasta"] ?? "FAC-2026-9999";
        var nitEmisor = _configuration?["Facturacion:NitEmisor"] ?? "9001234567";

        invoice.ResolucionDianNumero = string.IsNullOrWhiteSpace(invoice.ResolucionDianNumero) || invoice.ResolucionDianNumero == "18764028920000"
            ? resolucionDian
            : invoice.ResolucionDianNumero;

        invoice.RangoAutorizadoDesde = string.IsNullOrWhiteSpace(invoice.RangoAutorizadoDesde) || invoice.RangoAutorizadoDesde == "FAC-2026-0001"
            ? rangoDesde
            : invoice.RangoAutorizadoDesde;

        invoice.RangoAutorizadoHasta = string.IsNullOrWhiteSpace(invoice.RangoAutorizadoHasta) || invoice.RangoAutorizadoHasta == "FAC-2026-9999"
            ? rangoHasta
            : invoice.RangoAutorizadoHasta;

        // Extraer límite superior del rango autorizado para validación
        int maxAutorizado = ExtraerConsecutivoDeNumero(invoice.RangoAutorizadoHasta);

        // 6. Cálculo obligatorio y exacto de Subtotal e IVA por cada ítem usando PorcentajeIva
        decimal calcSubtotal = 0m;
        decimal calcImpuestos = 0m;

        if (order.OrderItems != null && order.OrderItems.Any())
        {
            foreach (var item in order.OrderItems)
            {
                var rate = item.Product != null && item.Product.PorcentajeIva >= 0 ? item.Product.PorcentajeIva : 19.00m;
                var baseUnit = rate > 0 ? (item.PrecioUnitario / (1m + (rate / 100m))) : item.PrecioUnitario;
                var itemBaseTotal = baseUnit * item.Cantidad;
                var itemIvaTotal = (item.PrecioUnitario * item.Cantidad) - itemBaseTotal;

                calcSubtotal += itemBaseTotal;
                calcImpuestos += itemIvaTotal;
            }
        }
        else
        {
            calcSubtotal = order.Total > 0 ? Math.Round(order.Total / 1.19m, 2) : 0m;
            calcImpuestos = order.Total - calcSubtotal;
        }

        invoice.Subtotal = Math.Round(calcSubtotal, 2);
        invoice.Impuestos = Math.Round(calcImpuestos, 2);
        invoice.Total = order.Total;

        if (invoice.FechaEmision == default)
        {
            invoice.FechaEmision = DateTime.UtcNow;
        }

        if (invoice.Estado == "pagada" && invoice.FechaPago == null)
        {
            invoice.FechaPago = DateTime.UtcNow;
        }

        // 7. Generación de consecutivo único dentro de transacción con reintento si hay colisión
        var year = DateTime.UtcNow.Year;
        var prefix = $"FAC-{year}-";
        const int maxIntentos = 5;

        for (int intento = 1; intento <= maxIntentos; intento++)
        {
            var hasExternalTx = _context.Database.CurrentTransaction != null;
            var transaction = hasExternalTx ? null : await _context.Database.BeginTransactionAsync();

            try
            {
                // Consultar el mayor consecutivo del año actual
                var facturasDelAnio = await _context.Invoices
                    .Where(i => i.NumeroFactura.StartsWith(prefix))
                    .Select(i => i.NumeroFactura)
                    .ToListAsync();

                int maxConsecutivoActual = 0;
                foreach (var num in facturasDelAnio)
                {
                    int seq = ExtraerConsecutivoDeNumero(num);
                    if (seq > maxConsecutivoActual)
                    {
                        maxConsecutivoActual = seq;
                    }
                }

                int siguienteConsecutivo = maxConsecutivoActual + 1;

                // Validar que el consecutivo no supere el rango autorizado
                if (maxAutorizado > 0 && siguienteConsecutivo > maxAutorizado)
                {
                    throw new InvalidOperationException($"El consecutivo de facturación generado ({siguienteConsecutivo}) supera el límite autorizado por la DIAN en la resolución actual ({invoice.RangoAutorizadoHasta}). Solicite un nuevo rango a la DIAN.");
                }

                invoice.NumeroFactura = $"FAC-{year}-{siguienteConsecutivo:D4}";

                // Generar CUFE determinístico DIAN usando NIT configurado
                invoice.CUFE = GenerarCufe(invoice.NumeroFactura, invoice.FechaEmision, invoice.Total, invoice.Impuestos, nitEmisor);

                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();

                if (transaction != null)
                {
                    await transaction.CommitAsync();
                }

                return invoice;
            }
            catch (DbUpdateException ex)
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync();
                }
                _context.Entry(invoice).State = EntityState.Detached;

                if (intento >= maxIntentos)
                {
                    throw new InvalidOperationException("No se pudo generar un número de factura único tras múltiples intentos debido a concurrencia. Intente nuevamente.", ex);
                }

                await Task.Delay(50 * intento);
            }
            finally
            {
                if (transaction != null)
                {
                    await transaction.DisposeAsync();
                }
            }
        }

        throw new InvalidOperationException("Error inesperado al generar el consecutivo de la factura.");
    }

    public static string GenerarCufe(string numeroFactura, DateTime fechaEmision, decimal total, decimal impuestos, string nitEmisor)
    {
        var rawCufe = $"{numeroFactura}{fechaEmision:yyyyMMddHHmmss}{total:F2}{impuestos:F2}{nitEmisor}";
        using var sha = SHA256.Create();
        var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(rawCufe));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static int ExtraerConsecutivoDeNumero(string? numero)
    {
        if (string.IsNullOrWhiteSpace(numero)) return 0;
        var match = Regex.Match(numero, @"(\d+)$");
        return match.Success && int.TryParse(match.Value, out int val) ? val : 0;
    }

    public async Task<Invoice?> UpdateAsync(Invoice invoice)
    {
        var existing = await _context.Invoices
            .Include(i => i.Order).ThenInclude(o => o!.Pagos)
            .FirstOrDefaultAsync(i => i.Id == invoice.Id);

        if (existing == null) return null;

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
            await _context.SaveChangesAsync();
            return existing;
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("No se pudo actualizar la factura debido a una restricción de datos.", ex);
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var existing = await _context.Invoices.FindAsync(id);
        if (existing == null) return false;

        existing.Activo = false;
        _context.Invoices.Update(existing);

        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("No se pudo eliminar la factura porque está asociada a otros registros.", ex);
        }
    }

    public async Task<List<Order>> GetOrdersAvailableForInvoicingAsync()
    {
        return await _context.Orders
            .Where(o => o.Activo && o.Estado != "cancelado")
            .Where(o => !_context.Invoices.Any(i => i.OrderId == o.Id && i.Activo && i.Estado != "cancelada"))
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
