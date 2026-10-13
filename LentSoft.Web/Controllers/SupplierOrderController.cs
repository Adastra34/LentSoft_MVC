using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Services;

namespace LentSoft.Web.Controllers;

[Authorize(Roles = "admin")]
public class SupplierOrderController : Controller
{
    private readonly LentSoftDbContext _context;
    private readonly IInventoryService _inventoryService;
    private readonly IPdfPurchaseOrderService _pdfService;

    public SupplierOrderController(
        LentSoftDbContext context, 
        IInventoryService inventoryService,
        IPdfPurchaseOrderService pdfService)
    {
        _context = context;
        _inventoryService = inventoryService;
        _pdfService = pdfService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SupplierOrder model)
    {
        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json");

        if (ModelState.IsValid)
        {
            var targetProd = await _context.Products.FindAsync(model.ProductId);
            if (targetProd == null)
            {
                var error = "El producto de inventario seleccionado no existe.";
                if (isAjax) return Json(new { success = false, message = error });
                TempData["ErrorMessage"] = error;
                return RedirectToDashboardTab();
            }

            model.Total = model.Cantidad * model.PrecioUnitario;
            if (model.Fecha == default) model.Fecha = DateTime.UtcNow;
            model.Activo = true;

            bool isReceived = string.Equals(model.Estado, InventoryConstants.SupplierOrderStates.Recibido, StringComparison.OrdinalIgnoreCase);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (isReceived)
                {
                    await _inventoryService.AumentarStockAsync(
                        model.ProductId, 
                        model.Cantidad, 
                        InventoryConstants.MovementTypes.Entrada, 
                        User.Identity?.Name);
                }

                _context.SupplierOrders.Add(model);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                if (isAjax) return Json(new { success = true, message = "Pedido a proveedor registrado correctamente.", data = model });
                TempData["SuccessMessage"] = "Pedido a proveedor registrado correctamente.";
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                var error = "El producto fue modificado por otro usuario, vuelve a intentarlo.";
                if (isAjax) return Json(new { success = false, message = error });
                TempData["ErrorMessage"] = error;
            }
            catch (InvalidOperationException ex)
            {
                await transaction.RollbackAsync();
                if (isAjax) return Json(new { success = false, message = ex.Message });
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var error = $"Error al registrar el pedido a proveedor: {ex.Message}";
                if (isAjax) return Json(new { success = false, message = error });
                TempData["ErrorMessage"] = error;
            }
        }
        else
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Verifique los datos del pedido a proveedor.";
            if (isAjax) return Json(new { success = false, message = firstError });
            TempData["ErrorMessage"] = firstError;
        }

        return RedirectToDashboardTab();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(SupplierOrder model)
    {
        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json");

        var existing = await _context.SupplierOrders.FindAsync(model.Id);
        if (existing == null)
        {
            if (isAjax) return Json(new { success = false, message = "No se encontró el pedido a proveedor especificado." });
            TempData["ErrorMessage"] = "No se encontró el pedido a proveedor especificado.";
            return RedirectToDashboardTab();
        }

        bool wasReceived = string.Equals(existing.Estado, InventoryConstants.SupplierOrderStates.Recibido, StringComparison.OrdinalIgnoreCase);
        bool isNowReceived = string.Equals(model.Estado, InventoryConstants.SupplierOrderStates.Recibido, StringComparison.OrdinalIgnoreCase);

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Si pasa a recibido y no estaba recibido previamente, sumamos stock y creamos movimiento
            if (!wasReceived && isNowReceived)
            {
                await _inventoryService.AumentarStockAsync(
                    model.ProductId, 
                    model.Cantidad, 
                    InventoryConstants.MovementTypes.Entrada, 
                    User.Identity?.Name);
            }

            existing.NumeroPedido = model.NumeroPedido;
            existing.SupplierId = model.SupplierId;
            existing.ProductId = model.ProductId;
            existing.Cantidad = model.Cantidad;
            existing.PrecioUnitario = model.PrecioUnitario;
            existing.Total = model.Cantidad * model.PrecioUnitario;
            existing.Estado = model.Estado;
            existing.Notas = model.Notas;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            if (isAjax) return Json(new { success = true, message = "Pedido a proveedor actualizado correctamente.", data = existing });
            TempData["SuccessMessage"] = "Pedido a proveedor actualizado correctamente.";
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            var error = "El producto fue modificado por otro usuario, vuelve a intentarlo.";
            if (isAjax) return Json(new { success = false, message = error });
            TempData["ErrorMessage"] = error;
        }
        catch (InvalidOperationException ex)
        {
            await transaction.RollbackAsync();
            if (isAjax) return Json(new { success = false, message = ex.Message });
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            var error = $"Error al actualizar el pedido a proveedor: {ex.Message}";
            if (isAjax) return Json(new { success = false, message = error });
            TempData["ErrorMessage"] = error;
        }

        return RedirectToDashboardTab();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json");

        var existing = await _context.SupplierOrders.FindAsync(id);
        if (existing != null)
        {
            existing.Activo = false;
            await _context.SaveChangesAsync();
            if (isAjax) return Json(new { success = true, message = "Pedido a proveedor eliminado correctamente.", id });
            TempData["SuccessMessage"] = "Pedido a proveedor eliminado correctamente.";
        }
        else
        {
            if (isAjax) return Json(new { success = false, message = "No se encontró el pedido a proveedor especificado." });
            TempData["ErrorMessage"] = "No se encontró el pedido a proveedor especificado.";
        }

        return RedirectToDashboardTab();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Recibir(int id)
    {
        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json");

        try
        {
            var updatedOrder = await _inventoryService.ProcesarRecepcionPedidoProveedorAsync(id, User.Identity?.Name);
            var msg = $"Pedido {updatedOrder.NumeroPedido} recibido satisfactoriamente. Stock sumado al inventario.";
            if (isAjax) return Json(new { success = true, message = msg, data = updatedOrder });
            TempData["SuccessMessage"] = msg;
        }
        catch (InvalidOperationException ex)
        {
            if (isAjax) return Json(new { success = false, message = ex.Message });
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception ex)
        {
            var msg = $"Error al recibir el pedido: {ex.Message}";
            if (isAjax) return Json(new { success = false, message = msg });
            TempData["ErrorMessage"] = msg;
        }

        return RedirectToDashboardTab();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecibirParcial(int id, int cantidadRecibida)
    {
        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json");

        if (cantidadRecibida <= 0)
        {
            var msg = "La cantidad a recibir debe ser mayor a 0.";
            if (isAjax) return Json(new { success = false, message = msg });
            TempData["ErrorMessage"] = msg;
            return RedirectToDashboardTab();
        }

        try
        {
            var updatedOrder = await _inventoryService.ProcesarRecepcionParcialPedidoProveedorAsync(id, cantidadRecibida, User.Identity?.Name);
            var msg = $"Recepción de {cantidadRecibida} unidad(es) registrada en el pedido {updatedOrder.NumeroPedido}.";
            if (isAjax) return Json(new { success = true, message = msg, data = updatedOrder });
            TempData["SuccessMessage"] = msg;
        }
        catch (InvalidOperationException ex)
        {
            if (isAjax) return Json(new { success = false, message = ex.Message });
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception ex)
        {
            var msg = $"Error al registrar recepción parcial: {ex.Message}";
            if (isAjax) return Json(new { success = false, message = msg });
            TempData["ErrorMessage"] = msg;
        }

        return RedirectToDashboardTab();
    }

    [HttpGet]
    public async Task<IActionResult> DescargarPdf(int id)
    {
        var order = await _context.SupplierOrders
            .Include(o => o.Supplier)
            .Include(o => o.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound("No se encontró el pedido a proveedor.");
        }

        var pdfBytes = _pdfService.GeneratePurchaseOrderPdf(order);
        var filename = $"OrdenCompra_{order.NumeroPedido}.pdf";
        return File(pdfBytes, "application/pdf", filename);
    }

    [HttpGet]
    public async Task<IActionResult> DetalleJson(int id)
    {
        var order = await _context.SupplierOrders
            .Include(o => o.Supplier)
            .Include(o => o.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return Json(new { success = false, message = "Pedido no encontrado." });
        }

        var pendiente = Math.Max(0, order.Cantidad - order.CantidadRecibida);

        return Json(new
        {
            success = true,
            data = new
            {
                order.Id,
                order.NumeroPedido,
                SupplierId = order.SupplierId,
                SupplierNombre = order.Supplier?.Nombre ?? "N/A",
                SupplierContacto = order.Supplier?.Contacto ?? "N/A",
                SupplierTelefono = order.Supplier?.Telefono ?? "N/A",
                SupplierDireccion = order.Supplier?.Direccion ?? "N/A",
                SupplierTiempoEntrega = order.Supplier?.TiempoEntregaDias,
                order.ProductId,
                ProductNombre = order.Product?.Nombre ?? "N/A",
                ProductSku = order.Product?.Sku ?? $"PRD-{order.ProductId:D4}",
                order.Cantidad,
                order.CantidadRecibida,
                Pendiente = pendiente,
                order.PrecioUnitario,
                order.Total,
                order.Estado,
                Fecha = order.Fecha.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                order.Notas
            }
        });
    }

    private IActionResult RedirectToDashboardTab()
    {
        return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "pedidos", innerTab = "proveedores" });
    }
}
