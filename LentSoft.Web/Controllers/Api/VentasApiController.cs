using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Services;

namespace LentSoft.Web.Controllers.Api;

[ApiController]
[Route("api/ventas")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class VentasApiController : ControllerBase
{
    private readonly LentSoftDbContext _context;
    private readonly IPagoVentaService _pagoVentaService;

    public VentasApiController(LentSoftDbContext context, IPagoVentaService pagoVentaService)
    {
        _context = context;
        _pagoVentaService = pagoVentaService;
    }

    [HttpGet]
    public async Task<IActionResult> GetVentas([FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        var query = _context.Orders
            .Where(o => o.Activo)
            .Include(o => o.User)
            .Include(o => o.Pagos)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(o =>
                o.Id.ToString().Contains(term) ||
                (o.User != null && (o.User.Nombre.ToLower().Contains(term) || o.User.Apellido.ToLower().Contains(term))) ||
                o.Estado.ToLower().Contains(term) ||
                o.EstadoPago.ToLower().Contains(term));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(o => o.FechaPedido)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new
            {
                o.Id,
                NumeroPedido = $"#ORD-{o.Id:D4}",
                Cliente = o.User != null ? o.User.NombreCompleto : "Cliente",
                Documento = o.User != null ? o.User.NumeroDocumento : "",
                o.FechaPedido,
                o.Total,
                Abonado = o.Pagos.Sum(p => p.Monto),
                SaldoPendiente = Math.Max(0m, o.Total - o.Pagos.Sum(p => p.Monto)),
                o.EstadoPago,
                o.Estado
            })
            .ToListAsync();

        return Ok(new
        {
            page,
            pageSize,
            total,
            totalPages = (int)Math.Ceiling((double)total / pageSize),
            items
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetVentaDetalle(int id)
    {
        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .Include(o => o.Pagos)
            .Include(o => o.FormulaOptica)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id && o.Activo);

        if (order == null)
        {
            return NotFound(new { message = "Venta no encontrada." });
        }

        var totalAbonado = order.Pagos.Sum(p => p.Monto);
        var saldo = Math.Max(0m, order.Total - totalAbonado);

        return Ok(new
        {
            order.Id,
            NumeroPedido = $"#ORD-{order.Id:D4}",
            Cliente = new
            {
                order.User?.Id,
                Nombre = order.User?.NombreCompleto,
                Documento = order.User?.NumeroDocumento,
                Telefono = order.User?.Telefono,
                Email = order.User?.Email
            },
            order.FechaPedido,
            order.Total,
            TotalAbonado = totalAbonado,
            SaldoPendiente = saldo,
            order.EstadoPago,
            order.Estado,
            TieneFormulaOptica = order.FormulaOpticaId.HasValue,
            FormulaOpticaId = order.FormulaOpticaId,
            Items = order.OrderItems.Select(oi => new
            {
                oi.Id,
                oi.ProductId,
                Producto = oi.Product?.Nombre ?? "Producto",
                oi.Cantidad,
                oi.PrecioUnitario,
                oi.Subtotal
            }),
            Pagos = order.Pagos.OrderByDescending(p => p.FechaPago).Select(p => new
            {
                p.Id,
                p.NumeroComprobante,
                p.Monto,
                p.MetodoPago,
                p.FechaPago,
                p.Responsable
            })
        });
    }

    [HttpGet("{id:int}/pagos")]
    public async Task<IActionResult> GetPagosDeVenta(int id)
    {
        var pagos = await _pagoVentaService.ObtenerAbonosPorVentaAsync(id);
        return Ok(pagos.Select(p => new
        {
            p.Id,
            p.NumeroComprobante,
            p.Monto,
            p.MetodoPago,
            p.FechaPago,
            p.Responsable
        }));
    }
}
