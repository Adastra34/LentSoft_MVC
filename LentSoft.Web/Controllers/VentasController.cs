using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.ViewModels;

namespace LentSoft.Web.Controllers;

[Authorize(Roles = "ventas")]
public class VentasController : Controller
{
    private readonly LentSoftDbContext _context;

    public VentasController(LentSoftDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string section = "general")
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

        var facturas = await _context.Invoices
            .Include(i => i.Order).ThenInclude(o => o.User)
            .OrderByDescending(i => i.FechaEmision)
            .ToListAsync();

        var productos = await _context.Products
            .OrderBy(p => p.Nombre)
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
            Facturas = facturas,
            Productos = productos,
            UsuarioActual = usuario,
            ActiveSection = section
        };

        return View("~/Views/Dashboard/Ventas.cshtml", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOrderStatus(int id, string estado)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order != null)
        {
            order.Estado = estado;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Estado del pedido actualizado.";
        }
        return RedirectToAction("Index", new { section = "ventas" });
    }
}
