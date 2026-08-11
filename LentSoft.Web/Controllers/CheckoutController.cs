using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Models.ViewModels;
using LentSoft.Web.Services;

namespace LentSoft.Web.Controllers;

[Authorize]
public class CheckoutController : Controller
{
    private readonly ICartService _cartService;
    private readonly LentSoftDbContext _context;

    public CheckoutController(ICartService cartService, LentSoftDbContext context)
    {
        _cartService = cartService;
        _context = context;
    }

    private int GetCurrentUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdStr, out var userId) ? userId : 0;
    }

    /// <summary>
    /// GET /Checkout/Index
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        var cart = await _cartService.GetCartAsync(userId);
        if (cart == null || !cart.CartItems.Any())
        {
            TempData["ErrorMessage"] = "Tu carrito está vacío. Agrega productos antes de realizar el pago.";
            return RedirectToAction("Index", "Cart");
        }

        var viewModel = new CheckoutViewModel
        {
            Cart = cart
        };

        return View(viewModel);
    }

    /// <summary>
    /// POST /Checkout/ConfirmarPago
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarPago(CheckoutViewModel model)
    {
        var userId = GetCurrentUserId();
        var cart = await _cartService.GetCartAsync(userId);

        if (cart == null || !cart.CartItems.Any())
        {
            TempData["ErrorMessage"] = "Tu carrito está vacío.";
            return RedirectToAction("Index", "Cart");
        }

        if (!ModelState.IsValid)
        {
            model.Cart = cart;
            return View("Index", model);
        }

        try
        {
            // Create Order and OrderItems
            var order = new Order
            {
                UserId = userId,
                FechaPedido = DateTime.UtcNow,
                Estado = "pagado",
                DireccionEnvio = model.DireccionEnvio,
                Total = cart.CartItems.Sum(ci => ci.Subtotal),
                MetodoPagoSimulado = "Tarjeta terminada en " + model.NumeroTarjeta.Substring(Math.Max(0, model.NumeroTarjeta.Length - 4))
            };

            foreach (var item in cart.CartItems)
            {
                // Reduce stock from available warehouse
                var pStock = await _context.ProductStocks
                    .FirstOrDefaultAsync(ps => ps.ProductId == item.ProductId && ps.WarehouseId == 1)
                    ?? await _context.ProductStocks.FirstOrDefaultAsync(ps => ps.ProductId == item.ProductId);

                if (pStock != null)
                {
                    pStock.Cantidad = Math.Max(0, pStock.Cantidad - item.Cantidad);
                }

                order.OrderItems.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = item.PrecioUnitario
                });
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Clear the cart
            await _cartService.ClearCartAsync(userId);

            TempData["SuccessMessage"] = "¡Pago simulado procesado correctamente!";
            return RedirectToAction("Confirmacion", new { orderId = order.Id });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al procesar el pago: {ex.Message}";
            model.Cart = cart;
            return View("Index", model);
        }
    }

    /// <summary>
    /// GET /Checkout/Confirmacion/{orderId}
    /// </summary>
    public async Task<IActionResult> Confirmacion(int orderId)
    {
        var userId = GetCurrentUserId();
        var order = await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

        if (order == null)
        {
            return NotFound();
        }

        return View(order);
    }
}
