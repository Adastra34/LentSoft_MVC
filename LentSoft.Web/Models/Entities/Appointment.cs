using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LentSoft.Web.Models.Entities;

public class Appointment : IValidatableObject
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    public int? OptometraId { get; set; }

    [Required(ErrorMessage = "El servicio es obligatorio")]
    [StringLength(100)]
    public string Servicio { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha y hora son obligatorias")]
    public DateTime FechaHora { get; set; }

    [Required]
    [StringLength(20)]
    public string Estado { get; set; } = "pendiente";

    [StringLength(500)]
    public string? Notas { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public bool Activo { get; set; } = true;

    public int VecesReprogramada { get; set; } = 0;

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(OptometraId))]
    public User? Optometra { get; set; }

    // --- Horario Laboral Config & Validations ---
    public static readonly DayOfWeek[] DiasLaborales = new[]
    {
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday,
        DayOfWeek.Saturday
    };

    public const int HoraInicio = 8;
    public const int HoraFin = 18;
    public const int DuracionCitaMinutos = 60;

    public static bool EsHorarioLaboral(DateTime fechaHora)
    {
        // El día de la semana no sea Domingo
        if (!DiasLaborales.Contains(fechaHora.DayOfWeek))
        {
            return false;
        }

        // La hora esté entre las 8:00 y las 18:00
        var time = fechaHora.TimeOfDay;
        if (time < new TimeSpan(HoraInicio, 0, 0) || time > new TimeSpan(HoraFin, 0, 0))
        {
            return false;
        }

        return true;
    }

    public static async System.Threading.Tasks.Task<bool> HayDisponibilidad(
        DbContext context,
        int optometraId,
        DateTime fechaHora,
        int? excluirCitaId = null)
    {
        var duracion = TimeSpan.FromMinutes(DuracionCitaMinutos);
        var inicioNueva = fechaHora;
        var finNueva = fechaHora.Add(duracion);

        var appointmentsDbSet = context.Set<Appointment>();

        var citasConflicto = await appointmentsDbSet
            .Where(a => a.Activo 
                     && a.OptometraId == optometraId 
                     && a.Estado != "cancelada"
                     && (!excluirCitaId.HasValue || a.Id != excluirCitaId.Value))
            .ToListAsync();

        foreach (var cita in citasConflicto)
        {
            var inicioExistente = cita.FechaHora;
            var finExistente = cita.FechaHora.Add(duracion);

            if (inicioNueva < finExistente && inicioExistente < finNueva)
            {
                return false;
            }
        }

        return true;
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validEstados = new[] { "pendiente", "confirmada", "completada", "cancelada" };
        if (!validEstados.Contains(Estado))
        {
            yield return new ValidationResult(
                "El estado de la cita debe ser: pendiente, confirmada, completada o cancelada",
                new[] { nameof(Estado) });
        }
    }
}
