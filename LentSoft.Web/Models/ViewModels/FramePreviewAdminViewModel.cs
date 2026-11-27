using LentSoft.Web.Models.Entities;
using LentSoft.Web.Models.FrameModels;

namespace LentSoft.Web.Models.ViewModels;

public class FramePreviewAdminViewModel
{
    public IReadOnlyList<FramePreviewAdminItem> Items { get; set; } = Array.Empty<FramePreviewAdminItem>();
    public int? FeaturedProductId { get; set; }
}

public class FramePreviewAdminItem
{
    public Product Product { get; set; } = null!;
    public FrameModelEntry? ModelEntry { get; set; }

    public string StatusLabel => ModelEntry != null && !string.IsNullOrWhiteSpace(ModelEntry.ModelPath)
        ? (ModelEntry.ModelType.Equals("glb", StringComparison.OrdinalIgnoreCase) ? "Modelo 3D" : "Imagen 2D")
        : (!string.IsNullOrWhiteSpace(Product.ImagenOverlayUrl) ? "Imagen 2D (Catálogo)" : "Sin modelo");

    public string StatusCode => ModelEntry != null && !string.IsNullOrWhiteSpace(ModelEntry.ModelPath)
        ? (ModelEntry.ModelType.Equals("glb", StringComparison.OrdinalIgnoreCase) ? "glb" : "png")
        : (!string.IsNullOrWhiteSpace(Product.ImagenOverlayUrl) ? "png" : "none");
}
