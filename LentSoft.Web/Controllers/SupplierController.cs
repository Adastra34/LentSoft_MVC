using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Controllers;

[Authorize(Roles = "admin")]
public class SupplierController : Controller
{
    private readonly LentSoftDbContext _context;

    public SupplierController(LentSoftDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Admin: Create new supplier
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Supplier supplier)
    {
        if (string.IsNullOrWhiteSpace(supplier.Id))
        {
            var count = await _context.Suppliers.CountAsync();
            supplier.Id = $"PROV{(count + 1):D3}";
        }
        else
        {
            supplier.Id = supplier.Id.Trim().ToUpper();
        }

        if (await _context.Suppliers.AnyAsync(s => s.Id == supplier.Id))
        {
            TempData["ErrorMessage"] = $"El código de proveedor {supplier.Id} ya existe.";
            return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "proveedores" });
        }

        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Datos del proveedor no válidos.";
            TempData["ErrorMessage"] = firstError;
            return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "proveedores" });
        }

        try
        {
            supplier.FechaRegistro = DateTime.UtcNow;
            supplier.Activo = true;

            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Proveedor creado exitosamente.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al crear el proveedor: {ex.Message}";
        }

        return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "proveedores" });
    }

    /// <summary>
    /// Admin: Edit supplier
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Supplier supplier)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Datos del proveedor no válidos.";
            TempData["ErrorMessage"] = firstError;
            return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "proveedores" });
        }

        try
        {
            var existing = await _context.Suppliers.FindAsync(supplier.Id);
            if (existing == null)
            {
                TempData["ErrorMessage"] = "Proveedor no encontrado.";
                return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "proveedores" });
            }

            existing.Nombre = supplier.Nombre.Trim();
            existing.TipoProductos = supplier.TipoProductos.Trim();
            existing.Telefono = supplier.Telefono.Trim();
            existing.Correo = supplier.Correo.Trim().ToLower();

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Proveedor actualizado exitosamente.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al actualizar el proveedor: {ex.Message}";
        }

        return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "proveedores" });
    }

    /// <summary>
    /// Admin: Delete supplier (soft delete / remove)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier != null)
            {
                supplier.Activo = false;
                _context.Suppliers.Update(supplier);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Proveedor eliminado exitosamente.";
            }
            else
            {
                TempData["ErrorMessage"] = "Proveedor no encontrado.";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al eliminar el proveedor: {ex.Message}";
        }

        return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "proveedores" });
    }
}
