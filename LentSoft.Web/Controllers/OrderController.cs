using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Services;

namespace LentSoft.Web.Controllers;

[Authorize(Roles = "admin,ventas")]
public class OrderController : Controller
{
    private readonly IOrderService _orderService;
    private readonly LentSoftDbContext _context;

    public OrderController(IOrderService orderService, LentSoftDbContext context)
    {
        _orderService = orderService;
        _context = context;
    }

    private IActionResult RedirectToReturnDashboard()
    {
        var referer = Request.Headers["Referer"].ToString();
        if (!string.IsNullOrEmpty(referer) && referer.Contains("/Ventas", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction("Index", "Ventas", new { section = "ventas" });
        }

        if (User.IsInRole("ventas") && !User.IsInRole("admin"))
        {
            return RedirectToAction("Index", "Ventas", new { section = "ventas" });
        }

        return RedirectToAction("Admin", "Dashboard", new { section = "ventas" });
    }

    /// <summary>
    /// User's orders
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> MisPedidos()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var orders = await _orderService.GetByUserIdAsync(userId);
        return View(orders);
    }

    /// <summary>
    /// Order details JSON API
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetJson(int id)
    {
        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        return Json(new
        {
            order.Id,
            NumeroVenta = $"VEN-{order.Id:D3}",
            ClienteNombre = order.User != null ? order.User.NombreCompleto : "Cliente Genérico",
            ClienteDocumento = order.User?.NumeroDocumento ?? "N/A",
            ClienteTelefono = order.User?.Telefono ?? "N/A",
            order.Total,
            order.Estado,
            Fecha = order.FechaPedido.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
            MetodoPagoYObs = order.DireccionEnvio,
            Items = order.OrderItems.Select(oi => new
            {
                oi.Id,
                oi.ProductId,
                ProductoNombre = oi.Product != null ? oi.Product.Nombre : "Producto",
                oi.Cantidad,
                oi.PrecioUnitario,
                Subtotal = oi.Cantidad * oi.PrecioUnitario
            })
        });
    }

    /// <summary>
    /// Crear nueva venta (Order + OrderItems + User check)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSale(
        string NombreCliente,
        string DocumentoCliente,
        string? TelefonoCliente,
        DateTime? FechaVenta,
        string MetodoPago,
        string Estado,
        string? Observaciones,
        decimal Total,
        List<int> ProductIds,
        List<int> Cantidades,
        List<decimal> PreciosUnitarios)
    {
        if (string.IsNullOrWhiteSpace(NombreCliente))
        {
            TempData["ErrorMessage"] = "El nombre del cliente es obligatorio.";
            return RedirectToReturnDashboard();
        }

        try
        {
            // 1. Buscar o crear cliente
            var docClean = string.IsNullOrWhiteSpace(DocumentoCliente) ? "" : DocumentoCliente.Trim();
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                (!string.IsNullOrEmpty(docClean) && u.NumeroDocumento == docClean) ||
                (u.Nombre.ToLower() == NombreCliente.Trim().ToLower()));

            if (user == null)
            {
                var parts = NombreCliente.Trim().Split(' ', 2);
                user = new User
                {
                    Nombre = parts[0],
                    Apellido = parts.Length > 1 ? parts[1] : "Cliente",
                    TipoDocumento = "CC",
                    NumeroDocumento = string.IsNullOrEmpty(docClean) ? $"NIT-{DateTime.UtcNow.Ticks % 1000000}" : docClean,
                    Email = $"cliente_{DateTime.UtcNow.Ticks}@lentsoft.com",
                    PasswordHash = "DEFAULT_HASH",
                    Telefono = TelefonoCliente,
                    Role = "usuario"
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            // 2. Crear Order
            var order = new Order
            {
                UserId = user.Id,
                Total = Total > 0 ? Total : 0,
                Estado = string.IsNullOrWhiteSpace(Estado) ? "pendiente" : Estado.ToLower(),
                DireccionEnvio = $"Pago: {MetodoPago ?? "Efectivo"} | Obs: {Observaciones ?? ""}",
                FechaPedido = FechaVenta.HasValue && FechaVenta.Value != default ? FechaVenta.Value.ToUniversalTime() : DateTime.UtcNow
            };

            // 3. Crear OrderItems
            if (ProductIds != null && ProductIds.Count > 0)
            {
                decimal totalCalculado = 0;
                for (int i = 0; i < ProductIds.Count; i++)
                {
                    if (i < Cantidades.Count && i < PreciosUnitarios.Count && ProductIds[i] > 0 && Cantidades[i] > 0)
                    {
                        var sub = Cantidades[i] * PreciosUnitarios[i];
                        totalCalculado += sub;
                        order.OrderItems.Add(new OrderItem
                        {
                            ProductId = ProductIds[i],
                            Cantidad = Cantidades[i],
                            PrecioUnitario = PreciosUnitarios[i]
                        });
                    }
                }
                if (order.Total == 0 && totalCalculado > 0)
                {
                    order.Total = totalCalculado;
                }
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Venta VEN-{order.Id:D3} registrada exitosamente.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al registrar la venta: {ex.Message}";
        }

        return RedirectToReturnDashboard();
    }

    /// <summary>
    /// Update order status / notes
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string estado, string? metodoPago, string? observaciones)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order != null)
        {
            order.Estado = estado.ToLower();
            if (!string.IsNullOrEmpty(metodoPago) || !string.IsNullOrEmpty(observaciones))
            {
                order.DireccionEnvio = $"Pago: {metodoPago ?? "Efectivo"} | Obs: {observaciones ?? ""}";
            }
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Venta VEN-{id:D3} actualizada exitosamente.";
        }
        else
        {
            TempData["ErrorMessage"] = "No se encontró la venta a actualizar.";
        }

        return RedirectToReturnDashboard();
    }

    /// <summary>
    /// Delete sale / order
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result = await _orderService.DeleteAsync(id);
            if (result)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Venta eliminada exitosamente." });
                }
                TempData["SuccessMessage"] = "Venta eliminada exitosamente.";
            }
            else
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "No se encontró la venta." });
                }
                TempData["ErrorMessage"] = "No se encontró la venta.";
            }
        }
        catch (Exception ex)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = ex.Message });
            }
            TempData["ErrorMessage"] = $"Error al eliminar la venta: {ex.Message}";
        }

        return RedirectToReturnDashboard();
    }
}
