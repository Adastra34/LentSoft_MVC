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

        var product = await _context.Products.FindAsync(movement.ProductId);
        if (product == null)
        {
            TempData["ErrorMessage"] = "El producto seleccionado no existe.";
            return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "historial" });
        }

        var tipo = movement.Tipo?.Trim();
        if (string.Equals(tipo, "Salida", StringComparison.OrdinalIgnoreCase))
        {
            tipo = "Salida";
            if (product.Stock < movement.Cantidad)
            {
                TempData["ErrorMessage"] = $"Stock insuficiente. Stock actual de {product.Nombre}: {product.Stock}, intentó retirar: {movement.Cantidad}.";
                return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "historial" });
            }
            product.Stock -= movement.Cantidad;
        }
        else
        {
            tipo = "Entrada";
            product.Stock += movement.Cantidad;
        }

        movement.Tipo = tipo;
        movement.Fecha = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(movement.Responsable))
        {
            var userName = User.Identity?.Name;
            movement.Responsable = string.IsNullOrWhiteSpace(userName) ? "Administrador" : userName;
        }

        _context.InventoryMovements.Add(movement);
        _context.Products.Update(product);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Movimiento de {tipo} registrado exitosamente. Nuevo stock: {product.Stock}.";
        return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "historial" });
    }
}
