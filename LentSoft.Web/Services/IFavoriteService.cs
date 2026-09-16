using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public interface IFavoriteService
{
    Task<List<Product>> GetFavoritesByUserIdAsync(int userId);
    Task<HashSet<int>> GetUserFavoriteProductIdsAsync(int userId);
    Task<bool> IsFavoriteAsync(int userId, int productId);
    Task<bool> ToggleFavoriteAsync(int userId, int productId);
}
