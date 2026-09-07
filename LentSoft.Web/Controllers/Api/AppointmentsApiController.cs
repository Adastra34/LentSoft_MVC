using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Controllers.Api;

[ApiController]
[Route("api/appointments")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class AppointmentsApiController : ControllerBase
{
    private readonly LentSoftDbContext _context;

    public AppointmentsApiController(LentSoftDbContext context)
    {
        _context = context;
    }

    private int GetCurrentUserId()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idStr, out var id) ? id : 0;
    }

    [HttpGet]
    public async Task<IActionResult> GetAppointments()
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var now = DateTime.UtcNow;

        var allAppointments = await _context.Appointments
            .Include(a => a.Optometra)
            .Where(a => a.UserId == userId && a.Activo)
            .OrderByDescending(a => a.FechaHora)
            .ToListAsync();

        var proximaCitaEntity = allAppointments
            .Where(a => a.FechaHora >= now && 
                        !a.Estado.Equals("cancelada", StringComparison.OrdinalIgnoreCase) && 
                        !a.Estado.Equals("completada", StringComparison.OrdinalIgnoreCase))
            .OrderBy(a => a.FechaHora)
            .FirstOrDefault();

        var proximaCita = proximaCitaEntity == null ? null : new
        {
            proximaCitaEntity.Id,
            proximaCitaEntity.Servicio,
            proximaCitaEntity.FechaHora,
            Estado = char.ToUpper(proximaCitaEntity.Estado[0]) + proximaCitaEntity.Estado.Substring(1).ToLower(),
            EstadoRaw = proximaCitaEntity.Estado.ToLower(),
            proximaCitaEntity.Notas,
            OptometraNombre = proximaCitaEntity.Optometra != null 
                ? $"Dr(a). {proximaCitaEntity.Optometra.NombreCompleto}" 
                : "Optómetra de turno"
        };

        var historial = allAppointments
            .Where(a => proximaCitaEntity == null || a.Id != proximaCitaEntity.Id)
            .Select(a => new
            {
                a.Id,
                a.Servicio,
                a.FechaHora,
                Estado = a.Estado.Equals("completada", StringComparison.OrdinalIgnoreCase) 
                    ? "Asistió" 
                    : (char.ToUpper(a.Estado[0]) + a.Estado.Substring(1).ToLower()),
                EstadoRaw = a.Estado.ToLower(),
                a.Notas,
                OptometraNombre = a.Optometra != null 
                    ? $"Dr(a). {a.Optometra.NombreCompleto}" 
                    : "Optómetra de turno"
            })
            .ToList();

        return Ok(new
        {
            ProximaCita = proximaCita,
            Historial = historial
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentApiRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        DateTime fechaLocal;
        DateTime ahoraLocal;
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
            ahoraLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            fechaLocal = request.FechaHora.Kind == DateTimeKind.Utc
                ? TimeZoneInfo.ConvertTimeFromUtc(request.FechaHora, tz)
                : request.FechaHora;
        }
        catch
        {
            ahoraLocal = DateTime.Now;
            fechaLocal = request.FechaHora;
        }

        if (fechaLocal <= ahoraLocal)
        {
            return BadRequest(new { message = "La fecha y hora de la cita debe ser a futuro." });
        }

        // 1. Validar horario laboral en hora local (Colombia)
        if (!Appointment.EsHorarioLaboral(fechaLocal))
        {
            return BadRequest(new { message = "Las citas solo pueden agendarse de lunes a sábado, entre 8:00 a.m. y 6:00 p.m." });
        }

        try
        {
            // 2. Buscar primer optómetra disponible
            var optometras = await _context.Users
                .Where(u => u.Role == "optometra" && u.Activo)
                .ToListAsync();

            User? optDisponible = null;
            foreach (var opt in optometras)
            {
                if (await Appointment.HayDisponibilidad(_context, opt.Id, fechaLocal))
                {
                    optDisponible = opt;
                    break;
                }
            }

            if (optDisponible == null)
            {
                return BadRequest(new { message = "No hay optómetras disponibles en ese horario. Por favor elige otro horario." });
            }

            var appointment = new Appointment
            {
                UserId = userId,
                OptometraId = optDisponible.Id,
                Servicio = request.Servicio.Trim(),
                FechaHora = fechaLocal,
                Notas = request.Notas?.Trim(),
                Estado = "pendiente",
                FechaCreacion = DateTime.UtcNow,
                Activo = true
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                appointment.Id,
                appointment.Servicio,
                appointment.FechaHora,
                Estado = "Pendiente",
                EstadoRaw = "pendiente",
                appointment.Notas,
                OptometraNombre = $"Dr(a). {optDisponible.NombreCompleto}",
                Message = "Cita agendada exitosamente."
            });
        }
        catch (DbUpdateException dbEx)
        {
            var msg = dbEx.InnerException?.Message ?? dbEx.Message;
            return BadRequest(new { message = msg });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Error al agendar la cita: {ex.Message}" });
        }
    }

    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> CancelAppointment(int id)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        try
        {
            var cita = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
            if (cita == null)
            {
                return NotFound(new { message = "Cita no encontrada o no pertenece a tu usuario." });
            }

            if (cita.Estado != "pendiente" && cita.Estado != "confirmada")
            {
                return BadRequest(new { message = "Solo puedes cancelar citas pendientes o confirmadas." });
            }

            cita.Estado = "cancelada";
            await _context.SaveChangesAsync();

            return Ok(new { message = "La cita ha sido cancelada exitosamente." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Error al cancelar la cita: {ex.Message}" });
        }
    }

    [HttpPut("{id}/reschedule")]
    public async Task<IActionResult> RescheduleAppointment(int id, [FromBody] RescheduleAppointmentApiRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        DateTime fechaLocal;
        DateTime ahoraLocal;
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
            ahoraLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            fechaLocal = request.NuevaFechaHora.Kind == DateTimeKind.Utc
                ? TimeZoneInfo.ConvertTimeFromUtc(request.NuevaFechaHora, tz)
                : request.NuevaFechaHora;
        }
        catch
        {
            ahoraLocal = DateTime.Now;
            fechaLocal = request.NuevaFechaHora;
        }

        if (fechaLocal <= ahoraLocal)
        {
            return BadRequest(new { message = "La nueva fecha y hora debe ser a futuro." });
        }

        if (!Appointment.EsHorarioLaboral(fechaLocal))
        {
            return BadRequest(new { message = "Las citas solo pueden agendarse de lunes a sábado, entre 8:00 a.m. y 6:00 p.m." });
        }

        try
        {
            var cita = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
            if (cita == null)
            {
                return NotFound(new { message = "Cita no encontrada o no pertenece a tu usuario." });
            }

            if (cita.Estado == "completada" || cita.Estado == "cancelada")
            {
                return BadRequest(new { message = "No se puede reprogramar una cita completada o cancelada." });
            }

            if (cita.VecesReprogramada >= 1)
            {
                return BadRequest(new { message = "Esta cita ya fue reprogramada una vez. Si necesitas otro cambio, por favor cancela la cita y agenda una nueva." });
            }

            var optometras = await _context.Users.Where(u => u.Role == "optometra" && u.Activo).ToListAsync();
            User? optDisponible = null;

            if (cita.OptometraId.HasValue)
            {
                var currentOpt = optometras.FirstOrDefault(o => o.Id == cita.OptometraId.Value);
                if (currentOpt != null && await Appointment.HayDisponibilidad(_context, currentOpt.Id, fechaLocal, cita.Id))
                {
                    optDisponible = currentOpt;
                }
            }

            if (optDisponible == null)
            {
                foreach (var opt in optometras)
                {
                    if (await Appointment.HayDisponibilidad(_context, opt.Id, fechaLocal, cita.Id))
                    {
                        optDisponible = opt;
                        break;
                    }
                }
            }

            if (optDisponible == null)
            {
                return BadRequest(new { message = "No hay optómetras disponibles en ese horario. Por favor elige otro horario." });
            }

            cita.FechaHora = fechaLocal;
            cita.OptometraId = optDisponible.Id;
            cita.VecesReprogramada += 1;
            cita.Estado = "pendiente";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                cita.Id,
                cita.Servicio,
                cita.FechaHora,
                Estado = "Pendiente",
                EstadoRaw = "pendiente",
                OptometraNombre = $"Dr(a). {optDisponible.NombreCompleto}",
                Message = "La cita ha sido reprogramada exitosamente."
            });
        }
        catch (DbUpdateException dbEx)
        {
            var msg = dbEx.InnerException?.Message ?? dbEx.Message;
            return BadRequest(new { message = msg });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Error al reprogramar la cita: {ex.Message}" });
        }
    }
}

public class CreateAppointmentApiRequest
{
    [Required(ErrorMessage = "El servicio es obligatorio")]
    public string Servicio { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha y hora son obligatorias")]
    public DateTime FechaHora { get; set; }

    public string? Notas { get; set; }
}

public class RescheduleAppointmentApiRequest
{
    [Required(ErrorMessage = "La nueva fecha y hora son obligatorias")]
    public DateTime NuevaFechaHora { get; set; }
}
