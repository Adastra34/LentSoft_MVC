using System.ComponentModel.DataAnnotations;

namespace LentSoft.Web.Models.ViewModels;

public class UserProfileViewModel
{
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100, MinimumLength = 3)]
    [Display(Name = "Nombre Completo")]
    public string Nombre { get; set; } = string.Empty;

    [EmailAddress]
    [Display(Name = "Correo Electrónico")]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }
}
