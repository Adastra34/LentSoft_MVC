using System.ComponentModel.DataAnnotations;

namespace LentSoft.Web.Models.ViewModels;

public class UserProfileViewModel
{
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100, MinimumLength = 3)]
    [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s'-]+$", ErrorMessage = "El nombre solo puede contener letras y espacios (no se permiten números)")]
    [Display(Name = "Nombre Completo")]
    public string Nombre { get; set; } = string.Empty;

    [EmailAddress]
    [Display(Name = "Correo Electrónico")]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [RegularExpression(@"^[+]?[0-9\s-]{7,15}$", ErrorMessage = "Formato de teléfono inválido")]
    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }
}
