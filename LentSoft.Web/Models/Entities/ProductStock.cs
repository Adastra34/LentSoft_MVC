using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LentSoft.Web.Models.Entities;

public class ProductStock
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProductId { get; set; }

    [Required]
    public int WarehouseId { get; set; }

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "La cantidad no puede ser negativa")]
    public int Cantidad { get; set; } = 0;

    // Navigation properties
    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }

    [ForeignKey(nameof(WarehouseId))]
    public Warehouse? Warehouse { get; set; }
}
