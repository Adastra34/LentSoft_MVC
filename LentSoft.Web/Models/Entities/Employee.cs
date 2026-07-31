using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LentSoft.Web.Models.Entities;

public class Employee
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es obligatorio")]
    [EmailAddress(ErrorMessage = "El email no es válido")]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Telefono { get; set; }

    [Required(ErrorMessage = "El puesto es obligatorio")]
    [StringLength(50)]
    public string Puesto { get; set; } = string.Empty;

    [Required(ErrorMessage = "El departamento es obligatorio")]
    [StringLength(50)]
    public string Departamento { get; set; } = string.Empty;

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "El salario no puede ser negativo")]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Salario { get; set; }

    public DateTime FechaContratacion { get; set; } = DateTime.UtcNow;

    public bool Activo { get; set; } = true;
}
