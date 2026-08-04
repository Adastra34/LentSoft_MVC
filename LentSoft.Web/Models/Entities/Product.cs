using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LentSoft.Web.Models.Entities;

public class Product : IValidatableObject
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre del producto es obligatorio")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 200 caracteres")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Descripcion { get; set; }

    [Required(ErrorMessage = "El precio es obligatorio")]
    [Range(0, double.MaxValue, ErrorMessage = "El precio no puede ser negativo")]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Precio { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? PrecioDescuento { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El costo de compra no puede ser negativo")]
    [Column(TypeName = "decimal(10,2)")]
    public decimal CostoCompra { get; set; } = 0.00m;

    [Required(ErrorMessage = "La categoría es obligatoria")]
    [StringLength(50)]
    public string Categoria { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Marca { get; set; }

    [NotMapped]
    public int Stock => ProductStocks?.Sum(ps => ps.Cantidad) ?? 0;

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "El stock mínimo no puede ser negativo")]
    public int StockMinimo { get; set; } = 5;

    [StringLength(500)]
    public string? ImagenUrl { get; set; }

    public bool Activo { get; set; } = true;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "decimal(3,2)")]
    public decimal Rating { get; set; } = 4.8m;

    public int ReviewCount { get; set; } = 12;

    public bool EsDestacado { get; set; } = false;

    [StringLength(100)]
    public string? Material { get; set; }

    [StringLength(100)]
    public string? Color { get; set; }

    [StringLength(100)]
    public string? Proteccion { get; set; }

    [StringLength(100)]
    public string? Estilo { get; set; }

    [StringLength(100)]
    public string? Tamanio { get; set; }

    [StringLength(255)]
    public string? ImagenOverlayUrl { get; set; }

    // Navigation properties
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();
    public ICollection<ProductStock> ProductStocks { get; set; } = new List<ProductStock>();

    /// <summary>
    /// Calcular porcentaje de descuento
    /// </summary>
    public int GetDiscountPercentage()
    {
        if (PrecioDescuento == null || PrecioDescuento >= Precio || Precio == 0)
            return 0;

        return (int)Math.Round(((Precio - PrecioDescuento.Value) / Precio) * 100);
    }

    /// <summary>
    /// Calcular porcentaje de margen de ganancia respecto al precio de venta final
    /// </summary>
    public int GetMargenPorcentaje()
    {
        var finalPrice = GetFinalPrice();
        if (CostoCompra <= 0 || finalPrice <= 0 || finalPrice <= CostoCompra)
            return 0;

        return (int)Math.Round(((finalPrice - CostoCompra) / finalPrice) * 100);
    }

    /// <summary>
    /// Obtener precio final con descuento si aplica
    /// </summary>
    public decimal GetFinalPrice()
    {
        return PrecioDescuento.HasValue && PrecioDescuento.Value < Precio
            ? PrecioDescuento.Value
            : Precio;
    }

    /// <summary>
    /// Verificar si hay stock disponible
    /// </summary>
    public bool HasStock(int quantity = 1)
    {
        return Stock >= quantity;
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PrecioDescuento.HasValue && PrecioDescuento.Value >= Precio)
        {
            yield return new ValidationResult(
                "El precio de descuento debe ser menor al precio regular",
                new[] { nameof(PrecioDescuento) });
        }
    }
}
