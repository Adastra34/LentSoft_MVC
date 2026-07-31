using System.ComponentModel.DataAnnotations;

namespace LentSoft.Web.Models.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "El tipo de documento es obligatorio")]
    [Display(Name = "Tipo de Documento")]
    public string TipoDocumento { get; set; } = "CC";

    [Required(ErrorMessage = "El número de documento es obligatorio")]
    [StringLength(30, MinimumLength = 5, ErrorMessage = "El número de documento debe tener entre 5 y 30 caracteres")]
    [Display(Name = "Número de Documento")]
    public string NumeroDocumento { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El apellido debe tener entre 2 y 100 caracteres")]
    [Display(Name = "Apellido")]
    public string Apellido { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono es obligatorio")]
    [Phone(ErrorMessage = "Formato de teléfono inválido")]
    [Display(Name = "Número de Teléfono")]
    public string Telefono { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es obligatorio")]
    [EmailAddress(ErrorMessage = "Formato de email inválido")]
    [Display(Name = "Correo Electrónico")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirma tu contraseña")]
    [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar Contraseña")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
