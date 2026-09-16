using System.ComponentModel.DataAnnotations;
using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Models.ViewModels;

public class CheckoutViewModel
{
    public Cart? Cart { get; set; }

    [Required(ErrorMessage = "El número de tarjeta es obligatorio")]
    [StringLength(19, MinimumLength = 13, ErrorMessage = "El número de tarjeta debe tener entre 13 y 19 dígitos")]
    public string NumeroTarjeta { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre en la tarjeta es obligatorio")]
    [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres")]
    public string NombreTarjeta { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha de expiración es obligatoria")]
    [RegularExpression(@"^(0[1-9]|1[0-2])\/[0-9]{2}$", ErrorMessage = "La fecha debe estar en formato MM/AA")]
    public string Expiracion { get; set; } = string.Empty;

    [Required(ErrorMessage = "El código de seguridad (CVV) es obligatorio")]
    [RegularExpression(@"^[0-9]{3,4}$", ErrorMessage = "El CVV debe tener 3 o 4 dígitos")]
    public string Cvv { get; set; } = string.Empty;

    [Required(ErrorMessage = "La dirección de envío es obligatoria")]
    [StringLength(500, ErrorMessage = "La dirección no puede exceder los 500 caracteres")]
    public string DireccionEnvio { get; set; } = string.Empty;
}
