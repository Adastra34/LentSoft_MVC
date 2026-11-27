using LentSoft.Web.Models.FrameModels;

namespace LentSoft.Web.Services.FrameModels;

/// <summary>
/// Almacén persistente en JSON para modelos 3D/2D de monturas y producto destacado.
/// </summary>
public interface IFrameModelStore
{
    Task<FrameModelsManifest> GetManifestAsync();
    Task<FrameModelEntry?> GetByProductAsync(int productId);
    Task<int?> GetFeaturedProductIdAsync();
    Task SetFeaturedProductIdAsync(int? productId);
    Task<FrameModelEntry> SaveModelAsync(FrameModelEntry entry);
    Task<bool> DeleteModelAsync(int productId);
}
