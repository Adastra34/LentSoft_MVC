using LentSoft.Web.Models.Reviews;

namespace LentSoft.Web.Services.Reviews;

/// <summary>
/// Almacén persistente en JSON para reseñas de productos.
/// </summary>
public interface IReviewStore
{
    Task<IReadOnlyList<ProductReview>> GetByProductAsync(int productId);
    Task<IReadOnlyList<ProductReview>> GetAllAsync();
    Task<ReviewSummary> GetSummaryAsync(int productId);
    Task<Dictionary<int, ReviewSummary>> GetSummariesAsync(IEnumerable<int> productIds);
    Task<ProductReview?> GetByIdAsync(Guid reviewId);
    Task<ProductReview?> GetByUserAndProductAsync(string userId, int productId);
    Task<(bool Success, string? ErrorMessage, ProductReview? Review)> AddAsync(ProductReview review, bool isAdmin);
    Task<(bool Success, string? ErrorMessage, ProductReview? Review)> UpdateAsync(Guid reviewId, string userId, bool isAdmin, int rating, string comment);
    Task<(bool Success, string? ErrorMessage)> DeleteAsync(Guid reviewId, string userId, bool isAdmin);
}
