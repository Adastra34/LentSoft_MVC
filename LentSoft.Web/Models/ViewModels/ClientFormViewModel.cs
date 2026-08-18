using System.ComponentModel.DataAnnotations;

namespace LentSoft.Web.Models.ViewModels;

public class ClientFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El apellido debe tener entre 2 y 100 caracteres")]
    [Display(Name = "Apellido")]
    public string Apellido { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es obligatorio")]
    [EmailAddress(ErrorMessage = "Ingresa un correo electrónico válido (ejemplo: usuario@dominio.com)")]
    [Display(Name = "Correo Electrónico")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [Required(ErrorMessage = "El tipo de documento es obligatorio")]
    [Display(Name = "Tipo de Documento")]
    public string TipoDocumento { get; set; } = "CC";

    [Required(ErrorMessage = "El número de documento es obligatorio")]
    [Display(Name = "Número de Documento")]
    public string NumeroDocumento { get; set; } = string.Empty;

    [Display(Name = "Contraseña")]
    public string? Password { get; set; }
}
