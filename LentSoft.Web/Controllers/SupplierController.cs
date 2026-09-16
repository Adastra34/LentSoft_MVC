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
        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json");

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
            var msg = $"El código de proveedor {supplier.Id} ya existe.";
            if (isAjax) return Json(new { success = false, message = msg });
            TempData["ErrorMessage"] = msg;
            return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "proveedores" });
        }

        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Datos del proveedor no válidos.";
            if (isAjax) return Json(new { success = false, message = firstError });
            TempData["ErrorMessage"] = firstError;
            return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "proveedores" });
        }

        try
        {
            supplier.Contacto = supplier.Contacto?.Trim();
            if (string.IsNullOrWhiteSpace(supplier.TipoProductos) && !string.IsNullOrWhiteSpace(supplier.TipoProducto)) supplier.TipoProductos = supplier.TipoProducto;
            if (string.IsNullOrWhiteSpace(supplier.Correo) && !string.IsNullOrWhiteSpace(supplier.Email)) supplier.Correo = supplier.Email;
            supplier.LogoUrl = supplier.LogoUrl?.Trim();
            supplier.FechaRegistro = DateTime.UtcNow;
            supplier.Activo = true;

            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();

            if (isAjax) return Json(new { success = true, message = "Proveedor creado exitosamente.", data = supplier });
            TempData["SuccessMessage"] = "Proveedor creado exitosamente.";
        }
        catch (Exception ex)
        {
            if (isAjax) return Json(new { success = false, message = $"Error al crear el proveedor: {ex.Message}" });
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
        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json");

        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Datos del proveedor no válidos.";
            if (isAjax) return Json(new { success = false, message = firstError });
            TempData["ErrorMessage"] = firstError;
            return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "proveedores" });
        }

        try
        {
            var existing = await _context.Suppliers.FindAsync(supplier.Id);
            if (existing == null)
            {
                if (isAjax) return Json(new { success = false, message = "Proveedor no encontrado." });
                TempData["ErrorMessage"] = "Proveedor no encontrado.";
                return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "proveedores" });
            }

            existing.Nombre = supplier.Nombre.Trim();
            existing.Contacto = supplier.Contacto?.Trim();
            existing.TipoProductos = (string.IsNullOrWhiteSpace(supplier.TipoProductos) ? supplier.TipoProducto : supplier.TipoProductos)?.Trim() ?? string.Empty;
            existing.Telefono = supplier.Telefono.Trim();
            existing.Correo = (string.IsNullOrWhiteSpace(supplier.Correo) ? supplier.Email : supplier.Correo)?.Trim().ToLower() ?? string.Empty;
            existing.LogoUrl = supplier.LogoUrl?.Trim();

            await _context.SaveChangesAsync();

            if (isAjax) return Json(new { success = true, message = "Proveedor actualizado exitosamente.", data = existing });
            TempData["SuccessMessage"] = "Proveedor actualizado exitosamente.";
        }
        catch (Exception ex)
        {
            if (isAjax) return Json(new { success = false, message = $"Error al actualizar el proveedor: {ex.Message}" });
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
        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json");

        try
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier != null)
            {
                supplier.Activo = false;
                _context.Suppliers.Update(supplier);
                await _context.SaveChangesAsync();
                if (isAjax) return Json(new { success = true, message = "Proveedor eliminado exitosamente.", id });
                TempData["SuccessMessage"] = "Proveedor eliminado exitosamente.";
            }
            else
            {
                if (isAjax) return Json(new { success = false, message = "Proveedor no encontrado." });
                TempData["ErrorMessage"] = "Proveedor no encontrado.";
            }
        }
        catch (Exception ex)
        {
            if (isAjax) return Json(new { success = false, message = $"Error al eliminar el proveedor: {ex.Message}" });
            TempData["ErrorMessage"] = $"Error al eliminar el proveedor: {ex.Message}";
        }

        return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "proveedores" });
    }

    /// <summary>
    /// Admin: Toggle supplier active status (POST)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ToggleStatus(string id)
    {
        try
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                return Json(new { success = false, message = "Proveedor no encontrado." });
            }
            supplier.Activo = !supplier.Activo;
            _context.Suppliers.Update(supplier);
            await _context.SaveChangesAsync();
            return Json(new { success = true, active = supplier.Activo, message = $"Estado del proveedor actualizado a {(supplier.Activo ? "Activo" : "Inactivo")}." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error al actualizar estado: {ex.Message}" });
        }
    }
}
