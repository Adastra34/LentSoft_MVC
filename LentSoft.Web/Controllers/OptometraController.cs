using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Models.ViewModels;

namespace LentSoft.Web.Controllers;

[Authorize(Roles = "optometra")]
public class OptometraController : Controller
{
    private readonly LentSoftDbContext _context;

    public OptometraController(LentSoftDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string section = "dashboard")
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int userId = int.TryParse(userIdStr, out var idParsed) ? idParsed : 3;
        var usuario = await _context.Users.FindAsync(userId);

        var now = DateTime.UtcNow;
        var inicioMes = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var hoy = now.Date;

        var citas = await _context.Appointments
            .Include(a => a.User)
            .OrderByDescending(a => a.FechaHora)
            .ToListAsync();

        var pacientes = await _context.Users
            .Where(u => u.Role == "usuario")
            .OrderBy(u => u.Nombre)
            .ToListAsync();

        var historiales = await _context.HistorialesClinicos
            .Include(h => h.Paciente)
            .Include(h => h.Optometra)
            .OrderByDescending(h => h.Fecha)
            .ToListAsync();

        var examenes = await _context.ExamenesVisuales
            .Include(e => e.Paciente)
            .Include(e => e.Optometra)
            .OrderByDescending(e => e.Fecha)
            .ToListAsync();

        var formulas = await _context.FormulasOpticas
            .Include(f => f.Paciente)
            .Include(f => f.Optometra)
            .OrderByDescending(f => f.Fecha)
            .ToListAsync();

        var proximaCitaObj = citas
            .Where(c => c.FechaHora >= now && !c.Estado.Equals("cancelada", StringComparison.OrdinalIgnoreCase) && !c.Estado.Equals("atendida", StringComparison.OrdinalIgnoreCase) && !c.Estado.Equals("completada", StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.FechaHora)
            .FirstOrDefault() ?? citas
            .Where(c => !c.Estado.Equals("cancelada", StringComparison.OrdinalIgnoreCase) && !c.Estado.Equals("atendida", StringComparison.OrdinalIgnoreCase) && !c.Estado.Equals("completada", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(c => c.FechaHora)
            .FirstOrDefault();

        string proximaCitaStr = proximaCitaObj != null
            ? proximaCitaObj.FechaHora.ToLocalTime().ToString("dd MMM", new System.Globalization.CultureInfo("es-ES"))
            : "Sin citas";

        var viewModel = new DashboardOptometraViewModel
        {
            TotalPacientes = pacientes.Count,
            CitasHoy = citas.Count(c => c.FechaHora.Date == hoy),
            CitasPendientes = citas.Count(c => c.Estado.Equals("pendiente", StringComparison.OrdinalIgnoreCase)),
            ExamenesEsteMes = examenes.Count(e => e.Fecha >= inicioMes),
            TotalExamenes = examenes.Count,
            TotalFormulas = formulas.Count,
            ProximaCitaFecha = proximaCitaStr,
            Pacientes = pacientes,
            Citas = citas,
            UsuarioActual = usuario,
            HistorialClinico = historiales,
            ExamenesVisuales = examenes,
            FormulasOpticas = formulas,
            ActiveSection = section
        };

        return View("~/Views/Dashboard/Optometra.cshtml", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAppointmentStatus(int id, string estado)
    {
        var cita = await _context.Appointments.FindAsync(id);
        if (cita != null)
        {
            cita.Estado = estado;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Estado de cita actualizado con éxito.";
        }
        return RedirectToAction("Index", new { section = "citas" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearHistorialClinico(int pacienteId, string diagnostico, string tratamiento, string? observaciones)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int optometraId = int.TryParse(userIdStr, out var idParsed) ? idParsed : 3;

        var historial = new HistorialClinico
        {
            PacienteId = pacienteId,
            OptometraId = optometraId,
            Fecha = DateTime.UtcNow,
            Diagnostico = diagnostico,
            Tratamiento = tratamiento ?? string.Empty,
            Observaciones = observaciones
        };

        _context.HistorialesClinicos.Add(historial);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Diagnóstico en Historial Clínico registrado exitosamente.";

        return RedirectToAction("Index", new { section = "historial" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearExamenVisual(int pacienteId, string tipoExamen, string ojoDerecho, string ojoIzquierdo, string resultado)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int optometraId = int.TryParse(userIdStr, out var idParsed) ? idParsed : 3;

        var examen = new ExamenVisual
        {
            PacienteId = pacienteId,
            OptometraId = optometraId,
            Fecha = DateTime.UtcNow,
            TipoExamen = tipoExamen,
            OjoDerecho = ojoDerecho ?? string.Empty,
            OjoIzquierdo = ojoIzquierdo ?? string.Empty,
            Resultado = resultado ?? string.Empty
        };

        _context.ExamenesVisuales.Add(examen);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Examen Visual registrado exitosamente.";

        return RedirectToAction("Index", new { section = "examenes" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearFormulaOptica(int pacienteId, string esferaOD, string cilindroOD, string ejeOD, string esferaOI, string cilindroOI, string ejeOI, string? adicion, string? distanciaPupilar, string? observaciones)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int optometraId = int.TryParse(userIdStr, out var idParsed) ? idParsed : 3;

        var formula = new FormulaOptica
        {
            PacienteId = pacienteId,
            OptometraId = optometraId,
            Fecha = DateTime.UtcNow,
            EsferaOD = esferaOD,
            CilindroOD = cilindroOD,
            EjeOD = ejeOD,
            EsferaOI = esferaOI,
            CilindroOI = cilindroOI,
            EjeOI = ejeOI,
            Adicion = adicion,
            DistanciaPupilar = distanciaPupilar,
            Observaciones = observaciones
        };

        _context.FormulasOpticas.Add(formula);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Fórmula Óptica registrada exitosamente.";

        return RedirectToAction("Index", new { section = "formulas" });
    }
}
