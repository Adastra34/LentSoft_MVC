using System.ComponentModel.DataAnnotations;

namespace LentSoft.Web.Models.ViewModels;

public class EmployeeFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
    [Display(Name = "Nombre completo")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es obligatorio")]
    [EmailAddress(ErrorMessage = "Ingresa un correo electrónico válido (ejemplo: usuario@dominio.com)")]
    [Display(Name = "Correo Electrónico")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [Required(ErrorMessage = "El puesto es obligatorio")]
    [Display(Name = "Puesto / Cargo")]
    public string Puesto { get; set; } = "Empleado";

    [Required(ErrorMessage = "El departamento es obligatorio")]
    [Display(Name = "Departamento")]
    public string Departamento { get; set; } = "General";

    [Range(0, double.MaxValue, ErrorMessage = "El salario debe ser mayor o igual a 0")]
    [Display(Name = "Salario")]
    public decimal Salario { get; set; }

    [Required(ErrorMessage = "El rol es obligatorio")]
    [Display(Name = "Rol en el Sistema")]
    public string Rol { get; set; } = "Trabajador";

    public bool Activo { get; set; } = true;
}
