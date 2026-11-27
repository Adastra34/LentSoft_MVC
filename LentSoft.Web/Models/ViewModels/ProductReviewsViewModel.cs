using LentSoft.Web.Models.Reviews;

namespace LentSoft.Web.Models.ViewModels;

public class ProductReviewsViewModel
{
    public int ProductId { get; set; }
    public ReviewSummary Summary { get; set; } = new();
    public IReadOnlyList<ProductReview> Reviews { get; set; } = Array.Empty<ProductReview>();
    public bool IsAuthenticated { get; set; }
    public bool IsAdmin { get; set; }
    public string? CurrentUserId { get; set; }
    public ProductReview? CurrentUserReview { get; set; }
}
