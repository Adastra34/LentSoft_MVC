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
    private readonly LentSoftDbContext _context;

    public DashboardController(
        IDashboardService dashboardService,
        IProductService productService,
        IUserService userService,
        IOrderService orderService,
        IFavoriteService favoriteService,
        IInvoiceService invoiceService,
        LentSoftDbContext context)
    {
        _dashboardService = dashboardService;
        _productService = productService;
        _userService = userService;
        _orderService = orderService;
        _favoriteService = favoriteService;
        _invoiceService = invoiceService;
        _context = context;
    }

    /// <summary>
    /// Admin dashboard — reconstruido con sidebar y 6 secciones
    /// </summary>
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Admin(
        string section = "general",
        string subtab = "productos",
        string? searchTerm = null,
        int page = 1,
        int pageSize = 5,
        string? clientesSearch = null,
        int clientesPage = 1,
        int clientesPageSize = 5,
        string? trabajadoresSearch = null,
        int trabajadoresPage = 1,
        int trabajadoresPageSize = 5)
    {
        var now = DateTime.UtcNow;
        var inicioMes = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var inicioMesAnterior = inicioMes.AddMonths(-1);

        // ── Stats del mes actual ──
        var ventasMes = await _context.Orders
            .Where(o => o.Estado != "cancelado" && o.FechaPedido >= inicioMes)
            .SumAsync(o => (decimal?)o.Total) ?? 0;

        var ventasMesAnterior = await _context.Orders
            .Where(o => o.Estado != "cancelado" && o.FechaPedido >= inicioMesAnterior && o.FechaPedido < inicioMes)
            .SumAsync(o => (decimal?)o.Total) ?? 0;

        var pedidosActivos = await _context.Orders
            .CountAsync(o => o.Estado == "pendiente" || o.Estado == "procesando" || o.Estado == "enviado");

        var pedidosActivosAnterior = Math.Max(1, pedidosActivos - 1); // mock anterior

        var clientesTotales = await _context.Users.CountAsync(u => u.Role == "usuario");
        var clientesAnterior = Math.Max(1, clientesTotales - 1); // mock

        var productosEnStock = await _context.Products.CountAsync(p => p.Activo && p.Stock > 0);
        var productosAnterior = Math.Max(1, productosEnStock); // mock estable

        // ── Datos para las secciones ──
        var pedidosRecientes = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .OrderByDescending(o => o.FechaPedido)
            .Take(10)
            .ToListAsync();

        var productos = await _context.Products.OrderBy(p => p.Nombre).ToListAsync();

        // ── Clientes (Paginados y Filtrados) ──
        var clientesQuery = _context.Users.Where(u => u.Role == "usuario");
        if (!string.IsNullOrWhiteSpace(clientesSearch))
        {
            var term = clientesSearch.Trim().ToLower();
            clientesQuery = clientesQuery.Where(u => u.Nombre.ToLower().Contains(term) || u.Apellido.ToLower().Contains(term) || u.Email.ToLower().Contains(term));
        }
        else if (section == "usuarios" && subtab == "clientes" && !string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            clientesQuery = clientesQuery.Where(u => u.Nombre.ToLower().Contains(term) || u.Apellido.ToLower().Contains(term) || u.Email.ToLower().Contains(term));
            clientesSearch = searchTerm;
        }

        var clientesTotalCount = await clientesQuery.CountAsync();
        var clientesList = await clientesQuery
            .OrderBy(u => u.Nombre)
            .Skip((clientesPage - 1) * clientesPageSize)
            .Take(clientesPageSize)
            .ToListAsync();

        // ── Trabajadores (Paginados y Filtrados) ──
        var trabajadoresQuery = _context.Employees.AsQueryable();
        if (!string.IsNullOrWhiteSpace(trabajadoresSearch))
        {
            var term = trabajadoresSearch.Trim().ToLower();
            trabajadoresQuery = trabajadoresQuery.Where(e => e.Nombre.ToLower().Contains(term) || e.Email.ToLower().Contains(term) || e.Puesto.ToLower().Contains(term));
        }
        else if (section == "usuarios" && subtab == "trabajadores" && !string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            trabajadoresQuery = trabajadoresQuery.Where(e => e.Nombre.ToLower().Contains(term) || e.Email.ToLower().Contains(term) || e.Puesto.ToLower().Contains(term));
            trabajadoresSearch = searchTerm;
        }

        var trabajadoresTotalCount = await trabajadoresQuery.CountAsync();
        var rawTrabajadores = await trabajadoresQuery
            .OrderBy(e => e.Nombre)
            .Skip((trabajadoresPage - 1) * trabajadoresPageSize)
            .Take(trabajadoresPageSize)
            .ToListAsync();

        var trabajadoresList = new List<TrabajadorItemViewModel>();
        foreach (var emp in rawTrabajadores)
        {
            int pedidosCount = 0;
            var userAcc = await _context.Users.FirstOrDefaultAsync(u => u.Email == emp.Email);
            if (userAcc != null)
            {
                pedidosCount = await _context.Orders.CountAsync(o => o.UserId == userAcc.Id);
            }

            trabajadoresList.Add(new TrabajadorItemViewModel
            {
                Id = emp.Id,
                Nombre = emp.Nombre,
                Email = emp.Email,
                Telefono = emp.Telefono,
                Puesto = emp.Puesto,
                Departamento = emp.Departamento,
                Salario = emp.Salario,
                Rol = string.IsNullOrWhiteSpace(emp.Rol) ? "Trabajador" : emp.Rol,
                Activo = emp.Activo,
                PedidosCount = pedidosCount
            });
        }

        var ventas = await _context.Orders
            .Include(o => o.User)
            .OrderByDescending(o => o.FechaPedido)
            .ToListAsync();

        var citas = await _context.Appointments
            .Include(a => a.User)
            .OrderByDescending(a => a.FechaHora)
            .ToListAsync();

        // Obtener facturas usando IInvoiceService (paginadas y filtradas)
        var (facturasList, facturasTotalCount) = await _invoiceService.GetAllAsync(searchTerm, page, pageSize);
        var pedidosDisponibles = await _invoiceService.GetOrdersAvailableForInvoicingAsync();

        var viewModel = new DashboardAdminViewModel
        {
            // Stats
            VentasDelMes = ventasMes,
            VentasDelMesAnterior = ventasMesAnterior,
            PedidosActivos = pedidosActivos,
            PedidosActivosAnterior = pedidosActivosAnterior,
            ClientesTotales = clientesTotales,
            ClientesTotalesAnterior = clientesAnterior,
            ProductosEnStock = productosEnStock,
            ProductosEnStockAnterior = productosAnterior,

            // Secciones
            PedidosRecientes = pedidosRecientes,
            Productos = productos,
            Ventas = ventas,
            Citas = citas,

            // Clientes
            Clientes = clientesList,
            ClientesSearchTerm = clientesSearch,
            ClientesPage = clientesPage,
            ClientesPageSize = clientesPageSize,
            ClientesTotalCount = clientesTotalCount,

            // Trabajadores
            Trabajadores = trabajadoresList,
            TrabajadoresSearchTerm = trabajadoresSearch,
            TrabajadoresPage = trabajadoresPage,
            TrabajadoresPageSize = trabajadoresPageSize,
            TrabajadoresTotalCount = trabajadoresTotalCount,

            // Facturas
            Facturas = facturasList,
            FacturasSearchTerm = searchTerm,
            FacturasPage = page,
            FacturasPageSize = pageSize,
            FacturasTotalCount = facturasTotalCount,
            PedidosDisponibles = pedidosDisponibles,

            // Mock proveedores
            Proveedores = GetMockProveedores(),
            HistorialMovimientos = GetMockMovimientos(),

            // Navigation
            ActiveSection = section,
            ActiveSubTab = subtab
        };

        return View(viewModel);
    }

    /// <summary>
    /// User dashboard — migrated from Views/dashboard-usuario.html
    /// </summary>
    public async Task<IActionResult> Usuario(string section = "perfil")
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _userService.GetByIdAsync(userId);

        if (user == null)
            return RedirectToAction("Login", "Auth");

        var pedidos = await _orderService.GetByUserIdAsync(userId);
        var favoritos = await _favoriteService.GetFavoritesByUserIdAsync(userId);

        var viewModel = new DashboardUsuarioViewModel
        {
            Usuario = user,
            Pedidos = pedidos,
            Favoritos = favoritos,
            ActiveSection = section
        };

        // Load appointments through the user navigation property
        viewModel.Citas = await _context.Appointments
            .Where(a => a.UserId == userId)
            .ToListAsync();

        return View(viewModel);
    }

    /// <summary>
    /// Update user profile — migrated from dashboard-usuario.html profile form
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

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _userService.UpdateProfileAsync(userId, model.Nombre, model.Telefono);

        TempData["SuccessMessage"] = "Perfil actualizado exitosamente.";
        return RedirectToAction("Usuario", new { section = "perfil" });
    }

    /// <summary>
    /// Change password — migrated from dashboard-usuario.html config section
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

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var success = await _userService.ChangePasswordAsync(userId, model.CurrentPassword, model.NewPassword);

        if (!success)
        {
            TempData["ErrorMessage"] = "La contraseña actual es incorrecta.";
            return RedirectToAction("Usuario", new { section = "configuracion" });
        }

        TempData["SuccessMessage"] = "Contraseña actualizada exitosamente.";
        return RedirectToAction("Usuario", new { section = "configuracion" });
    }

    // ── Admin: Crear cita ──
    [HttpPost]
    [Authorize(Roles = "admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAppointment(int UserId, string Servicio, DateTime FechaHora, string? Notas)
    {
        var appointment = new Appointment
        {
            UserId = UserId,
            Servicio = Servicio,
            FechaHora = FechaHora,
            Notas = Notas,
            Estado = "pendiente",
            FechaCreacion = DateTime.UtcNow
        };
        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Cita creada exitosamente.";
        return RedirectToAction("Admin", new { section = "citas" });
    }

    // ── Admin: Actualizar estado de cita ──
    [HttpPost]
    [Authorize(Roles = "admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAppointmentStatus(int id, string estado)
    {
        var cita = await _context.Appointments.FindAsync(id);
        if (cita != null)
        {
            cita.Estado = estado;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Estado de cita actualizado.";
        }
        return RedirectToAction("Admin", new { section = "citas" });
    }

    // ── Admin: Eliminar cita ──
    [HttpPost]
    [Authorize(Roles = "admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAppointment(int id)
    {
        var cita = await _context.Appointments.FindAsync(id);
        if (cita != null)
        {
            _context.Appointments.Remove(cita);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cita eliminada exitosamente.";
        }
        return RedirectToAction("Admin", new { section = "citas" });
    }

    // ── Mock data ──
    private static List<ProveedorMock> GetMockProveedores() => new()
    {
        new() { Id = 1, Nombre = "Óptica Global S.A.", Contacto = "Carlos Ruiz", Telefono = "555-1001", Email = "ventas@opticaglobal.com", Categoria = "Monturas", Estado = "activo" },
        new() { Id = 2, Nombre = "LensTech Colombia", Contacto = "Ana López", Telefono = "555-1002", Email = "contacto@lenstech.co", Categoria = "Lentes de contacto", Estado = "activo" },
        new() { Id = 3, Nombre = "Distribuidora Visual", Contacto = "Pedro Gómez", Telefono = "555-1003", Email = "info@distvisual.com", Categoria = "Accesorios", Estado = "activo" },
        new() { Id = 4, Nombre = "Ray-Ban Distribuidor", Contacto = "María Fernández", Telefono = "555-1004", Email = "dist@rayban.co", Categoria = "Lentes de sol", Estado = "activo" },
        new() { Id = 5, Nombre = "Oakley Partner", Contacto = "José Martínez", Telefono = "555-1005", Email = "partner@oakley.co", Categoria = "Monturas deportivas", Estado = "inactivo" }
    };

    private static List<MovimientoInventarioMock> GetMockMovimientos() => new()
    {
        new() { Id = 1, Producto = "Lentes Ray-Ban Aviator", Tipo = "entrada", Cantidad = 20, Fecha = DateTime.UtcNow.AddDays(-2), Responsable = "Ana Martínez" },
        new() { Id = 2, Producto = "Lentes de Contacto Acuvue", Tipo = "salida", Cantidad = 5, Fecha = DateTime.UtcNow.AddDays(-1), Responsable = "Juan Pérez" },
        new() { Id = 3, Producto = "Montura Oakley Sport", Tipo = "entrada", Cantidad = 10, Fecha = DateTime.UtcNow.AddDays(-3), Responsable = "Ana Martínez" },
        new() { Id = 4, Producto = "Estuche Premium", Tipo = "salida", Cantidad = 15, Fecha = DateTime.UtcNow.AddDays(-1), Responsable = "Juan Pérez" },
        new() { Id = 5, Producto = "Líquido Limpiador", Tipo = "entrada", Cantidad = 50, Fecha = DateTime.UtcNow.AddDays(-5), Responsable = "Ana Martínez" }
    };
}
