using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Controllers;

[Authorize(Roles = "admin")]
public class SupplierController : Controller
{
    private readonly LentSoftDbContext _context;
    private readonly IWebHostEnvironment _webHostEnvironment;

    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxImageSizeBytes = 5 * 1024 * 1024; // 5 MB

    public SupplierController(LentSoftDbContext context, IWebHostEnvironment webHostEnvironment)
    {
        _context = context;
        _webHostEnvironment = webHostEnvironment;
    }

    private async Task<(bool success, string? relativePath, string? errorMessage)> ProcessSupplierLogoAsync(IFormFile file)
    {
        if (file.Length == 0)
        {
            return (false, null, "El archivo de imagen está vacío.");
        }

        if (file.Length > MaxImageSizeBytes)
        {
            return (false, null, "El tamaño del logo no debe superar los 5 MB.");
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(ext))
        {
            return (false, null, "Formato de imagen no permitido. Solo se aceptan archivos .jpg, .jpeg, .png y .webp.");
        }

        try
        {
            var webRoot = _webHostEnvironment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadsDir = Path.Combine(webRoot, "uploads", "suppliers");
            if (!Directory.Exists(uploadsDir))
            {
                Directory.CreateDirectory(uploadsDir);
            }

            var uniqueFileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(uploadsDir, uniqueFileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativePath = $"/uploads/suppliers/{uniqueFileName}";
            return (true, relativePath, null);
        }
        catch (Exception ex)
        {
            return (false, null, $"Error al guardar el logo en el servidor: {ex.Message}");
        }
    }

    private void DeleteLocalSupplierLogo(string? logoUrl)
    {
        if (string.IsNullOrWhiteSpace(logoUrl)) return;

        var normalized = logoUrl.Replace('\\', '/').TrimStart('/');
        if (normalized.StartsWith("uploads/suppliers/", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var webRoot = _webHostEnvironment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var fullPath = Path.Combine(webRoot, normalized.Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
            catch
            {
                // No bloquear
            }
        }
    }

    /// <summary>
    /// Admin: Create new supplier
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Supplier supplier, IFormFile? LogoArchivo)
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

        if (LogoArchivo != null && LogoArchivo.Length > 0)
        {
            var (success, relativePath, error) = await ProcessSupplierLogoAsync(LogoArchivo);
            if (!success)
            {
                if (isAjax) return Json(new { success = false, message = error });
                TempData["ErrorMessage"] = error;
                return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "proveedores" });
            }
            supplier.LogoUrl = relativePath;
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
    public async Task<IActionResult> Edit(Supplier supplier, IFormFile? LogoArchivo)
    {
        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json");

        var existing = await _context.Suppliers.FindAsync(supplier.Id);
        if (existing == null)
        {
            if (isAjax) return Json(new { success = false, message = "Proveedor no encontrado." });
            TempData["ErrorMessage"] = "Proveedor no encontrado.";
            return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "proveedores" });
        }

        string? oldLogoToDelete = null;

        if (LogoArchivo != null && LogoArchivo.Length > 0)
        {
            var (success, relativePath, error) = await ProcessSupplierLogoAsync(LogoArchivo);
            if (!success)
            {
                if (isAjax) return Json(new { success = false, message = error });
                TempData["ErrorMessage"] = error;
                return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "proveedores" });
            }
            oldLogoToDelete = existing.LogoUrl;
            supplier.LogoUrl = relativePath;
        }
        else
        {
            // Conservar el logo existente si no se sube nada nuevo
            if (string.IsNullOrWhiteSpace(supplier.LogoUrl))
            {
                supplier.LogoUrl = existing.LogoUrl;
            }
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
            existing.Nombre = supplier.Nombre.Trim();
            existing.Contacto = supplier.Contacto?.Trim();
            existing.TipoProductos = (string.IsNullOrWhiteSpace(supplier.TipoProductos) ? supplier.TipoProducto : supplier.TipoProductos)?.Trim() ?? string.Empty;
            existing.Telefono = supplier.Telefono.Trim();
            existing.Correo = (string.IsNullOrWhiteSpace(supplier.Correo) ? supplier.Email : supplier.Correo)?.Trim().ToLower() ?? string.Empty;
            existing.LogoUrl = supplier.LogoUrl?.Trim();

            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(oldLogoToDelete))
            {
                DeleteLocalSupplierLogo(oldLogoToDelete);
            }

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
