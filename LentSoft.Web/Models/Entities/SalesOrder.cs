using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LentSoft.Web.Models.Entities;

/// <summary>
/// Modelo independiente para pedidos de venta a clientes (no vinculado a la DB de inventario)
/// </summary>
public class SalesOrder
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "El número de pedido es obligatorio")]
    [StringLength(50)]
    public string NumeroPedido { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre del cliente es obligatorio")]
    [StringLength(150)]
    public string ClienteNombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El producto es obligatorio")]
    [StringLength(200)]
    public string ProductoNombre { get; set; } = string.Empty;

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
    public string Estado { get; set; } = "pendiente"; // "pendiente", "enviado", "entregado", "cancelado"

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    [StringLength(500)]
    public string? Notas { get; set; }

    public bool Activo { get; set; } = true;
}
