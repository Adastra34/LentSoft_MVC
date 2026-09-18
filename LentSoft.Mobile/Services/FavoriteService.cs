using LentSoft.Mobile.Models;

namespace LentSoft.Mobile.Services;

public class FavoriteService : IFavoriteService
{
    private readonly List<ProductDto> _favorites = new();

    public IReadOnlyList<ProductDto> Favorites => _favorites.AsReadOnly();

    public event EventHandler? FavoritesChanged;

    public bool IsFavorite(int productId)
    {
        return _favorites.Any(p => p.Id == productId);
    }

    public void ToggleFavorite(ProductDto product)
    {
        if (product == null) return;

        var existing = _favorites.FirstOrDefault(p => p.Id == product.Id);
        if (existing != null)
        {
            _favorites.Remove(existing);
        }
        else
        {
            _favorites.Add(product);
        }

        FavoritesChanged?.Invoke(this, EventArgs.Empty);
    }
}
