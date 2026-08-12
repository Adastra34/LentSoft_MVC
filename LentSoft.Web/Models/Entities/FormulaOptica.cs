using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LentSoft.Web.Models.Entities;

public class FormulaOptica
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "El paciente es obligatorio")]
    public int UserId { get; set; }

    [Required(ErrorMessage = "La fecha es obligatoria")]
    public DateTime Fecha { get; set; }

    [Required(ErrorMessage = "La esfera para el ojo derecho es obligatoria")]
    [StringLength(50)]
    public string EsferaOD { get; set; } = string.Empty;

    [Required(ErrorMessage = "El cilindro para el ojo derecho es obligatorio")]
    [StringLength(50)]
    public string CilindroOD { get; set; } = string.Empty;

    [Required(ErrorMessage = "El eje para el ojo derecho es obligatorio")]
    [StringLength(50)]
    public string EjeOD { get; set; } = string.Empty;

    [Required(ErrorMessage = "La esfera para el ojo izquierdo es obligatoria")]
    [StringLength(50)]
    public string EsferaOI { get; set; } = string.Empty;

    [Required(ErrorMessage = "El cilindro para el ojo izquierdo es obligatorio")]
    [StringLength(50)]
    public string CilindroOI { get; set; } = string.Empty;

    [Required(ErrorMessage = "El eje para el ojo izquierdo es obligatorio")]
    [StringLength(50)]
    public string EjeOI { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Observaciones { get; set; }

    [Required(ErrorMessage = "El tipo de lente es obligatorio")]
    [StringLength(100)]
    public string TipoLente { get; set; } = string.Empty;

    [StringLength(50)]
    public string? DistanciaPupilar { get; set; }

    [NotMapped]
    public string Estado => Fecha.AddMonths(12).Date >= DateTime.UtcNow.Date ? "Vigente" : "Vencida";

    [Required]
    public int OptometraId { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public bool Activo { get; set; } = true;

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(OptometraId))]
    public User Optometra { get; set; } = null!;
}
