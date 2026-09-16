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

    [StringLength(100)]
    public string? Contacto { get; set; }

    [Required(ErrorMessage = "El tipo de productos es obligatorio")]
    [StringLength(100, ErrorMessage = "El tipo de productos no puede superar los 100 caracteres")]
    public string TipoProductos { get; set; } = string.Empty;

    public string TipoProducto
    {
        get => TipoProductos;
        set => TipoProductos = value;
    }

    [Required(ErrorMessage = "El teléfono es obligatorio")]
    [Phone(ErrorMessage = "El formato de teléfono no es válido")]
    [StringLength(20)]
    public string Telefono { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es obligatorio")]
    [EmailAddress(ErrorMessage = "El correo electrónico no es válido")]
    [StringLength(100)]
    public string Correo { get; set; } = string.Empty;

    public string Email
    {
        get => Correo;
        set => Correo = value;
    }

    [StringLength(500)]
    public string? LogoUrl { get; set; }

    public bool Activo { get; set; } = true;

    public string Estado
    {
        get => Activo ? "Activo" : "Inactivo";
        set => Activo = string.Equals(value, "Activo", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
}
