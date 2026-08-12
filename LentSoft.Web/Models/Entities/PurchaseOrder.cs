using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LentSoft.Web.Models.Entities;

public class PurchaseOrder : IValidatableObject
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "El proveedor es obligatorio")]
    [StringLength(20)]
    public string SupplierId { get; set; } = string.Empty;

    public DateTime FechaPedido { get; set; } = DateTime.UtcNow;

    public DateTime? FechaEstimadaEntrega { get; set; }

    [Required]
    [StringLength(20)]
    public string Estado { get; set; } = "Pendiente"; // "Pendiente", "Parcial", "Recibido", "Cancelado"

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "El total no puede ser negativo")]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Total { get; set; }

    public bool Activo { get; set; } = true;

    // Navigation properties
    [ForeignKey(nameof(SupplierId))]
    public Supplier? Supplier { get; set; }

    public ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validEstados = new[] { "Pendiente", "Parcial", "Recibido", "Cancelado" };
        if (!validEstados.Contains(Estado, StringComparer.OrdinalIgnoreCase))
        {
            yield return new ValidationResult(
                "El estado debe ser: Pendiente, Parcial, Recibido o Cancelado",
                new[] { nameof(Estado) });
        }
    }
}
