using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Models.ViewModels;
using LentSoft.Web.Services;

namespace LentSoft.Web.Controllers;

[Authorize(Roles = "ventas")]
public class VentasController : Controller
{
    private readonly LentSoftDbContext _context;
    private readonly IInvoiceService _invoiceService;
    private readonly ISaleConfirmationTokenService _saleConfirmationTokenService;
    private readonly IPagoVentaService _pagoVentaService;
    private readonly IPdfReciboService _pdfReciboService;
    private readonly IPasarelaPagoService _pasarelaPagoService;

    public VentasController(
        LentSoftDbContext context, 
        IInvoiceService invoiceService, 
        ISaleConfirmationTokenService saleConfirmationTokenService,
        IPagoVentaService pagoVentaService,
        IPdfReciboService pdfReciboService,
        IPasarelaPagoService pasarelaPagoService)
    {
        _context = context;
        _invoiceService = invoiceService;
        _saleConfirmationTokenService = saleConfirmationTokenService;
        _pagoVentaService = pagoVentaService;
        _pdfReciboService = pdfReciboService;
        _pasarelaPagoService = pasarelaPagoService;
    }

    public async Task<IActionResult> Index(
        string section = "general", 
        string subtab = "productos", 
        string? searchTerm = null, 
        int page = 1, 
        int pageSize = 5,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        string? filtroRapido = null)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var usuario = await _context.Users.FindAsync(userId);

        var now = DateTime.UtcNow;
        var inicioMes = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // ── Filtrado Backend por Fechas (Requisito 4) ──
        DateTime? desde = fechaDesde;
        DateTime? hasta = fechaHasta;

        if (!string.IsNullOrWhiteSpace(filtroRapido))
        {
            switch (filtroRapido.ToLower())
            {
                case "hoy":
                    desde = now.Date;
                    hasta = now.Date.AddDays(1).AddTicks(-1);
                    break;
                case "semana":
                    var diff = (int)now.DayOfWeek - (int)DayOfWeek.Monday;
                    if (diff < 0) diff += 7;
                    desde = now.Date.AddDays(-diff);
                    hasta = now.Date.AddDays(1).AddTicks(-1);
                    break;
                case "mes":
                    desde = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                    hasta = desde.Value.AddMonths(1).AddTicks(-1);
                    break;
                case "anio":
                    desde = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    hasta = new DateTime(now.Year, 12, 31, 23, 59, 59, DateTimeKind.Utc);
                    break;
            }
        }

        var queryVentas = _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .Include(o => o.Pagos)
            .Include(o => o.Transacciones)
            .AsQueryable();

        if (desde.HasValue)
        {
            queryVentas = queryVentas.Where(o => o.FechaPedido >= desde.Value);
        }
        if (hasta.HasValue)
        {
            queryVentas = queryVentas.Where(o => o.FechaPedido <= hasta.Value);
        }

        var ventas = await queryVentas
            .OrderByDescending(o => o.FechaPedido)
            .ToListAsync();

        var (facturasList, facturasTotalCount) = await _invoiceService.GetAllAsync(searchTerm, page, pageSize);
        var pedidosDisponibles = await _invoiceService.GetOrdersAvailableForInvoicingAsync();

        var productos = await _context.Products
            .Where(p => p.Activo)
            .OrderBy(p => p.Nombre)
            .ToListAsync();

        var clientes = await _context.Users
            .Where(u => u.Activo && u.Role == "usuario")
            .OrderBy(u => u.Nombre)
            .ThenBy(u => u.Apellido)
            .ToListAsync();

        var ventasDelMes = ventas
            .Where(v => v.Estado != "cancelado" && v.FechaPedido >= inicioMes)
            .Sum(v => v.Total);

        var pedidosActivos = ventas
            .Count(v => v.Estado == "pendiente" || v.Estado == "procesando" || v.Estado == "enviado");

        var clientesAtendidos = ventas
            .Select(v => v.UserId)
            .Distinct()
            .Count();

        var totalVentasConteo = ventas.Count(v => v.Estado != "cancelado");
        var ticketPromedio = totalVentasConteo > 0 ? (ventasDelMes / totalVentasConteo) : 0;

        var pedidosVentas = await _context.SalesOrders
            .Where(o => o.Activo)
            .OrderByDescending(o => o.Fecha)
            .ToListAsync();

        var viewModel = new DashboardVentasViewModel
        {
            VentasDelMes = ventasDelMes,
            PedidosActivos = pedidosActivos,
            ClientesAtendidos = clientesAtendidos,
            TicketPromedio = ticketPromedio,
            Ventas = ventas,
            FiltroFechaDesde = fechaDesde,
            FiltroFechaHasta = fechaHasta,
            FiltroRapido = filtroRapido,
            Facturas = facturasList,
            FacturasSearchTerm = searchTerm,
            FacturasPage = page,
            FacturasPageSize = pageSize,
            FacturasTotalCount = facturasTotalCount,
            PedidosDisponibles = pedidosDisponibles,
            Productos = productos,
            PedidosVentas = pedidosVentas,
            HistorialMovimientos = await _context.InventoryMovements.Include(m => m.Product).OrderByDescending(m => m.Fecha).ToListAsync(),
            Clientes = clientes,
            UsuarioActual = usuario,
            ActiveSection = section,
            ActiveSubTab = subtab
        };

        return View("~/Views/Dashboard/Ventas.cshtml", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOrderStatus(int id, string estado)
    {
        try
        {
            var order = await _context.Orders
                .Include(o => o.Pagos)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order != null)
            {
                var estadoLower = estado?.ToLower();
                var saldoPendiente = order.SaldoPendiente;

                if (estadoLower == "pagado" && saldoPendiente > 0)
                {
                    TempData["ErrorMessage"] = $"No se puede marcar el pedido como 'Pagado' porque tiene un saldo pendiente de ${saldoPendiente:N0}. Registre el abono correspondiente.";
                    return RedirectToAction("Index", new { section = "ventas" });
                }

                if (estadoLower == "cancelado" && order.Pagos != null && order.Pagos.Any())
                {
                    TempData["ErrorMessage"] = $"No se puede cancelar el pedido #ORD-{order.Id:D4} porque ya cuenta con abonos registrados (${order.Pagos.Sum(p => p.Monto):N0}). Por favor reverse o audite los abonos primero.";
                    return RedirectToAction("Index", new { section = "ventas" });
                }

                order.Estado = estadoLower ?? order.Estado;
                if (estadoLower == "pagado") order.EstadoPago = "pagado";
                else if (estadoLower == "cancelado") order.EstadoPago = "cancelado";

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Estado del pedido actualizado correctamente.";
            }
            else
            {
                TempData["ErrorMessage"] = "Pedido no encontrado.";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al actualizar el estado del pedido: {ex.Message}";
        }

        return RedirectToAction("Index", new { section = "ventas" });
    }

    // ── Endpoint Registrar Abono (Requisito 1) ──
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarAbono(int ventaId, decimal monto, string metodoPago)
    {
        try
        {
            var responsable = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "Vendedor";
            var pago = await _pagoVentaService.RegistrarAbonoAsync(ventaId, monto, metodoPago, responsable);

            TempData["SuccessMessage"] = $"Abono de ${monto:N0} registrado correctamente. Comprobante N° {pago.NumeroComprobante}.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al registrar abono: {ex.Message}";
        }

        return RedirectToAction("Index", new { section = "ventas" });
    }

    // ── Endpoint Descargar Comprobante PDF (Requisito 1) ──
    [HttpGet]
    public async Task<IActionResult> DescargarComprobante(int pagoId)
    {
        var pago = await _pagoVentaService.ObtenerPagoPorIdAsync(pagoId);
        if (pago == null)
        {
            TempData["ErrorMessage"] = "No se encontró el comprobante de pago solicitado.";
            return RedirectToAction("Index", new { section = "ventas" });
        }

        try
        {
            var pdfBytes = _pdfReciboService.GenerateReciboPdf(pago);
            var filename = $"Recibo-{pago.NumeroComprobante}.pdf";
            return File(pdfBytes, "application/pdf", filename);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al generar el comprobante PDF: {ex.Message}";
            return RedirectToAction("Index", new { section = "ventas" });
        }
    }

    // ── Endpoint Cobrar con Tarjeta de Crédito en Ventas (Requisito 3) ──
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CobrarTarjeta(TarjetaPagoDTO dto)
    {
        try
        {
            dto.Responsable = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "Vendedor";
            var resultado = await _pasarelaPagoService.ProcesarPagoTarjetaAsync(dto);

            if (resultado.Exitoso)
            {
                TempData["SuccessMessage"] = $"¡Cobro con tarjeta exitoso! {resultado.Mensaje}";
            }
            else
            {
                TempData["ErrorMessage"] = $"Cobro con tarjeta rechazado: {resultado.Mensaje}";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error en procesamiento de cobro con tarjeta: {ex.Message}";
        }

        return RedirectToAction("Index", new { section = "ventas" });
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmarCompra(string token)
    {
        var viewModel = new SaleConfirmationViewModel();

        if (string.IsNullOrWhiteSpace(token))
        {
            viewModel.IsValid = false;
            viewModel.ErrorMessage = "El token de confirmación de venta está ausente o no es válido.";
            return View("~/Views/Ventas/ConfirmarCompra.cshtml", viewModel);
        }

        var saleId = _saleConfirmationTokenService.ValidateToken(token);
        if (saleId == null)
        {
            viewModel.IsValid = false;
            viewModel.ErrorMessage = "Este enlace ya no es válido o ha expirado.";
            return View("~/Views/Ventas/ConfirmarCompra.cshtml", viewModel);
        }

        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == saleId.Value);

        if (order == null || !order.Activo)
        {
            viewModel.IsValid = false;
            viewModel.ErrorMessage = "La venta asociada no existe o ha sido dada de baja.";
            return View("~/Views/Ventas/ConfirmarCompra.cshtml", viewModel);
        }

        viewModel.IsValid = true;
        viewModel.Order = order;
        return View("~/Views/Ventas/ConfirmarCompra.cshtml", viewModel);
    }
}
