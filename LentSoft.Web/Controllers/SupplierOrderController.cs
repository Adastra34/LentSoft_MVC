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

    public SupplierOrderController(LentSoftDbContext context, IInventoryService inventoryService)
    {
        _context = context;
        _inventoryService = inventoryService;
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

    private IActionResult RedirectToDashboardTab()
    {
        return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "pedidos", innerTab = "proveedores" });
    }
}
