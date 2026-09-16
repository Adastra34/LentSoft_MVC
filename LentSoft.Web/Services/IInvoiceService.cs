using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public interface IInvoiceService
{
    Task<(List<Invoice> Items, int TotalCount)> GetAllAsync(string? searchTerm, int page, int pageSize);
    Task<Invoice?> GetByIdAsync(int id);
    Task<Invoice> CreateAsync(Invoice invoice);
    Task<Invoice?> UpdateAsync(Invoice invoice);
    Task<bool> DeleteAsync(int id);
    Task<List<Order>> GetOrdersAvailableForInvoicingAsync();
}
