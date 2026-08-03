using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LentSoft.Web.Models.Entities;

public class Invoice : IValidatableObject
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string NumeroFactura { get; set; } = string.Empty;

    [Required]
    public int OrderId { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Subtotal { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Impuestos { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Total { get; set; }

    [Required]
    [StringLength(20)]
    public string Estado { get; set; } = "pendiente";

    public DateTime FechaEmision { get; set; } = DateTime.UtcNow;

    public DateTime? FechaPago { get; set; }

    [StringLength(50)]
    public string? MetodoPago { get; set; }

    public bool Activo { get; set; } = true;

    // Navigation properties
    [ForeignKey(nameof(OrderId))]
    public Order Order { get; set; } = null!;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validEstados = new[] { "pendiente", "pagada", "cancelada" };
        if (!validEstados.Contains(Estado))
        {
            yield return new ValidationResult(
                "El estado de factura debe ser: pendiente, pagada o cancelada",
                new[] { nameof(Estado) });
        }
    }
}
