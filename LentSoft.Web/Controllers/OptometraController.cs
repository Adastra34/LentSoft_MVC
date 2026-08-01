using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.ViewModels;
using LentSoft.Web.Models.Entities;

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

        var historiales = await _context.HistorialesClinicos
            .Include(h => h.User)
            .Include(h => h.Optometra)
            .OrderByDescending(h => h.Fecha)
            .ToListAsync();

        var examenes = await _context.ExamenesVisuales
            .Include(e => e.User)
            .Include(e => e.Optometra)
            .OrderByDescending(e => e.Fecha)
            .ToListAsync();

        var formulas = await _context.FormulasOpticas
            .Include(f => f.User)
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
            TempData["SuccessMessage"] = "Estado de cita actualizado.";
        }
        return RedirectToAction("Index", new { section = "citas" });
    }

    // ── CRUD Historial Clínico ──
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateHistorial(HistorialClinico model)
    {
        if (ModelState.IsValid)
        {
            model.OptometraId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            model.FechaCreacion = DateTime.UtcNow;
            model.Fecha = DateTime.SpecifyKind(model.Fecha, DateTimeKind.Utc);
            _context.HistorialesClinicos.Add(model);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Historial clínico creado exitosamente.";
        }
        else
        {
            TempData["ErrorMessage"] = "Error al crear el historial clínico. Por favor verifica los datos.";
        }
        return RedirectToAction("Index", new { section = "historial" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditHistorial(HistorialClinico model)
    {
        var existing = await _context.HistorialesClinicos.FindAsync(model.Id);
        if (existing != null)
        {
            existing.UserId = model.UserId;
            existing.Fecha = DateTime.SpecifyKind(model.Fecha, DateTimeKind.Utc);
            existing.Diagnostico = model.Diagnostico;
            existing.Tratamiento = model.Tratamiento;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Historial clínico actualizado exitosamente.";
        }
        else
        {
            TempData["ErrorMessage"] = "No se encontró el registro a editar.";
        }
        return RedirectToAction("Index", new { section = "historial" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteHistorial(int id)
    {
        var existing = await _context.HistorialesClinicos.FindAsync(id);
        if (existing != null)
        {
            _context.HistorialesClinicos.Remove(existing);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Historial clínico eliminado exitosamente.";
        }
        return RedirectToAction("Index", new { section = "historial" });
    }

    // ── CRUD Examen Visual ──
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateExamen(ExamenVisual model)
    {
        if (ModelState.IsValid)
        {
            model.OptometraId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            model.FechaCreacion = DateTime.UtcNow;
            model.Fecha = DateTime.SpecifyKind(model.Fecha, DateTimeKind.Utc);
            _context.ExamenesVisuales.Add(model);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Examen visual registrado exitosamente.";
        }
        else
        {
            TempData["ErrorMessage"] = "Error al registrar el examen visual. Por favor verifica los datos.";
        }
        return RedirectToAction("Index", new { section = "examenes" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditExamen(ExamenVisual model)
    {
        var existing = await _context.ExamenesVisuales.FindAsync(model.Id);
        if (existing != null)
        {
            existing.UserId = model.UserId;
            existing.Fecha = DateTime.SpecifyKind(model.Fecha, DateTimeKind.Utc);
            existing.TipoExamen = model.TipoExamen;
            existing.OjoDerecho = model.OjoDerecho;
            existing.OjoIzquierdo = model.OjoIzquierdo;
            existing.Resultado = model.Resultado;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Examen visual actualizado exitosamente.";
        }
        else
        {
            TempData["ErrorMessage"] = "No se encontró el registro a editar.";
        }
        return RedirectToAction("Index", new { section = "examenes" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteExamen(int id)
    {
        var existing = await _context.ExamenesVisuales.FindAsync(id);
        if (existing != null)
        {
            _context.ExamenesVisuales.Remove(existing);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Examen visual eliminado exitosamente.";
        }
        return RedirectToAction("Index", new { section = "examenes" });
    }

    // ── CRUD Fórmula Óptica ──
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFormula(FormulaOptica model)
    {
        if (ModelState.IsValid)
        {
            model.OptometraId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            model.FechaCreacion = DateTime.UtcNow;
            model.Fecha = DateTime.SpecifyKind(model.Fecha, DateTimeKind.Utc);
            _context.FormulasOpticas.Add(model);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Fórmula óptica creada exitosamente.";
        }
        else
        {
            TempData["ErrorMessage"] = "Error al crear la fórmula óptica. Por favor verifica los datos.";
        }
        return RedirectToAction("Index", new { section = "formulas" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditFormula(FormulaOptica model)
    {
        var existing = await _context.FormulasOpticas.FindAsync(model.Id);
        if (existing != null)
        {
            existing.UserId = model.UserId;
            existing.Fecha = DateTime.SpecifyKind(model.Fecha, DateTimeKind.Utc);
            existing.EsferaOD = model.EsferaOD;
            existing.CilindroOD = model.CilindroOD;
            existing.EjeOD = model.EjeOD;
            existing.EsferaOI = model.EsferaOI;
            existing.CilindroOI = model.CilindroOI;
            existing.EjeOI = model.EjeOI;
            existing.Observaciones = model.Observaciones;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Fórmula óptica actualizada exitosamente.";
        }
        else
        {
            TempData["ErrorMessage"] = "No se encontró el registro a editar.";
        }
        return RedirectToAction("Index", new { section = "formulas" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFormula(int id)
    {
        var existing = await _context.FormulasOpticas.FindAsync(id);
        if (existing != null)
        {
            _context.FormulasOpticas.Remove(existing);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Fórmula óptica eliminada exitosamente.";
        }
        return RedirectToAction("Index", new { section = "formulas" });
    }

    // ── CRUD Pacientes (Solo Editar Datos Básicos) ──
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPaciente(int id, string nombre, string apellido, string email, string? telefono)
    {
        var paciente = await _context.Users.FindAsync(id);
        if (paciente != null && paciente.Role == "usuario")
        {
            paciente.Nombre = nombre;
            paciente.Apellido = apellido;
            paciente.Email = email;
            paciente.Telefono = telefono;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Datos básicos del paciente actualizados.";
        }
        else
        {
            TempData["ErrorMessage"] = "Error al editar el paciente o no cuenta con los permisos.";
        }
        return RedirectToAction("Index", new { section = "pacientes" });
    }

    // ── Citas ──
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAppointment(int UserId, string Servicio, DateTime FechaHora, string? Notas)
    {
        var appointment = new Appointment
        {
            UserId = UserId,
            Servicio = Servicio,
            FechaHora = DateTime.SpecifyKind(FechaHora, DateTimeKind.Utc),
            Notas = Notas,
            Estado = "pendiente",
            FechaCreacion = DateTime.UtcNow
        };
        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Cita creada exitosamente.";
        return RedirectToAction("Index", new { section = "citas" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAppointment(int id)
    {
        var cita = await _context.Appointments.FindAsync(id);
        if (cita != null)
        {
            _context.Appointments.Remove(cita);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cita eliminada exitosamente.";
        }
        return RedirectToAction("Index", new { section = "citas" });
    }
}
