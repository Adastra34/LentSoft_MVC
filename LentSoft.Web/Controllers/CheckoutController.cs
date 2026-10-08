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
    private readonly IPasarelaPagoService _pasarelaPagoService;

    public CheckoutController(
        ICartService cartService, 
        LentSoftDbContext context,
        IPasarelaPagoService pasarelaPagoService)
    {
        _cartService = cartService;
        _context = context;
        _pasarelaPagoService = pasarelaPagoService;
    }

    private int GetCurrentUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdStr, out var userId) ? userId : 0;
    }

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

        var total = cart.CartItems.Sum(ci => ci.Subtotal);

        // 1. Procesar pago a través del servicio de pasarela de tarjetas
        var dto = new TarjetaPagoDTO
        {
            NumeroTarjeta = model.NumeroTarjeta,
            NombreTitular = model.NombreTarjeta,
            FechaExpiracion = model.Expiracion,
            Cvv = model.Cvv,
            Monto = total,
            Responsable = "Cliente (Checkout Web)"
        };

        var resultadoPasarela = await _pasarelaPagoService.ProcesarPagoTarjetaAsync(dto);

        if (!resultadoPasarela.Exitoso)
        {
            ModelState.AddModelError("", $"Error en la pasarela de pago: {resultadoPasarela.Mensaje}");
            TempData["ErrorMessage"] = $"Pago rechazado por la pasarela: {resultadoPasarela.Mensaje}";
            model.Cart = cart;
            return View("Index", model);
        }

        try
        {
            // 2. Crear Order y OrderItems tras aprobación
            var marca = _pasarelaPagoService.DetectarMarcaTarjeta(model.NumeroTarjeta);
            var ultimos4 = model.NumeroTarjeta.Substring(Math.Max(0, model.NumeroTarjeta.Length - 4));

            var order = new Order
            {
                UserId = userId,
                FechaPedido = DateTime.UtcNow,
                Estado = "pagado",
                EstadoPago = "pagado",
                DireccionEnvio = model.DireccionEnvio,
                Total = total,
                MontoPagado = total,
                MetodoPagoSimulado = $"Tarjeta {marca} (****{ultimos4})"
            };

            foreach (var item in cart.CartItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.Stock = Math.Max(0, product.Stock - item.Cantidad);
                    if (product.Stock > 85) product.Stock = 85;

                    if (product.Stock == 0)
                    {
                        product.Activo = false;
                    }

                    var movement = new InventoryMovement
                    {
                        ProductId = product.Id,
                        NombreProducto = product.Nombre,
                        Tipo = "Salida",
                        Cantidad = item.Cantidad,
                        Fecha = DateTime.UtcNow,
                        Responsable = "Venta Online (Cliente)"
                    };
                    _context.InventoryMovements.Add(movement);
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

            // Vincular la transacción de pasarela creada a esta nueva Orden
            if (resultadoPasarela.Transaccion != null)
            {
                resultadoPasarela.Transaccion.VentaId = order.Id;
                _context.TransaccionesPagos.Update(resultadoPasarela.Transaccion);
            }

            // Registrar el abono / pago completo automáticamente
            var pago = new PagoVenta
            {
                VentaId = order.Id,
                Monto = total,
                FechaPago = DateTime.UtcNow,
                MetodoPago = $"Tarjeta {marca} (****{ultimos4})",
                Responsable = "Cliente (Pasarela Web)",
                NumeroComprobante = $"REC-{DateTime.UtcNow.Year}-{(await _context.PagosVentas.CountAsync()) + 1:D4}"
            };
            _context.PagosVentas.Add(pago);

            await _context.SaveChangesAsync();

            // Clear cart
            await _cartService.ClearCartAsync(userId);

            TempData["SuccessMessage"] = $"¡Pago procesado exitosamente por pasarela! {resultadoPasarela.Mensaje}";
            return RedirectToAction("Confirmacion", new { orderId = order.Id });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al registrar la orden: {ex.Message}";
            model.Cart = cart;
            return View("Index", model);
        }
    }

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
