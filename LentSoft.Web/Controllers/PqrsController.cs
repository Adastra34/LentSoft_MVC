using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Services;

namespace LentSoft.Web.Controllers;

[Authorize]
public class PqrsController : Controller
{
    private readonly IPqrsService _pqrsService;

    public PqrsController(IPqrsService pqrsService)
    {
        _pqrsService = pqrsService;
    }

    /// <summary>
    /// Historial de PQRS radicadas por el cliente autenticado.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out var userId))
        {
            return Challenge();
        }

        var lista = await _pqrsService.ObtenerPorUsuarioAsync(userId);
        return View(lista);
    }

    /// <summary>
    /// Formulario para que el cliente radique una nueva PQRS.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "usuario")]
    public IActionResult Crear()
    {
        return View(new Pqrs());
    }

    /// <summary>
    /// Procesa el radicado de una nueva PQRS enviada por el cliente.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "usuario")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(Pqrs model)
    {
        if (User.IsInRole("admin"))
        {
            return Forbid();
        }

        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out var userId))
        {
            return Challenge();
        }

        model.UserId = userId;
        ModelState.Remove(nameof(model.User));
        ModelState.Remove(nameof(model.UserId));
        ModelState.Remove(nameof(model.Radicado));

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var nuevaPqrs = await _pqrsService.CrearAsync(model);
        TempData["SuccessMessage"] = $"Su solicitud ha sido radicada correctamente con el código {nuevaPqrs.CodigoRadicado}.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Detalle de una PQRS específica para el usuario propietario o administrador.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Detalle(int id)
    {
        var pqrs = await _pqrsService.ObtenerPorIdAsync(id);
        if (pqrs == null)
        {
            return NotFound();
        }

        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int.TryParse(userIdString, out var userId);
        var esAdmin = User.IsInRole("admin");

        if (!esAdmin && pqrs.UserId != userId)
        {
            return Forbid();
        }

        return View(pqrs);
    }

    /// <summary>
    /// Panel de administración para consultar todas las PQRS.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> AdminIndex(string? estado)
    {
        var lista = await _pqrsService.ObtenerTodosAsync(estado);
        ViewBag.FiltroEstado = estado;
        return View(lista);
    }

    /// <summary>
    /// Formulario administrativo para responder y actualizar el estado de una PQRS.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Responder(int id)
    {
        var pqrs = await _pqrsService.ObtenerPorIdAsync(id);
        if (pqrs == null)
        {
            return NotFound();
        }

        return View(pqrs);
    }

    /// <summary>
    /// Procesa la respuesta administrativa y actualiza el estado de la PQRS.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Responder(int id, string respuestaAdmin, string nuevoEstado)
    {
        if (string.IsNullOrWhiteSpace(respuestaAdmin))
        {
            ModelState.AddModelError("respuestaAdmin", "Debe ingresar una respuesta para el radicado.");
            var pqrs = await _pqrsService.ObtenerPorIdAsync(id);
            if (pqrs == null) return NotFound();
            return View(pqrs);
        }

        var exitoso = await _pqrsService.ResponderAsync(id, respuestaAdmin, nuevoEstado);
        if (!exitoso)
        {
            return NotFound();
        }

        TempData["SuccessMessage"] = "La respuesta administrativa ha sido guardada exitosamente y el estado fue actualizado.";
        return RedirectToAction(nameof(AdminIndex));
    }
}
