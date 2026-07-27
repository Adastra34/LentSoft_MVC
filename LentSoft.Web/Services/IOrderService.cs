using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public interface IOrderService
{
    Task<List<Order>> GetAllAsync();
    Task<List<Order>> GetByUserIdAsync(int userId);
    Task<Order?> GetByIdAsync(int id);
    Task<Order> CreateAsync(Order order);
    Task<Order?> UpdateStatusAsync(int id, string estado);
    Task<bool> DeleteAsync(int id);
}
