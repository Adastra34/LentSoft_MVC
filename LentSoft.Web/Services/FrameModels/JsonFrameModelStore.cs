using System.Text.Json;
using LentSoft.Web.Models.FrameModels;

namespace LentSoft.Web.Services.FrameModels;

/// <summary>
/// Implementación thread-safe de IFrameModelStore respaldada por App_Data/frame-models.json.
/// </summary>
public class JsonFrameModelStore : IFrameModelStore
{
    private readonly string _filePath;
    private readonly ILogger<JsonFrameModelStore> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public JsonFrameModelStore(IWebHostEnvironment env, ILogger<JsonFrameModelStore> logger)
        : this(Path.Combine(env.ContentRootPath, "App_Data", "frame-models.json"), logger)
    {
    }

    public JsonFrameModelStore(string filePath, ILogger<JsonFrameModelStore> logger)
    {
        _filePath = filePath;
        _logger = logger;
        EnsureStorageExists();
    }

    private void EnsureStorageExists()
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        if (!File.Exists(_filePath))
        {
            File.WriteAllText(_filePath, "{\"featuredProductId\":null,\"models\":[]}");
        }
    }

    private async Task<FrameModelsManifest> ReadInternalAsync()
    {
        EnsureStorageExists();
        try
        {
            var json = await File.ReadAllTextAsync(_filePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new FrameModelsManifest();
            }

            var manifest = JsonSerializer.Deserialize<FrameModelsManifest>(json, JsonOptions);
            manifest ??= new FrameModelsManifest();
            manifest.Models ??= new List<FrameModelEntry>();
            return manifest;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al leer {FilePath}. Se devolverá un manifiesto vacío.", _filePath);
            return new FrameModelsManifest();
        }
    }

    private async Task WriteInternalAsync(FrameModelsManifest manifest)
    {
        EnsureStorageExists();
        var tmpPath = $"{_filePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            var json = JsonSerializer.Serialize(manifest, JsonOptions);
            await File.WriteAllTextAsync(tmpPath, json);
            File.Move(tmpPath, _filePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tmpPath))
            {
                try { File.Delete(tmpPath); } catch { /* ignorar limpieza */ }
            }
        }
    }

    public async Task<FrameModelsManifest> GetManifestAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            return await ReadInternalAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<FrameModelEntry?> GetByProductAsync(int productId)
    {
        await _semaphore.WaitAsync();
        try
        {
            var manifest = await ReadInternalAsync();
            return manifest.Models.FirstOrDefault(m => m.ProductId == productId);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<int?> GetFeaturedProductIdAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            var manifest = await ReadInternalAsync();
            return manifest.FeaturedProductId;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task SetFeaturedProductIdAsync(int? productId)
    {
        await _semaphore.WaitAsync();
        try
        {
            var manifest = await ReadInternalAsync();
            manifest.FeaturedProductId = productId;
            await WriteInternalAsync(manifest);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<FrameModelEntry> SaveModelAsync(FrameModelEntry entry)
    {
        await _semaphore.WaitAsync();
        try
        {
            var manifest = await ReadInternalAsync();
            entry.UpdatedAtUtc = DateTime.UtcNow;

            var idx = manifest.Models.FindIndex(m => m.ProductId == entry.ProductId);
            if (idx >= 0)
            {
                manifest.Models[idx] = entry;
            }
            else
            {
                manifest.Models.Add(entry);
            }

            await WriteInternalAsync(manifest);
            return entry;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> DeleteModelAsync(int productId)
    {
        await _semaphore.WaitAsync();
        try
        {
            var manifest = await ReadInternalAsync();
            var removed = manifest.Models.RemoveAll(m => m.ProductId == productId) > 0;
            if (removed)
            {
                await WriteInternalAsync(manifest);
            }
            return removed;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
