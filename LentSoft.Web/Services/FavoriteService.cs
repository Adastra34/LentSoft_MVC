using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public class FavoriteService : IFavoriteService
{
    private readonly LentSoftDbContext _context;

    public FavoriteService(LentSoftDbContext context)
    {
        _context = context;
    }

    public async Task<List<Product>> GetFavoritesByUserIdAsync(int userId)
    {
        return await _context.Favorites
            .Where(f => f.UserId == userId && f.Product.Activo)
            .Include(f => f.Product)
            .Select(f => f.Product)
            .ToListAsync();
    }

    public async Task<HashSet<int>> GetUserFavoriteProductIdsAsync(int userId)
    {
        var productIds = await _context.Favorites
            .Where(f => f.UserId == userId)
            .Select(f => f.ProductId)
            .ToListAsync();

        return new HashSet<int>(productIds);
    }

    public async Task<bool> IsFavoriteAsync(int userId, int productId)
    {
        return await _context.Favorites
            .AnyAsync(f => f.UserId == userId && f.ProductId == productId);
    }

    public async Task<bool> ToggleFavoriteAsync(int userId, int productId)
    {
        var existing = await _context.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);

        if (existing != null)
        {
            _context.Favorites.Remove(existing);
            await _context.SaveChangesAsync();
            return false; // Removed from favorites
        }
        else
        {
            var favorite = new Favorite
            {
                UserId = userId,
                ProductId = productId,
                FechaAgregado = DateTime.UtcNow
            };
            _context.Favorites.Add(favorite);
            await _context.SaveChangesAsync();
            return true; // Added to favorites
        }
    }
}
