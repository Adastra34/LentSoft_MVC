using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Models.ViewModels;
using LentSoft.Web.Services;

namespace LentSoft.Web.Controllers;

[Authorize(Roles = "ventas,admin")]
public class VentasController : Controller
{
    private readonly LentSoftDbContext _context;
    private readonly IVentasService _ventasService;
    private readonly IInvoiceService _invoiceService;
    private readonly ISaleConfirmationTokenService _saleConfirmationTokenService;
    private readonly IPagoVentaService _pagoVentaService;
    private readonly IPdfReciboService _pdfReciboService;
    private readonly IPasarelaPagoService _pasarelaPagoService;
    private readonly IEmailService _emailService;
    private readonly ILogger<VentasController> _logger;

    public VentasController(
        LentSoftDbContext context,
        IVentasService ventasService,
        IInvoiceService invoiceService,
        ISaleConfirmationTokenService saleConfirmationTokenService,
        IPagoVentaService pagoVentaService,
        IPdfReciboService pdfReciboService,
        IPasarelaPagoService pasarelaPagoService,
        IEmailService emailService,
        ILogger<VentasController> logger)
    {
        _context = context;
        _ventasService = ventasService;
        _invoiceService = invoiceService;
        _saleConfirmationTokenService = saleConfirmationTokenService;
        _pagoVentaService = pagoVentaService;
        _pdfReciboService = pdfReciboService;
        _pasarelaPagoService = pasarelaPagoService;
        _emailService = emailService;
        _logger = logger;
    }

    [HttpGet]
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

        // Indicadores calculados mediante consultas agregadas desde VentasService
        var indicadores = await _ventasService.ObtenerIndicadoresVentasAsync();

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

        var pedidosVentas = await _context.SalesOrders
            .Where(o => o.Activo)
            .OrderByDescending(o => o.Fecha)
            .ToListAsync();

        var historialMovimientos = await _context.InventoryMovements
            .Include(m => m.Product)
            .OrderByDescending(m => m.Fecha)
            .Take(20)
            .ToListAsync();

        var viewModel = new DashboardVentasViewModel
        {
            VentasDelMes = indicadores.VentasDelMes,
            PedidosActivos = indicadores.PedidosActivos,
            ClientesAtendidos = indicadores.ClientesAtendidos,
            TicketPromedio = indicadores.TicketPromedio,
            Ventas = new List<Order>(), // Index ya no carga ventas en memoria; DataTables serverSide se encarga
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
            HistorialMovimientos = historialMovimientos,
            Clientes = clientes,
            UsuarioActual = usuario,
            ActiveSection = section,
            ActiveSubTab = subtab
        };

        return View("~/Views/Dashboard/Ventas.cshtml", viewModel);
    }

    /// <summary>
    /// Endpoint JSON serverSide para DataTables con paginación, filtros de fecha, estado y búsqueda
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListarVentas([FromQuery] VentasFilterRequest request)
    {
        try
        {
            var response = await _ventasService.ObtenerVentasDataTablesAsync(request);
            return Json(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar consulta serverSide de ventas para DataTables");
            return Json(new DataTablesResponse<VentaDataTablesDto>
            {
                Draw = request.Draw,
                RecordsTotal = 0,
                RecordsFiltered = 0,
                Data = new List<VentaDataTablesDto>()
            });
        }
    }

    /// <summary>
    /// Registrar venta desde el módulo de Ventas con validación estricta de stock y transacción única
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarVenta(RegistrarVentaDto dto)
    {
        try
        {
            var responsable = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "Ventas";
            var order = await _ventasService.RegistrarVentaAsync(dto, responsable);

            TempData["SuccessMessage"] = $"Venta #{order.Id} registrada exitosamente.";

            // Envío opcional de correo de confirmación de venta si el cliente tiene correo corporativo o real
            try
            {
                var cliente = await _context.Users.FindAsync(order.UserId);
                if (cliente != null && !string.IsNullOrWhiteSpace(cliente.Email) && !cliente.Email.EndsWith("@cliente.com", StringComparison.OrdinalIgnoreCase))
                {
                    var token = _saleConfirmationTokenService.GenerateToken(order.Id);
                    var scheme = Request.Scheme;
                    var host = Request.Host;
                    var confirmLink = $"{scheme}://{host}/Ventas/ConfirmarCompra?token={token}";
                    await _emailService.SendSaleConfirmationEmailAsync(cliente.Email, order, confirmLink);
                }
            }
            catch (Exception mailEx)
            {
                _logger.LogWarning(mailEx, "No se pudo enviar el correo de confirmación para el pedido #{OrderId}", order.Id);
            }
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Parámetros inválidos al registrar venta");
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Regla de negocio no satisfecha al registrar venta");
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Entidad no encontrada al registrar venta");
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al registrar la venta");
            TempData["ErrorMessage"] = "Ocurrió un error al procesar la venta. Intente nuevamente.";
        }

        return RedirectToAction("Index", new { section = "ventas" });
    }

    /// <summary>
    /// Cambiar estado del pedido respetando transiciones válidas y actualizando factura si se cancela
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOrderStatus(int id, string estado)
    {
        try
        {
            await _ventasService.CambiarEstadoPedidoAsync(id, estado);
            TempData["SuccessMessage"] = "Estado del pedido actualizado correctamente.";
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Transición de estado inválida para el pedido #{OrderId}: {Estado}", id, estado);
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Pedido #{OrderId} no encontrado para actualizar estado", id);
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al actualizar el estado del pedido #{OrderId}", id);
            TempData["ErrorMessage"] = "Ocurrió un error al actualizar el estado del pedido. Intente nuevamente.";
        }

        return RedirectToAction("Index", new { section = "ventas" });
    }

    /// <summary>
    /// Registrar Abono con número de comprobante único y reintento ante colisión
    /// </summary>
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
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Monto inválido para abono en venta #{VentaId}", ventaId);
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validación de abono rechazada para venta #{VentaId}", ventaId);
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Venta #{VentaId} no encontrada al abonar", ventaId);
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al registrar abono en venta #{VentaId}", ventaId);
            TempData["ErrorMessage"] = "Ocurrió un error al registrar el abono. Intente nuevamente.";
        }

        return RedirectToAction("Index", new { section = "ventas" });
    }

    /// <summary>
    /// Descargar comprobante PDF oficial de recibo de caja
    /// </summary>
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
            _logger.LogError(ex, "Error al generar el comprobante PDF para el pago #{PagoId}", pagoId);
            TempData["ErrorMessage"] = "Error al generar el comprobante PDF del recibo de pago.";
            return RedirectToAction("Index", new { section = "ventas" });
        }
    }

    /// <summary>
    /// Cobrar con tarjeta en Ventas validando existencia, saldo y guardando en transacción atómica
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CobrarTarjeta(TarjetaPagoDTO dto)
    {
        try
        {
            dto.Responsable = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "Vendedor";

            // Validación previa en controlador antes de contactar pasarela
            if (dto.VentaId.HasValue && dto.VentaId.Value > 0)
            {
                var venta = await _context.Orders.Include(o => o.Pagos).FirstOrDefaultAsync(o => o.Id == dto.VentaId.Value);
                if (venta == null || !venta.Activo)
                {
                    TempData["ErrorMessage"] = "La venta especificada no existe o no se encuentra activa.";
                    return RedirectToAction("Index", new { section = "ventas" });
                }

                if (string.Equals(venta.Estado, "cancelado", StringComparison.OrdinalIgnoreCase))
                {
                    TempData["ErrorMessage"] = "No es posible procesar pagos sobre una venta cancelada.";
                    return RedirectToAction("Index", new { section = "ventas" });
                }

                var saldo = venta.SaldoPendiente;
                if (dto.Monto > saldo)
                {
                    TempData["ErrorMessage"] = $"El monto a cobrar (${dto.Monto:N0}) supera el saldo pendiente (${saldo:N0}).";
                    return RedirectToAction("Index", new { section = "ventas" });
                }
            }

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
            // NUNCA loguear número de tarjeta, CVV ni fecha de expiración
            var ultimos4 = !string.IsNullOrWhiteSpace(dto.NumeroTarjeta) && dto.NumeroTarjeta.Length >= 4
                ? dto.NumeroTarjeta[^4..]
                : "desconocido";
            _logger.LogError(ex, "Error inesperado al procesar cobro con tarjeta terminada en {UltimosDigitos}", ultimos4);
            TempData["ErrorMessage"] = "Ocurrió un error al procesar el cobro con tarjeta. Intente nuevamente.";
        }

        return RedirectToAction("Index", new { section = "ventas" });
    }

    [HttpGet]
    public async Task<IActionResult> GetFormulasPorPaciente(int userId)
    {
        var formulas = await _ventasService.ObtenerFormulasPorPacienteAsync(userId);
        var result = formulas.Select(f => new
        {
            id = f.Id,
            fecha = f.Fecha.ToString("dd/MM/yyyy"),
            tipoLente = f.TipoLente,
            estado = f.Estado
        });

        return Json(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VincularFormula(int orderId, int? formulaOpticaId)
    {
        try
        {
            await _ventasService.VincularFormulaAsync(orderId, formulaOpticaId);
            if (formulaOpticaId.HasValue && formulaOpticaId.Value > 0)
            {
                TempData["SuccessMessage"] = $"Fórmula óptica #{formulaOpticaId.Value} vinculada correctamente al pedido #ORD-{orderId:D4}.";
            }
            else
            {
                TempData["SuccessMessage"] = $"Fórmula óptica desvinculada del pedido #ORD-{orderId:D4}.";
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error de validación al vincular fórmula óptica al pedido #{OrderId}", orderId);
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Pedido #{OrderId} no encontrado para vincular fórmula", orderId);
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al vincular fórmula óptica al pedido #{OrderId}", orderId);
            TempData["ErrorMessage"] = "Ocurrió un error al vincular la fórmula óptica. Intente nuevamente.";
        }

        return RedirectToAction("Index", new { section = "ventas" });
    }

    [HttpGet]
    public async Task<IActionResult> GetFormulaVigentePorCliente(int userId)
    {
        var formula = await _ventasService.ObtenerFormulaVigentePorClienteAsync(userId);
        if (formula == null)
        {
            return Json(new { encontrado = false, message = "El cliente no tiene fórmulas ópticas registradas." });
        }

        bool esVigente = formula.Fecha.AddMonths(12).Date >= DateTime.UtcNow.Date;

        return Json(new
        {
            encontrado = true,
            esVigente = esVigente,
            formula = new
            {
                id = formula.Id,
                fecha = formula.Fecha.ToString("yyyy-MM-dd"),
                fechaFormato = formula.Fecha.ToString("dd/MM/yyyy"),
                esferaOD = formula.EsferaOD ?? "",
                cilindroOD = formula.CilindroOD ?? "",
                ejeOD = formula.EjeOD ?? "",
                esferaOI = formula.EsferaOI ?? "",
                cilindroOI = formula.CilindroOI ?? "",
                ejeOI = formula.EjeOI ?? "",
                tipoLente = formula.TipoLente ?? "",
                distanciaPupilar = formula.DistanciaPupilar ?? "",
                observaciones = formula.Observaciones ?? "",
                estado = formula.Estado
            }
        });
    }

    [HttpGet]
    public async Task<IActionResult> ReporteProductosVendidos(DateTime? fechaInicio, DateTime? fechaFin)
    {
        try
        {
            var reporte = await _ventasService.ObtenerReporteProductosVendidosAsync(fechaInicio, fechaFin);
            return Json(reporte);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar reporte de productos vendidos");
            return StatusCode(500, new { error = "Ocurrió un error al generar el reporte de productos vendidos." });
        }
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
