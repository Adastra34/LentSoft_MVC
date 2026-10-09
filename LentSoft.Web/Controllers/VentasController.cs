using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Models.ViewModels;
using LentSoft.Web.Services;

using Microsoft.AspNetCore.RateLimiting;

namespace LentSoft.Web.Controllers;

[Authorize(Roles = "ventas,admin")]
public class VentasController : Controller
{
    private readonly LentSoftDbContext _context;
    private readonly IInvoiceService _invoiceService;
    private readonly ISaleConfirmationTokenService _saleConfirmationTokenService;
    private readonly IPagoVentaService _pagoVentaService;
    private readonly IPdfReciboService _pdfReciboService;
    private readonly IPasarelaPagoService _pasarelaPagoService;
    private readonly ISalesOrderService _salesOrderService;

    public VentasController(
        LentSoftDbContext context, 
        IInvoiceService invoiceService, 
        ISaleConfirmationTokenService saleConfirmationTokenService,
        IPagoVentaService pagoVentaService,
        IPdfReciboService pdfReciboService,
        IPasarelaPagoService pasarelaPagoService,
        ISalesOrderService salesOrderService)
    {
        _context = context;
        _invoiceService = invoiceService;
        _saleConfirmationTokenService = saleConfirmationTokenService;
        _pagoVentaService = pagoVentaService;
        _pdfReciboService = pdfReciboService;
        _pasarelaPagoService = pasarelaPagoService;
        _salesOrderService = salesOrderService;
    }

    public async Task<IActionResult> Index(
        string section = "general", 
        string subtab = "productos", 
        string? searchTerm = null, 
        int page = 1, 
        int pageSize = 5,
        string? ventasSearchTerm = null,
        int ventasPage = 1,
        int ventasPageSize = 10,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        string? filtroRapido = null,
        int movPage = 1,
        int movPageSize = 15,
        int pedVentasPage = 1,
        int pedVentasPageSize = 10)
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

        // ── Paginación y Filtrado de Ventas en BD con AsNoTracking() ──
        var queryVentas = _context.Orders
            .Where(o => o.Activo)
            .Include(o => o.User)
            .Include(o => o.Pagos)
            .Include(o => o.Transacciones)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(ventasSearchTerm))
        {
            var term = ventasSearchTerm.Trim().ToLower();
            queryVentas = queryVentas.Where(o =>
                o.Id.ToString().Contains(term) ||
                (o.User != null && (
                    o.User.Nombre.ToLower().Contains(term) ||
                    o.User.Apellido.ToLower().Contains(term) ||
                    (o.User.Nombre + " " + o.User.Apellido).ToLower().Contains(term)
                )) ||
                o.Estado.ToLower().Contains(term) ||
                o.EstadoPago.ToLower().Contains(term));
        }

        if (desde.HasValue)
        {
            queryVentas = queryVentas.Where(o => o.FechaPedido >= desde.Value);
        }
        if (hasta.HasValue)
        {
            queryVentas = queryVentas.Where(o => o.FechaPedido <= hasta.Value);
        }

        var ventasTotalCount = await queryVentas.CountAsync();
        if (ventasPage < 1) ventasPage = 1;
        if (ventasPageSize < 1) ventasPageSize = 10;

        var ventas = await queryVentas
            .OrderByDescending(o => o.FechaPedido)
            .Skip((ventasPage - 1) * ventasPageSize)
            .Take(ventasPageSize)
            .ToListAsync();

        var (facturasList, facturasTotalCount) = await _invoiceService.GetAllAsync(searchTerm, page, pageSize);
        var pedidosDisponibles = await _invoiceService.GetOrdersAvailableForInvoicingAsync();

        var productos = await _context.Products
            .Where(p => p.Activo)
            .OrderBy(p => p.Nombre)
            .Take(100)
            .AsNoTracking()
            .ToListAsync();

        var clientes = await _context.Users
            .Where(u => u.Activo && u.Role == "usuario")
            .OrderBy(u => u.Nombre)
            .ThenBy(u => u.Apellido)
            .AsNoTracking()
            .ToListAsync();

        // ── KPIs calculados directamente en BD sin traer colecciones ──
        var ventasDelMes = await _context.Orders
            .Where(o => o.Activo && o.Estado != "cancelado" && o.FechaPedido >= inicioMes)
            .SumAsync(o => (decimal?)o.Total) ?? 0;

        var pedidosActivos = await _context.Orders
            .Where(o => o.Activo && (o.Estado == "pendiente" || o.Estado == "procesando" || o.Estado == "enviado"))
            .CountAsync();

        var clientesAtendidos = await _context.Orders
            .Where(o => o.Activo)
            .Select(o => o.UserId)
            .Distinct()
            .CountAsync();

        var totalVentasConteo = await _context.Orders
            .Where(o => o.Activo && o.Estado != "cancelado")
            .CountAsync();

        var ticketPromedio = totalVentasConteo > 0 ? (ventasDelMes / totalVentasConteo) : 0;

        // ── Pedidos de Ventas (a través de ISalesOrderService) ──
        var (pedidosVentas, pedidosVentasTotal) = await _salesOrderService.GetAllAsync(null, pedVentasPage, pedVentasPageSize);

        // ── Paginación de HistorialMovimientos (InventoryMovements) ──
        var movQuery = _context.InventoryMovements.Include(m => m.Product).AsNoTracking().AsQueryable();
        var movTotalCount = await movQuery.CountAsync();
        if (movPage < 1) movPage = 1;
        if (movPageSize < 1) movPageSize = 15;
        var movimientos = await movQuery
            .OrderByDescending(m => m.Fecha)
            .Skip((movPage - 1) * movPageSize)
            .Take(movPageSize)
            .ToListAsync();

        var viewModel = new DashboardVentasViewModel
        {
            VentasDelMes = ventasDelMes,
            PedidosActivos = pedidosActivos,
            ClientesAtendidos = clientesAtendidos,
            TicketPromedio = ticketPromedio,
            Ventas = ventas,
            VentasSearchTerm = ventasSearchTerm,
            VentasPage = ventasPage,
            VentasPageSize = ventasPageSize,
            VentasTotalCount = ventasTotalCount,
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
            PedidosVentasPage = pedVentasPage,
            PedidosVentasPageSize = pedVentasPageSize,
            PedidosVentasTotalCount = pedidosVentasTotal,
            HistorialMovimientos = movimientos,
            MovimientosPage = movPage,
            MovimientosPageSize = movPageSize,
            MovimientosTotalCount = movTotalCount,
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

                var estadoAnterior = order.Estado;
                order.Estado = estadoLower ?? order.Estado;
                if (estadoLower == "pagado") order.EstadoPago = "pagado";
                else if (estadoLower == "cancelado") order.EstadoPago = "cancelado";

                if (estadoAnterior != order.Estado)
                {
                    var audit = new AuditoriaVenta
                    {
                        TipoEntidad = "Pedido",
                        EntidadId = order.Id,
                        EstadoAnterior = estadoAnterior,
                        EstadoNuevo = order.Estado,
                        Accion = "CambioEstadoPedido",
                        Usuario = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "Ventas",
                        Fecha = DateTime.UtcNow,
                        Detalles = $"Pedido #ORD-{order.Id:D4} cambió de estado: {estadoAnterior} -> {order.Estado} (Estado Pago: {order.EstadoPago})"
                    };
                    _context.AuditoriasVentas.Add(audit);
                }

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

    // ── Endpoint Cobrar con Tarjeta de Crédito en Ventas (Requisito 3 con Rate Limiting) ──
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("tarjeta-pago")]
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

    // ── Endpoint Exportar Listado de Ventas a CSV ──
    [HttpGet]
    public async Task<IActionResult> ExportarVentasCsv(DateTime? fechaDesde, DateTime? fechaHasta, string? searchTerm)
    {
        var query = _context.Orders
            .Where(o => o.Activo)
            .Include(o => o.User)
            .Include(o => o.Pagos)
            .AsNoTracking()
            .AsQueryable();

        if (fechaDesde.HasValue) query = query.Where(o => o.FechaPedido >= fechaDesde.Value);
        if (fechaHasta.HasValue) query = query.Where(o => o.FechaPedido <= fechaHasta.Value);
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(o => o.Id.ToString().Contains(term) || (o.User != null && (o.User.Nombre.ToLower().Contains(term) || o.User.Apellido.ToLower().Contains(term))));
        }

        var list = await query.OrderByDescending(o => o.FechaPedido).ToListAsync();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("ID;Cliente;Documento;Fecha;Total;Abonado;Saldo;EstadoPago;EstadoOrden");

        foreach (var v in list)
        {
            var abonado = v.Pagos?.Sum(p => p.Monto) ?? v.MontoPagado;
            var saldo = Math.Max(0m, v.Total - abonado);
            sb.AppendLine($"#ORD-{v.Id:D4};\"{v.User?.NombreCompleto ?? "N/A"}\";{v.User?.NumeroDocumento ?? "N/A"};{v.FechaPedido:dd/MM/yyyy HH:mm};{v.Total};{abonado};{saldo};{v.EstadoPago};{v.Estado}");
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", $"ventas_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv");
    }

    // ── Endpoints Integración Fórmula Óptica - Ventas ──
    [HttpGet]
    public async Task<IActionResult> GetFormulasPorPaciente(int userId)
    {
        var formulas = await _context.FormulasOpticas
            .Where(f => f.UserId == userId && f.Activo)
            .OrderByDescending(f => f.Fecha)
            .ToListAsync();

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
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Pedido no encontrado.";
                return RedirectToAction("Index", new { section = "ventas" });
            }

            if (formulaOpticaId.HasValue && formulaOpticaId.Value > 0)
            {
                var formula = await _context.FormulasOpticas
                    .FirstOrDefaultAsync(f => f.Id == formulaOpticaId.Value && f.Activo);

                if (formula == null)
                {
                    TempData["ErrorMessage"] = "La fórmula óptica seleccionada no existe o está inactiva.";
                    return RedirectToAction("Index", new { section = "ventas" });
                }

                if (formula.UserId != order.UserId)
                {
                    TempData["ErrorMessage"] = "La fórmula óptica no pertenece al paciente de este pedido.";
                    return RedirectToAction("Index", new { section = "ventas" });
                }

                order.FormulaOpticaId = formulaOpticaId.Value;
                TempData["SuccessMessage"] = $"Fórmula óptica #{formula.Id} vinculada correctamente al pedido #ORD-{order.Id:D4}.";
            }
            else
            {
                order.FormulaOpticaId = null;
                TempData["SuccessMessage"] = $"Fórmula óptica desvinculada del pedido #ORD-{order.Id:D4}.";
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al vincular fórmula óptica: {ex.Message}";
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

    /// <summary>
    /// Requisito 3: Consulta de solo lectura de fórmula óptica vigente por cliente para el módulo de ventas
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetFormulaVigentePorCliente(int userId)
    {
        var formula = await _context.FormulasOpticas
            .Where(f => f.UserId == userId && f.Activo)
            .OrderByDescending(f => f.Fecha)
            .FirstOrDefaultAsync();

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
}
