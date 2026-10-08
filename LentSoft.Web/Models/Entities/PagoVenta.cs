using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace LentSoft.Web.Models.Entities;

public class PagoVenta
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int VentaId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto abonado debe ser mayor a 0.")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Monto { get; set; }

    public DateTime FechaPago { get; set; } = DateTime.UtcNow;

    [Required]
    [StringLength(50)]
    public string MetodoPago { get; set; } = "Efectivo";

    [Required]
    [StringLength(100)]
    public string Responsable { get; set; } = "Ventas";

    [Required]
    [StringLength(50)]
    public string NumeroComprobante { get; set; } = string.Empty;

    // Navigation Property
    [ForeignKey(nameof(VentaId))]
    [ValidateNever]
    public Order? Venta { get; set; }
}
