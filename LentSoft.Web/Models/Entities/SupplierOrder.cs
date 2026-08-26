using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LentSoft.Web.Models.Entities;

/// <summary>
/// Modelo de pedidos a proveedores vinculado a la DB de Inventario (Supplier y Product)
/// </summary>
public class SupplierOrder
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "El número de pedido es obligatorio")]
    [StringLength(50)]
    public string NumeroPedido { get; set; } = string.Empty;

    [Required(ErrorMessage = "El proveedor es obligatorio")]
    [StringLength(20)]
    public string SupplierId { get; set; } = string.Empty;

    [Required(ErrorMessage = "El producto de inventario es obligatorio")]
    public int ProductId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser al menos 1")]
    public int Cantidad { get; set; } = 1;

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "El precio unitario no puede ser negativo")]
    [Column(TypeName = "decimal(10,2)")]
    public decimal PrecioUnitario { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "El total no puede ser negativo")]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Total { get; set; }

    [Required]
    [StringLength(30)]
    public string Estado { get; set; } = "pendiente"; // "pendiente", "recibido", "cancelado"

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    [StringLength(500)]
    public string? Notas { get; set; }

    public bool Activo { get; set; } = true;

    // Navigation properties
    [ForeignKey(nameof(SupplierId))]
    public Supplier? Supplier { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }
}
