using System.ComponentModel.DataAnnotations;

namespace LentSoft.Web.Models.ViewModels;

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "La contraseña actual es obligatoria")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña Actual")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nueva contraseña es obligatoria")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
    [DataType(DataType.Password)]
    [Display(Name = "Nueva Contraseña")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirma tu nueva contraseña")]
    [Compare("NewPassword", ErrorMessage = "Las contraseñas no coinciden")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar Nueva Contraseña")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
