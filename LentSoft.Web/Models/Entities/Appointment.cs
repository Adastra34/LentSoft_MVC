using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LentSoft.Web.Models.Entities;

public class Appointment : IValidatableObject
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

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

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

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
