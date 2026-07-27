using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LentSoft.Web.Models.Entities;

public class FormulaOptica
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PacienteId { get; set; }

    [Required]
    public int OptometraId { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    [StringLength(20)]
    public string EsferaOD { get; set; } = "0.00";

    [StringLength(20)]
    public string CilindroOD { get; set; } = "0.00";

    [StringLength(20)]
    public string EjeOD { get; set; } = "0°";

    [StringLength(20)]
    public string EsferaOI { get; set; } = "0.00";

    [StringLength(20)]
    public string CilindroOI { get; set; } = "0.00";

    [StringLength(20)]
    public string EjeOI { get; set; } = "0°";

    [StringLength(20)]
    public string? Adicion { get; set; }

    [StringLength(20)]
    public string? DistanciaPupilar { get; set; }

    [StringLength(500)]
    public string? Observaciones { get; set; }

    // Navigation properties
    [ForeignKey(nameof(PacienteId))]
    public User Paciente { get; set; } = null!;

    [ForeignKey(nameof(OptometraId))]
    public User Optometra { get; set; } = null!;
}
