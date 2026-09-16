using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Models.ViewModels;

public class ProductListViewModel
{
    public List<Product> Products { get; set; } = new();
    public List<Product> FeaturedProducts { get; set; } = new();

    // Filter values
    public string? Categoria { get; set; }
    public string? Marca { get; set; }
    public string? RangoPrecio { get; set; }

    // Available filter options
    public List<string> Categorias { get; set; } = new();
    public List<string> Marcas { get; set; } = new();

    // Favorites for current user (empty if not authenticated)
    public HashSet<int> FavoriteProductIds { get; set; } = new();
}
