using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LentSoft.Web.Models.FrameModels;
using LentSoft.Web.Models.ViewModels;
using LentSoft.Web.Services;
using LentSoft.Web.Services.FrameModels;

namespace LentSoft.Web.Controllers;

[Authorize(Roles = "admin")]
public class FramePreviewAdminController : Controller
{
    private readonly IProductService _productService;
    private readonly IFrameModelStore _frameModelStore;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FramePreviewAdminController> _logger;

    private const long MaxGlbBytes = 15L * 1024 * 1024; // 15 MB
    private const long MaxPngBytes = 5L * 1024 * 1024;  // 5 MB

    private static readonly byte[] GlbMagic = { 0x67, 0x6C, 0x54, 0x46 }; // "glTF"
    private static readonly byte[] PngMagic = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    public FramePreviewAdminController(
        IProductService productService,
        IFrameModelStore frameModelStore,
        IWebHostEnvironment env,
        ILogger<FramePreviewAdminController> logger)
    {
        _productService = productService;
        _frameModelStore = frameModelStore;
        _env = env;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var gafas = await _productService.GetGafasAsync();
        var manifest = await _frameModelStore.GetManifestAsync();
        var byProduct = manifest.Models.ToDictionary(m => m.ProductId, m => m);

        var vm = new FramePreviewAdminViewModel
        {
            FeaturedProductId = manifest.FeaturedProductId,
            Items = gafas.Select(g => new FramePreviewAdminItem
            {
                Product = g,
                ModelEntry = byProduct.TryGetValue(g.Id, out var entry) ? entry : null
            }).ToList()
        };

        return View("~/Views/Dashboard/PrevisualizacionMarcos.cshtml", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(16 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 16 * 1024 * 1024)]
    public async Task<IActionResult> UploadModel(
        int productId,
        IFormFile? modelFile,
        double scale = 1.0,
        double offsetX = 0.0,
        double offsetY = 0.0,
        double offsetZ = 0.0,
        double rotationX = 0.0,
        double rotationY = 0.0,
        double rotationZ = 0.0)
    {
        var product = await _productService.GetByIdAsync(productId);
        if (product == null)
        {
            return NotFound(new { success = false, message = "Producto no encontrado." });
        }

        if (modelFile == null || modelFile.Length == 0)
        {
            return BadRequest(new { success = false, message = "Selecciona un archivo .glb o .png válido." });
        }

        var ext = Path.GetExtension(modelFile.FileName)?.ToLowerInvariant();
        if (ext != ".glb" && ext != ".png")
        {
            return BadRequest(new { success = false, message = "Formato no permitido. Solo se aceptan archivos .glb (3D) o .png (2D)." });
        }

        var maxBytes = ext == ".glb" ? MaxGlbBytes : MaxPngBytes;
        if (modelFile.Length > maxBytes)
        {
            return BadRequest(new { success = false, message = $"El archivo excede el tamaño máximo permitido ({(ext == ".glb" ? "15 MB" : "5 MB")})." });
        }

        try
        {
            using var ms = new MemoryStream();
            await modelFile.CopyToAsync(ms);
            var bytes = ms.ToArray();

            if (!ValidateMagicBytes(ext, bytes))
            {
                return BadRequest(new
                {
                    success = false,
                    message = ext == ".glb"
                        ? "El archivo no corresponde a un modelo GLB binario válido (cabecera glTF inválida)."
                        : "El archivo no corresponde a una imagen PNG válida."
                });
            }

            var modelsDir = Path.Combine(_env.WebRootPath, "models", "frames");
            Directory.CreateDirectory(modelsDir);

            var safeFileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(modelsDir, safeFileName);
            await System.IO.File.WriteAllBytesAsync(fullPath, bytes);

            var existing = await _frameModelStore.GetByProductAsync(productId);
            var oldPath = existing?.ModelPath;

            var entry = new FrameModelEntry
            {
                ProductId = productId,
                ModelPath = $"/models/frames/{safeFileName}",
                ModelType = ext.TrimStart('.'),
                Scale = Clamp(scale, 0.1, 5.0),
                OffsetX = Clamp(offsetX, -2.0, 2.0),
                OffsetY = Clamp(offsetY, -2.0, 2.0),
                OffsetZ = Clamp(offsetZ, -2.0, 2.0),
                RotationX = Clamp(rotationX, -180.0, 180.0),
                RotationY = Clamp(rotationY, -180.0, 180.0),
                RotationZ = Clamp(rotationZ, -180.0, 180.0)
            };

            await _frameModelStore.SaveModelAsync(entry);

            // Eliminar archivo antiguo si ya no está referenciado
            if (!string.IsNullOrWhiteSpace(oldPath) && !string.Equals(oldPath, entry.ModelPath, StringComparison.OrdinalIgnoreCase))
            {
                await TryDeleteUnusedFrameFileAsync(oldPath);
            }

            return Json(new { success = true, message = "Modelo subido correctamente.", entry });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al subir modelo de montura para producto {ProductId}", productId);
            return StatusCode(500, new { success = false, message = "No se pudo guardar el archivo del modelo." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAdjustments(
        int productId,
        double scale = 1.0,
        double offsetX = 0.0,
        double offsetY = 0.0,
        double offsetZ = 0.0,
        double rotationX = 0.0,
        double rotationY = 0.0,
        double rotationZ = 0.0)
    {
        var product = await _productService.GetByIdAsync(productId);
        if (product == null)
        {
            return NotFound(new { success = false, message = "Producto no encontrado." });
        }

        try
        {
            var existing = await _frameModelStore.GetByProductAsync(productId);
            var entry = existing ?? new FrameModelEntry
            {
                ProductId = productId,
                ModelPath = product.ImagenOverlayUrl ?? string.Empty,
                ModelType = "png"
            };

            entry.Scale = Clamp(scale, 0.1, 5.0);
            entry.OffsetX = Clamp(offsetX, -2.0, 2.0);
            entry.OffsetY = Clamp(offsetY, -2.0, 2.0);
            entry.OffsetZ = Clamp(offsetZ, -2.0, 2.0);
            entry.RotationX = Clamp(rotationX, -180.0, 180.0);
            entry.RotationY = Clamp(rotationY, -180.0, 180.0);
            entry.RotationZ = Clamp(rotationZ, -180.0, 180.0);

            await _frameModelStore.SaveModelAsync(entry);
            return Json(new { success = true, message = "Ajustes guardados correctamente.", entry });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar ajustes de previsualización para producto {ProductId}", productId);
            return StatusCode(500, new { success = false, message = "No se pudieron guardar los ajustes." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteModel(int productId)
    {
        try
        {
            var existing = await _frameModelStore.GetByProductAsync(productId);
            var deleted = await _frameModelStore.DeleteModelAsync(productId);
            if (deleted && existing != null && !string.IsNullOrWhiteSpace(existing.ModelPath))
            {
                await TryDeleteUnusedFrameFileAsync(existing.ModelPath);
            }

            return Json(new { success = true, message = "Modelo eliminado. El producto volverá al respaldo 2D por defecto." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar modelo de montura para producto {ProductId}", productId);
            return StatusCode(500, new { success = false, message = "No se pudo eliminar el modelo." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetFeatured(int productId)
    {
        var product = await _productService.GetByIdAsync(productId);
        if (product == null)
        {
            return NotFound(new { success = false, message = "Producto no encontrado." });
        }

        try
        {
            await _frameModelStore.SetFeaturedProductIdAsync(productId);
            return Json(new { success = true, message = $"\"{product.Nombre}\" marcada como gafa destacada en la página de inicio.", featuredProductId = productId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al marcar producto {ProductId} como destacado en inicio", productId);
            return StatusCode(500, new { success = false, message = "No se pudo actualizar la gafa destacada." });
        }
    }

    private static bool ValidateMagicBytes(string ext, byte[] bytes)
    {
        if (ext == ".glb")
        {
            // Cabecera GLB v2: 12 bytes mínimos + magic "glTF"
            if (bytes.Length < 20) return false;
            return bytes.Take(4).SequenceEqual(GlbMagic);
        }

        if (ext == ".png")
        {
            if (bytes.Length < 16) return false;
            return bytes.Take(8).SequenceEqual(PngMagic);
        }

        return false;
    }

    private static double Clamp(double value, double min, double max)
    {
        if (double.IsNaN(value) || double.IsInfinity(value)) return min;
        return Math.Max(min, Math.Min(max, value));
    }

    private async Task TryDeleteUnusedFrameFileAsync(string relativePath)
    {
        if (!relativePath.StartsWith("/models/frames/", StringComparison.OrdinalIgnoreCase))
            return;

        var manifest = await _frameModelStore.GetManifestAsync();
        if (manifest.Models.Any(m => string.Equals(m.ModelPath, relativePath, StringComparison.OrdinalIgnoreCase)))
            return;

        var safeName = Path.GetFileName(relativePath);
        var fullPath = Path.Combine(_env.WebRootPath, "models", "frames", safeName);
        if (System.IO.File.Exists(fullPath))
        {
            try
            {
                System.IO.File.Delete(fullPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo eliminar archivo antiguo {FilePath}", fullPath);
            }
        }
    }
}
