using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

/// <summary>
/// Product service — migrated from Controllers/ProductController.js
/// </summary>
public class ProductService : IProductService
{
    private readonly LentSoftDbContext _context;

    public ProductService(LentSoftDbContext context)
    {
        _context = context;
    }

    public async Task<List<Product>> GetAllAsync()
    {
        return await _context.Products
            .OrderBy(p => p.Nombre)
            .ToListAsync();
    }

    public async Task<List<Product>> GetActiveAsync()
    {
        return await _context.Products
            .Where(p => p.Activo)
            .OrderBy(p => p.Nombre)
            .ToListAsync();
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _context.Products.FindAsync(id);
    }

    /// <summary>
    /// Filter products by category, brand, and price range.
    /// Migrated from the filter dropdowns in tienda.html
    /// </summary>
    public async Task<List<Product>> FilterAsync(string? categoria, string? marca, string? rangoPrecio)
    {
        var query = _context.Products.Where(p => p.Activo).AsQueryable();

        if (!string.IsNullOrEmpty(categoria))
        {
            query = query.Where(p => p.Categoria == categoria);
        }

        if (!string.IsNullOrEmpty(marca))
        {
            query = query.Where(p => p.Marca == marca);
        }

        if (!string.IsNullOrEmpty(rangoPrecio))
        {
            query = rangoPrecio switch
            {
                "menos-1000" => query.Where(p => p.Precio < 1000),
                "1000-2000" => query.Where(p => p.Precio >= 1000 && p.Precio <= 2000),
                "mas-2000" => query.Where(p => p.Precio > 2000),
                _ => query
            };
        }

        return await query.OrderBy(p => p.Nombre).ToListAsync();
    }

    public async Task<List<string>> GetCategoriasAsync()
    {
        return await _context.Products
            .Where(p => p.Activo)
            .Select(p => p.Categoria)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }

    public async Task<List<string>> GetMarcasAsync()
    {
        return await _context.Products
            .Where(p => p.Activo && p.Marca != null)
            .Select(p => p.Marca!)
            .Distinct()
            .OrderBy(m => m)
            .ToListAsync();
    }

    public async Task<Product> CreateAsync(Product product)
    {
        product.FechaCreacion = DateTime.UtcNow;
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }

    public async Task<Product?> UpdateAsync(int id, Product updated)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return null;

        product.Nombre = updated.Nombre;
        product.Descripcion = updated.Descripcion;
        product.Precio = updated.Precio;
        product.PrecioDescuento = updated.PrecioDescuento;
        product.Categoria = updated.Categoria;
        product.Marca = updated.Marca;
        product.Stock = updated.Stock;
        product.ImagenUrl = updated.ImagenUrl;
        product.Activo = updated.Activo;

        await _context.SaveChangesAsync();
        return product;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return false;

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<Product>> GetBestSellersAsync(int count = 3)
    {
        // Return the first N active products ordered by stock (most popular proxy)
        return await _context.Products
            .Where(p => p.Activo)
            .OrderByDescending(p => p.Stock)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<Product>> GetFeaturedAsync()
    {
        return await _context.Products
            .Where(p => p.Activo && p.EsDestacado)
            .OrderByDescending(p => p.Rating)
            .ToListAsync();
    }

    public async Task<List<Product>> GetGafasAsync()
    {
        return await _context.Products
            .Where(p => p.Activo && (p.Categoria == "lentes-sol" || p.Categoria == "monturas" || p.Categoria == "lentes-graduados"))
            .OrderBy(p => p.Nombre)
            .ToListAsync();
    }
}
