using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LentSoft.Web.Models.Entities;

/// <summary>
/// Registro de auditoría para cambios de estado en Appointments.
/// Poblada por el trigger trg_Appointment_Auditoria.
/// </summary>
public class AuditoriaCita
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int AppointmentId { get; set; }

    [Required]
    [StringLength(20)]
    public string EstadoAnterior { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string EstadoNuevo { get; set; } = string.Empty;

    public DateTime FechaCambio { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey(nameof(AppointmentId))]
    public Appointment Appointment { get; set; } = null!;
}
