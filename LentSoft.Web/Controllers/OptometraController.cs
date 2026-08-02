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

    public async Task<IActionResult> Index(string section = "dashboard", int? detalleId = null)
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

        ViewBag.DetalleId = detalleId;
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
            existing.Antecedentes = model.Antecedentes;
            existing.ExamenesRealizados = model.ExamenesRealizados;
            existing.Observaciones = model.Observaciones;
            existing.Estado = model.Estado;

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
            existing.TonometriaOD = model.TonometriaOD;
            existing.TonometriaOI = model.TonometriaOI;
            existing.EsferaOD = model.EsferaOD;
            existing.CilindroOD = model.CilindroOD;
            existing.EjeOD = model.EjeOD;
            existing.AdicionOD = model.AdicionOD;
            existing.EsferaOI = model.EsferaOI;
            existing.CilindroOI = model.CilindroOI;
            existing.EjeOI = model.EjeOI;
            existing.AdicionOI = model.AdicionOI;
            existing.SegmentoAnterior = model.SegmentoAnterior;
            existing.SegmentoPosterior = model.SegmentoPosterior;
            existing.Diagnostico = model.Diagnostico;
            existing.Tratamiento = model.Tratamiento;
            existing.Observaciones = model.Observaciones;

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
            existing.TipoLente = model.TipoLente;
            existing.DistanciaPupilar = model.DistanciaPupilar;

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

    // ── CRUD Pacientes (Tarea 1) ──
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePaciente(string nombre, string apellido, string email, string? telefono, string tipoDocumento, string numeroDocumento, DateTime? fechaNacimiento, string? genero, string? direccion, string? eps, string? observacionesPaciente)
    {
        var existingEmail = await _context.Users.AnyAsync(u => u.Email == email);
        if (existingEmail)
        {
            TempData["ErrorMessage"] = "El correo ya está registrado.";
            return RedirectToAction("Index", new { section = "pacientes" });
        }

        var existingDoc = await _context.Users.AnyAsync(u => u.NumeroDocumento == numeroDocumento);
        if (existingDoc)
        {
            TempData["ErrorMessage"] = "El número de documento ya está registrado.";
            return RedirectToAction("Index", new { section = "pacientes" });
        }

        var tempPassword = "user123";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);

        var paciente = new User
        {
            Nombre = nombre,
            Apellido = apellido,
            Email = email,
            Telefono = telefono,
            TipoDocumento = tipoDocumento,
            NumeroDocumento = numeroDocumento,
            PasswordHash = passwordHash,
            Role = "usuario",
            FechaRegistro = DateTime.UtcNow,
            FechaNacimiento = fechaNacimiento.HasValue ? DateTime.SpecifyKind(fechaNacimiento.Value, DateTimeKind.Utc) : null,
            Genero = genero,
            Direccion = direccion,
            EPS = eps,
            EstadoPaciente = "Activo",
            ObservacionesPaciente = observacionesPaciente
        };

        _context.Users.Add(paciente);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Paciente creado exitosamente.";
        return RedirectToAction("Index", new { section = "pacientes" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPaciente(int id, string nombre, string apellido, string email, string? telefono, string tipoDocumento, string numeroDocumento, DateTime? fechaNacimiento, string? genero, string? direccion, string? eps, string? estadoPaciente, string? observacionesPaciente)
    {
        var paciente = await _context.Users.FindAsync(id);
        if (paciente != null && paciente.Role == "usuario")
        {
            paciente.Nombre = nombre;
            paciente.Apellido = apellido;
            paciente.Email = email;
            paciente.Telefono = telefono;
            paciente.TipoDocumento = tipoDocumento;
            paciente.NumeroDocumento = numeroDocumento;
            paciente.FechaNacimiento = fechaNacimiento.HasValue ? DateTime.SpecifyKind(fechaNacimiento.Value, DateTimeKind.Utc) : null;
            paciente.Genero = genero;
            paciente.Direccion = direccion;
            paciente.EPS = eps;
            paciente.EstadoPaciente = estadoPaciente ?? "Activo";
            paciente.ObservacionesPaciente = observacionesPaciente;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Datos del paciente actualizados exitosamente.";
        }
        else
        {
            TempData["ErrorMessage"] = "No se pudo actualizar el paciente.";
        }
        return RedirectToAction("Index", new { section = "pacientes" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePaciente(int id)
    {
        var paciente = await _context.Users.FindAsync(id);
        if (paciente != null && paciente.Role == "usuario")
        {
            paciente.EstadoPaciente = "Inactivo";
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Paciente desactivado (Inactivo) exitosamente.";
        }
        else
        {
            TempData["ErrorMessage"] = "No se pudo desactivar el paciente.";
        }
        return RedirectToAction("Index", new { section = "pacientes" });
    }

    // ── Citas (Tarea 2) ──
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
        TempData["SuccessMessage"] = "Cita programada exitosamente.";
        return RedirectToAction("Index", new { section = "citas" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAppointment(int id, int UserId, string Servicio, DateTime FechaHora, string? Notas, string Estado)
    {
        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment != null)
        {
            appointment.UserId = UserId;
            appointment.Servicio = Servicio;
            appointment.FechaHora = DateTime.SpecifyKind(FechaHora, DateTimeKind.Utc);
            appointment.Notas = Notas;
            appointment.Estado = Estado;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cita actualizada exitosamente.";
        }
        else
        {
            TempData["ErrorMessage"] = "No se encontró la cita a editar.";
        }
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

    // ── Perfil Profesional (Tarea 6) ──
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(string nombre, string apellido, string email, string? telefono, string? registroMedico, string? universidad, string? especialidadDetalle, int? aniosExperiencia)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.Nombre = nombre;
            user.Apellido = apellido;
            user.Email = email;
            user.Telefono = telefono;
            user.RegistroMedico = registroMedico;
            user.Universidad = universidad;
            user.EspecialidadDetalle = especialidadDetalle;
            user.AniosExperiencia = aniosExperiencia;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Perfil actualizado exitosamente.";
        }
        else
        {
            TempData["ErrorMessage"] = "No se pudo actualizar el perfil.";
        }
        return RedirectToAction("Index", new { section = "perfil" });
    }

    // ── Historial Detalle (Tarea 3) ──
    public IActionResult HistorialDetalle(int id)
    {
        return RedirectToAction("Index", new { section = "historial", detalleId = id });
    }
}
