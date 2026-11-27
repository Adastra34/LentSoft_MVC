using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LentSoft.Web.Models.Entities;

public class ProductImage
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }

    [Required]
    [StringLength(500)]
    public string ImagenUrl { get; set; } = string.Empty;

    public int Orden { get; set; } = 0;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
