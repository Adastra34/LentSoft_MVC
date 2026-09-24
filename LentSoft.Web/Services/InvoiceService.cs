using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public class InvoiceService : IInvoiceService
{
    private readonly LentSoftDbContext _context;

    public InvoiceService(LentSoftDbContext context)
    {
        _context = context;
    }

    public async Task<(List<Invoice> Items, int TotalCount)> GetAllAsync(string? searchTerm, int page, int pageSize)
    {
        var query = _context.Invoices
            .Where(i => i.Activo)
            .Include(i => i.Order)
                .ThenInclude(o => o!.User)
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

        // 4. Validar estado respecto al saldo del pedido
        var saldoPendiente = order.SaldoPendiente;
        if (saldoPendiente > 0 && invoice.Estado == "pagada")
        {
            invoice.Estado = "pendiente";
        }

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
            invoice.ResolucionDianNumero = "18764028920000";
        }
        if (string.IsNullOrWhiteSpace(invoice.RangoAutorizadoDesde))
        {
            invoice.RangoAutorizadoDesde = "FAC-2026-0001";
        }
        if (string.IsNullOrWhiteSpace(invoice.RangoAutorizadoHasta))
        {
            invoice.RangoAutorizadoHasta = "FAC-2026-9999";
        }

        if (invoice.FechaEmision == default)
        {
            invoice.FechaEmision = DateTime.UtcNow;
        }

        // Generar CUFE determinístico DIAN
        var rawCufe = $"{invoice.NumeroFactura}{invoice.FechaEmision:yyyyMMddHHmmss}{invoice.Total:F2}{invoice.Impuestos:F2}9001234567";
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
            await _context.SaveChangesAsync();
            return invoice;
        }
        catch (DbUpdateException ex)
        {
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

        // Si el pedido tiene saldo pendiente, no permitir marcar la factura como pagada manualmente
        if (existing.Order != null && existing.Order.SaldoPendiente > 0 && invoice.Estado == "pagada")
        {
            throw new InvalidOperationException("No se puede marcar la factura como 'Pagada' manualmente porque el pedido asociado aún tiene un saldo pendiente de pago.");
        }

        existing.Estado = invoice.Estado;
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
            .Where(o => o.Activo)
            .Include(o => o.User)
            .OrderByDescending(o => o.FechaPedido)
            .ToListAsync();
    }
}
