using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public interface ISalesOrderService
{
    Task<SalesOrder> CreateAsync(SalesOrder model, string responsable);
    Task<SalesOrder?> EditAsync(SalesOrder model, string responsable);
    Task<bool> DeleteAsync(int id, string responsable);
    Task<SalesOrder?> GetByIdAsync(int id);
    Task<(List<SalesOrder> Items, int TotalCount)> GetAllAsync(string? searchTerm, int page, int pageSize);
}
