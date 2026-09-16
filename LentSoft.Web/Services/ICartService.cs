using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public interface ICartService
{
    Task<CartItem> AddToCartAsync(int userId, int productId, int cantidad);
    Task<Cart?> GetCartAsync(int userId);
    Task<CartItem?> UpdateQuantityAsync(int userId, int productId, int cantidad);
    Task<bool> RemoveItemAsync(int userId, int productId);
    Task<bool> ClearCartAsync(int userId);
}
