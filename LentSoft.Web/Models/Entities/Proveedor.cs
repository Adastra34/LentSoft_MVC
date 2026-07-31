using System.ComponentModel.DataAnnotations;

namespace LentSoft.Web.Models.Entities;

/// <summary>
/// Proveedor de productos (antes era ProveedorMock — ahora entidad real en BD)
/// </summary>
public class Proveedor
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre del proveedor es obligatorio")]
    [StringLength(200)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Contacto { get; set; }

    [StringLength(50)]
    public string? Telefono { get; set; }

    [StringLength(200)]
    [EmailAddress]
    public string? Email { get; set; }

    /// <summary>Tipo de producto que suministra (ej. "Monturas", "Lentes de contacto")</summary>
    [StringLength(100)]
    public string? TipoProducto { get; set; }

    [StringLength(20)]
    public string Estado { get; set; } = "activo";

    /// <summary>Ruta relativa al logo, ej. /img/proveedores/logo.png</summary>
    [StringLength(500)]
    public string? LogoUrl { get; set; }

    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
}
