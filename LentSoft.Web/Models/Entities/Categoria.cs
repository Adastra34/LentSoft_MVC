using System.ComponentModel.DataAnnotations;

namespace LentSoft.Web.Models.Entities;

/// <summary>
/// Categoría de producto (Gafas, Lentes, Accesorios)
/// </summary>
public class Categoria
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    // Navigation
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
