using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LentSoft.Web.Models.Entities;

/// <summary>
/// Registro de auditoría para cambios de estado en pedidos, facturas y abonos.
/// </summary>
public class AuditoriaVenta
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string TipoEntidad { get; set; } = string.Empty; // "Pedido", "Factura", "PagoVenta", "SalesOrder"

    [Required]
    public int EntidadId { get; set; }

    [StringLength(50)]
    public string? EstadoAnterior { get; set; }

    [StringLength(50)]
    public string? EstadoNuevo { get; set; }

    [Required]
    [StringLength(100)]
    public string Accion { get; set; } = string.Empty; // "CambioEstado", "Creacion", "AbonoRegistrado", "Cancelacion"

    [StringLength(150)]
    public string Usuario { get; set; } = "Sistema";

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    [StringLength(500)]
    public string? Detalles { get; set; }
}
