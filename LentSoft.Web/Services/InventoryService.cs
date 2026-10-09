using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public class InventoryService : IInventoryService
{
    private readonly LentSoftDbContext _context;
    private readonly InventorySettings _settings;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public InventoryService(
        LentSoftDbContext context,
        IOptions<InventorySettings> options,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _settings = options.Value;
        _httpContextAccessor = httpContextAccessor;
    }

    public int MaxStock => _settings.MaxStock;
    public int LowStockThreshold => _settings.LowStockThreshold;

    public async Task<Product> AumentarStockAsync(int productId, int cantidad, string motivo, string? responsable = null)
    {
        if (cantidad <= 0)
        {
            throw new ArgumentException("La cantidad debe ser mayor a 0.", nameof(cantidad));
        }

        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            throw new KeyNotFoundException($"El producto seleccionado con ID {productId} no existe.");
        }

        if (product.Stock + cantidad > MaxStock)
        {
            throw new InvalidOperationException($"El stock no puede superar las {MaxStock} unidades. Stock actual de {product.Nombre}: {product.Stock}, intentó agregar: {cantidad}.");
        }

        product.Stock += cantidad;
        if (product.Stock > 0 && !product.Activo)
        {
            product.Activo = true;
        }

        var movement = new InventoryMovement
        {
            ProductId = product.Id,
            NombreProducto = product.Nombre,
            Tipo = motivo,
            Cantidad = cantidad,
            Fecha = DateTime.UtcNow,
            Responsable = ResolveResponsable(responsable)
        };
        _context.InventoryMovements.Add(movement);

        try
        {
            await _context.SaveChangesAsync();
            return product;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException("El producto fue modificado por otro usuario, vuelve a intentarlo.", ex);
        }
    }

    public async Task<Product> DisminuirStockAsync(int productId, int cantidad, string motivo, string? responsable = null)
    {
        if (cantidad <= 0)
        {
            throw new ArgumentException("La cantidad debe ser mayor a 0.", nameof(cantidad));
        }

        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            throw new KeyNotFoundException($"El producto seleccionado con ID {productId} no existe.");
        }

        if (product.Stock < cantidad)
        {
            throw new InvalidOperationException($"Stock insuficiente. Stock actual de {product.Nombre}: {product.Stock}, intentó retirar: {cantidad}.");
        }

        product.Stock -= cantidad;
        if (product.Stock > MaxStock)
        {
            product.Stock = MaxStock;
        }

        if (product.Stock == 0)
        {
            product.Activo = false;
        }

        var movement = new InventoryMovement
        {
            ProductId = product.Id,
            NombreProducto = product.Nombre,
            Tipo = motivo,
            Cantidad = cantidad,
            Fecha = DateTime.UtcNow,
            Responsable = ResolveResponsable(responsable)
        };
        _context.InventoryMovements.Add(movement);

        try
        {
            await _context.SaveChangesAsync();
            return product;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException("El producto fue modificado por otro usuario, vuelve a intentarlo.", ex);
        }
    }

    public async Task<SupplierOrder> ProcesarRecepcionPedidoProveedorAsync(int supplierOrderId, string? responsable = null)
    {
        var order = await _context.SupplierOrders
            .Include(s => s.Product)
            .FirstOrDefaultAsync(s => s.Id == supplierOrderId);

        if (order == null)
        {
            throw new KeyNotFoundException($"El pedido a proveedor con ID {supplierOrderId} no existe.");
        }

        if (string.Equals(order.Estado, InventoryConstants.SupplierOrderStates.Recibido, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("El pedido a proveedor ya fue marcado como recibido previamente.");
        }

        var product = order.Product ?? await _context.Products.FindAsync(order.ProductId);
        if (product == null)
        {
            throw new KeyNotFoundException($"El producto asociado al pedido no existe.");
        }

        if (product.Stock + order.Cantidad > MaxStock)
        {
            throw new InvalidOperationException($"El stock no puede superar las {MaxStock} unidades. Stock actual de {product.Nombre}: {product.Stock}, intentó agregar: {order.Cantidad}.");
        }

        product.Stock += order.Cantidad;
        if (product.Stock > 0 && !product.Activo)
        {
            product.Activo = true;
        }

        order.Estado = InventoryConstants.SupplierOrderStates.Recibido;

        var movement = new InventoryMovement
        {
            ProductId = product.Id,
            NombreProducto = product.Nombre,
            Tipo = InventoryConstants.MovementTypes.Entrada,
            Cantidad = order.Cantidad,
            Fecha = DateTime.UtcNow,
            Responsable = ResolveResponsable(responsable)
        };
        _context.InventoryMovements.Add(movement);

        try
        {
            await _context.SaveChangesAsync();
            return order;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException("El producto fue modificado por otro usuario, vuelve a intentarlo.", ex);
        }
    }

    public async Task RegistrarMovimientoAsync(int productId, string tipo, int cantidad, string? responsable = null)
    {
        var product = await _context.Products.FindAsync(productId);
        var nombreProducto = product?.Nombre ?? $"Producto #{productId}";

        var movement = new InventoryMovement
        {
            ProductId = productId,
            NombreProducto = nombreProducto,
            Tipo = tipo,
            Cantidad = cantidad > 0 ? cantidad : 1,
            Fecha = DateTime.UtcNow,
            Responsable = ResolveResponsable(responsable)
        };
        _context.InventoryMovements.Add(movement);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException("El producto fue modificado por otro usuario, vuelve a intentarlo.", ex);
        }
    }

    private string ResolveResponsable(string? overrideName)
    {
        if (!string.IsNullOrWhiteSpace(overrideName))
        {
            return overrideName.Trim();
        }

        var user = _httpContextAccessor.HttpContext?.User;
        var name = user?.Identity?.Name 
                   ?? user?.FindFirst(ClaimTypes.Name)?.Value 
                   ?? user?.FindFirst(ClaimTypes.Email)?.Value;

        return string.IsNullOrWhiteSpace(name) ? "Administrador" : name;
    }
}
