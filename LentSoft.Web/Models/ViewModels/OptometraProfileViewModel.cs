using System.ComponentModel.DataAnnotations;

namespace LentSoft.Web.Models.ViewModels;

public class OptometraProfileViewModel
{
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El apellido debe tener entre 2 y 100 caracteres")]
    [Display(Name = "Apellido")]
    public string Apellido { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es obligatorio")]
    [EmailAddress(ErrorMessage = "Ingresa un correo electrónico válido")]
    [Display(Name = "Correo Electrónico")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [Display(Name = "Registro Médico / Licencia")]
    public string? RegistroMedico { get; set; }

    [Display(Name = "Universidad de Egreso")]
    public string? Universidad { get; set; }

    [Display(Name = "Especialidad")]
    public string? EspecialidadDetalle { get; set; }

    [Range(0, 60, ErrorMessage = "Los años de experiencia deben estar entre 0 y 60")]
    [Display(Name = "Años de Experiencia")]
    public int? AniosExperiencia { get; set; }
}
