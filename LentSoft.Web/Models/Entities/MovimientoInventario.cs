using System.ComponentModel.DataAnnotations;

namespace LentSoft.Web.Models.Entities;

/// <summary>
/// Movimiento de inventario (antes era MovimientoInventarioMock — ahora entidad real en BD)
/// </summary>
public class MovimientoInventario
{
    [Key]
    public int Id { get; set; }

    /// <summary>Nombre del producto involucrado (string libre para histórico)</summary>
    [Required(ErrorMessage = "El producto es obligatorio")]
    [StringLength(200)]
    public string Producto { get; set; } = string.Empty;

    /// <summary>"entrada" o "salida"</summary>
    [Required(ErrorMessage = "El tipo es obligatorio")]
    [StringLength(20)]
    public string Tipo { get; set; } = "entrada";

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
    public int Cantidad { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    /// <summary>Nombre de la empresa/persona responsable del movimiento</summary>
    [Required(ErrorMessage = "El responsable es obligatorio")]
    [StringLength(200)]
    public string Responsable { get; set; } = string.Empty;
}
