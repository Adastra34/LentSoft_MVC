using System.ComponentModel.DataAnnotations;

namespace LentSoft.Web.Models.DTOs;

public class RegistrarClienteRapidoDTO
{
    [Required(ErrorMessage = "El tipo de documento es obligatorio")]
    [RegularExpression(@"^(CC|CE|TI|Pasaporte)$", ErrorMessage = "Tipo de documento no válido")]
    public string TipoDocumento { get; set; } = "CC";

    [Required(ErrorMessage = "El número de documento es obligatorio")]
    [StringLength(20, MinimumLength = 5, ErrorMessage = "El número de documento debe tener entre 5 y 20 caracteres")]
    [RegularExpression(@"^[a-zA-Z0-9]{5,20}$", ErrorMessage = "El número de documento solo debe contener letras o números (mínimo 5 caracteres, sin espacios)")]
    public string NumeroDocumento { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
    [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s'-]+$", ErrorMessage = "El nombre solo debe contener letras (no se permiten números ni símbolos)")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El apellido debe tener entre 2 y 100 caracteres")]
    [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s'-]+$", ErrorMessage = "El apellido solo debe contener letras (no se permiten números ni símbolos)")]
    public string Apellido { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono es obligatorio")]
    [StringLength(15, MinimumLength = 7, ErrorMessage = "El teléfono debe tener entre 7 y 15 dígitos")]
    [RegularExpression(@"^[+]?[0-9\s-]{7,15}$", ErrorMessage = "El número de teléfono debe tener un formato válido (solo dígitos)")]
    public string Telefono { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es obligatorio")]
    [EmailAddress(ErrorMessage = "Ingresa un correo electrónico válido (ejemplo: usuario@dominio.com)")]
    [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Ingresa un formato de correo electrónico válido")]
    public string Email { get; set; } = string.Empty;
}

