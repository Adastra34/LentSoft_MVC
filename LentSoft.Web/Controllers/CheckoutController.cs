using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    private readonly IInventoryService _inventoryService;
    private readonly IInvoiceService _invoiceService;
    private readonly ILogger<CheckoutController> _logger;

    public CheckoutController(
        ICartService cartService, 
        LentSoftDbContext context,
        IPasarelaPagoService pasarelaPagoService,
        IInventoryService inventoryService,
        IInvoiceService invoiceService,
        ILogger<CheckoutController> logger)
    {
        _cartService = cartService;
        _context = context;
        _pasarelaPagoService = pasarelaPagoService;
        _inventoryService = inventoryService;
        _invoiceService = invoiceService;
        _logger = logger;
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

        if (!string.IsNullOrWhiteSpace(model.NumeroTarjeta))
        {
            var numeroLimpio = LimpiarNumeroTarjeta(model.NumeroTarjeta);
            if (numeroLimpio.Length < 13 || numeroLimpio.Length > 19 || !numeroLimpio.All(char.IsDigit) || !_pasarelaPagoService.ValidarAlgoritmoLuhn(numeroLimpio))
            {
                ModelState.AddModelError(nameof(model.NumeroTarjeta), "El número de tarjeta no es válido. Revisa que esté completo y escrito correctamente.");
            }
        }

        if (!string.IsNullOrWhiteSpace(model.Expiracion) && EstaTarjetaVencida(model.Expiracion))
        {
            ModelState.AddModelError(nameof(model.Expiracion), "La tarjeta está vencida. Usa una tarjeta con fecha vigente.");
        }

        if (!ModelState.IsValid)
        {
            model.Cart = cart;
            return View("Index", model);
        }

        foreach (var item in cart.CartItems)
        {
            if (item.Product == null || !item.Product.HasStock(item.Cantidad))
            {
                var nombre = item.Product?.Nombre ?? $"Producto #{item.ProductId}";
                var disponible = item.Product?.Stock ?? 0;
                ModelState.AddModelError("", $"No hay stock suficiente de {nombre}. Disponible: {disponible}.");
            }
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
            Responsable = $"Cliente #{userId} (Checkout Web)"
        };

        var resultadoPasarela = await _pasarelaPagoService.ProcesarPagoTarjetaAsync(dto);

        if (!resultadoPasarela.Exitoso)
        {
            ModelState.AddModelError("", TraducirMensajePasarela(resultadoPasarela.Mensaje));
            model.Cart = cart;
            return View("Index", model);
        }

        using var dbTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 2. Crear Order y OrderItems tras aprobación
            var numeroLimpio = LimpiarNumeroTarjeta(model.NumeroTarjeta);
            var marca = _pasarelaPagoService.DetectarMarcaTarjeta(numeroLimpio);
            var ultimos4 = numeroLimpio.Substring(Math.Max(0, numeroLimpio.Length - 4));

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
                await _inventoryService.DisminuirStockAsync(
                    item.ProductId, 
                    item.Cantidad, 
                    InventoryConstants.MovementMotives.Venta, 
                    "Venta Online (Cliente)",
                    "Venta checkout online",
                    "Venta Web");

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
            await dbTransaction.CommitAsync();

            // Clear cart
            await _cartService.ClearCartAsync(userId);

            // 3. Generar factura electrónica automáticamente (no bloquear la compra si falla)
            try
            {
                await _invoiceService.CreateAsync(new Invoice { OrderId = order.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo generar automáticamente la factura para la orden {OrderId}", order.Id);
            }

            TempData["SuccessMessage"] = $"¡Pago procesado exitosamente por pasarela! {resultadoPasarela.Mensaje}";
            return RedirectToAction("Confirmacion", new { orderId = order.Id });
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync();
            TempData["ErrorMessage"] = "El producto fue modificado por otro usuario, vuelve a intentarlo.";
            model.Cart = cart;
            return View("Index", model);
        }
        catch (InvalidOperationException ex)
        {
            await dbTransaction.RollbackAsync();
            TempData["ErrorMessage"] = ex.Message;
            model.Cart = cart;
            return View("Index", model);
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
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

    private static string LimpiarNumeroTarjeta(string? numeroTarjeta)
    {
        return numeroTarjeta?.Replace(" ", "").Replace("-", "").Trim() ?? string.Empty;
    }

    private static bool EstaTarjetaVencida(string fechaExpiracion)
    {
        var parts = fechaExpiracion.Split('/');
        if (parts.Length != 2) return false;
        if (!int.TryParse(parts[0].Trim(), out int mes) || mes < 1 || mes > 12) return false;
        if (!int.TryParse(parts[1].Trim(), out int anioShort) || anioShort < 0 || anioShort > 99) return false;

        int anio = 2000 + anioShort;
        var lastDayOfMonth = DateTime.DaysInMonth(anio, mes);
        var expDate = new DateTime(anio, mes, lastDayOfMonth, 23, 59, 59, DateTimeKind.Utc);
        return expDate < DateTime.UtcNow;
    }

    private static string TraducirMensajePasarela(string? mensaje)
    {
        if (string.IsNullOrWhiteSpace(mensaje))
        {
            return "No pudimos procesar el pago. Inténtalo nuevamente.";
        }

        if (mensaje.Contains("Luhn", StringComparison.OrdinalIgnoreCase))
        {
            return "No pudimos validar tu tarjeta. Revisa el número e inténtalo de nuevo.";
        }

        if (mensaje.Contains("vencida", StringComparison.OrdinalIgnoreCase))
        {
            return "Tu tarjeta está vencida. Usa otra tarjeta.";
        }

        if (mensaje.Contains("rechazada por el banco", StringComparison.OrdinalIgnoreCase))
        {
            return "Tu banco rechazó el pago. Intenta con otra tarjeta.";
        }

        if (mensaje.Contains("límite máximo", StringComparison.OrdinalIgnoreCase))
        {
            return "Demasiados intentos fallidos. Espera unos minutos e inténtalo de nuevo.";
        }

        return "No pudimos procesar el pago. Inténtalo nuevamente.";
    }
}
