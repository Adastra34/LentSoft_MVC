using System.ComponentModel.DataAnnotations;

namespace LentSoft.Web.Models.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "El tipo de documento es obligatorio")]
    [RegularExpression(@"^(CC|CE|TI|Pasaporte)$", ErrorMessage = "Tipo de documento no válido")]
    [Display(Name = "Tipo de Documento")]
    public string TipoDocumento { get; set; } = "CC";

    [Required(ErrorMessage = "El número de documento es obligatorio")]
    [StringLength(20, MinimumLength = 5, ErrorMessage = "El número de documento debe tener entre 5 y 20 caracteres")]
    [RegularExpression(@"^[a-zA-Z0-9]{5,20}$", ErrorMessage = "El número de documento solo debe contener letras o números (mínimo 5 caracteres, sin espacios)")]
    [Display(Name = "Número de Documento")]
    public string NumeroDocumento { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
    [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s'-]+$", ErrorMessage = "El nombre solo debe contener letras (no se permiten números ni símbolos)")]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El apellido debe tener entre 2 y 100 caracteres")]
    [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s'-]+$", ErrorMessage = "El apellido solo debe contener letras (no se permiten números ni símbolos)")]
    [Display(Name = "Apellido")]
    public string Apellido { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono es obligatorio")]
    [StringLength(15, MinimumLength = 7, ErrorMessage = "El teléfono debe tener entre 7 y 15 dígitos")]
    [RegularExpression(@"^[+]?[0-9\s-]{7,15}$", ErrorMessage = "El número de teléfono debe tener un formato válido (solo dígitos)")]
    [Display(Name = "Número de Teléfono")]
    public string Telefono { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es obligatorio")]
    [EmailAddress(ErrorMessage = "Ingresa un correo electrónico válido (ejemplo: usuario@dominio.com)")]
    [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Ingresa un formato de correo electrónico válido")]
    [Display(Name = "Correo Electrónico")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$", 
        ErrorMessage = "La contraseña debe contener al menos una mayúscula, una minúscula, un número y un carácter especial (ej. @, $, !, %, *, ?, &).")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirma tu contraseña")]
    [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar Contraseña")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
