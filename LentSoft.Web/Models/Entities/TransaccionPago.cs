using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace LentSoft.Web.Models.Entities;

public class TransaccionPago
{
    [Key]
    public int Id { get; set; }

    public int? VentaId { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Monto { get; set; }

    [Required]
    [StringLength(30)]
    public string Estado { get; set; } = "Aprobada"; // Aprobada, Rechazada, Pendiente

    [StringLength(50)]
    public string? CodigoAutorizacion { get; set; }

    [StringLength(10)]
    public string? UltimosDigitosTarjeta { get; set; }

    [StringLength(50)]
    public string? MarcaTarjeta { get; set; } // Visa, Mastercard, Amex, etc.

    [StringLength(250)]
    public string? MensajeRespuesta { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    // Navigation Property
    [ForeignKey(nameof(VentaId))]
    [ValidateNever]
    public Order? Venta { get; set; }
}
