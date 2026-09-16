using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LentSoft.Web.Services;

namespace LentSoft.Web.Controllers.Api;

[ApiController]
[Route("api/cart")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class CartApiController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartApiController(ICartService cartService)
    {
        _cartService = cartService;
    }

    private int GetCurrentUserId()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idStr, out var id) ? id : 0;
    }

    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var cart = await _cartService.GetCartAsync(userId);
        if (cart == null)
        {
            return Ok(new
            {
                Id = 0,
                Total = 0m,
                ItemCount = 0,
                Items = Array.Empty<object>()
            });
        }

        var total = cart.CartItems.Sum(ci => ci.Subtotal);
        var items = cart.CartItems.Select(ci => new
        {
            ci.Id,
            ci.ProductId,
            ProductoNombre = ci.Product?.Nombre ?? "Producto",
            ProductoImagen = ci.Product?.ImagenUrl,
            ci.Cantidad,
            ci.PrecioUnitario,
            ci.Subtotal
        });

        return Ok(new
        {
            cart.Id,
            Total = total,
            ItemCount = cart.CartItems.Sum(ci => ci.Cantidad),
            Items = items
        });
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddCartItemApiRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        try
        {
            var item = await _cartService.AddToCartAsync(userId, request.ProductId, request.Cantidad <= 0 ? 1 : request.Cantidad);
            return Ok(new
            {
                success = true,
                message = "Producto añadido al carrito",
                item = new
                {
                    item.Id,
                    item.ProductId,
                    item.Cantidad,
                    item.PrecioUnitario,
                    item.Subtotal
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete("items/{productId:int}")]
    public async Task<IActionResult> RemoveItem(int productId)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var result = await _cartService.RemoveItemAsync(userId, productId);
        return Ok(new { success = result });
    }
}

public class AddCartItemApiRequest
{
    [Required]
    public int ProductId { get; set; }

    public int Cantidad { get; set; } = 1;
}
