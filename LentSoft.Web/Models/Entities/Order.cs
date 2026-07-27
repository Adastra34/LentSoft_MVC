using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LentSoft.Web.Models.Entities;

public class Order : IValidatableObject
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "El pedido debe estar asociado a un usuario")]
    public int UserId { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "El total no puede ser negativo")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Total { get; set; }

    [Required]
    [StringLength(20)]
    public string Estado { get; set; } = "pendiente";

    [StringLength(500)]
    public string? DireccionEnvio { get; set; }

    public DateTime FechaPedido { get; set; } = DateTime.UtcNow;

    public DateTime? FechaEntrega { get; set; }

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validEstados = new[] { "pendiente", "procesando", "enviado", "entregado", "cancelado" };
        if (!validEstados.Contains(Estado))
        {
            yield return new ValidationResult(
                "El estado debe ser: pendiente, procesando, enviado, entregado o cancelado",
                new[] { nameof(Estado) });
        }
    }
}
