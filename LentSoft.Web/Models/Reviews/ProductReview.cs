namespace LentSoft.Web.Models.Reviews;

/// <summary>
/// Representa una reseña de cliente o administrador almacenada en JSON (sin entidad EF).
/// </summary>
public class ProductReview
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int ProductoId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public bool IsAdminReview { get; set; }
}

/// <summary>
/// Resumen estadístico de calificaciones de un producto.
/// </summary>
public class ReviewSummary
{
    public int ProductoId { get; set; }
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public Dictionary<int, int> Distribution { get; set; } = new()
    {
        { 5, 0 },
        { 4, 0 },
        { 3, 0 },
        { 2, 0 },
        { 1, 0 }
    };
}
