using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Controllers;

[Authorize(Roles = "admin")]
public class ProveedorController : Controller
{
    private readonly LentSoftDbContext _context;
    private readonly IWebHostEnvironment _env;

    public ProveedorController(LentSoftDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    /// <summary>
    /// Create new Proveedor (POST)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Proveedor proveedor, IFormFile? logoFile)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            TempData["ErrorMessage"] = string.IsNullOrWhiteSpace(errors) ? "Datos del proveedor no válidos." : errors;
            return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "proveedores" });
        }

        if (logoFile != null && logoFile.Length > 0)
        {
            var logoUrl = await GuardarLogo(logoFile);
            if (logoUrl != null)
                proveedor.LogoUrl = logoUrl;
        }

        proveedor.FechaRegistro = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(proveedor.Nombre))
        {
            TempData["ErrorMessage"] = "El nombre del proveedor es obligatorio.";
            return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "proveedores" });
        }

        try
        {
            _context.Proveedores.Add(proveedor);
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
    /// Get Proveedor data as JSON (for edit modal)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetById(int id)
    {
        var proveedor = await _context.Proveedores.FindAsync(id);
        if (proveedor == null) return NotFound();

        return Json(new
        {
            proveedor.Id,
            proveedor.Nombre,
            proveedor.Contacto,
            proveedor.Telefono,
            proveedor.Email,
            proveedor.TipoProducto,
            proveedor.Estado,
            proveedor.LogoUrl
        });
    }

    /// <summary>
    /// Edit Proveedor (POST)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Proveedor proveedor, IFormFile? logoFile)
    {
        var existing = await _context.Proveedores.FindAsync(proveedor.Id);
        if (existing == null)
        {
            TempData["ErrorMessage"] = "Proveedor no encontrado.";
            return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "proveedores" });
        }

        existing.Nombre = proveedor.Nombre;
        existing.Contacto = proveedor.Contacto;
        existing.Telefono = proveedor.Telefono;
        existing.Email = proveedor.Email;
        existing.TipoProducto = proveedor.TipoProducto;
        existing.Estado = proveedor.Estado;

        if (logoFile != null && logoFile.Length > 0)
        {
            var logoUrl = await GuardarLogo(logoFile);
            if (logoUrl != null)
                existing.LogoUrl = logoUrl;
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Proveedor actualizado exitosamente.";
        return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "proveedores" });
    }

    /// <summary>
    /// Delete Proveedor (POST)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var proveedor = await _context.Proveedores.FindAsync(id);
        if (proveedor != null)
        {
            _context.Proveedores.Remove(proveedor);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Proveedor eliminado exitosamente.";
        }
        else
        {
            TempData["ErrorMessage"] = "Proveedor no encontrado.";
        }

        return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "proveedores" });
    }

    // ── Helpers ──

    private async Task<string?> GuardarLogo(IFormFile file)
    {
        try
        {
            var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!extensionesPermitidas.Contains(ext)) return null;

            var carpeta = Path.Combine(_env.WebRootPath, "img", "proveedores");
            Directory.CreateDirectory(carpeta);

            var nombreArchivo = $"{Guid.NewGuid()}{ext}";
            var rutaCompleta = Path.Combine(carpeta, nombreArchivo);

            using var stream = new FileStream(rutaCompleta, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/img/proveedores/{nombreArchivo}";
        }
        catch
        {
            return null;
        }
    }
}
