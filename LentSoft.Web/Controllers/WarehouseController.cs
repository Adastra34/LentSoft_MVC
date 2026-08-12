using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Controllers;

[Authorize(Roles = "admin")]
public class WarehouseController : Controller
{
    private readonly LentSoftDbContext _context;

    public WarehouseController(LentSoftDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Admin: Create new warehouse
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Warehouse warehouse)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Datos de la bodega no válidos.";
            TempData["ErrorMessage"] = firstError;
            return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "bodegas" });
        }

        warehouse.Activo = true;
        _context.Warehouses.Add(warehouse);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Bodega '{warehouse.Nombre}' creada exitosamente.";
        return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "bodegas" });
    }

    /// <summary>
    /// Admin: Edit warehouse
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Warehouse warehouse)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Datos de la bodega no válidos.";
            TempData["ErrorMessage"] = firstError;
            return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "bodegas" });
        }

        var existing = await _context.Warehouses.FindAsync(warehouse.Id);
        if (existing == null)
        {
            TempData["ErrorMessage"] = "Bodega no encontrada.";
            return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "bodegas" });
        }

        existing.Nombre = warehouse.Nombre.Trim();
        existing.Direccion = warehouse.Direccion?.Trim();

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Bodega actualizada exitosamente.";
        return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "bodegas" });
    }

    /// <summary>
    /// Admin: Delete warehouse (soft delete)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var warehouse = await _context.Warehouses.FindAsync(id);
        if (warehouse != null)
        {
            warehouse.Activo = false;
            _context.Warehouses.Update(warehouse);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Bodega desactivada exitosamente.";
        }
        else
        {
            TempData["ErrorMessage"] = "Bodega no encontrada.";
        }

        return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "bodegas" });
    }
}
