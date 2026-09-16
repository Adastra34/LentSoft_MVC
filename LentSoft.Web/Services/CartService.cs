using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public class CartService : ICartService
{
    private readonly LentSoftDbContext _context;

    public CartService(LentSoftDbContext context)
    {
        _context = context;
    }

    private async Task<Cart> GetOrCreateCartAsync(int userId)
    {
        var cart = await _context.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            cart = new Cart
            {
                UserId = userId,
                FechaCreacion = DateTime.UtcNow
            };
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();
        }

        return cart;
    }

    public async Task<CartItem> AddToCartAsync(int userId, int productId, int cantidad)
    {
        var cart = await GetOrCreateCartAsync(userId);
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            throw new ArgumentException("Producto no encontrado");
        }

        var price = product.PrecioDescuento.HasValue && product.PrecioDescuento < product.Precio
            ? product.PrecioDescuento.Value
            : product.Precio;

        var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
        if (cartItem != null)
        {
            cartItem.Cantidad += cantidad;
            cartItem.PrecioUnitario = price; // Update to latest price
        }
        else
        {
            cartItem = new CartItem
            {
                CartId = cart.Id,
                ProductId = productId,
                Cantidad = cantidad,
                PrecioUnitario = price
            };
            _context.CartItems.Add(cartItem);
        }

        await _context.SaveChangesAsync();
        return cartItem;
    }

    public async Task<Cart?> GetCartAsync(int userId)
    {
        return await _context.Carts
            .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public async Task<CartItem?> UpdateQuantityAsync(int userId, int productId, int cantidad)
    {
        var cart = await _context.Carts
            .FirstOrDefaultAsync(c => c.UserId == userId);
        if (cart == null) return null;

        var cartItem = await _context.CartItems
            .FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.ProductId == productId);

        if (cartItem == null) return null;

        if (cantidad <= 0)
        {
            _context.CartItems.Remove(cartItem);
            cartItem = null;
        }
        else
        {
            cartItem.Cantidad = cantidad;
        }

        await _context.SaveChangesAsync();
        return cartItem;
    }

    public async Task<bool> RemoveItemAsync(int userId, int productId)
    {
        var cart = await _context.Carts
            .FirstOrDefaultAsync(c => c.UserId == userId);
        if (cart == null) return false;

        var cartItem = await _context.CartItems
            .FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.ProductId == productId);

        if (cartItem == null) return false;

        _context.CartItems.Remove(cartItem);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ClearCartAsync(int userId)
    {
        var cart = await _context.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.UserId == userId);
        if (cart == null) return false;

        _context.CartItems.RemoveRange(cart.CartItems);
        await _context.SaveChangesAsync();
        return true;
    }
}
