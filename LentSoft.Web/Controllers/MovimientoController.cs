using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Controllers;

[Authorize(Roles = "admin")]
public class MovimientoController : Controller
{
    private readonly LentSoftDbContext _context;

    public MovimientoController(LentSoftDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Create new MovimientoInventario (POST)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MovimientoInventario movimiento)
    {
        if (string.IsNullOrWhiteSpace(movimiento.Producto) || string.IsNullOrWhiteSpace(movimiento.Responsable))
        {
            TempData["ErrorMessage"] = "El producto y el responsable son obligatorios.";
            return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "historial" });
        }

        movimiento.Fecha = DateTime.UtcNow;
        _context.MovimientosInventario.Add(movimiento);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Movimiento registrado exitosamente.";
        return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "historial" });
    }

    /// <summary>
    /// Get movimiento data as JSON (for edit modal)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetById(int id)
    {
        var mov = await _context.MovimientosInventario.FindAsync(id);
        if (mov == null) return NotFound();

        return Json(new
        {
            mov.Id,
            mov.Producto,
            mov.Tipo,
            mov.Cantidad,
            Fecha = mov.Fecha.ToString("yyyy-MM-ddTHH:mm"),
            mov.Responsable
        });
    }

    /// <summary>
    /// Edit MovimientoInventario (POST)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(MovimientoInventario movimiento)
    {
        var existing = await _context.MovimientosInventario.FindAsync(movimiento.Id);
        if (existing == null)
        {
            TempData["ErrorMessage"] = "Movimiento no encontrado.";
            return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "historial" });
        }

        existing.Producto = movimiento.Producto;
        existing.Tipo = movimiento.Tipo;
        existing.Cantidad = movimiento.Cantidad;
        existing.Responsable = movimiento.Responsable;
        // No actualizamos Fecha para preservar la fecha original del movimiento

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Movimiento actualizado exitosamente.";
        return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "historial" });
    }

    /// <summary>
    /// Delete MovimientoInventario (POST)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var mov = await _context.MovimientosInventario.FindAsync(id);
        if (mov != null)
        {
            _context.MovimientosInventario.Remove(mov);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Movimiento eliminado exitosamente.";
        }
        else
        {
            TempData["ErrorMessage"] = "Movimiento no encontrado.";
        }

        return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "historial" });
    }
}
