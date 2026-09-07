using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LentSoft.Web.Services;

namespace LentSoft.Web.Controllers.Api;

[ApiController]
[Route("api/orders")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class OrdersApiController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersApiController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    private int GetCurrentUserId()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idStr, out var id) ? id : 0;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var orders = await _orderService.GetByUserIdAsync(userId);
        var result = orders.Select(o => new
        {
            o.Id,
            o.Total,
            Estado = char.ToUpper(o.Estado[0]) + o.Estado.Substring(1).ToLower(),
            EstadoRaw = o.Estado.ToLower(),
            o.FechaPedido,
            o.FechaEntrega,
            o.DireccionEnvio,
            o.MetodoPagoSimulado,
            Items = o.OrderItems.Select(oi => new
            {
                oi.Id,
                oi.ProductId,
                ProductoNombre = oi.Product?.Nombre ?? "Producto",
                ProductoImagen = oi.Product?.ImagenUrl,
                oi.Cantidad,
                oi.PrecioUnitario,
                oi.Subtotal
            })
        });

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOrderById(int id)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var order = await _orderService.GetByIdAsync(id);
        if (order == null || (order.UserId != userId && !User.IsInRole("admin")))
            return NotFound(new { message = "Pedido no encontrado." });

        return Ok(new
        {
            order.Id,
            order.Total,
            Estado = char.ToUpper(order.Estado[0]) + order.Estado.Substring(1).ToLower(),
            EstadoRaw = order.Estado.ToLower(),
            order.FechaPedido,
            order.FechaEntrega,
            order.DireccionEnvio,
            order.MetodoPagoSimulado,
            Items = order.OrderItems.Select(oi => new
            {
                oi.Id,
                oi.ProductId,
                ProductoNombre = oi.Product?.Nombre ?? "Producto",
                ProductoImagen = oi.Product?.ImagenUrl,
                oi.Cantidad,
                oi.PrecioUnitario,
                oi.Subtotal
            })
        });
    }
}
