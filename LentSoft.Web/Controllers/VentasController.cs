using System.IO;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LentSoft.Web.Data;
using LentSoft.Web.Models.DTOs;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Models.ViewModels;
using LentSoft.Web.Services;

namespace LentSoft.Web.Controllers;

[Authorize(Roles = "ventas,admin")]
public class VentasController : Controller
{
    private readonly IVentasService _ventasService;
    private readonly IPagoVentaService _pagoVentaService;
    private readonly IPdfReciboService _pdfReciboService;
    private readonly IPasarelaPagoService _pasarelaPagoService;
    private readonly ISaleConfirmationTokenService _saleConfirmationTokenService;
    private readonly IProductService _productService;
    private readonly IInventoryService _inventoryService;
    private readonly IFileStorageService _fileStorageService;
    private readonly LentSoftDbContext _context;
    private readonly ILogger<VentasController> _logger;

    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxImageSizeBytes = 5 * 1024 * 1024; // 5 MB

    public VentasController(
        IVentasService ventasService,
        IPagoVentaService pagoVentaService,
        IPdfReciboService pdfReciboService,
        IPasarelaPagoService pasarelaPagoService,
        ISaleConfirmationTokenService saleConfirmationTokenService,
        IProductService productService,
        IInventoryService inventoryService,
        IFileStorageService fileStorageService,
        LentSoftDbContext context,
        ILogger<VentasController> logger)
    {
        _ventasService = ventasService;
        _pagoVentaService = pagoVentaService;
        _pdfReciboService = pdfReciboService;
        _pasarelaPagoService = pasarelaPagoService;
        _saleConfirmationTokenService = saleConfirmationTokenService;
        _productService = productService;
        _inventoryService = inventoryService;
        _fileStorageService = fileStorageService;
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string section = "general", 
        string subtab = "productos", 
        string? searchTerm = null, 
        int page = 1, 
        int pageSize = 5,
        string? prodSearch = null,
        string? prodEstado = null,
        string? prodCategoria = null,
        string? prodProveedor = null,
        bool prodStockBajo = false,
        string? prodSortBy = "nombre",
        string prodSortDir = "asc",
        int prodPage = 1,
        int prodPageSize = 20,
        string? movSearch = null,
        string? movTipo = null,
        int? movProductId = null,
        DateTime? movFechaInicio = null,
        DateTime? movFechaFin = null,
        string? movMotivo = null,
        string movSort = "desc",
        int movPage = 1,
        int movPageSize = 20,
        string? pedSearch = null,
        string? pedEstado = null,
        int pedPage = 1,
        int pedPageSize = 15)
    {
        // Fallback para searchTerm genérico según pestaña activa
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            if (section == "inventarios" && subtab == "productos" && string.IsNullOrWhiteSpace(prodSearch)) prodSearch = searchTerm;
            if (section == "inventarios" && subtab == "historial" && string.IsNullOrWhiteSpace(movSearch)) movSearch = searchTerm;
            if (section == "inventarios" && subtab == "pedidos-ventas" && string.IsNullOrWhiteSpace(pedSearch)) pedSearch = searchTerm;
        }

        if (section == "inventarios")
        {
            await _productService.AsignarSkusConsecutivosFaltantesAsync();
        }

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var viewModel = await _ventasService.ObtenerDashboardAsync(
            section,
            subtab,
            searchTerm,
            page,
            pageSize,
            userId,
            pedSearch,
            pedEstado,
            pedPage,
            pedPageSize,
            movSearch,
            movTipo,
            movProductId,
            movFechaInicio,
            movFechaFin,
            movMotivo,
            movSort,
            movPage,
            movPageSize);

        // Siempre proveer lista completa de productos y proveedores sin seguimiento (AsNoTracking) para filtros y modales
        var todosLosProductos = await _context.Products.AsNoTracking().Include(p => p.Supplier).OrderBy(p => p.Nombre).ToListAsync();
        var proveedores = await _context.Suppliers.AsNoTracking().Where(s => s.Activo).OrderBy(s => s.Nombre).ToListAsync();
        viewModel.TodosLosProductos = todosLosProductos;
        viewModel.Proveedores = proveedores;

        // ── Productos (Paginados y Filtrados en BD sin seguimiento) ──
        var prodQuery = _context.Products.AsNoTracking().Include(p => p.Supplier).AsQueryable();
        if (!string.IsNullOrWhiteSpace(prodSearch))
        {
            var s = prodSearch.Trim().ToLower();
            prodQuery = prodQuery.Where(p => p.Nombre.ToLower().Contains(s)
                || (p.Sku != null && p.Sku.ToLower().Contains(s))
                || (p.Marca != null && p.Marca.ToLower().Contains(s))
                || (p.Categoria != null && p.Categoria.ToLower().Contains(s))
                || (p.Ubicacion != null && p.Ubicacion.ToLower().Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(prodEstado))
        {
            var est = prodEstado.Trim().ToLower();
            if (est == "activo") prodQuery = prodQuery.Where(p => p.Activo);
            else if (est == "inactivo") prodQuery = prodQuery.Where(p => !p.Activo);
        }

        if (!string.IsNullOrWhiteSpace(prodCategoria))
        {
            var cat = prodCategoria.Trim().ToLower();
            if (cat == "lentes-sol")
                prodQuery = prodQuery.Where(p => p.Categoria != null && (p.Categoria.ToLower().Contains("sol") || p.Categoria.ToLower().Contains("lentes-sol")));
            else if (cat == "monturas")
                prodQuery = prodQuery.Where(p => p.Categoria != null && p.Categoria.ToLower().Contains("montura"));
            else if (cat == "lentes-contacto")
                prodQuery = prodQuery.Where(p => p.Categoria != null && (p.Categoria.ToLower().Contains("contacto") || p.Categoria.ToLower().Contains("lentes-contacto")));
            else if (cat == "lentes-graduados")
                prodQuery = prodQuery.Where(p => p.Categoria != null && (p.Categoria.ToLower().Contains("graduado") || p.Categoria.ToLower().Contains("lentes-graduados")));
            else if (cat == "accesorios")
                prodQuery = prodQuery.Where(p => p.Categoria != null && p.Categoria.ToLower().Contains("accesorio"));
            else
                prodQuery = prodQuery.Where(p => p.Categoria != null && p.Categoria.ToLower().Contains(cat));
        }

        if (!string.IsNullOrWhiteSpace(prodProveedor))
        {
            var supp = prodProveedor.Trim();
            prodQuery = prodQuery.Where(p => p.SupplierId == supp || (p.Supplier != null && p.Supplier.Nombre.ToLower().Contains(supp.ToLower())));
        }

        if (prodStockBajo)
        {
            prodQuery = prodQuery.Where(p => p.Stock <= p.StockMinimo);
        }

        prodQuery = (prodSortBy?.ToLower(), prodSortDir?.ToLower()) switch
        {
            ("sku", "desc") => prodQuery.OrderByDescending(p => p.Sku),
            ("sku", _) => prodQuery.OrderBy(p => p.Sku),
            ("categoria", "desc") => prodQuery.OrderByDescending(p => p.Categoria),
            ("categoria", _) => prodQuery.OrderBy(p => p.Categoria),
            ("marca", "desc") => prodQuery.OrderByDescending(p => p.Marca),
            ("marca", _) => prodQuery.OrderBy(p => p.Marca),
            ("proveedor", "desc") => prodQuery.OrderByDescending(p => p.Supplier != null ? p.Supplier.Nombre : ""),
            ("proveedor", _) => prodQuery.OrderBy(p => p.Supplier != null ? p.Supplier.Nombre : ""),
            ("precio", "desc") => prodQuery.OrderByDescending(p => (double)p.Precio),
            ("precio", _) => prodQuery.OrderBy(p => (double)p.Precio),
            ("stock", "desc") => prodQuery.OrderByDescending(p => p.Stock),
            ("stock", _) => prodQuery.OrderBy(p => p.Stock),
            ("estado", "desc") => prodQuery.OrderByDescending(p => p.Activo),
            ("estado", _) => prodQuery.OrderBy(p => p.Activo),
            (_, "desc") => prodQuery.OrderByDescending(p => p.Nombre),
            _ => prodQuery.OrderBy(p => p.Nombre)
        };

        var prodTotalCount = await prodQuery.CountAsync();
        var productosPaginados = await prodQuery
            .Skip((prodPage - 1) * prodPageSize)
            .Take(prodPageSize)
            .ToListAsync();

        if (section == "inventarios")
        {
            viewModel.Productos = productosPaginados;
        }

        viewModel.ProdSearchTerm = prodSearch;
        viewModel.ProdEstado = prodEstado;
        viewModel.ProdCategoria = prodCategoria;
        viewModel.ProdProveedor = prodProveedor;
        viewModel.ProdStockBajo = prodStockBajo;
        viewModel.ProdSortBy = prodSortBy;
        viewModel.ProdSortDir = prodSortDir ?? "asc";
        viewModel.ProdPage = prodPage;
        viewModel.ProdPageSize = prodPageSize;
        viewModel.ProdTotalCount = prodTotalCount;

        viewModel.ActiveSection = section;
        viewModel.ActiveSubTab = subtab;

        return View("~/Views/Dashboard/Ventas.cshtml", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ListarVentas(
        [FromForm] int draw,
        [FromForm] int start,
        [FromForm] int length,
        [FromForm] string? estado,
        [FromForm] string? estadoPago,
        [FromForm] DateTime? fechaDesde,
        [FromForm] DateTime? fechaHasta,
        [FromForm] string? filtroRapido)
    {
        try
        {
            var searchValue = Request.Form["search[value]"].FirstOrDefault();
            int.TryParse(Request.Form["order[0][column]"].FirstOrDefault(), out var sortCol);
            var sortDir = Request.Form["order[0][dir]"].FirstOrDefault() ?? "desc";

            var dtRequest = new DataTablesRequestDTO
            {
                Draw = draw,
                Start = start,
                Length = length > 0 ? length : 10,
                SearchValue = searchValue,
                SortColumnIndex = sortCol,
                SortDirection = sortDir,
                Estado = estado,
                EstadoPago = estadoPago,
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta,
                FiltroRapido = filtroRapido
            };

            var result = await _ventasService.ListarVentasDataTablesAsync(dtRequest);
            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al listar ventas para DataTables");
            return Json(new DataTablesResult<VentaRowDTO>
            {
                Draw = draw,
                RecordsTotal = 0,
                RecordsFiltered = 0,
                Data = new List<VentaRowDTO>()
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExportarVentasCsv(
        string? searchValue = null,
        string? estado = null,
        string? estadoPago = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        string? filtroRapido = null)
    {
        try
        {
            var filtro = new DataTablesRequestDTO
            {
                SearchValue = searchValue,
                Estado = estado,
                EstadoPago = estadoPago,
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta,
                FiltroRapido = filtroRapido,
                SortColumnIndex = 2,
                SortDirection = "desc"
            };

            var ventas = await _ventasService.ExportarVentasFiltradasAsync(filtro);

            var sb = new StringBuilder();
            sb.AppendLine("ID Pedido,Cliente,Documento,Telefono,Fecha,Cant. Items,Detalle Productos,Total Venta,Monto Abonado,Saldo Pendiente,Estado Pago,Estado Pedido,Metodo Pago,Formula Optica,Factura");

            static string EscapeCsv(string? val)
            {
                if (string.IsNullOrEmpty(val)) return "\"\"";
                return $"\"{val.Replace("\"", "\"\"")}\"";
            }

            foreach (var v in ventas)
            {
                sb.AppendLine(string.Join(",",
                    EscapeCsv($"#ORD-{v.Id:D4}"),
                    EscapeCsv(v.ClienteNombre),
                    EscapeCsv(v.ClienteDocumento),
                    EscapeCsv(v.ClienteTelefono),
                    EscapeCsv(v.FechaPedido.ToString("yyyy-MM-dd HH:mm")),
                    v.TotalItems,
                    EscapeCsv(v.ResumenItems),
                    v.Total.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                    v.MontoPagado.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                    v.SaldoPendiente.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                    EscapeCsv(v.EstadoPago),
                    EscapeCsv(v.Estado),
                    EscapeCsv(v.MetodoPago),
                    EscapeCsv(v.FormulaDetalle),
                    EscapeCsv(v.FacturaNumero ?? "Sin factura")
                ));
            }

            var preamble = Encoding.UTF8.GetPreamble();
            var contentBytes = Encoding.UTF8.GetBytes(sb.ToString());
            var finalBytes = new byte[preamble.Length + contentBytes.Length];
            Buffer.BlockCopy(preamble, 0, finalBytes, 0, preamble.Length);
            Buffer.BlockCopy(contentBytes, 0, finalBytes, preamble.Length, contentBytes.Length);

            var fileName = $"Ventas_LentSoft_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv";
            return File(finalBytes, "text/csv; charset=utf-8", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al exportar CSV completo de ventas");
            TempData["ErrorMessage"] = "Ocurrió un error al generar la exportación CSV de ventas.";
            return RedirectToAction("Index", new { section = "ventas" });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarVenta(RegistrarVentaDTO dto)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() 
                             ?? "Datos del formulario de venta incompletos o inválidos.";
            TempData["ErrorMessage"] = firstError;
            return RedirectToAction("Index", new { section = "ventas" });
        }

        try
        {
            var responsable = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "Vendedor";
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var order = await _ventasService.RegistrarVentaAsync(dto, responsable, baseUrl);

            TempData["SuccessMessage"] = $"¡Venta registrada exitosamente! Pedido N° #ORD-{order.Id:D4} generado.";
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validación de negocio al registrar venta");
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Argumento inválido al registrar venta");
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no controlado al registrar la venta");
            TempData["ErrorMessage"] = "Ocurrió un error inesperado al registrar la venta. Por favor intente nuevamente.";
        }

        return RedirectToAction("Index", new { section = "ventas" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOrderStatus(int id, string estado)
    {
        try
        {
            var responsable = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "Vendedor";
            var (success, message) = await _ventasService.CambiarEstadoOrdenAsync(id, estado, responsable);
            if (success)
            {
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = message;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar estado de la orden {OrderId}", id);
            TempData["ErrorMessage"] = "Ocurrió un error al intentar cambiar el estado del pedido.";
        }

        return RedirectToAction("Index", new { section = "ventas" });
    }

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
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validación al registrar abono en la venta {VentaId}", ventaId);
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Argumento inválido al registrar abono en la venta {VentaId}", ventaId);
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no controlado al registrar abono en la venta {VentaId}", ventaId);
            TempData["ErrorMessage"] = "Ocurrió un error al procesar el abono. Por favor intente más tarde.";
        }

        return RedirectToAction("Index", new { section = "ventas" });
    }

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
            _logger.LogError(ex, "Error al generar comprobante PDF {PagoId}", pagoId);
            TempData["ErrorMessage"] = "Ocurrió un error al generar el archivo del comprobante PDF.";
            return RedirectToAction("Index", new { section = "ventas" });
        }
    }

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
            _logger.LogError(ex, "Error en procesamiento de cobro con tarjeta para venta {VentaId}", dto.VentaId);
            TempData["ErrorMessage"] = "Ocurrió un error inesperado durante el cobro con tarjeta.";
        }

        return RedirectToAction("Index", new { section = "ventas" });
    }

    [HttpGet]
    public async Task<IActionResult> GetFormulasPorPaciente(int userId)
    {
        try
        {
            var formulas = await _ventasService.ObtenerFormulasPorPacienteAsync(userId);
            var result = formulas.Select(f => new
            {
                id = f.Id,
                fecha = f.Fecha,
                tipoLente = f.TipoLente,
                estado = f.Estado,
                esVigente = f.EsVigente
            });
            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener fórmulas del paciente {UserId}", userId);
            return Json(new List<object>());
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetFormulaVigentePorCliente(int userId)
    {
        try
        {
            var dto = await _ventasService.ObtenerFormulaVigentePorClienteAsync(userId);
            if (!dto.Encontrado || dto.Formula == null)
            {
                return Json(new { encontrado = false, message = dto.Mensaje ?? "El cliente no tiene fórmulas ópticas registradas." });
            }

            return Json(new
            {
                encontrado = true,
                esVigente = dto.EsVigente,
                formula = new
                {
                    id = dto.Formula.Id,
                    fecha = dto.Formula.Fecha,
                    fechaFormato = dto.Formula.FechaFormato,
                    esferaOD = dto.Formula.EsferaOD,
                    cilindroOD = dto.Formula.CilindroOD,
                    ejeOD = dto.Formula.EjeOD,
                    esferaOI = dto.Formula.EsferaOI,
                    cilindroOI = dto.Formula.CilindroOI,
                    ejeOI = dto.Formula.EjeOI,
                    tipoLente = dto.Formula.TipoLente,
                    distanciaPupilar = dto.Formula.DistanciaPupilar,
                    observaciones = dto.Formula.Observaciones,
                    estado = dto.Formula.Estado
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar fórmula vigente para cliente {UserId}", userId);
            return Json(new { encontrado = false, message = "Error al consultar la fórmula óptica." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VincularFormula(int orderId, int? formulaOpticaId)
    {
        try
        {
            var responsable = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "Vendedor";
            var (success, message) = await _ventasService.VincularFormulaAsync(orderId, formulaOpticaId, responsable);
            if (success)
            {
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = message;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al vincular fórmula a la orden {OrderId}", orderId);
            TempData["ErrorMessage"] = "Ocurrió un error al vincular la fórmula óptica al pedido.";
        }

        return RedirectToAction("Index", new { section = "ventas" });
    }

    // ══════════════════════════════════════════════════════════════════════════════
    // 4.1 PEDIDOS DE VENTA (SalesOrder) — Acciones propias de VentasController
    // ══════════════════════════════════════════════════════════════════════════════

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSalesOrder(CrearPedidoVentaDTO dto)
    {
        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json");

        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault()
                             ?? "Los datos del pedido de venta no son válidos.";
            if (isAjax) return Json(new { success = false, message = firstError });
            TempData["ErrorMessage"] = firstError;
            return RedirectToAction("Index", new { section = "inventarios", subtab = "pedidos-ventas" });
        }

        var responsable = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "Vendedor";
        var resultado = await _ventasService.CrearPedidoVentaAsync(dto, responsable);

        if (isAjax)
        {
            return Json(new { success = resultado.Exito, message = resultado.Mensaje, warning = resultado.Advertencia, id = resultado.PedidoId });
        }

        if (resultado.Exito)
        {
            TempData["SuccessMessage"] = resultado.Mensaje;
            if (!string.IsNullOrWhiteSpace(resultado.Advertencia))
            {
                TempData["WarningMessage"] = resultado.Advertencia;
            }
        }
        else
        {
            TempData["ErrorMessage"] = resultado.Mensaje;
        }

        return RedirectToAction("Index", new { section = "inventarios", subtab = "pedidos-ventas" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSalesOrder(EditarPedidoVentaDTO dto)
    {
        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json");

        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault()
                             ?? "Los datos para editar el pedido de venta no son válidos.";
            if (isAjax) return Json(new { success = false, message = firstError });
            TempData["ErrorMessage"] = firstError;
            return RedirectToAction("Index", new { section = "inventarios", subtab = "pedidos-ventas" });
        }

        var responsable = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "Vendedor";
        var resultado = await _ventasService.EditarPedidoVentaAsync(dto, responsable);

        if (isAjax)
        {
            return Json(new { success = resultado.Exito, message = resultado.Mensaje, warning = resultado.Advertencia, id = resultado.PedidoId });
        }

        if (resultado.Exito)
        {
            TempData["SuccessMessage"] = resultado.Mensaje;
            if (!string.IsNullOrWhiteSpace(resultado.Advertencia))
            {
                TempData["WarningMessage"] = resultado.Advertencia;
            }
        }
        else
        {
            TempData["ErrorMessage"] = resultado.Mensaje;
        }

        return RedirectToAction("Index", new { section = "inventarios", subtab = "pedidos-ventas" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSalesOrder(int id)
    {
        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json");

        var responsable = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "Vendedor";
        var resultado = await _ventasService.EliminarPedidoVentaAsync(id, responsable);

        if (isAjax)
        {
            return Json(new { success = resultado.Exito, message = resultado.Mensaje, warning = resultado.Advertencia, id = resultado.PedidoId });
        }

        if (resultado.Exito)
        {
            if (!string.IsNullOrWhiteSpace(resultado.Advertencia))
            {
                TempData["WarningMessage"] = resultado.Advertencia;
            }
            else
            {
                TempData["SuccessMessage"] = resultado.Mensaje;
            }
        }
        else
        {
            TempData["ErrorMessage"] = resultado.Mensaje;
        }

        return RedirectToAction("Index", new { section = "inventarios", subtab = "pedidos-ventas" });
    }

    [HttpGet]
    public async Task<IActionResult> ReporteProductosVendidos(DateTime? fechaInicio, DateTime? fechaFin)
    {
        try
        {
            var reporte = await _ventasService.ObtenerReporteProductosVendidosAsync(fechaInicio, fechaFin);
            return Json(new { success = true, data = reporte });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener reporte de productos vendidos");
            return Json(new { success = false, message = "Error al generar el reporte de productos vendidos." });
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

        var order = await _ventasService.ObtenerVentaPorIdAsync(saleId.Value);

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

    // ══════════════════════════════════════════════════════════════════════════════
    // 4.7 NOTA DE MANTENIBILIDAD — DUPLICACIÓN DE ACCIONES DE INVENTARIO EN VENTAS
    // Las siguientes acciones (CreateProduct, EditProduct, DeleteProduct, ToggleProductStatus,
    // BulkToggleStatus, CreateMovement, KardexJson, ExportarExcelDatos) duplican flujo de
    // DashboardController / ProductController / InventoryMovementController para mantener
    // aislada la navegación del rol 'ventas' hacia ~/Views/Dashboard/Ventas.cshtml sin tocar
    // controladores de otros módulos. Quedan documentadas para futura unificación con el
    // equipo de Inventario en un servicio/controlador compartido de catálogo y kardex.
    // ══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Crea un producto desde la vista de Inventarios del módulo de Ventas.
    /// NOTA DE ARQUITECTURA (Sección 4.7): Acción duplicada respecto al módulo de Inventario;
    /// pendiente de unificar en un endpoint/servicio compartido con el equipo de Inventario.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProduct(Product product, IFormFile? ImagenArchivo)
    {
        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json");

        if (ImagenArchivo != null && ImagenArchivo.Length > 0)
        {
            var (success, relativePath, error) = await _fileStorageService.SaveImageAsync(ImagenArchivo, "products", AllowedImageExtensions, MaxImageSizeBytes);
            if (!success)
            {
                if (isAjax) return Json(new { success = false, message = error });
                TempData["ErrorMessage"] = error;
                return RedirectToAction("Index", new { section = "inventarios", subtab = "productos" });
            }
            product.ImagenUrl = relativePath;
        }

        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Datos del producto no válidos.";
            if (isAjax) return Json(new { success = false, message = firstError });
            TempData["ErrorMessage"] = firstError;
            return RedirectToAction("Index", new { section = "inventarios", subtab = "productos" });
        }

        if (product.Stock == 0)
        {
            product.Activo = false;
        }

        try
        {
            var created = await _productService.CreateAsync(product);
            if (isAjax) return Json(new { success = true, message = "Producto creado exitosamente.", data = created });
            TempData["SuccessMessage"] = "Producto creado exitosamente.";
        }
        catch (DbUpdateConcurrencyException)
        {
            var msg = "El producto fue modificado por otro usuario, vuelve a intentarlo.";
            if (isAjax) return Json(new { success = false, message = msg });
            TempData["ErrorMessage"] = msg;
        }
        catch (Exception ex)
        {
            if (isAjax) return Json(new { success = false, message = $"Error al crear el producto: {ex.Message}" });
            TempData["ErrorMessage"] = $"Error al crear el producto: {ex.Message}";
        }

        return RedirectToAction("Index", new { section = "inventarios", subtab = "productos" });
    }

    /// <summary>
    /// Actualiza un producto desde la vista de Inventarios del módulo de Ventas.
    /// NOTA DE ARQUITECTURA (Sección 4.7): Duplicación documentada para futura unificación con el equipo de Inventario.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProduct(Product product, IFormFile? ImagenArchivo)
    {
        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json");

        var existingProduct = await _productService.GetByIdAsync(product.Id);
        if (existingProduct == null)
        {
            if (isAjax) return Json(new { success = false, message = "Producto no encontrado." });
            TempData["ErrorMessage"] = "Producto no encontrado.";
            return RedirectToAction("Index", new { section = "inventarios", subtab = "productos" });
        }

        string? oldImageUrlToDelete = null;

        if (ImagenArchivo != null && ImagenArchivo.Length > 0)
        {
            var (success, relativePath, error) = await _fileStorageService.SaveImageAsync(ImagenArchivo, "products", AllowedImageExtensions, MaxImageSizeBytes);
            if (!success)
            {
                if (isAjax) return Json(new { success = false, message = error });
                TempData["ErrorMessage"] = error;
                return RedirectToAction("Index", new { section = "inventarios", subtab = "productos" });
            }
            oldImageUrlToDelete = existingProduct.ImagenUrl;
            product.ImagenUrl = relativePath;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(product.ImagenUrl))
            {
                product.ImagenUrl = existingProduct.ImagenUrl;
            }
        }

        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Datos del producto no válidos.";
            if (isAjax) return Json(new { success = false, message = firstError });
            TempData["ErrorMessage"] = firstError;
            return RedirectToAction("Index", new { section = "inventarios", subtab = "productos" });
        }

        if (product.Stock == 0)
        {
            product.Activo = false;
        }

        try
        {
            var updated = await _productService.UpdateAsync(product.Id, product);
            if (updated == null)
            {
                if (isAjax) return Json(new { success = false, message = "Producto no encontrado." });
                TempData["ErrorMessage"] = "Producto no encontrado.";
            }
            else
            {
                if (!string.IsNullOrEmpty(oldImageUrlToDelete))
                {
                    _fileStorageService.DeleteFile(oldImageUrlToDelete);
                }

                bool stockAgotado = updated.Stock == 0;
                string msg = stockAgotado
                    ? "Producto actualizado. El stock llegó a 0 y fue inactivado automáticamente."
                    : "Producto actualizado exitosamente.";

                if (isAjax)
                {
                    return Json(new { 
                        success = true, 
                        message = msg, 
                        data = updated, 
                        stockAgotado = stockAgotado 
                    });
                }

                if (stockAgotado)
                {
                    TempData["WarningMessage"] = msg;
                }
                else
                {
                    TempData["SuccessMessage"] = msg;
                }
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            var msg = "El producto fue modificado por otro usuario, vuelve a intentarlo.";
            if (isAjax) return Json(new { success = false, message = msg });
            TempData["ErrorMessage"] = msg;
        }
        catch (Exception ex)
        {
            if (isAjax) return Json(new { success = false, message = $"Error al actualizar el producto: {ex.Message}" });
            TempData["ErrorMessage"] = $"Error al actualizar el producto: {ex.Message}";
        }

        return RedirectToAction("Index", new { section = "inventarios", subtab = "productos" });
    }

    /// <summary>
    /// Elimina (inactiva) un producto desde la vista de Inventarios del módulo de Ventas.
    /// NOTA DE ARQUITECTURA (Sección 4.7): Duplicación documentada para futura unificación con el equipo de Inventario.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json");

        try
        {
            var deleted = await _productService.DeleteAsync(id);
            if (deleted)
            {
                if (isAjax) return Json(new { success = true, message = "Producto eliminado exitosamente.", id });
                TempData["SuccessMessage"] = "Producto eliminado exitosamente.";
            }
            else
            {
                if (isAjax) return Json(new { success = false, message = "Producto no encontrado." });
                TempData["ErrorMessage"] = "Producto no encontrado.";
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            var msg = "El producto fue modificado por otro usuario, vuelve a intentarlo.";
            if (isAjax) return Json(new { success = false, message = msg });
            TempData["ErrorMessage"] = msg;
        }
        catch (Exception ex)
        {
            if (isAjax) return Json(new { success = false, message = $"Error al eliminar el producto: {ex.Message}" });
            TempData["ErrorMessage"] = $"Error al eliminar el producto: {ex.Message}";
        }

        return RedirectToAction("Index", new { section = "inventarios", subtab = "productos" });
    }

    /// <summary>
    /// Alterna el estado activo/inactivo de un producto desde la vista de Inventarios del módulo de Ventas.
    /// NOTA DE ARQUITECTURA (Sección 4.7): Duplicación documentada para futura unificación con el equipo de Inventario.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleProductStatus(int id)
    {
        try
        {
            var product = await _productService.GetByIdAsync(id);
            if (product == null)
            {
                return Json(new { success = false, message = "Producto no encontrado." });
            }

            if (product.Activo)
            {
                var success = await _productService.DeleteAsync(id);
                return Json(new { success, active = false, activo = false, message = "Producto inactivado exitosamente." });
            }
            else
            {
                if (product.Stock == 0)
                {
                    return Json(new { success = false, message = "No se puede activar un producto con stock 0. Reponga el inventario primero." });
                }
                var success = await _productService.ReactivateAsync(id);
                return Json(new { success, active = true, activo = true, message = "Producto activado exitosamente." });
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error al actualizar estado: {ex.Message}" });
        }
    }

    /// <summary>
    /// Alterna en lote el estado activo/inactivo de productos seleccionados desde Ventas.
    /// NOTA DE ARQUITECTURA (Sección 4.7): Duplicación documentada para futura unificación con el equipo de Inventario.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkToggleStatus([FromBody] BulkToggleRequest request)
    {
        if (request?.Ids == null || !request.Ids.Any())
            return Json(new { success = false, message = "No se seleccionaron productos." });

        int count = 0;
        foreach (var id in request.Ids)
        {
            var p = await _productService.GetByIdAsync(id);
            if (p == null) continue;
            if (request.Activo)
            {
                if (p.Stock > 0 && !p.Activo)
                {
                    await _productService.ReactivateAsync(id);
                    count++;
                }
            }
            else
            {
                if (p.Activo)
                {
                    await _productService.DeleteAsync(id);
                    count++;
                }
            }
        }
        return Json(new { success = true, modified = count, message = $"{count} productos actualizados correctamente." });
    }

    /// <summary>
    /// Registra un movimiento de inventario (Entrada/Salida) desde el módulo de Ventas.
    /// NOTA DE ARQUITECTURA (Sección 4.7): Duplicación documentada para futura unificación con el equipo de Inventario.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateMovement(InventoryMovement movement)
    {
        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json");

        if (movement.ProductId <= 0 || movement.Cantidad <= 0)
        {
            var msg = "Debe seleccionar un producto y especificar una cantidad mayor a 0.";
            if (isAjax) return Json(new { success = false, message = msg });
            TempData["ErrorMessage"] = msg;
            return RedirectToAction("Index", new { section = "inventarios", subtab = "historial" });
        }

        var isSalida = string.Equals(movement.Tipo?.Trim(), InventoryConstants.MovementTypes.Salida, StringComparison.OrdinalIgnoreCase);

        if (isSalida && (string.IsNullOrWhiteSpace(movement.Motivo) || movement.Motivo.Trim() == "Otro"))
        {
            var msg = "El motivo es obligatorio para salidas de inventario (Venta, Merma, Daño, Devolución, Garantía, etc.).";
            if (isAjax) return Json(new { success = false, message = msg });
            TempData["ErrorMessage"] = msg;
            return RedirectToAction("Index", new { section = "inventarios", subtab = "historial" });
        }

        var responsable = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "Ventas";
        var motivo = !string.IsNullOrWhiteSpace(movement.Motivo) ? movement.Motivo.Trim() : (isSalida ? InventoryConstants.MovementMotives.Venta : InventoryConstants.MovementMotives.Compra);

        try
        {
            Product product;
            if (isSalida)
            {
                product = await _inventoryService.DisminuirStockAsync(
                    movement.ProductId, 
                    movement.Cantidad, 
                    motivo,
                    responsable,
                    movement.Nota,
                    movement.Referencia);

                if (product.Stock == 0)
                {
                    TempData["WarningMessage"] = $"Se inhabilitó el producto {product.Nombre}";
                }
                else if (product.Stock <= (product.StockMinimo > 0 ? product.StockMinimo : _inventoryService.LowStockThreshold))
                {
                    TempData["WarningMessage"] = $"¡Alerta de Stock Mínimo! El producto {product.Nombre} está por agotarse (Stock: {product.Stock}).";
                }
            }
            else
            {
                product = await _inventoryService.AumentarStockAsync(
                    movement.ProductId, 
                    movement.Cantidad, 
                    motivo,
                    responsable,
                    movement.Nota,
                    movement.Referencia);
            }

            var tipoStr = isSalida ? InventoryConstants.MovementTypes.Salida : InventoryConstants.MovementTypes.Entrada;
            var successMsg = $"Movimiento de {tipoStr} ({motivo}) registrado exitosamente. Nuevo stock: {product.Stock}.";
            
            if (isAjax)
            {
                return Json(new { success = true, message = successMsg, stock = product.Stock, activo = product.Activo });
            }

            if (TempData["WarningMessage"] != null)
            {
                TempData["SuccessMessage"] = successMsg + " " + TempData["WarningMessage"];
            }
            else
            {
                TempData["SuccessMessage"] = successMsg;
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            var msg = "El producto fue modificado por otro usuario, vuelve a intentarlo.";
            if (isAjax) return Json(new { success = false, message = msg });
            TempData["ErrorMessage"] = msg;
        }
        catch (InvalidOperationException ex)
        {
            if (isAjax) return Json(new { success = false, message = ex.Message });
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception ex)
        {
            var msg = $"Error al registrar el movimiento: {ex.Message}";
            if (isAjax) return Json(new { success = false, message = msg });
            TempData["ErrorMessage"] = msg;
        }

        return RedirectToAction("Index", new { section = "inventarios", subtab = "historial" });
    }

    /// <summary>
    /// Consulta el kardex de un producto en JSON desde el módulo de Ventas.
    /// NOTA DE ARQUITECTURA (Sección 4.7): Duplicación documentada para futura unificación con el equipo de Inventario.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> KardexJson(int productId)
    {
        var product = await _context.Products.AsNoTracking().Include(p => p.Supplier).FirstOrDefaultAsync(p => p.Id == productId);
        if (product == null)
        {
            return Json(new { success = false, message = "Producto no encontrado." });
        }

        var movimientos = await _context.InventoryMovements
            .AsNoTracking()
            .Where(m => m.ProductId == productId)
            .OrderByDescending(m => m.Fecha)
            .ThenByDescending(m => m.Id)
            .Select(m => new {
                m.Id,
                Fecha = m.Fecha.ToString("yyyy-MM-dd HH:mm"),
                m.Tipo,
                m.Motivo,
                m.Cantidad,
                m.StockResultante,
                Responsable = m.Responsable ?? "Ventas",
                m.Referencia,
                m.Nota
            })
            .ToListAsync();

        return Json(new {
            success = true,
            product = new {
                product.Id,
                product.Nombre,
                product.Sku,
                product.Stock,
                StockMinimo = product.StockMinimo > 0 ? product.StockMinimo : 10,
                product.Precio,
                product.Categoria,
                product.Marca,
                Proveedor = product.Supplier != null ? product.Supplier.Nombre : "Sin proveedor",
                product.ImagenUrl
            },
            movimientos
        });
    }

    /// <summary>
    /// Exporta datos filtrados del historial de movimientos desde el módulo de Ventas.
    /// NOTA DE ARQUITECTURA (Sección 4.7): Duplicación documentada para futura unificación con el equipo de Inventario.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExportarExcelDatos(string? search, string? tipo, int? productId, DateTime? fechaInicio, DateTime? fechaFin, string? sort)
    {
        var query = _context.InventoryMovements.AsNoTracking().Include(m => m.Product).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(m => (m.NombreProducto != null && m.NombreProducto.ToLower().Contains(s))
                || (m.Product != null && m.Product.Nombre.ToLower().Contains(s))
                || (m.Responsable != null && m.Responsable.ToLower().Contains(s))
                || (m.Referencia != null && m.Referencia.ToLower().Contains(s))
                || m.Id.ToString().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(tipo))
        {
            var t = tipo.Trim().ToLower();
            query = query.Where(m => m.Tipo != null && m.Tipo.ToLower() == t);
        }

        if (productId.HasValue && productId.Value > 0)
        {
            query = query.Where(m => m.ProductId == productId.Value);
        }

        if (fechaInicio.HasValue)
        {
            var fInicioUtc = DateTime.SpecifyKind(fechaInicio.Value.Date, DateTimeKind.Utc);
            query = query.Where(m => m.Fecha >= fInicioUtc);
        }

        if (fechaFin.HasValue)
        {
            var fFinUtc = DateTime.SpecifyKind(fechaFin.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(m => m.Fecha <= fFinUtc);
        }

        query = (sort == "asc")
            ? query.OrderBy(m => m.Fecha)
            : query.OrderByDescending(m => m.Fecha);

        var data = await query.Select(m => new {
            Id = m.Id,
            Fecha = m.Fecha.ToString("yyyy-MM-dd HH:mm"),
            Producto = m.NombreProducto ?? (m.Product != null ? m.Product.Nombre : "Producto #" + m.ProductId),
            Sku = m.Product != null ? m.Product.Sku : "",
            Tipo = m.Tipo,
            Cantidad = m.Cantidad,
            StockResultante = m.StockResultante,
            Motivo = m.Motivo,
            Responsable = m.Responsable ?? "Ventas",
            Referencia = m.Referencia ?? "",
            Nota = m.Nota ?? ""
        }).ToListAsync();

        return Json(new { success = true, data });
    }
}
