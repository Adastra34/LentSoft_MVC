using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LentSoft.Web.Models.Entities;

public class HistorialClinico
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PacienteId { get; set; }

    [Required]
    public int OptometraId { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    [Required(ErrorMessage = "El diagnóstico es obligatorio")]
    [StringLength(500)]
    public string Diagnostico { get; set; } = string.Empty;

    [StringLength(500)]
    public string Tratamiento { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Observaciones { get; set; }

    // Navigation properties
    [ForeignKey(nameof(PacienteId))]
    public User Paciente { get; set; } = null!;

    [ForeignKey(nameof(OptometraId))]
    public User Optometra { get; set; } = null!;
}
