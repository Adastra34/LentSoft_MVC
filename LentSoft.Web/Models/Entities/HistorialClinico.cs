using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LentSoft.Web.Models.Entities;

public class HistorialClinico
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "El paciente es obligatorio")]
    public int UserId { get; set; }

    [Required(ErrorMessage = "La fecha es obligatoria")]
    public DateTime Fecha { get; set; }

    [Required(ErrorMessage = "El diagnóstico es obligatorio")]
    [StringLength(2000, ErrorMessage = "El diagnóstico no puede superar los 2000 caracteres")]
    public string Diagnostico { get; set; } = string.Empty;

    [Required(ErrorMessage = "El tratamiento es obligatorio")]
    [StringLength(2000, ErrorMessage = "El tratamiento no puede superar los 2000 caracteres")]
    public string Tratamiento { get; set; } = string.Empty;

    [Required]
    public int OptometraId { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(OptometraId))]
    public User Optometra { get; set; } = null!;
}
