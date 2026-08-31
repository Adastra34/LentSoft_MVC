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
            .Where(a => a.Activo)
            .Include(a => a.User)
            .Include(a => a.Optometra)
            .OrderByDescending(a => a.FechaHora)
            .ToListAsync();

        var pacientes = await _context.Users
            .Where(u => u.Role == "usuario" && u.Activo)
            .OrderBy(u => u.Nombre)
            .ToListAsync();

        var historiales = await _context.HistorialesClinicos
            .Where(h => h.Activo)
            .Include(h => h.User)
            .Include(h => h.Optometra)
            .OrderByDescending(h => h.Fecha)
            .ToListAsync();

        var examenes = await _context.ExamenesVisuales
            .Where(e => e.Activo)
            .Include(e => e.User)
            .Include(e => e.Optometra)
            .OrderByDescending(e => e.Fecha)
            .ToListAsync();

        var formulas = await _context.FormulasOpticas
            .Where(f => f.Activo)
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
        ViewBag.Optometras = await _context.Users
            .Where(u => u.Role == "optometra" && u.Activo)
            .OrderBy(u => u.Nombre)
            .ToListAsync();
        return View("~/Views/Dashboard/Optometra.cshtml", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAppointmentStatus(int id, string estado)
    {
        try
        {
            var cita = await _context.Appointments.FindAsync(id);
            if (cita != null)
            {
                cita.Estado = estado;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Estado de cita actualizado.";
            }
            else
            {
                TempData["ErrorMessage"] = "Cita no encontrada.";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al actualizar la cita: {ex.Message}";
        }

        return RedirectToAction("Index", new { section = "citas" });
    }

    // ── CRUD Historial Clínico ──
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateHistorial(HistorialClinico model)
    {
        ModelState.Remove("User");
        ModelState.Remove("Optometra");
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Error al crear el historial clínico. Por favor verifica los datos.";
            TempData["ErrorMessage"] = firstError;
            return RedirectToAction("Index", new { section = "historial" });
        }

        try
        {
            model.OptometraId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            model.FechaCreacion = DateTime.UtcNow;
            model.Fecha = DateTime.SpecifyKind(model.Fecha, DateTimeKind.Utc);
            _context.HistorialesClinicos.Add(model);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Historial clínico creado exitosamente.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al crear el historial clínico: {ex.Message}";
        }

        return RedirectToAction("Index", new { section = "historial" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditHistorial(HistorialClinico model)
    {
        ModelState.Remove("User");
        ModelState.Remove("Optometra");
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Datos del historial no válidos.";
            TempData["ErrorMessage"] = firstError;
            return RedirectToAction("Index", new { section = "historial" });
        }

        try
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
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al actualizar el historial clínico: {ex.Message}";
        }

        return RedirectToAction("Index", new { section = "historial" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteHistorial(int id)
    {
        try
        {
            var existing = await _context.HistorialesClinicos.FindAsync(id);
            if (existing != null)
            {
                existing.Activo = false;
                _context.HistorialesClinicos.Update(existing);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Historial clínico eliminado exitosamente.";
            }
            else
            {
                TempData["ErrorMessage"] = "Registro no encontrado.";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al eliminar el historial clínico: {ex.Message}";
        }

        return RedirectToAction("Index", new { section = "historial" });
    }

    // ── CRUD Examen Visual ──
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateExamen(ExamenVisual model)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Error al registrar el examen visual.";
            TempData["ErrorMessage"] = firstError;
            return RedirectToAction("Index", new { section = "examenes" });
        }

        try
        {
            model.OptometraId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            model.FechaCreacion = DateTime.UtcNow;
            model.Fecha = DateTime.SpecifyKind(model.Fecha, DateTimeKind.Utc);
            _context.ExamenesVisuales.Add(model);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Examen visual registrado exitosamente.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al registrar el examen visual: {ex.Message}";
        }

        return RedirectToAction("Index", new { section = "examenes" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditExamen(ExamenVisual model)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Datos del examen no válidos.";
            TempData["ErrorMessage"] = firstError;
            return RedirectToAction("Index", new { section = "examenes" });
        }

        try
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
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al actualizar el examen visual: {ex.Message}";
        }

        return RedirectToAction("Index", new { section = "examenes" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteExamen(int id)
    {
        try
        {
            var existing = await _context.ExamenesVisuales.FindAsync(id);
            if (existing != null)
            {
                existing.Activo = false;
                _context.ExamenesVisuales.Update(existing);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Examen visual eliminado exitosamente.";
            }
            else
            {
                TempData["ErrorMessage"] = "Registro no encontrado.";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al eliminar el examen visual: {ex.Message}";
        }

        return RedirectToAction("Index", new { section = "examenes" });
    }

    // ── CRUD Fórmula Óptica ──
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFormula(FormulaOptica model)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Error al crear la fórmula óptica.";
            TempData["ErrorMessage"] = firstError;
            return RedirectToAction("Index", new { section = "formulas" });
        }

        try
        {
            model.OptometraId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            model.FechaCreacion = DateTime.UtcNow;
            model.Fecha = DateTime.SpecifyKind(model.Fecha, DateTimeKind.Utc);
            _context.FormulasOpticas.Add(model);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Fórmula óptica creada exitosamente.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al crear la fórmula óptica: {ex.Message}";
        }

        return RedirectToAction("Index", new { section = "formulas" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditFormula(FormulaOptica model)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Datos de la fórmula no válidos.";
            TempData["ErrorMessage"] = firstError;
            return RedirectToAction("Index", new { section = "formulas" });
        }

        try
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
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al actualizar la fórmula óptica: {ex.Message}";
        }

        return RedirectToAction("Index", new { section = "formulas" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFormula(int id)
    {
        try
        {
            var existing = await _context.FormulasOpticas.FindAsync(id);
            if (existing != null)
            {
                existing.Activo = false;
                _context.FormulasOpticas.Update(existing);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Fórmula óptica eliminada exitosamente.";
            }
            else
            {
                TempData["ErrorMessage"] = "Registro no encontrado.";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al eliminar la fórmula óptica: {ex.Message}";
        }

        return RedirectToAction("Index", new { section = "formulas" });
    }

    // ── CRUD Pacientes (Tarea 1) ──
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePaciente(PacienteFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Datos del paciente no válidos.";
            TempData["ErrorMessage"] = firstError;
            return RedirectToAction("Index", new { section = "pacientes" });
        }

        try
        {
            var existingEmail = await _context.Users.AnyAsync(u => u.Email == model.Email);
            if (existingEmail)
            {
                TempData["ErrorMessage"] = "El correo ya está registrado.";
                return RedirectToAction("Index", new { section = "pacientes" });
            }

            var existingDoc = await _context.Users.AnyAsync(u => u.NumeroDocumento == model.NumeroDocumento);
            if (existingDoc)
            {
                TempData["ErrorMessage"] = "El número de documento ya está registrado.";
                return RedirectToAction("Index", new { section = "pacientes" });
            }

            var tempPassword = "user123";
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);

            var paciente = new User
            {
                Nombre = model.Nombre.Trim(),
                Apellido = model.Apellido.Trim(),
                Email = model.Email.Trim().ToLower(),
                Telefono = model.Telefono?.Trim(),
                TipoDocumento = string.IsNullOrWhiteSpace(model.TipoDocumento) ? "CC" : model.TipoDocumento,
                NumeroDocumento = model.NumeroDocumento.Trim(),
                PasswordHash = passwordHash,
                Role = "usuario",
                FechaRegistro = DateTime.UtcNow,
                FechaNacimiento = model.FechaNacimiento.HasValue ? DateTime.SpecifyKind(model.FechaNacimiento.Value, DateTimeKind.Utc) : null,
                Genero = model.Genero,
                Direccion = model.Direccion,
                EPS = model.EPS,
                EstadoPaciente = string.IsNullOrWhiteSpace(model.EstadoPaciente) ? "Activo" : model.EstadoPaciente,
                ObservacionesPaciente = model.ObservacionesPaciente,
                Activo = true
            };

            _context.Users.Add(paciente);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Paciente creado exitosamente.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al crear el paciente: {ex.Message}";
        }

        return RedirectToAction("Index", new { section = "pacientes" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPaciente(PacienteFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Datos del paciente no válidos.";
            TempData["ErrorMessage"] = firstError;
            return RedirectToAction("Index", new { section = "pacientes" });
        }

        try
        {
            var paciente = await _context.Users.FindAsync(model.Id);
            if (paciente != null && paciente.Role == "usuario")
            {
                paciente.Nombre = model.Nombre.Trim();
                paciente.Apellido = model.Apellido.Trim();
                paciente.Email = model.Email.Trim().ToLower();
                paciente.Telefono = model.Telefono?.Trim();
                paciente.TipoDocumento = string.IsNullOrWhiteSpace(model.TipoDocumento) ? paciente.TipoDocumento : model.TipoDocumento;
                paciente.NumeroDocumento = model.NumeroDocumento.Trim();
                paciente.FechaNacimiento = model.FechaNacimiento.HasValue ? DateTime.SpecifyKind(model.FechaNacimiento.Value, DateTimeKind.Utc) : null;
                paciente.Genero = model.Genero;
                paciente.Direccion = model.Direccion;
                paciente.EPS = model.EPS;
                paciente.EstadoPaciente = model.EstadoPaciente ?? "Activo";
                paciente.ObservacionesPaciente = model.ObservacionesPaciente;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Datos del paciente actualizados exitosamente.";
            }
            else
            {
                TempData["ErrorMessage"] = "No se pudo actualizar el paciente.";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al actualizar el paciente: {ex.Message}";
        }

        return RedirectToAction("Index", new { section = "pacientes" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePaciente(int id)
    {
        try
        {
            var paciente = await _context.Users.FindAsync(id);
            if (paciente != null && paciente.Role == "usuario")
            {
                paciente.Activo = false;
                paciente.EstadoPaciente = "Inactivo";
                _context.Users.Update(paciente);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Paciente desactivado (Inactivo) exitosamente.";
            }
            else
            {
                TempData["ErrorMessage"] = "No se pudo desactivar el paciente.";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al desactivar el paciente: {ex.Message}";
        }

        return RedirectToAction("Index", new { section = "pacientes" });
    }

    // ── Citas (Tarea 2) ──
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAppointment(AppointmentFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Datos de la cita no válidos.";
            TempData["ErrorMessage"] = firstError;
            return RedirectToAction("Index", new { section = "citas" });
        }

        // 1. Validar horario laboral
        if (!Appointment.EsHorarioLaboral(model.FechaHora))
        {
            TempData["ErrorMessage"] = "Las citas solo pueden agendarse de lunes a sábado, entre 8:00 a.m. y 6:00 p.m.";
            return RedirectToAction("Index", new { section = "citas" });
        }

        var optId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // 2. Validar disponibilidad del optómetra
        if (!await Appointment.HayDisponibilidad(_context, optId, model.FechaHora))
        {
            TempData["ErrorMessage"] = "El optómetra ya tiene una cita agendada en ese horario. Por favor elige otro horario.";
            return RedirectToAction("Index", new { section = "citas" });
        }

        try
        {
            var appointment = new Appointment
            {
                UserId = model.UserId,
                OptometraId = optId,
                Servicio = model.Servicio.Trim(),
                FechaHora = DateTime.SpecifyKind(model.FechaHora, DateTimeKind.Utc),
                Notas = model.Notas?.Trim(),
                Estado = string.IsNullOrWhiteSpace(model.Estado) ? "pendiente" : model.Estado,
                FechaCreacion = DateTime.UtcNow,
                Activo = true
            };
            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cita programada exitosamente.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al programar la cita: {ex.Message}";
        }

        return RedirectToAction("Index", new { section = "citas" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAppointment(AppointmentFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Datos de la cita no válidos.";
            TempData["ErrorMessage"] = firstError;
            return RedirectToAction("Index", new { section = "citas" });
        }

        // 1. Validar horario laboral
        if (!Appointment.EsHorarioLaboral(model.FechaHora))
        {
            TempData["ErrorMessage"] = "Las citas solo pueden agendarse de lunes a sábado, entre 8:00 a.m. y 6:00 p.m.";
            return RedirectToAction("Index", new { section = "citas" });
        }

        try
        {
            var appointment = await _context.Appointments.FindAsync(model.Id);
            if (appointment != null)
            {
                // 2. Validar disponibilidad del optómetra
                var optId = appointment.OptometraId ?? int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                if (!await Appointment.HayDisponibilidad(_context, optId, model.FechaHora, appointment.Id))
                {
                    TempData["ErrorMessage"] = "El optómetra ya tiene una cita agendada en ese horario. Por favor elige otro horario.";
                    return RedirectToAction("Index", new { section = "citas" });
                }

                appointment.UserId = model.UserId;
                appointment.Servicio = model.Servicio.Trim();
                appointment.FechaHora = DateTime.SpecifyKind(model.FechaHora, DateTimeKind.Utc);
                appointment.Notas = model.Notas?.Trim();
                appointment.Estado = model.Estado;
                appointment.OptometraId = optId;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cita actualizada exitosamente.";
            }
            else
            {
                TempData["ErrorMessage"] = "No se encontró la cita a editar.";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al actualizar la cita: {ex.Message}";
        }

        return RedirectToAction("Index", new { section = "citas" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAppointment(int id)
    {
        try
        {
            var cita = await _context.Appointments.FindAsync(id);
            if (cita != null)
            {
                cita.Activo = false;
                _context.Appointments.Update(cita);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cita eliminada exitosamente.";
            }
            else
            {
                TempData["ErrorMessage"] = "Cita no encontrada.";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al eliminar la cita: {ex.Message}";
        }

        return RedirectToAction("Index", new { section = "citas" });
    }

    // ── Perfil Profesional (Tarea 6) ──
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(OptometraProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Datos del perfil no válidos.";
            TempData["ErrorMessage"] = firstError;
            return RedirectToAction("Index", new { section = "perfil" });
        }

        try
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.Nombre = model.Nombre.Trim();
                user.Apellido = model.Apellido.Trim();
                user.Email = model.Email.Trim().ToLower();
                user.Telefono = model.Telefono?.Trim();
                user.RegistroMedico = model.RegistroMedico?.Trim();
                user.Universidad = model.Universidad?.Trim();
                user.EspecialidadDetalle = model.EspecialidadDetalle?.Trim();
                user.AniosExperiencia = model.AniosExperiencia;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Perfil actualizado exitosamente.";
            }
            else
            {
                TempData["ErrorMessage"] = "No se pudo actualizar el perfil.";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al actualizar el perfil: {ex.Message}";
        }

        return RedirectToAction("Index", new { section = "perfil" });
    }

    // ── Historial Detalle (Tarea 3) ──
    public IActionResult HistorialDetalle(int id)
    {
        return RedirectToAction("Index", new { section = "historial", detalleId = id });
    }
    // ── Reporte de Citas por Estado (Parte 4) ──
    [HttpGet]
    public async Task<IActionResult> ReporteCitas(DateTime? desde, DateTime? hasta)
    {
        var optometraId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var fechaInicio = desde ?? DateTime.UtcNow.AddMonths(-1);
        var fechaFin = hasta ?? DateTime.UtcNow;

        var resultados = new List<ReporteCitasEstadoDto>();
        try
        {
            resultados = await _context.Database
                .SqlQueryRaw<ReporteCitasEstadoDto>(
                    "EXEC sp_ReporteCitasPorEstado @FechaInicio = {0}, @FechaFin = {1}, @OptometraId = {2}",
                    fechaInicio, fechaFin, optometraId)
                .ToListAsync();
        }
        catch
        {
            // Si el SP aún no existe, hacer la consulta LINQ equivalente
            var grupos = await _context.Appointments
                .Where(a => a.Activo
                         && a.OptometraId == optometraId
                         && a.FechaHora >= fechaInicio
                         && a.FechaHora <= fechaFin)
                .GroupBy(a => a.Estado)
                .Select(g => new ReporteCitasEstadoDto { Estado = g.Key, Total = g.Count() })
                .ToListAsync();
            resultados = grupos;
        }

        ViewBag.ReporteCitas = resultados;
        ViewBag.ReporteDesde = fechaInicio;
        ViewBag.ReporteHasta = fechaFin;
        return RedirectToAction("Index", new { section = "reportes" });
    }

    // ── Historial Completo del Paciente (Parte 4) ──
    [HttpGet]
    public async Task<IActionResult> HistorialCompleto(int pacienteId)
    {
        var resultados = new List<HistorialCompletoDto>();
        try
        {
            resultados = await _context.Database
                .SqlQueryRaw<HistorialCompletoDto>(
                    "EXEC sp_HistorialCompletoPaciente @PacienteId = {0}",
                    pacienteId)
                .ToListAsync();
        }
        catch
        {
            // Fallback LINQ si el SP aún no existe
            var historiales = await _context.HistorialesClinicos
                .Where(h => h.UserId == pacienteId && h.Activo)
                .Select(h => new HistorialCompletoDto
                {
                    TipoRegistro = "Historial",
                    Fecha = h.Fecha,
                    Descripcion = h.Diagnostico ?? "Sin diagnóstico",
                    Detalles = h.Observaciones
                }).ToListAsync();

            var examenes = await _context.ExamenesVisuales
                .Where(e => e.UserId == pacienteId && e.Activo)
                .Select(e => new HistorialCompletoDto
                {
                    TipoRegistro = "Examen",
                    Fecha = e.Fecha,
                    Descripcion = e.TipoExamen ?? "Examen visual",
                    Detalles = e.Resultado
                }).ToListAsync();

            var formulas = await _context.FormulasOpticas
                .Where(f => f.UserId == pacienteId && f.Activo)
                .Select(f => new HistorialCompletoDto
                {
                    TipoRegistro = "Formula",
                    Fecha = f.Fecha,
                    Descripcion = f.TipoLente ?? "Fórmula óptica",
                    Detalles = f.Observaciones
                }).ToListAsync();

            resultados = historiales
                .Concat(examenes)
                .Concat(formulas)
                .OrderByDescending(r => r.Fecha)
                .ToList();
        }

        ViewBag.HistorialCompleto = resultados;
        ViewBag.PacienteId = pacienteId;
        return RedirectToAction("Index", new { section = "historial", detalleId = pacienteId });
    }
}
