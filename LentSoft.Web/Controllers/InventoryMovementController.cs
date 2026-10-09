using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Services;

namespace LentSoft.Web.Controllers;

[Authorize(Roles = "admin")]
public class InventoryMovementController : Controller
{
    private readonly IInventoryService _inventoryService;

    public InventoryMovementController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    /// <summary>
    /// Admin: Create new inventory movement and update product stock
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InventoryMovement movement)
    {
        if (movement.ProductId <= 0 || movement.Cantidad <= 0)
        {
            TempData["ErrorMessage"] = "Debe seleccionar un producto y especificar una cantidad mayor a 0.";
            return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "historial" });
        }

        var isSalida = string.Equals(movement.Tipo?.Trim(), InventoryConstants.MovementTypes.Salida, StringComparison.OrdinalIgnoreCase);

        try
        {
            Product product;
            if (isSalida)
            {
                product = await _inventoryService.DisminuirStockAsync(
                    movement.ProductId, 
                    movement.Cantidad, 
                    InventoryConstants.MovementTypes.Salida);

                if (product.Stock == 0)
                {
                    TempData["WarningMessage"] = $"Se inhabilitó el producto {product.Nombre}";
                }
                else if (product.Stock <= _inventoryService.LowStockThreshold)
                {
                    TempData["WarningMessage"] = $"¡Alerta de Stock Mínimo! El producto {product.Nombre} está por agotarse (Stock: {product.Stock}).";
                }
            }
            else
            {
                product = await _inventoryService.AumentarStockAsync(
                    movement.ProductId, 
                    movement.Cantidad, 
                    InventoryConstants.MovementTypes.Entrada);
            }

            var tipoStr = isSalida ? InventoryConstants.MovementTypes.Salida : InventoryConstants.MovementTypes.Entrada;
            var successMsg = $"Movimiento de {tipoStr} registrado exitosamente. Nuevo stock: {product.Stock}.";
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
            TempData["ErrorMessage"] = "El producto fue modificado por otro usuario, vuelve a intentarlo.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al registrar el movimiento de inventario: {ex.Message}";
        }

        return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "historial" });
    }
}
