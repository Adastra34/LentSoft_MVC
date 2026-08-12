using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LentSoft.Web.Models.Entities;

public class PurchaseOrderItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PurchaseOrderId { get; set; }

    [Required]
    public int ProductId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad solicitada debe ser mayor a 0")]
    public int CantidadSolicitada { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "La cantidad recibida no puede ser negativa")]
    public int CantidadRecibida { get; set; } = 0;

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "El costo unitario no puede ser negativo")]
    [Column(TypeName = "decimal(10,2)")]
    public decimal CostoUnitario { get; set; }

    // Computed property (not mapped to DB)
    [NotMapped]
    public decimal Subtotal => CantidadSolicitada * CostoUnitario;

    // Navigation properties
    [ForeignKey(nameof(PurchaseOrderId))]
    public PurchaseOrder? PurchaseOrder { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }
}
