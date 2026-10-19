using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public class InvoiceService : IInvoiceService
{
    private readonly LentSoftDbContext _context;
    private readonly IConfiguration _configuration;

    public InvoiceService(LentSoftDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public InvoiceService(LentSoftDbContext context)
        : this(context, new ConfigurationBuilder().Build())
    {
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
            .FirstOrDefaultAsync(i => i.OrderId == invoice.OrderId && i.Activo && i.Estado != "cancelada");

        if (facturaExistente != null)
        {
            throw new InvalidOperationException($"El pedido #ORD-{invoice.OrderId:D4} ya cuenta con una factura activa ({facturaExistente.NumeroFactura}).");
        }

        // 2. Cargar pedido asociado
        var order = await _context.Orders
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .Include(o => o.Pagos)
            .FirstOrDefaultAsync(o => o.Id == invoice.OrderId);

        if (order == null || !order.Activo)
        {
            throw new KeyNotFoundException($"No se encontró la orden #{invoice.OrderId} para la factura.");
        }

        if (order.Estado == "cancelado")
        {
            throw new InvalidOperationException($"No se puede facturar el pedido #ORD-{invoice.OrderId:D4} porque está cancelado.");
        }

        // 3. Fijar método de pago desde la venta original
        if (!string.IsNullOrWhiteSpace(order.MetodoPagoSimulado))
        {
            invoice.MetodoPago = order.MetodoPagoSimulado;
        }

        // 4. Calcular estado real automáticamente según el saldo del pedido
        invoice.Estado = CalcularEstadoSegunPedido(order, invoice.Estado);

        // 5. Lectura de resolución DIAN, rango autorizado y NIT emisor desde IConfiguration ("Facturacion") con respaldo
        var facturacionSec = _configuration.GetSection("Facturacion");
        var resolucionDian = facturacionSec["ResolucionDianNumero"] ?? (!string.IsNullOrWhiteSpace(invoice.ResolucionDianNumero) ? invoice.ResolucionDianNumero : "18764028920000");
        var rangoDesde = facturacionSec["RangoAutorizadoDesde"] ?? (!string.IsNullOrWhiteSpace(invoice.RangoAutorizadoDesde) ? invoice.RangoAutorizadoDesde : "FAC-2026-0001");
        var rangoHasta = facturacionSec["RangoAutorizadoHasta"] ?? (!string.IsNullOrWhiteSpace(invoice.RangoAutorizadoHasta) ? invoice.RangoAutorizadoHasta : "FAC-2026-9999");
        var nitEmisor = facturacionSec["NitEmisor"] ?? "9001234567";

        invoice.ResolucionDianNumero = resolucionDian;
        invoice.RangoAutorizadoDesde = rangoDesde;
        invoice.RangoAutorizadoHasta = rangoHasta;

        // Validar límite superior del rango autorizado
        int maxConsecutivoPermitido = int.MaxValue;
        if (!string.IsNullOrWhiteSpace(rangoHasta))
        {
            var lastDash = rangoHasta.LastIndexOf('-');
            if (lastDash >= 0 && int.TryParse(rangoHasta.Substring(lastDash + 1), out var maxPermitido))
            {
                maxConsecutivoPermitido = maxPermitido;
            }
        }

        // 6. Calcular Subtotal e Impuestos SIEMPRE por ítem con PorcentajeIva del producto
        decimal calcSubtotal = 0;
        decimal calcImpuestos = 0;

        if (order.OrderItems != null && order.OrderItems.Any())
        {
            foreach (var item in order.OrderItems)
            {
                var rate = item.Product != null && item.Product.PorcentajeIva >= 0 ? item.Product.PorcentajeIva : 19.00m;
                var itemTotal = item.Subtotal > 0 ? item.Subtotal : (item.PrecioUnitario * item.Cantidad);
                var baseTotal = rate > 0 ? (itemTotal / (1m + (rate / 100m))) : itemTotal;
                var ivaTotal = itemTotal - baseTotal;

                calcSubtotal += baseTotal;
                calcImpuestos += ivaTotal;
            }
        }
        else
        {
            calcSubtotal = Math.Round(order.Total / 1.19m, 2);
            calcImpuestos = order.Total - calcSubtotal;
        }

        invoice.Total = order.Total;
        invoice.Subtotal = Math.Round(calcSubtotal, 2);
        invoice.Impuestos = Math.Round(calcImpuestos, 2);

        if (invoice.FechaEmision == default)
        {
            invoice.FechaEmision = DateTime.UtcNow;
        }

        if (invoice.Estado == "pagada" && invoice.FechaPago == null)
        {
            invoice.FechaPago = DateTime.UtcNow;
        }

        // 7. Número de factura único y consecutivo (mayor consecutivo + 1 con transacción y reintento)
        var year = DateTime.UtcNow.Year;
        var prefix = $"FAC-{year}-";

        var existingFacturas = await _context.Invoices
            .Where(i => i.NumeroFactura.StartsWith(prefix))
            .Select(i => i.NumeroFactura)
            .ToListAsync();

        int maxSeq = 0;
        foreach (var fac in existingFacturas)
        {
            if (fac.Length > prefix.Length && int.TryParse(fac.Substring(prefix.Length), out var seq) && seq > maxSeq)
            {
                maxSeq = seq;
            }
        }

        using var dbTx = await _context.Database.BeginTransactionAsync();
        const int maxRetries = 5;

        for (int retry = 0; retry < maxRetries; retry++)
        {
            var nextSeq = maxSeq + 1 + retry;
            if (nextSeq > maxConsecutivoPermitido)
            {
                throw new InvalidOperationException($"El consecutivo de facturación ({nextSeq}) supera el rango autorizado por la DIAN ({rangoHasta}).");
            }

            invoice.NumeroFactura = $"{prefix}{nextSeq:D4}";

            // Generar CUFE determinístico DIAN con NIT emisor configurado
            var rawCufe = $"{invoice.NumeroFactura}{invoice.FechaEmision:yyyyMMddHHmmss}{invoice.Total:F2}{invoice.Impuestos:F2}{nitEmisor}";
            using (var sha = SHA256.Create())
            {
                var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(rawCufe));
                invoice.CUFE = Convert.ToHexString(hashBytes).ToLower();
            }

            try
            {
                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();
                await dbTx.CommitAsync();
                return invoice;
            }
            catch (DbUpdateException ex)
            {
                if (retry == maxRetries - 1)
                {
                    var innerMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    throw new InvalidOperationException($"No se pudo crear la factura debido a una restricción de datos en la base de datos: {innerMsg}", ex);
                }
                _context.Entry(invoice).State = EntityState.Detached;
            }
        }

        return invoice;
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
        var ordersWithActiveInvoice = _context.Invoices
            .Where(i => i.Activo && i.Estado != "cancelada")
            .Select(i => i.OrderId);

        return await _context.Orders
            .Where(o => o.Activo && o.Estado != "cancelado" && !ordersWithActiveInvoice.Contains(o.Id))
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
