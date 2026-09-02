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

    public async Task<List<Product>> GetAllAsync(bool includeInactive = false)
    {
        var query = _context.Products.AsQueryable();
        if (!includeInactive)
        {
            query = query.Where(p => p.Activo);
        }

        return await query
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
                "menos-500000" => query.Where(p => p.Precio < 500000),
                "500000-1500000" => query.Where(p => p.Precio >= 500000 && p.Precio <= 1500000),
                "mas-1500000" => query.Where(p => p.Precio > 1500000),
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

        try
        {
            await _context.SaveChangesAsync();

            // Registro automático en Historial de Movimientos de Inventario
            var movement = new InventoryMovement
            {
                ProductId = product.Id,
                NombreProducto = product.Nombre,
                Tipo = "Alta",
                Cantidad = product.Stock > 0 ? product.Stock : 1,
                Fecha = DateTime.UtcNow,
                Responsable = "Sistema / Admin"
            };
            _context.InventoryMovements.Add(movement);
            await _context.SaveChangesAsync();

            return product;
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("No se pudo crear el producto debido a una restricción de datos.", ex);
        }
    }

    public async Task<Product?> UpdateAsync(int id, Product updated)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return null;

        int stockDelta = updated.Stock - product.Stock;

        product.Nombre = updated.Nombre;
        product.Descripcion = updated.Descripcion;
        product.Precio = updated.Precio;
        product.PrecioDescuento = updated.PrecioDescuento;
        product.Categoria = updated.Categoria;
        product.Marca = updated.Marca;
        product.Stock = updated.Stock;
        product.ImagenUrl = updated.ImagenUrl;
        product.Activo = updated.Activo;
        if (!string.IsNullOrEmpty(updated.SupplierId)) product.SupplierId = updated.SupplierId;

        try
        {
            var movementTipo = stockDelta > 0 ? "Entrada" : (stockDelta < 0 ? "Salida" : "Edición");
            var movementCantidad = Math.Abs(stockDelta) > 0 ? Math.Abs(stockDelta) : (product.Stock > 0 ? product.Stock : 1);
            var movement = new InventoryMovement
            {
                ProductId = product.Id,
                NombreProducto = product.Nombre,
                Tipo = movementTipo,
                Cantidad = movementCantidad,
                Fecha = DateTime.UtcNow,
                Responsable = "Sistema / Admin"
            };
            _context.InventoryMovements.Add(movement);

            await _context.SaveChangesAsync();
            return product;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException("El producto fue modificado por otro usuario.", ex);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("No se pudieron guardar los cambios del producto debido a una restricción de datos.", ex);
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return false;

        product.Activo = false;
        _context.Products.Update(product);

        var movement = new InventoryMovement
        {
            ProductId = product.Id,
            NombreProducto = product.Nombre,
            Tipo = "Baja",
            Cantidad = product.Stock > 0 ? product.Stock : 1,
            Fecha = DateTime.UtcNow,
            Responsable = "Sistema / Admin"
        };
        _context.InventoryMovements.Add(movement);

        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("No se puede eliminar este producto porque tiene pedidos o registros asociados.", ex);
        }
    }

    public async Task<bool> ReactivateAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return false;

        product.Activo = true;
        _context.Products.Update(product);

        var movement = new InventoryMovement
        {
            ProductId = product.Id,
            NombreProducto = product.Nombre,
            Tipo = "Reactivación",
            Cantidad = product.Stock > 0 ? product.Stock : 1,
            Fecha = DateTime.UtcNow,
            Responsable = "Sistema / Admin"
        };
        _context.InventoryMovements.Add(movement);

        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("No se pudo reactivar el producto debido a una restricción de base de datos.", ex);
        }
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
