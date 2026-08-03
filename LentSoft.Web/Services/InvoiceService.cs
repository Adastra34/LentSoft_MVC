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
                .ThenInclude(o => o.User)
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
                .ThenInclude(o => o.User)
            .Include(i => i.Order)
                .ThenInclude(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<Invoice> CreateAsync(Invoice invoice)
    {
        // Siempre garantizar número de factura único predeterminado (DIAN)
        var year = DateTime.UtcNow.Year;
        var count = (await _context.Invoices.CountAsync()) + 1;
        var candidate = $"FAC-{year}-{count:D4}";

        while (await _context.Invoices.AnyAsync(i => i.NumeroFactura == candidate))
        {
            count++;
            candidate = $"FAC-{year}-{count:D4}";
        }
        invoice.NumeroFactura = candidate;

        // Auto-calcular montos (Subtotal, IVA 19%, Total) basado en el pedido si no fueron provistos
        if (invoice.OrderId > 0)
        {
            var order = await _context.Orders.FindAsync(invoice.OrderId);
            if (order != null)
            {
                if (invoice.Total == 0) invoice.Total = order.Total;
                if (invoice.Subtotal == 0) invoice.Subtotal = Math.Round(invoice.Total / 1.19m, 2);
                if (invoice.Impuestos == 0) invoice.Impuestos = invoice.Total - invoice.Subtotal;
            }
        }

        if (invoice.FechaEmision == default)
        {
            invoice.FechaEmision = DateTime.UtcNow;
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
            throw new InvalidOperationException("No se pudo crear la factura debido a una restricción de datos en la base de datos.", ex);
        }
    }

    public async Task<Invoice?> UpdateAsync(Invoice invoice)
    {
        var existing = await _context.Invoices.FindAsync(invoice.Id);
        if (existing == null) return null;

        existing.Estado = invoice.Estado;
        existing.MetodoPago = invoice.MetodoPago;
        
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
