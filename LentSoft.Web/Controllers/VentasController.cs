using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.ViewModels;
using LentSoft.Web.Services;

namespace LentSoft.Web.Controllers;

[Authorize(Roles = "ventas")]
public class VentasController : Controller
{
    private readonly LentSoftDbContext _context;
    private readonly IInvoiceService _invoiceService;

    public VentasController(LentSoftDbContext context, IInvoiceService invoiceService)
    {
        _context = context;
        _invoiceService = invoiceService;
    }

    public async Task<IActionResult> Index(string section = "general", string? searchTerm = null, int page = 1, int pageSize = 5)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var usuario = await _context.Users.FindAsync(userId);

        var now = DateTime.UtcNow;
        var inicioMes = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var ventas = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .OrderByDescending(o => o.FechaPedido)
            .ToListAsync();

        var (facturasList, facturasTotalCount) = await _invoiceService.GetAllAsync(searchTerm, page, pageSize);
        var pedidosDisponibles = await _invoiceService.GetOrdersAvailableForInvoicingAsync();

        var productos = await _context.Products
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

        var viewModel = new DashboardVentasViewModel
        {
            VentasDelMes = ventasDelMes,
            PedidosActivos = pedidosActivos,
            ClientesAtendidos = clientesAtendidos,
            TicketPromedio = ticketPromedio,
            Ventas = ventas,
            Facturas = facturasList,
            FacturasSearchTerm = searchTerm,
            FacturasPage = page,
            FacturasPageSize = pageSize,
            FacturasTotalCount = facturasTotalCount,
            PedidosDisponibles = pedidosDisponibles,
            Productos = productos,
            Clientes = clientes,
            UsuarioActual = usuario,
            ActiveSection = section
        };

        return View("~/Views/Dashboard/Ventas.cshtml", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOrderStatus(int id, string estado)
    {
        try
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                order.Estado = estado;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Estado del pedido actualizado.";
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
}
