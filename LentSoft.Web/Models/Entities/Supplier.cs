using System.ComponentModel.DataAnnotations;

namespace LentSoft.Web.Models.Entities;

public class Supplier
{
    [Key]
    [StringLength(20)]
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre de la empresa/proveedor es obligatorio")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El tipo de productos es obligatorio")]
    [StringLength(100, ErrorMessage = "El tipo de productos no puede superar los 100 caracteres")]
    public string TipoProductos { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono es obligatorio")]
    [Phone(ErrorMessage = "El formato de teléfono no es válido")]
    [StringLength(20)]
    public string Telefono { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es obligatorio")]
    [EmailAddress(ErrorMessage = "El correo electrónico no es válido")]
    [StringLength(100)]
    public string Correo { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;

    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
}
