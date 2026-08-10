using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Services;

namespace LentSoft.Web.Controllers;

[Authorize]
public class OrderController : Controller
{
    private readonly IOrderService _orderService;
    private readonly LentSoftDbContext _context;

    public OrderController(IOrderService orderService, LentSoftDbContext context)
    {
        _orderService = orderService;
        _context = context;
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
    /// Crear nueva venta desde Admin o Portal de Ventas
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "admin,ventas")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        int? UserId,
        string Nombre, 
        string Apellido, 
        string? NumeroDocumento,
        string Telefono, 
        string Direccion, 
        int DescuentoPercent, 
        string Estado, 
        string MetodoPagoSimulado, 
        string? ItemsJson)
    {
        if (string.IsNullOrWhiteSpace(Nombre) || string.IsNullOrWhiteSpace(Apellido))
        {
            TempData["ErrorMessage"] = "Debe ingresar el Nombre y Apellido del cliente.";
            return RedirectToVentas();
        }

        // 1. Buscar o Crear Usuario / Cliente
        User? existingUser = null;
        if (UserId.HasValue && UserId.Value > 0)
        {
            existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == UserId.Value);
        }

        if (existingUser == null && !string.IsNullOrWhiteSpace(NumeroDocumento))
        {
            existingUser = await _context.Users.FirstOrDefaultAsync(u => u.NumeroDocumento == NumeroDocumento.Trim());
        }

        if (existingUser == null && !string.IsNullOrWhiteSpace(Telefono))
        {
            existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Telefono == Telefono.Trim());
        }

        if (existingUser == null)
        {
            existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Nombre.ToLower() == Nombre.Trim().ToLower() && u.Apellido.ToLower() == Apellido.Trim().ToLower());
        }

        if (existingUser == null)
        {
            var randomSuffix = new Random().Next(1000, 9999);
            var cleanNombre = System.Text.RegularExpressions.Regex.Replace(Nombre.ToLower().Trim(), @"\s+", "");
            var docNum = !string.IsNullOrWhiteSpace(NumeroDocumento) ? NumeroDocumento.Trim() : $"CLI{DateTime.UtcNow.Ticks.ToString()[^8..]}";
            existingUser = new User
            {
                Nombre = Nombre.Trim(),
                Apellido = Apellido.Trim(),
                Email = $"{cleanNombre}{randomSuffix}@cliente.com",
                Telefono = string.IsNullOrWhiteSpace(Telefono) ? "3000000000" : Telefono.Trim(),
                Direccion = string.IsNullOrWhiteSpace(Direccion) ? null : Direccion.Trim(),
                TipoDocumento = "CC",
                NumeroDocumento = docNum,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Cliente123!"),
                Role = "usuario",
                FechaRegistro = DateTime.UtcNow
            };
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(Nombre)) existingUser.Nombre = Nombre.Trim();
            if (!string.IsNullOrWhiteSpace(Apellido)) existingUser.Apellido = Apellido.Trim();
            if (!string.IsNullOrWhiteSpace(Telefono)) existingUser.Telefono = Telefono.Trim();
            if (!string.IsNullOrWhiteSpace(Direccion)) existingUser.Direccion = Direccion.Trim();
            if (!string.IsNullOrWhiteSpace(NumeroDocumento)) existingUser.NumeroDocumento = NumeroDocumento.Trim();
            await _context.SaveChangesAsync();
        }

        // 2. Procesar Ítems y Calcular Subtotal
        decimal subtotalVenta = 0;
        var orderItemsList = new List<OrderItem>();

        if (!string.IsNullOrWhiteSpace(ItemsJson))
        {
            try
            {
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var items = System.Text.Json.JsonSerializer.Deserialize<List<OrderItemInput>>(ItemsJson, options);
                if (items != null && items.Any())
                {
                    foreach (var item in items)
                    {
                        var product = await _context.Products.FindAsync(item.ProductId);
                        if (product != null)
                        {
                            var qty = Math.Max(1, item.Cantidad);
                            var precioUnit = product.PrecioDescuento ?? product.Precio;
                            var itemSubtotal = precioUnit * qty;
                            subtotalVenta += itemSubtotal;

                            orderItemsList.Add(new OrderItem
                            {
                                ProductId = product.Id,
                                Cantidad = qty,
                                PrecioUnitario = precioUnit
                            });

                            if (product.Stock >= qty)
                            {
                                product.Stock -= qty;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignore parse errors, check list below
            }
        }

        if (!orderItemsList.Any())
        {
            TempData["ErrorMessage"] = "Debe agregar al menos un producto a la venta.";
            return RedirectToVentas();
        }

        // 3. Aplicar Descuento (0 - 30%)
        var descPercent = Math.Clamp(DescuentoPercent, 0, 30);
        decimal totalFinal = subtotalVenta * (1m - (descPercent / 100m));

        // 4. Crear Orden
        var order = new Order
        {
            UserId = existingUser.Id,
            Total = Math.Round(totalFinal, 2),
            Estado = string.IsNullOrWhiteSpace(Estado) ? "pendiente" : Estado,
            MetodoPagoSimulado = string.IsNullOrWhiteSpace(MetodoPagoSimulado) ? "Efectivo" : MetodoPagoSimulado,
            DireccionEnvio = Direccion,
            FechaPedido = DateTime.UtcNow,
            OrderItems = orderItemsList
        };

        await _orderService.CreateAsync(order);
        TempData["SuccessMessage"] = "Venta registrada exitosamente.";

        return RedirectToVentas();
    }

    private IActionResult RedirectToVentas()
    {
        var referer = Request.Headers["Referer"].ToString();
        if (User.IsInRole("ventas") || (!string.IsNullOrEmpty(referer) && referer.Contains("/Ventas", StringComparison.OrdinalIgnoreCase)))
        {
            return RedirectToAction("Index", "Ventas", new { section = "ventas" });
        }
        return RedirectToAction("Admin", "Dashboard", new { section = "ventas" });
    }

    /// <summary>
    /// Admin: Update order status — migrated from OrderController.js update()
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "admin,ventas")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string estado)
    {
        await _orderService.UpdateStatusAsync(id, estado);
        TempData["SuccessMessage"] = "Estado del pedido actualizado.";
        return RedirectToVentas();
    }
}

public class OrderItemInput
{
    public int ProductId { get; set; }
    public int Cantidad { get; set; }
}
