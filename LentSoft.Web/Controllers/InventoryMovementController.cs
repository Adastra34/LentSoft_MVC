using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Controllers;

[Authorize(Roles = "admin")]
public class InventoryMovementController : Controller
{
    private readonly LentSoftDbContext _context;

    public InventoryMovementController(LentSoftDbContext context)
    {
        _context = context;
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

        movement.WarehouseId ??= 1;

        var product = await _context.Products
            .Include(p => p.ProductStocks)
            .FirstOrDefaultAsync(p => p.Id == movement.ProductId);

        if (product == null)
        {
            TempData["ErrorMessage"] = "El producto seleccionado no existe.";
            return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "historial" });
        }

        var productStock = await _context.ProductStocks
            .FirstOrDefaultAsync(ps => ps.ProductId == movement.ProductId && ps.WarehouseId == movement.WarehouseId.Value);

        if (productStock == null)
        {
            productStock = new ProductStock
            {
                ProductId = movement.ProductId,
                WarehouseId = movement.WarehouseId.Value,
                Cantidad = 0
            };
            _context.ProductStocks.Add(productStock);
        }

        var tipo = movement.Tipo?.Trim();
        if (string.Equals(tipo, "Salida", StringComparison.OrdinalIgnoreCase))
        {
            tipo = "Salida";
            if (productStock.Cantidad < movement.Cantidad)
            {
                TempData["ErrorMessage"] = $"Stock insuficiente en la bodega seleccionada. Stock actual de {product.Nombre}: {productStock.Cantidad}, intentó retirar: {movement.Cantidad}.";
                return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "historial" });
            }
            productStock.Cantidad -= movement.Cantidad;
        }
        else
        {
            tipo = "Entrada";
            productStock.Cantidad += movement.Cantidad;
        }

        movement.Tipo = tipo;
        movement.Fecha = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(movement.Responsable))
        {
            var userName = User.Identity?.Name;
            movement.Responsable = string.IsNullOrWhiteSpace(userName) ? "Administrador" : userName;
        }

        _context.InventoryMovements.Add(movement);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Movimiento de {tipo} registrado exitosamente. Nuevo stock en bodega: {productStock.Cantidad}.";
        return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "historial" });
    }
}
