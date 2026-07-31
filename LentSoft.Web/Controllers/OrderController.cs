using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LentSoft.Web.Services;

namespace LentSoft.Web.Controllers;

[Authorize]
public class OrderController : Controller
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// User's orders — migrated from dashboard-usuario.html "Mis Pedidos" section
    /// </summary>
    public async Task<IActionResult> MisPedidos()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var orders = await _orderService.GetByUserIdAsync(userId);
        return View(orders);
    }

    /// <summary>
    /// Order details
    /// </summary>
    public async Task<IActionResult> Details(int id)
    {
        var order = await _orderService.GetByIdAsync(id);
        if (order == null) return NotFound();

        // Ensure user can only see their own orders (unless admin)
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (order.UserId != userId && !User.IsInRole("admin"))
            return Forbid();

        return View(order);
    }

    /// <summary>
    /// Admin: Update order status — migrated from OrderController.js update()
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string estado)
    {
        await _orderService.UpdateStatusAsync(id, estado);
        TempData["SuccessMessage"] = "Estado del pedido actualizado.";
        return RedirectToAction("Admin", "Dashboard");
    }
}
