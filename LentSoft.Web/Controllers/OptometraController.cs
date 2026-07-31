using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
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
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
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

        var mockExamenes = GetMockExamenes();
        var mockFormulas = GetMockFormulas();

        var proximaCitaObj = citas
            .Where(c => c.FechaHora >= now && !c.Estado.Equals("cancelada", StringComparison.OrdinalIgnoreCase) && !c.Estado.Equals("atendida", StringComparison.OrdinalIgnoreCase) && !c.Estado.Equals("completada", StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.FechaHora)
            .FirstOrDefault() ?? citas
            .Where(c => !c.Estado.Equals("cancelada", StringComparison.OrdinalIgnoreCase) && !c.Estado.Equals("atendida", StringComparison.OrdinalIgnoreCase) && !c.Estado.Equals("completada", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(c => c.FechaHora)
            .FirstOrDefault();

        string proximaCitaStr = proximaCitaObj != null
            ? proximaCitaObj.FechaHora.ToLocalTime().ToString("dd MMM", new System.Globalization.CultureInfo("es-ES"))
            : "26 Jun";

        var viewModel = new DashboardOptometraViewModel
        {
            TotalPacientes = pacientes.Count,
            CitasHoy = citas.Count(c => c.FechaHora.Date == hoy),
            CitasPendientes = citas.Count(c => c.Estado.Equals("pendiente", StringComparison.OrdinalIgnoreCase)),
            ExamenesEsteMes = citas.Count(c => c.FechaHora >= inicioMes && c.Estado.Equals("completada", StringComparison.OrdinalIgnoreCase)),
            TotalExamenes = mockExamenes.Count,
            TotalFormulas = mockFormulas.Count,
            ProximaCitaFecha = proximaCitaStr,
            Pacientes = pacientes,
            Citas = citas,
            UsuarioActual = usuario,
            HistorialClinico = GetMockHistorial(),
            ExamenesVisuales = mockExamenes,
            FormulasOpticas = mockFormulas,
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
            TempData["SuccessMessage"] = "Estado de cita actualizado.";
        }
        return RedirectToAction("Index", new { section = "citas" });
    }

    // ── Mock data ──
    private static List<HistorialClinicoMock> GetMockHistorial() => new()
    {
        new() { Id = 1, Paciente = "Usuario Demo", Fecha = DateTime.UtcNow.AddDays(-30), Diagnostico = "Miopía leve OD -1.25 OI -1.50", Tratamiento = "Lentes correctivos", Optometra = "Dra. María García" },
        new() { Id = 2, Paciente = "Usuario Demo", Fecha = DateTime.UtcNow.AddDays(-60), Diagnostico = "Astigmatismo moderado bilateral", Tratamiento = "Lentes con cilindro", Optometra = "Dra. María García" },
        new() { Id = 3, Paciente = "Usuario Demo", Fecha = DateTime.UtcNow.AddDays(-90), Diagnostico = "Revisión de rutina - Sin cambios", Tratamiento = "Mantener fórmula actual", Optometra = "Dra. María García" }
    };

    private static List<ExamenVisualMock> GetMockExamenes() => new()
    {
        new() { Id = 1, Paciente = "Usuario Demo", Fecha = DateTime.UtcNow.AddDays(-30), TipoExamen = "Agudeza Visual", OjoDerecho = "20/25", OjoIzquierdo = "20/30", Resultado = "Requiere corrección" },
        new() { Id = 2, Paciente = "Usuario Demo", Fecha = DateTime.UtcNow.AddDays(-30), TipoExamen = "Refracción", OjoDerecho = "-1.25 -0.50 x 180", OjoIzquierdo = "-1.50 -0.75 x 175", Resultado = "Miopía con astigmatismo" },
        new() { Id = 3, Paciente = "Usuario Demo", Fecha = DateTime.UtcNow.AddDays(-60), TipoExamen = "Tonometría", OjoDerecho = "14 mmHg", OjoIzquierdo = "15 mmHg", Resultado = "Normal" }
    };

    private static List<FormulaOpticaMock> GetMockFormulas() => new()
    {
        new() { Id = 1, Paciente = "Usuario Demo", Fecha = DateTime.UtcNow.AddDays(-30), EsferaOD = "-1.25", CilindroOD = "-0.50", EjeOD = "180°", EsferaOI = "-1.50", CilindroOI = "-0.75", EjeOI = "175°", Observaciones = "Uso permanente. Control en 6 meses." },
        new() { Id = 2, Paciente = "Usuario Demo", Fecha = DateTime.UtcNow.AddDays(-180), EsferaOD = "-1.00", CilindroOD = "-0.25", EjeOD = "180°", EsferaOI = "-1.25", CilindroOI = "-0.50", EjeOI = "175°", Observaciones = "Fórmula anterior. Actualizada." }
    };
}
