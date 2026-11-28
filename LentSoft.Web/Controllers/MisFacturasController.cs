using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Models.ViewModels;
using LentSoft.Web.Services;

namespace LentSoft.Web.Controllers;

[Authorize]
public class MisFacturasController : Controller
{
    private const int MaxBackfillPerCall = 20;
    private const long SlowWarningThresholdMs = 2000;

    private readonly LentSoftDbContext _context;
    private readonly IInvoiceService _invoiceService;
    private readonly IPdfInvoiceService _pdfInvoiceService;
    private readonly ILogger<MisFacturasController> _logger;
    private readonly IMemoryCache? _memoryCache;

    public MisFacturasController(
        LentSoftDbContext context,
        IInvoiceService invoiceService,
        IPdfInvoiceService pdfInvoiceService,
        ILogger<MisFacturasController> logger,
        IMemoryCache? memoryCache = null)
    {
        _context = context;
        _invoiceService = invoiceService;
        _pdfInvoiceService = pdfInvoiceService;
        _logger = logger;
        _memoryCache = memoryCache;
    }

    private int GetCurrentUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdStr, out var userId) ? userId : 0;
    }

    [HttpGet]
    public async Task<IActionResult> Lista(int page = 1, int pageSize = 5)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0)
        {
            return Unauthorized();
        }

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 5;
        if (pageSize > 50) pageSize = 50;

        // C) Backfill idempotente de pedidos pagados del usuario que aún no tienen factura activa (máx 20 por llamada)
        var orderIdsSinFactura = await _context.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId
                        && o.Activo
                        && o.Estado != "cancelado"
                        && o.EstadoPago == "pagado"
                        && !_context.Invoices.Any(i => i.OrderId == o.Id && i.Activo && i.Estado != "cancelada"))
            .OrderByDescending(o => o.FechaPedido)
            .ThenByDescending(o => o.Id)
            .Take(MaxBackfillPerCall)
            .Select(o => o.Id)
            .ToListAsync();

        foreach (var orderId in orderIdsSinFactura)
        {
            try
            {
                await _invoiceService.CreateAsync(new Invoice { OrderId = orderId });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("ya cuenta con una factura activa", StringComparison.OrdinalIgnoreCase))
            {
                // Ignorar en condiciones de carrera o doble clic para mantener idempotencia
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al auto-facturar pedido histórico OrderId={OrderId} del usuario {UserId}", orderId, userId);
            }
        }

        // B) Consulta paginada ligera con AsNoTracking y proyección DTO (sin Include pesados ni N+1)
        var query = _context.Invoices
            .AsNoTracking()
            .Where(i => i.Activo && i.Estado != "cancelada" && i.Order != null && i.Order.UserId == userId);

        var totalCount = await query.CountAsync();
        var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling((double)totalCount / pageSize);
        if (page > totalPages) page = totalPages;

        var facturas = await query
            .OrderByDescending(i => i.FechaEmision)
            .ThenByDescending(i => i.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new FacturaResumenDto
            {
                Id = i.Id,
                NumeroFactura = i.NumeroFactura,
                FechaEmision = i.FechaEmision,
                FechaPago = i.FechaPago,
                Total = i.Total,
                Estado = i.Estado,
                OrderId = i.OrderId
            })
            .ToListAsync();

        var viewModel = new MisFacturasListaViewModel
        {
            Facturas = facturas,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return PartialView("_Lista", viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Detalle(int id)
    {
        var sw = Stopwatch.StartNew();
        var userId = GetCurrentUserId();
        if (userId <= 0)
        {
            RegistrarServerTiming("Detalle", id, sw);
            return Unauthorized();
        }

        var data = await _context.Invoices
            .AsNoTracking()
            .Where(i => i.Id == id && i.Activo && i.Order != null && i.Order.UserId == userId)
            .Select(i => new
            {
                i.Id,
                i.NumeroFactura,
                i.FechaEmision,
                i.FechaPago,
                i.Estado,
                i.MetodoPago,
                i.OrderId,
                i.Subtotal,
                i.Impuestos,
                i.Total,
                i.CUFE,
                i.ResolucionDianNumero,
                i.RangoAutorizadoDesde,
                i.RangoAutorizadoHasta,
                ClienteNombre = i.Order!.User != null ? (i.Order.User.Nombre + " " + i.Order.User.Apellido).Trim() : "Cliente",
                TipoDocumento = i.Order.User != null ? i.Order.User.TipoDocumento : "CC",
                NumeroDocumento = i.Order.User != null ? i.Order.User.NumeroDocumento : string.Empty,
                ClienteEmail = i.Order.User != null ? i.Order.User.Email : null,
                ClienteTelefono = i.Order.User != null ? i.Order.User.Telefono : null,
                DireccionEnvio = i.Order.DireccionEnvio,
                Items = i.Order.OrderItems.Select(oi => new
                {
                    ProductoNombre = oi.Product != null ? oi.Product.Nombre : ("Producto #" + oi.ProductId),
                    oi.Cantidad,
                    oi.PrecioUnitario,
                    PorcentajeIva = oi.Product != null && oi.Product.PorcentajeIva >= 0 ? oi.Product.PorcentajeIva : 19.00m
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (data == null)
        {
            RegistrarServerTiming("Detalle", id, sw);
            return NotFound();
        }

        var viewModel = new FacturaDetalleViewModel
        {
            Id = data.Id,
            NumeroFactura = data.NumeroFactura,
            FechaEmision = data.FechaEmision,
            FechaPago = data.FechaPago,
            Estado = data.Estado,
            MetodoPago = data.MetodoPago,
            OrderId = data.OrderId,
            ClienteNombre = data.ClienteNombre,
            ClienteDocumento = $"{data.TipoDocumento} {data.NumeroDocumento}".Trim(),
            ClienteEmail = data.ClienteEmail,
            ClienteTelefono = data.ClienteTelefono,
            DireccionEnvio = data.DireccionEnvio,
            Subtotal = data.Subtotal,
            Impuestos = data.Impuestos,
            Total = data.Total,
            CUFE = data.CUFE,
            ResolucionDianNumero = data.ResolucionDianNumero,
            RangoAutorizadoDesde = data.RangoAutorizadoDesde,
            RangoAutorizadoHasta = data.RangoAutorizadoHasta,
            Items = data.Items.Select(item =>
            {
                var subtotalItem = item.PrecioUnitario * item.Cantidad;
                var baseUnit = item.PorcentajeIva > 0
                    ? item.PrecioUnitario / (1m + (item.PorcentajeIva / 100m))
                    : item.PrecioUnitario;
                var baseTotal = baseUnit * item.Cantidad;
                var valorIva = subtotalItem - baseTotal;

                return new FacturaDetalleItemViewModel
                {
                    ProductoNombre = item.ProductoNombre,
                    Cantidad = item.Cantidad,
                    PrecioUnitarioBase = Math.Round(baseUnit, 2),
                    PorcentajeIva = item.PorcentajeIva,
                    ValorIva = Math.Round(valorIva, 2),
                    Subtotal = Math.Round(subtotalItem, 2)
                };
            }).ToList()
        };

        RegistrarServerTiming("Detalle", id, sw);
        return PartialView("_Detalle", viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Descargar(int id)
    {
        var sw = Stopwatch.StartNew();
        var userId = GetCurrentUserId();
        if (userId <= 0)
        {
            RegistrarServerTiming("Descargar", id, sw);
            return Unauthorized();
        }

        var invoice = await _context.Invoices
            .AsNoTracking()
            .Include(i => i.Order)
                .ThenInclude(o => o!.User)
            .Include(i => i.Order)
                .ThenInclude(o => o!.OrderItems)
                    .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(i => i.Id == id && i.Activo && i.Order != null && i.Order.UserId == userId);

        if (invoice == null)
        {
            RegistrarServerTiming("Descargar", id, sw);
            return NotFound();
        }

        var cacheKey = $"MisFacturas:Pdf:{invoice.Id}:{invoice.Estado}:{invoice.FechaPago?.Ticks ?? 0}";
        byte[]? pdfBytes = null;

        if (_memoryCache == null || !_memoryCache.TryGetValue(cacheKey, out pdfBytes) || pdfBytes == null)
        {
            pdfBytes = _pdfInvoiceService.GenerateInvoicePdf(invoice);
            _memoryCache?.Set(cacheKey, pdfBytes, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(15)
            });
        }

        RegistrarServerTiming("Descargar", id, sw);
        var filename = $"Factura-{invoice.NumeroFactura}.pdf";
        return File(pdfBytes, "application/pdf", filename);
    }

    private void RegistrarServerTiming(string accion, int invoiceId, Stopwatch sw)
    {
        sw.Stop();
        var elapsedMs = sw.ElapsedMilliseconds;
        Response.Headers["Server-Timing"] = $"app;dur={elapsedMs}";

        if (elapsedMs > SlowWarningThresholdMs)
        {
            _logger.LogWarning(
                "Rendimiento lento en MisFacturasController.{Accion} para InvoiceId={InvoiceId}: {ElapsedMs} ms",
                accion, invoiceId, elapsedMs);
        }
    }
}
