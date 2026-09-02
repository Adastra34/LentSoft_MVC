using System.ComponentModel.DataAnnotations;

namespace LentSoft.Web.Models.Entities;

public class InventoryMovement
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "El producto es obligatorio")]
    public int ProductId { get; set; }

    public Product? Product { get; set; }

    [StringLength(200)]
    public string? NombreProducto { get; set; }

    [Required(ErrorMessage = "El tipo de movimiento es obligatorio")]
    [StringLength(20)]
    public string Tipo { get; set; } = "Entrada"; // "Entrada" o "Salida"

    [Required(ErrorMessage = "La cantidad es obligatoria")]
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
    public int Cantidad { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    [StringLength(100)]
    public string? Responsable { get; set; }
}
