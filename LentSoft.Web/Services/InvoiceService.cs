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
        // Generar número de factura si no viene especificado
        if (string.IsNullOrWhiteSpace(invoice.NumeroFactura))
        {
            var year = DateTime.UtcNow.Year;
            var nextNum = (await _context.Invoices.CountAsync()) + 1;
            invoice.NumeroFactura = $"FAC-{year}-{nextNum:D4}";
        }

        // Si subtotal / total es 0, intentar auto-calcular basado en el pedido
        if ((invoice.Total == 0 || invoice.Subtotal == 0) && invoice.OrderId > 0)
        {
            var order = await _context.Orders.FindAsync(invoice.OrderId);
            if (order != null)
            {
                invoice.Total = order.Total;
                invoice.Subtotal = Math.Round(order.Total / 1.19m, 2);
                invoice.Impuestos = order.Total - invoice.Subtotal;
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

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();
        return invoice;
    }

    public async Task<Invoice?> UpdateAsync(Invoice invoice)
    {
        var existing = await _context.Invoices.FindAsync(invoice.Id);
        if (existing == null) return null;

        if (!string.IsNullOrWhiteSpace(invoice.Estado))
        {
            existing.Estado = invoice.Estado.Trim().ToLower();
        }

        if (!string.IsNullOrWhiteSpace(invoice.MetodoPago))
        {
            existing.MetodoPago = invoice.MetodoPago.Trim();
        }

        if (!string.IsNullOrWhiteSpace(invoice.NumeroFactura))
        {
            existing.NumeroFactura = invoice.NumeroFactura.Trim();
        }

        if (invoice.OrderId > 0)
        {
            existing.OrderId = invoice.OrderId;
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

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var existing = await _context.Invoices.FindAsync(id);
        if (existing == null) return false;

        _context.Invoices.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<Order>> GetOrdersAvailableForInvoicingAsync()
    {
        return await _context.Orders
            .Include(o => o.User)
            .OrderByDescending(o => o.FechaPedido)
            .ToListAsync();
    }
}
