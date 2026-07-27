using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LentSoft.Web.Models.Entities;

public class ExamenVisual
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PacienteId { get; set; }

    [Required]
    public int OptometraId { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    [Required(ErrorMessage = "El tipo de examen es obligatorio")]
    [StringLength(100)]
    public string TipoExamen { get; set; } = string.Empty;

    [StringLength(100)]
    public string OjoDerecho { get; set; } = string.Empty;

    [StringLength(100)]
    public string OjoIzquierdo { get; set; } = string.Empty;

    [StringLength(500)]
    public string Resultado { get; set; } = string.Empty;

    // Navigation properties
    [ForeignKey(nameof(PacienteId))]
    public User Paciente { get; set; } = null!;

    [ForeignKey(nameof(OptometraId))]
    public User Optometra { get; set; } = null!;
}
