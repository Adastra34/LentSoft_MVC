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
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly IProductService _productService;
    private readonly IUserService _userService;
    private readonly IOrderService _orderService;
    private readonly IFavoriteService _favoriteService;
    private readonly IInvoiceService _invoiceService;
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<DashboardController> _logger;
    private readonly LentSoftDbContext _context;
    private readonly InventorySettings _inventorySettings;

    public DashboardController(
        IDashboardService dashboardService,
        IProductService productService,
        IUserService userService,
        IOrderService orderService,
        IFavoriteService favoriteService,
        IInvoiceService invoiceService,
        IInventoryService inventoryService,
        ILogger<DashboardController> logger,
        LentSoftDbContext context,
        Microsoft.Extensions.Options.IOptions<InventorySettings> inventoryOptions)
    {
        _dashboardService = dashboardService;
        _productService = productService;
        _userService = userService;
        _orderService = orderService;
        _favoriteService = favoriteService;
        _invoiceService = invoiceService;
        _inventoryService = inventoryService;
        _logger = logger;
        _context = context;
        _inventorySettings = inventoryOptions?.Value ?? new InventorySettings();
    }

    /// <summary>
    /// Admin dashboard â€” reconstruido con sidebar y 6 secciones
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Admin([FromQuery] DashboardAdminQueryFilter filter)
    {
        ViewBag.IncludeInactive = filter.IncludeInactive;
        ViewBag.MovSort = filter.MovSort;

        var viewModel = await _dashboardService.GetDashboardAdminViewModelAsync(filter);

        ViewBag.Optometras = await _context.Users.AsNoTracking()
            .Where(u => u.Role == "optometra" && u.Activo)
            .OrderBy(u => u.Nombre)
            .ToListAsync();

        ViewBag.AuditoriaCitas = await _context.AuditoriaCitas.AsNoTracking()
            .OrderByDescending(a => a.FechaCambio)
            .Take(_inventorySettings.RecentItemsLimit * 2)
            .ToListAsync();

        return View("Admin", viewModel);
    }

    /// <summary>
    /// User dashboard â€” solo lectura de datos propios y gestión de sus citas/perfil/favoritos
    /// </summary>
    public async Task<IActionResult> Usuario(
        string section = "perfil",
        string? citasSearch = null,
        int citasPage = 1,
        int citasPageSize = 5)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _userService.GetByIdAsync(userId);

        if (user == null)
            return RedirectToAction("Login", "Auth");

        var pedidos = await _orderService.GetByUserIdAsync(userId);
        var favoritos = await _favoriteService.GetFavoritesByUserIdAsync(userId);

        // Cargar Historial Clínico y Fórmulas Ópticas estrictamente del usuario autenticado
        var historiales = await _context.HistorialesClinicos
            .Include(h => h.Optometra)
            .Where(h => h.UserId == userId && h.Activo)
            .OrderByDescending(h => h.Fecha)
            .ToListAsync();

        var formulas = await _context.FormulasOpticas
            .Include(f => f.Optometra)
            .Where(f => f.UserId == userId && f.Activo)
            .OrderByDescending(f => f.Fecha)
            .ToListAsync();

        // Citas del usuario autenticado con filtrado y paginación
        var citasQuery = _context.Appointments.Include(a => a.Optometra).Where(a => a.UserId == userId && a.Activo);
        if (!string.IsNullOrWhiteSpace(citasSearch))
        {
            var term = citasSearch.Trim().ToLower();
            citasQuery = citasQuery.Where(a => a.Servicio.ToLower().Contains(term) || a.Estado.ToLower().Contains(term));
        }

        var citasTotalCount = await citasQuery.CountAsync();
        var citasPaginadas = await citasQuery
            .OrderByDescending(a => a.FechaHora)
            .Skip((citasPage - 1) * citasPageSize)
            .Take(citasPageSize)
            .ToListAsync();

        var viewModel = new DashboardUsuarioViewModel
        {
            Usuario = user,
            Pedidos = pedidos,
            Favoritos = favoritos,
            Historiales = historiales,
            Formulas = formulas,
            Citas = citasPaginadas,
            CitasSearchTerm = citasSearch,
            CitasPage = citasPage,
            CitasPageSize = citasPageSize,
            CitasTotalCount = citasTotalCount,
            ActiveSection = section
        };

        return View(viewModel);
    }

    // â”€â”€ Usuario: Agendar Nueva Cita â”€â”€
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UserScheduleAppointment(string Servicio, DateTime FechaHora, string? Notas)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        if (string.IsNullOrWhiteSpace(Servicio) || FechaHora <= DateTime.UtcNow)
        {
            TempData["ErrorMessage"] = "Por favor selecciona un servicio y una fecha/hora válida a futuro.";
            return RedirectToAction("Usuario", new { section = "citas" });
        }

        // 1. Validar horario laboral
        if (!Appointment.EsHorarioLaboral(FechaHora))
        {
            TempData["ErrorMessage"] = "Las citas solo pueden agendarse de lunes a sábado, entre 8:00 a.m. y 6:00 p.m.";
            return RedirectToAction("Usuario", new { section = "citas" });
        }

        try
        {
            // 2. Buscar primer optómetra disponible
            var optometras = await _context.Users.Where(u => u.Role == "optometra" && u.Activo).ToListAsync();
            User? optDisponible = null;
            foreach (var opt in optometras)
            {
                if (await Appointment.HayDisponibilidad(_context, opt.Id, FechaHora))
                {
                    optDisponible = opt;
                    break;
                }
            }

            if (optDisponible == null)
            {
                TempData["ErrorMessage"] = "No hay optómetras disponibles en ese horario. Por favor elige otro horario.";
                return RedirectToAction("Usuario", new { section = "citas" });
            }

            var appointment = new Appointment
            {
                UserId = userId,
                OptometraId = optDisponible.Id,
                Servicio = Servicio.Trim(),
                FechaHora = FechaHora,
                Notas = Notas?.Trim(),
                Estado = "pendiente",
                FechaCreacion = DateTime.UtcNow
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cita agendada exitosamente.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al agendar la cita: {ex.Message}";
        }

        return RedirectToAction("Usuario", new { section = "citas" });
    }

    // â”€â”€ Usuario: Cancelar Cita â”€â”€
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UserCancelAppointment(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        try
        {
            var cita = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (cita == null)
            {
                TempData["ErrorMessage"] = "Cita no encontrada o no pertenece a tu usuario.";
                return RedirectToAction("Usuario", new { section = "citas" });
            }

            if (cita.Estado != "pendiente" && cita.Estado != "confirmada")
            {
                TempData["ErrorMessage"] = "Solo puedes cancelar citas pendientes o confirmadas.";
                return RedirectToAction("Usuario", new { section = "citas" });
            }

            cita.Estado = "cancelada";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "La cita ha sido cancelada exitosamente.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al cancelar la cita: {ex.Message}";
        }

        return RedirectToAction("Usuario", new { section = "citas" });
    }

    // â”€â”€ Usuario: Reprogramar Cita â”€â”€
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UserRescheduleAppointment(int id, DateTime NuevaFechaHora)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        try
        {
            var cita = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (cita == null)
            {
                TempData["ErrorMessage"] = "Cita no encontrada o no pertenece a tu usuario.";
                return RedirectToAction("Usuario", new { section = "citas" });
            }

            if (cita.Estado == "completada" || cita.Estado == "cancelada")
            {
                TempData["ErrorMessage"] = "No se puede reprogramar una cita completada o cancelada.";
                return RedirectToAction("Usuario", new { section = "citas" });
            }

            // 1. Limitar a 1 reprogramación
            if (cita.VecesReprogramada >= 1)
            {
                TempData["ErrorMessage"] = "Esta cita ya fue reprogramada una vez. Si necesitas otro cambio, por favor cancela la cita y agenda una nueva.";
                return RedirectToAction("Usuario", new { section = "citas" });
            }

            if (NuevaFechaHora <= DateTime.UtcNow)
            {
                TempData["ErrorMessage"] = "La nueva fecha y hora debe ser a futuro.";
                return RedirectToAction("Usuario", new { section = "citas" });
            }

            // 2. Validar horario laboral
            if (!Appointment.EsHorarioLaboral(NuevaFechaHora))
            {
                TempData["ErrorMessage"] = "Las citas solo pueden agendarse de lunes a sábado, entre 8:00 a.m. y 6:00 p.m.";
                return RedirectToAction("Usuario", new { section = "citas" });
            }

            // 3. Buscar primer optómetra disponible para la nueva fecha
            var optometras = await _context.Users.Where(u => u.Role == "optometra" && u.Activo).ToListAsync();
            User? optDisponible = null;
            if (cita.OptometraId.HasValue)
            {
                var currentOpt = optometras.FirstOrDefault(o => o.Id == cita.OptometraId.Value);
                if (currentOpt != null && await Appointment.HayDisponibilidad(_context, currentOpt.Id, NuevaFechaHora, cita.Id))
                {
                    optDisponible = currentOpt;
                }
            }

            if (optDisponible == null)
            {
                foreach (var opt in optometras)
                {
                    if (await Appointment.HayDisponibilidad(_context, opt.Id, NuevaFechaHora, cita.Id))
                    {
                        optDisponible = opt;
                        break;
                    }
                }
            }

            if (optDisponible == null)
            {
                TempData["ErrorMessage"] = "No hay optómetras disponibles en ese horario. Por favor elige otro horario.";
                return RedirectToAction("Usuario", new { section = "citas" });
            }

            cita.FechaHora = NuevaFechaHora;
            cita.OptometraId = optDisponible.Id;
            cita.VecesReprogramada += 1;
            cita.Estado = "pendiente";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "La cita ha sido reprogramada exitosamente.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al reprogramar la cita: {ex.Message}";
        }

        return RedirectToAction("Usuario", new { section = "citas" });
    }

    /// <summary>
    /// Update user profile â€” migrated from dashboard-usuario.html profile form
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(UserProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Datos no válidos.";
            return RedirectToAction("Usuario", new { section = "perfil" });
        }

        try
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _userService.UpdateProfileAsync(userId, model.Nombre, model.Telefono);

            TempData["SuccessMessage"] = "Perfil actualizado exitosamente.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al actualizar el perfil: {ex.Message}";
        }

        return RedirectToAction("Usuario", new { section = "perfil" });
    }

    /// <summary>
    /// Change password â€” migrated from dashboard-usuario.html config section
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            TempData["ErrorMessage"] = string.IsNullOrWhiteSpace(errors) ? "Datos no válidos." : errors;
            return RedirectToAction("Usuario", new { section = "configuracion" });
        }

        try
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var success = await _userService.ChangePasswordAsync(userId, model.CurrentPassword, model.NewPassword);

            if (!success)
            {
                TempData["ErrorMessage"] = "La contraseña actual es incorrecta.";
                return RedirectToAction("Usuario", new { section = "configuracion" });
            }

            TempData["SuccessMessage"] = "contraseña actualizada exitosamente.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al cambiar la contraseña: {ex.Message}";
        }

        return RedirectToAction("Usuario", new { section = "configuracion" });
    }

    // â”€â”€ Admin: Crear cita â”€â”€
    [HttpPost]
    [Authorize(Roles = "admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAppointment(int UserId, string Servicio, DateTime FechaHora, string? Notas, int? OptometraId)
    {
        if (FechaHora <= DateTime.UtcNow)
        {
            TempData["ErrorMessage"] = "La fecha y hora de la cita debe ser futura.";
            return RedirectToAction("Admin", new { section = "citas" });
        }

        // 1. Validar horario laboral
        if (!Appointment.EsHorarioLaboral(FechaHora))
        {
            TempData["ErrorMessage"] = "Las citas solo pueden agendarse de lunes a sábado, entre 8:00 a.m. y 6:00 p.m.";
            return RedirectToAction("Admin", new { section = "citas" });
        }

        // 2. Validar optómetra
        if (!OptometraId.HasValue)
        {
            TempData["ErrorMessage"] = "Debe seleccionar un optómetra.";
            return RedirectToAction("Admin", new { section = "citas" });
        }

        if (!await Appointment.HayDisponibilidad(_context, OptometraId.Value, FechaHora))
        {
            TempData["ErrorMessage"] = "El optómetra ya tiene una cita agendada en ese horario. Por favor elige otro horario.";
            return RedirectToAction("Admin", new { section = "citas" });
        }

        try
        {
            var appointment = new Appointment
            {
                UserId = UserId,
                OptometraId = OptometraId,
                Servicio = Servicio,
                FechaHora = FechaHora,
                Notas = Notas,
                Estado = "pendiente",
                FechaCreacion = DateTime.UtcNow
            };
            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cita creada exitosamente.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al crear la cita: {ex.Message}";
        }

        return RedirectToAction("Admin", new { section = "citas" });
    }

    // ── Admin: Actualizar estado de cita ──
    [HttpPost]
    [Authorize(Roles = "admin")]
    [ValidateAntiForgeryToken]
    [ActionName("ChangeAppointmentStatus")]
    public async Task<IActionResult> ChangeAppointmentStatus(int id, string? estado, string? nuevoEstado = null)
    {
        return await UpdateAppointmentStatus(id, estado, nuevoEstado);
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAppointmentStatus(int id, string? estado, string? nuevoEstado = null)
    {
        var targetEstado = !string.IsNullOrWhiteSpace(estado) ? estado : nuevoEstado;
        try
        {
            if (string.IsNullOrWhiteSpace(targetEstado) || !Appointment.EstadosValidos.Contains(targetEstado, StringComparer.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Estado no válido. Los valores permitidos son: pendiente, confirmada, completada, cancelada.";
                return RedirectToAction("Admin", new { section = "citas" });
            }

            var cita = await _context.Appointments.FindAsync(id);
            if (cita != null)
            {
                cita.Estado = targetEstado.ToLower();
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Estado de cita actualizado.";
            }
            else
            {
                TempData["ErrorMessage"] = "Cita no encontrada.";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al actualizar la cita: {ex.Message}";
        }

        return RedirectToAction("Admin", new { section = "citas" });
    }

    // ── Admin: Eliminar / Desactivar cita ──
    [HttpPost]
    [Authorize(Roles = "admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAppointment(int id)
    {
        try
        {
            var cita = await _context.Appointments.FindAsync(id);
            if (cita != null)
            {
                cita.Activo = false;
                _context.Appointments.Update(cita);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cita desactivada exitosamente.";

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Cita desactivada exitosamente." });
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Cita no encontrada.";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Cita no encontrada." });
                }
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al desactivar la cita: {ex.Message}";
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        return RedirectToAction("Admin", new { section = "citas" });
    }


    [HttpPost]
    [Authorize(Roles = "admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAppointment(int Id, int UserId, int? OptometraId, string Servicio, DateTime FechaHora, string? Notas)
    {
        var cita = await _context.Appointments.FindAsync(Id);
        if (cita == null)
        {
            TempData["ErrorMessage"] = "La cita no existe.";
            return RedirectToAction("Admin", new { section = "citas" });
        }

        if (OptometraId == null)
        {
            TempData["ErrorMessage"] = "Debe seleccionar un optómetra.";
            return RedirectToAction("Admin", new { section = "citas" });
        }

        if (!LentSoft.Web.Models.Entities.Appointment.EsHorarioLaboral(FechaHora))
        {
            TempData["ErrorMessage"] = "La hora seleccionada está fuera del horario de atención (8:00 AM a 6:00 PM) o no es un día hábil.";
            return RedirectToAction("Admin", new { section = "citas" });
        }

        if (!await LentSoft.Web.Models.Entities.Appointment.HayDisponibilidad(_context, OptometraId.Value, FechaHora, Id))
        {
            TempData["ErrorMessage"] = "El optómetra ya tiene una cita programada a esa hora (requiere 1 hora de margen).";
            return RedirectToAction("Admin", new { section = "citas" });
        }

        cita.UserId = UserId;
        cita.OptometraId = OptometraId;
        cita.Servicio = Servicio;
        cita.FechaHora = FechaHora;
        cita.Notas = Notas;

        _context.Appointments.Update(cita);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Cita actualizada correctamente.";
        return RedirectToAction("Admin", new { section = "citas" });
    }

    // ── GESTIÓN DE PEDIDOS DE VENTAS (INDEPENDIENTES) ──
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> CreateSalesOrder(SalesOrder model)
    {
        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json");

        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Verifique los campos ingresados para el pedido de venta.";
            if (isAjax) return Json(new { success = false, message = firstError });
            TempData["ErrorMessage"] = firstError;
            return RedirectToAction("Admin", new { section = "inventario", subtab = "pedidos", innerTab = "ventas" });
        }

        try
        {
            var order = await _inventoryService.ProcesarPedidoVentaAsync(model, model.ProductId, null, User.Identity?.Name);
            var successMsg = "Pedido de venta registrado correctamente.";
            if (isAjax) return Json(new { success = true, message = successMsg, data = order });
            TempData["SuccessMessage"] = successMsg;
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Producto no encontrado al crear pedido de venta #{NumeroPedido}", model.NumeroPedido);
            if (isAjax) return Json(new { success = false, message = ex.Message });
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Operación inválida al procesar pedido de venta #{NumeroPedido}", model.NumeroPedido);
            if (isAjax) return Json(new { success = false, message = ex.Message });
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al crear pedido de venta #{NumeroPedido}", model.NumeroPedido);
            var safeError = "Ocurrió un error inesperado al registrar el pedido de venta. Por favor intente más tarde.";
            if (isAjax) return Json(new { success = false, message = safeError });
            TempData["ErrorMessage"] = safeError;
        }

        return RedirectToAction("Admin", new { section = "inventario", subtab = "pedidos", innerTab = "ventas" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> EditSalesOrder(SalesOrder model)
    {
        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json");

        try
        {
            var existing = await _context.SalesOrders.FindAsync(model.Id);
            if (existing != null)
            {
                existing.NumeroPedido = model.NumeroPedido;
                existing.ClienteNombre = model.ClienteNombre;
                existing.ProductoNombre = model.ProductoNombre;
                existing.Cantidad = model.Cantidad;
                existing.PrecioUnitario = model.PrecioUnitario;
                existing.Total = model.Cantidad * model.PrecioUnitario;
                existing.Estado = model.Estado;
                existing.Notas = model.Notas;
                await _context.SaveChangesAsync();
                if (isAjax) return Json(new { success = true, message = "Pedido de venta actualizado correctamente.", data = existing });
                TempData["SuccessMessage"] = "Pedido de venta actualizado correctamente.";
            }
            else
            {
                if (isAjax) return Json(new { success = false, message = "No se encontró el pedido de venta especificado." });
                TempData["ErrorMessage"] = "No se encontró el pedido de venta especificado.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar pedido de venta #{Id}", model.Id);
            var safeMsg = "Ocurrió un error al actualizar el pedido de venta. Por favor intente más tarde.";
            if (isAjax) return Json(new { success = false, message = safeMsg });
            TempData["ErrorMessage"] = safeMsg;
        }

        if (User.IsInRole("ventas"))
        {
            return RedirectToAction("Index", "Ventas", new { section = "inventarios", subtab = "pedidos" });
        }
        return RedirectToAction("Admin", new { section = "inventario", subtab = "pedidos", innerTab = "ventas" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DeleteSalesOrder(int id)
    {
        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json");

        try
        {
            var existing = await _context.SalesOrders.FindAsync(id);
            if (existing != null)
            {
                existing.Activo = false;
                await _context.SaveChangesAsync();
                if (isAjax) return Json(new { success = true, message = "Pedido de venta eliminado correctamente.", id });
                TempData["SuccessMessage"] = "Pedido de venta eliminado correctamente.";
            }
            else
            {
                if (isAjax) return Json(new { success = false, message = "No se encontró el pedido de venta especificado." });
                TempData["ErrorMessage"] = "No se encontró el pedido de venta especificado.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar pedido de venta #{Id}", id);
            var safeMsg = "Ocurrió un error al eliminar el pedido de venta. Por favor intente más tarde.";
            if (isAjax) return Json(new { success = false, message = safeMsg });
            TempData["ErrorMessage"] = safeMsg;
        }

        if (User.IsInRole("ventas"))
        {
            return RedirectToAction("Index", "Ventas", new { section = "inventarios", subtab = "pedidos" });
        }
        return RedirectToAction("Admin", new { section = "inventario", subtab = "pedidos", innerTab = "ventas" });
    }

    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> ExportMovimientosData()
    {
        var movimientos = await _context.InventoryMovements
            .Include(m => m.Product)
            .OrderByDescending(m => m.Fecha)
            .Select(m => new
            {
                Id = "#MOV-" + m.Id,
                Producto = m.NombreProducto ?? (m.Product != null ? m.Product.Nombre : "Producto #" + m.ProductId),
                Tipo = m.Tipo,
                Cantidad = m.Cantidad,
                Fecha = m.Fecha.ToLocalTime().ToString("dd/MM/yyyy"),
                Hora = m.Fecha.ToLocalTime().ToString("HH:mm"),
                Responsable = m.Responsable ?? "Sistema / Admin"
            })
            .ToListAsync();

        return Json(movimientos);
    }

}
