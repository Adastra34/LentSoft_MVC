using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Services;

namespace LentSoft.Web.Controllers;

[Authorize(Roles = "admin")]
public class InventoryMovementController : Controller
{
    private readonly IInventoryService _inventoryService;
    private readonly LentSoftDbContext _context;

    public InventoryMovementController(IInventoryService inventoryService, LentSoftDbContext context)
    {
        _inventoryService = inventoryService;
        _context = context;
    }

    /// <summary>
    /// Admin: Create new inventory movement and update product stock
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InventoryMovement movement)
    {
        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json");

        if (movement.ProductId <= 0 || movement.Cantidad <= 0)
        {
            var msg = "Debe seleccionar un producto y especificar una cantidad mayor a 0.";
            if (isAjax) return Json(new { success = false, message = msg });
            TempData["ErrorMessage"] = msg;
            return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "historial" });
        }

        var isSalida = string.Equals(movement.Tipo?.Trim(), InventoryConstants.MovementTypes.Salida, StringComparison.OrdinalIgnoreCase);

        if (isSalida && (string.IsNullOrWhiteSpace(movement.Motivo) || movement.Motivo.Trim() == "Otro"))
        {
            var msg = "El motivo es obligatorio para salidas de inventario (Venta, Merma, Daño, Devolución, Garantía, etc.).";
            if (isAjax) return Json(new { success = false, message = msg });
            TempData["ErrorMessage"] = msg;
            return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "historial" });
        }

        // Responsable asignado en servidor con el usuario autenticado
        var responsable = User.Identity?.Name ?? "Administrador";
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

        return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "historial" });
    }

    /// <summary>
    /// GET /InventoryMovement/KardexJson?productId={id}
    /// Returns full chronological Kardex movements and rolling stock balance for a product
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> KardexJson(int productId)
    {
        var product = await _context.Products.Include(p => p.Supplier).FirstOrDefaultAsync(p => p.Id == productId);
        if (product == null)
        {
            return Json(new { success = false, message = "Producto no encontrado." });
        }

        var movimientos = await _context.InventoryMovements
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
                Responsable = m.Responsable ?? "Admin",
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
    /// GET /InventoryMovement/ExportarExcelDatos
    /// Returns ALL movements matching current filters for client-side SheetJS full export
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExportarExcelDatos(string? search, string? tipo, int? productId, DateTime? fechaInicio, DateTime? fechaFin, string? sort)
    {
        var query = _context.InventoryMovements.Include(m => m.Product).AsQueryable();

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
            Responsable = m.Responsable ?? "Admin",
            Referencia = m.Referencia ?? "",
            Nota = m.Nota ?? ""
        }).ToListAsync();

        return Json(new { success = true, data });
    }
}
