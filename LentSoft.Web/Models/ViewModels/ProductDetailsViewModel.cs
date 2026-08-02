using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Models.ViewModels;

public class ProductDetailsViewModel
{
    public Product Product { get; set; } = null!;
    public bool IsFavorite { get; set; }
    public bool IsAuthenticated { get; set; }
}
