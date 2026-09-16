using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;

namespace LentSoft.Web.Controllers.Api;

[ApiController]
[Route("api/invoices")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class InvoicesApiController : ControllerBase
{
    private readonly LentSoftDbContext _context;

    public InvoicesApiController(LentSoftDbContext context)
    {
        _context = context;
    }

    private int GetCurrentUserId()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idStr, out var id) ? id : 0;
    }

    [HttpGet]
    public async Task<IActionResult> GetInvoices()
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var invoices = await _context.Invoices
            .Where(i => i.Activo && i.Order != null && i.Order.UserId == userId)
            .OrderByDescending(i => i.FechaEmision)
            .Select(i => new
            {
                i.Id,
                i.NumeroFactura,
                i.OrderId,
                i.Subtotal,
                i.Impuestos,
                i.Total,
                Estado = char.ToUpper(i.Estado[0]) + i.Estado.Substring(1).ToLower(),
                EstadoRaw = i.Estado.ToLower(),
                i.FechaEmision,
                i.FechaPago,
                i.MetodoPago
            })
            .ToListAsync();

        return Ok(invoices);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetInvoiceById(int id)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var invoice = await _context.Invoices
            .Include(i => i.Order)
                .ThenInclude(o => o!.OrderItems)
                    .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(i => i.Id == id && i.Activo);

        if (invoice == null || (invoice.Order?.UserId != userId && !User.IsInRole("admin")))
            return NotFound(new { message = "Factura no encontrada." });

        return Ok(new
        {
            invoice.Id,
            invoice.NumeroFactura,
            invoice.OrderId,
            invoice.Subtotal,
            invoice.Impuestos,
            invoice.Total,
            Estado = char.ToUpper(invoice.Estado[0]) + invoice.Estado.Substring(1).ToLower(),
            EstadoRaw = invoice.Estado.ToLower(),
            invoice.FechaEmision,
            invoice.FechaPago,
            invoice.MetodoPago,
            Items = invoice.Order?.OrderItems.Select(oi => new
            {
                oi.Id,
                oi.ProductId,
                ProductoNombre = oi.Product?.Nombre ?? "Producto",
                oi.Cantidad,
                oi.PrecioUnitario,
                oi.Subtotal
            })
        });
    }
}
