using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace LentSoft.Web.Models.Entities;

public class Invoice : IValidatableObject
{
    [Key]
    public int Id { get; set; }

    [StringLength(50)]
    [ValidateNever]
    public string NumeroFactura { get; set; } = string.Empty;

    [Required]
    public int OrderId { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Subtotal { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Impuestos { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Total { get; set; }

    [Required]
    [StringLength(20)]
    public string Estado { get; set; } = "pendiente";

    public DateTime FechaEmision { get; set; } = DateTime.UtcNow;

    public DateTime? FechaPago { get; set; }

    [StringLength(50)]
    public string? MetodoPago { get; set; }

    public bool Activo { get; set; } = true;

    // Campos Formato DIAN
    [StringLength(50)]
    public string ResolucionDianNumero { get; set; } = "18764028920000";

    [StringLength(50)]
    public string RangoAutorizadoDesde { get; set; } = "FAC-2026-0001";

    [StringLength(50)]
    public string RangoAutorizadoHasta { get; set; } = "FAC-2026-9999";

    [StringLength(100)]
    public string? CUFE { get; set; }

    // Navigation properties
    [ForeignKey(nameof(OrderId))]
    [ValidateNever]
    public Order? Order { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validEstados = new[] { "pendiente", "pagada", "cancelada" };
        if (!validEstados.Contains(Estado?.ToLower()))
        {
            yield return new ValidationResult(
                "El estado de factura debe ser: pendiente, pagada o cancelada",
                new[] { nameof(Estado) });
        }
    }
}
