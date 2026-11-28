namespace LentSoft.Web.Models.FrameModels;

/// <summary>
/// Manifiesto raíz guardado en App_Data/frame-models.json (sin entidad EF).
/// </summary>
public class FrameModelsManifest
{
    public int? FeaturedProductId { get; set; }
    public List<FrameModelEntry> Models { get; set; } = new();
}

/// <summary>
/// Configuración de modelo 3D (.glb) o 2D (.png) asociada a un producto.
/// </summary>
public class FrameModelEntry
{
    public int ProductId { get; set; }
    public string ModelPath { get; set; } = string.Empty;
    public string ModelType { get; set; } = "png"; // "glb" o "png"
    public double Scale { get; set; } = 1.0;
    public double OffsetX { get; set; } = 0.0;
    public double OffsetY { get; set; } = 0.0;
    public double OffsetZ { get; set; } = 0.0;
    public double RotationX { get; set; } = 0.0;
    public double RotationY { get; set; } = 0.0;
    public double RotationZ { get; set; } = 0.0;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
