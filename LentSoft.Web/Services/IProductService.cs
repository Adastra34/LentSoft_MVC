using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public interface IProductService
{
    Task<List<Product>> GetAllAsync(bool includeInactive = false);
    Task<List<Product>> GetActiveAsync();
    Task<Product?> GetByIdAsync(int id);
    Task<List<Product>> FilterAsync(string? categoria, string? marca, string? rangoPrecio);
    Task<List<string>> GetCategoriasAsync();
    Task<List<string>> GetMarcasAsync();
    Task<Product> CreateAsync(Product product);
    Task<Product?> UpdateAsync(int id, Product product);
    Task<bool> DeleteAsync(int id);
    Task<bool> ReactivateAsync(int id);
    Task<List<Product>> GetBestSellersAsync(int count = 3);
    Task<List<Product>> GetFeaturedAsync();
    Task<List<Product>> GetGafasAsync();
    Task<List<Product>> GetProductosBajoStockAsync();
    Task<List<InventoryMovement>> GetKardexPorProductoAsync(int productId);
    Task<List<Product>> GetProductosSinMovimientoAsync();
}
