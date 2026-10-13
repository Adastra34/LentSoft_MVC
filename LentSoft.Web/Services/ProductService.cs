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
    private readonly InventorySettings _inventorySettings;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public ProductService(
        LentSoftDbContext context,
        Microsoft.Extensions.Options.IOptions<InventorySettings>? options = null,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _context = context;
        _inventorySettings = options?.Value ?? new InventorySettings();
        _httpContextAccessor = httpContextAccessor;
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
        if (product.Stock > _inventorySettings.MaxStock)
        {
            throw new InvalidOperationException($"El stock no puede superar las {_inventorySettings.MaxStock} unidades.");
        }

        if (product.Stock == 0)
        {
            product.Activo = false;
        }

        if (product.StockMinimo <= 0)
        {
            product.StockMinimo = _inventorySettings.LowStockThreshold > 0 ? _inventorySettings.LowStockThreshold : 10;
        }

        if (string.IsNullOrWhiteSpace(product.Sku))
        {
            product.Sku = await GenerateNextSkuAsync();
        }
        else
        {
            product.Sku = product.Sku.Trim();
            var skuExists = await _context.Products.AnyAsync(p => p.Sku == product.Sku);
            if (skuExists)
            {
                throw new InvalidOperationException($"El SKU '{product.Sku}' ya está registrado en otro producto.");
            }
        }

        product.FechaCreacion = DateTime.UtcNow;
        _context.Products.Add(product);

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            await _context.SaveChangesAsync();

            var responsable = ResolveCurrentUser();
            // Registro automático en Historial de Movimientos de Inventario
            var movement = new InventoryMovement
            {
                ProductId = product.Id,
                NombreProducto = product.Nombre,
                Tipo = InventoryConstants.MovementTypes.Alta,
                Cantidad = product.Stock > 0 ? product.Stock : 1,
                Fecha = DateTime.UtcNow,
                Responsable = responsable,
                Motivo = InventoryConstants.MovementMotives.Compra,
                StockResultante = product.Stock,
                Nota = "Alta de nuevo producto en el catálogo",
                Referencia = product.Sku
            };
            _context.InventoryMovements.Add(movement);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
            return product;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync();
            throw new InvalidOperationException("El producto fue modificado por otro usuario, vuelve a intentarlo.", ex);
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync();
            throw new InvalidOperationException("No se pudo crear el producto debido a una restricción de datos.", ex);
        }
    }

    public async Task<Product?> UpdateAsync(int id, Product updated)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return null;

        if (updated.Stock > _inventorySettings.MaxStock)
        {
            throw new InvalidOperationException($"El stock no puede superar las {_inventorySettings.MaxStock} unidades.");
        }

        if (!string.IsNullOrWhiteSpace(updated.Sku))
        {
            updated.Sku = updated.Sku.Trim();
            var skuExists = await _context.Products.AnyAsync(p => p.Id != id && p.Sku == updated.Sku);
            if (skuExists)
            {
                throw new InvalidOperationException($"El SKU '{updated.Sku}' ya está registrado en otro producto.");
            }
            product.Sku = updated.Sku;
        }
        else if (string.IsNullOrWhiteSpace(product.Sku))
        {
            product.Sku = await GenerateNextSkuAsync();
        }

        int stockDelta = updated.Stock - product.Stock;

        product.Nombre = updated.Nombre;
        product.Descripcion = updated.Descripcion;
        product.Precio = updated.Precio;
        product.PrecioDescuento = updated.PrecioDescuento;
        product.Categoria = updated.Categoria;
        product.Marca = updated.Marca;
        product.CostoCompra = updated.CostoCompra;
        product.StockMinimo = updated.StockMinimo > 0 ? updated.StockMinimo : 10;
        product.Ubicacion = updated.Ubicacion;
        product.Stock = updated.Stock;
        product.ImagenUrl = updated.ImagenUrl;
        product.Activo = updated.Stock == 0 ? false : updated.Activo;
        product.EscalaOverlay = updated.EscalaOverlay;
        product.OffsetXOverlay = updated.OffsetXOverlay;
        product.OffsetYOverlay = updated.OffsetYOverlay;
        if (!string.IsNullOrEmpty(updated.SupplierId)) product.SupplierId = updated.SupplierId;

        if (updated.RowVersion != null && updated.RowVersion.Length > 0)
        {
            _context.Entry(product).Property(p => p.RowVersion).OriginalValue = updated.RowVersion;
        }

        try
        {
            var responsable = ResolveCurrentUser();
            var movementTipo = stockDelta > 0 
                ? InventoryConstants.MovementTypes.Entrada 
                : (stockDelta < 0 ? InventoryConstants.MovementTypes.Salida : InventoryConstants.MovementTypes.Edicion);
            var movementCantidad = Math.Abs(stockDelta) > 0 ? Math.Abs(stockDelta) : (product.Stock > 0 ? product.Stock : 1);
            var movement = new InventoryMovement
            {
                ProductId = product.Id,
                NombreProducto = product.Nombre,
                Tipo = movementTipo,
                Cantidad = movementCantidad,
                Fecha = DateTime.UtcNow,
                Responsable = responsable,
                Motivo = stockDelta != 0 ? InventoryConstants.MovementMotives.AjustePorConteo : InventoryConstants.MovementMotives.Otro,
                StockResultante = product.Stock,
                Nota = stockDelta != 0 ? $"Ajuste en edición: delta {stockDelta:+0;-0;0}" : "Edición de datos del producto",
                Referencia = product.Sku
            };
            _context.InventoryMovements.Add(movement);

            await _context.SaveChangesAsync();
            return product;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException("El producto fue modificado por otro usuario, vuelve a intentarlo.", ex);
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

        var responsable = ResolveCurrentUser();
        var movement = new InventoryMovement
        {
            ProductId = product.Id,
            NombreProducto = product.Nombre,
            Tipo = InventoryConstants.MovementTypes.Baja,
            Cantidad = product.Stock > 0 ? product.Stock : 1,
            Fecha = DateTime.UtcNow,
            Responsable = responsable,
            Motivo = InventoryConstants.MovementMotives.Dano,
            StockResultante = product.Stock,
            Nota = "Inactivación/Baja de producto",
            Referencia = product.Sku
        };
        _context.InventoryMovements.Add(movement);

        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException("El producto fue modificado por otro usuario, vuelve a intentarlo.", ex);
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

        var responsable = ResolveCurrentUser();
        var movement = new InventoryMovement
        {
            ProductId = product.Id,
            NombreProducto = product.Nombre,
            Tipo = InventoryConstants.MovementTypes.Reactivacion,
            Cantidad = product.Stock > 0 ? product.Stock : 1,
            Fecha = DateTime.UtcNow,
            Responsable = responsable,
            Motivo = InventoryConstants.MovementMotives.Otro,
            StockResultante = product.Stock,
            Nota = "Reactivación de producto",
            Referencia = product.Sku
        };
        _context.InventoryMovements.Add(movement);

        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException("El producto fue modificado por otro usuario, vuelve a intentarlo.", ex);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("No se pudo reactivar el producto debido a una restricción de base de datos.", ex);
        }
    }

    private string ResolveCurrentUser()
    {
        var user = _httpContextAccessor?.HttpContext?.User;
        var name = user?.Identity?.Name 
                   ?? user?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value 
                   ?? user?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

        return string.IsNullOrWhiteSpace(name) ? "Administrador" : name;
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

    /// <summary>
    /// Genera el siguiente SKU consecutivo desde 1010 con incrementos de 1010 (1010, 2020, 3030, ...)
    /// </summary>
    public async Task<string> GenerateNextSkuAsync()
    {
        var existingSkus = await _context.Products
            .Where(p => p.Sku != null)
            .Select(p => p.Sku!)
            .ToListAsync();

        long maxNumericSku = 0;
        foreach (var s in existingSkus)
        {
            if (long.TryParse(s, out var num) && num >= 1010 && num % 1010 == 0)
            {
                if (num > maxNumericSku) maxNumericSku = num;
            }
        }

        long nextSku = maxNumericSku > 0 ? (maxNumericSku + 1010) : 1010;
        while (existingSkus.Contains(nextSku.ToString()))
        {
            nextSku += 1010;
        }

        return nextSku.ToString();
    }

    /// <summary>
    /// Asigna SKUs consecutivos (1010, 2020, 3030...) a todos los productos que no tengan SKU asignado
    /// </summary>
    public async Task AsignarSkusConsecutivosFaltantesAsync()
    {
        var prodsWithoutSku = await _context.Products
            .Where(p => string.IsNullOrWhiteSpace(p.Sku))
            .OrderBy(p => p.Id)
            .ToListAsync();

        if (!prodsWithoutSku.Any()) return;

        var existingSkus = await _context.Products
            .Where(p => p.Sku != null)
            .Select(p => p.Sku!)
            .ToListAsync();

        var existingSet = new HashSet<string>(existingSkus);
        long current = 1010;

        foreach (var prod in prodsWithoutSku)
        {
            while (existingSet.Contains(current.ToString()))
            {
                current += 1010;
            }
            prod.Sku = current.ToString();
            existingSet.Add(prod.Sku);
            current += 1010;
        }

        await _context.SaveChangesAsync();
    }
}
