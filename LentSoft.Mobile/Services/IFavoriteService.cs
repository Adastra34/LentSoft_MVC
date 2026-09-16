using LentSoft.Mobile.Models;

namespace LentSoft.Mobile.Services;

public interface IFavoriteService
{
    IReadOnlyList<ProductDto> Favorites { get; }
    event EventHandler? FavoritesChanged;

    bool IsFavorite(int productId);
    void ToggleFavorite(ProductDto product);
}
