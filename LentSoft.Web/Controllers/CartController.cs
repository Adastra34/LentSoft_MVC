using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LentSoft.Web.Services;

namespace LentSoft.Web.Controllers;

[Authorize]
public class CartController : Controller
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    private int GetCurrentUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdStr, out var userId) ? userId : 0;
    }

    /// <summary>
    /// GET /Cart/Index
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        var cart = await _cartService.GetCartAsync(userId);
        return View(cart);
    }

    /// <summary>
    /// POST /Cart/AddToCart
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AddToCart(int productId, int cantidad = 1)
    {
        var userId = GetCurrentUserId();
        try
        {
            await _cartService.AddToCartAsync(userId, productId, cantidad);
            return Json(new { success = true, message = "Producto añadido al carrito" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message, error = ex.Message });
        }
    }

    /// <summary>
    /// POST /Cart/UpdateQuantity
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> UpdateQuantity(int productId, int cantidad)
    {
        try
        {
            var userId = GetCurrentUserId();
            var item = await _cartService.UpdateQuantityAsync(userId, productId, cantidad);
            if (item == null)
            {
                var cart = await _cartService.GetCartAsync(userId);
                var newTotal = cart?.CartItems.Sum(ci => ci.Subtotal) ?? 0;
                return Json(new { success = true, deleted = true, totalGeneral = newTotal.ToString("C") });
            }
            else
            {
                var cart = await _cartService.GetCartAsync(userId);
                var newTotal = cart?.CartItems.Sum(ci => ci.Subtotal) ?? 0;
                return Json(new { 
                    success = true, 
                    deleted = false, 
                    cantidad = item.Cantidad, 
                    subtotal = item.Subtotal.ToString("C"), 
                    totalGeneral = newTotal.ToString("C") 
                });
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error al actualizar la cantidad: {ex.Message}" });
        }
    }

    /// <summary>
    /// POST /Cart/RemoveItem
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> RemoveItem(int productId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _cartService.RemoveItemAsync(userId, productId);
            var cart = await _cartService.GetCartAsync(userId);
            var newTotal = cart?.CartItems.Sum(ci => ci.Subtotal) ?? 0;
            return Json(new { success = result, totalGeneral = newTotal.ToString("C"), count = cart?.CartItems.Count ?? 0 });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error al eliminar el producto: {ex.Message}" });
        }
    }
}
