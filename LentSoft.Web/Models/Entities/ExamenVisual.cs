using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LentSoft.Web.Models.Entities;

public class ExamenVisual
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "El paciente es obligatorio")]
    public int UserId { get; set; }

    [Required(ErrorMessage = "La fecha es obligatoria")]
    public DateTime Fecha { get; set; }

    [Required(ErrorMessage = "El tipo de examen es obligatorio")]
    [StringLength(200, ErrorMessage = "El tipo de examen no puede superar los 200 caracteres")]
    public string TipoExamen { get; set; } = string.Empty;

    [Required(ErrorMessage = "La medición del ojo derecho es obligatoria")]
    [StringLength(500, ErrorMessage = "La medición no puede superar los 500 caracteres")]
    public string OjoDerecho { get; set; } = string.Empty;

    [Required(ErrorMessage = "La medición del ojo izquierdo es obligatoria")]
    [StringLength(500, ErrorMessage = "La medición no puede superar los 500 caracteres")]
    public string OjoIzquierdo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El resultado es obligatorio")]
    [StringLength(1000, ErrorMessage = "El resultado no puede superar los 1000 caracteres")]
    public string Resultado { get; set; } = string.Empty;

    [StringLength(100)]
    public string? TonometriaOD { get; set; }

    [StringLength(100)]
    public string? TonometriaOI { get; set; }

    [StringLength(50)]
    public string? EsferaOD { get; set; }

    [StringLength(50)]
    public string? CilindroOD { get; set; }

    [StringLength(50)]
    public string? EjeOD { get; set; }

    [StringLength(50)]
    public string? AdicionOD { get; set; }

    [StringLength(50)]
    public string? EsferaOI { get; set; }

    [StringLength(50)]
    public string? CilindroOI { get; set; }

    [StringLength(50)]
    public string? EjeOI { get; set; }

    [StringLength(50)]
    public string? AdicionOI { get; set; }

    [StringLength(1000)]
    public string? SegmentoAnterior { get; set; }

    [StringLength(1000)]
    public string? SegmentoPosterior { get; set; }

    [Required(ErrorMessage = "El diagnóstico es obligatorio")]
    [StringLength(1000, ErrorMessage = "El diagnóstico no puede superar los 1000 caracteres")]
    public string Diagnostico { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Tratamiento { get; set; }

    [StringLength(1000)]
    public string? Observaciones { get; set; }

    [Required]
    public int OptometraId { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(OptometraId))]
    public User Optometra { get; set; } = null!;
}
