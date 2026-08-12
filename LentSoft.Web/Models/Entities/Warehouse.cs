using System.ComponentModel.DataAnnotations;

namespace LentSoft.Web.Models.Entities;

public class Warehouse
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre de la bodega es obligatorio")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Direccion { get; set; }

    public bool Activo { get; set; } = true;

    // Navigation property
    public ICollection<ProductStock> ProductStocks { get; set; } = new List<ProductStock>();
}
