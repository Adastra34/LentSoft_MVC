using System.Text.Json;
using LentSoft.Web.Models.Reviews;

namespace LentSoft.Web.Services.Reviews;

/// <summary>
/// Implementación thread-safe de IReviewStore respaldada por App_Data/reviews.json.
/// </summary>
public class JsonReviewStore : IReviewStore
{
    private readonly string _filePath;
    private readonly ILogger<JsonReviewStore> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private const int MinCommentLength = 10;
    private const int MaxCommentLength = 1000;
    private const int MaxReviewsPerWindow = 5;
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(10);

    public JsonReviewStore(IWebHostEnvironment env, ILogger<JsonReviewStore> logger)
        : this(Path.Combine(env.ContentRootPath, "App_Data", "reviews.json"), logger)
    {
    }

    public JsonReviewStore(string filePath, ILogger<JsonReviewStore> logger)
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
            File.WriteAllText(_filePath, "[]");
        }
    }

    private async Task<List<ProductReview>> ReadAllInternalAsync()
    {
        EnsureStorageExists();
        try
        {
            var json = await File.ReadAllTextAsync(_filePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<ProductReview>();
            }

            var items = JsonSerializer.Deserialize<List<ProductReview>>(json, JsonOptions);
            return items ?? new List<ProductReview>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al leer o deserializar {FilePath}. Se devolverá una lista vacía para evitar caída de la aplicación.", _filePath);
            return new List<ProductReview>();
        }
    }

    private async Task WriteAllInternalAsync(List<ProductReview> reviews)
    {
        EnsureStorageExists();
        var tmpPath = $"{_filePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            var json = JsonSerializer.Serialize(reviews, JsonOptions);
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

    private static (bool Valid, string? Error, string NormalizedComment) ValidateInput(int rating, string? comment)
    {
        if (rating < 1 || rating > 5)
        {
            return (false, "La calificación debe estar entre 1 y 5 estrellas.", string.Empty);
        }

        var normalized = (comment ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length < MinCommentLength)
        {
            return (false, $"El comentario debe tener al menos {MinCommentLength} caracteres.", string.Empty);
        }

        if (normalized.Length > MaxCommentLength)
        {
            return (false, $"El comentario no puede superar los {MaxCommentLength} caracteres.", string.Empty);
        }

        return (true, null, normalized);
    }

    private static ReviewSummary BuildSummary(int productId, IEnumerable<ProductReview> productReviews)
    {
        var list = productReviews.ToList();
        var summary = new ReviewSummary
        {
            ProductoId = productId,
            TotalReviews = list.Count
        };

        if (list.Count > 0)
        {
            summary.AverageRating = Math.Round(list.Average(r => r.Rating), 1, MidpointRounding.AwayFromZero);
            foreach (var r in list)
            {
                if (r.Rating >= 1 && r.Rating <= 5)
                {
                    summary.Distribution[r.Rating]++;
                }
            }
        }

        return summary;
    }

    public async Task<IReadOnlyList<ProductReview>> GetByProductAsync(int productId)
    {
        await _semaphore.WaitAsync();
        try
        {
            var all = await ReadAllInternalAsync();
            return all
                .Where(r => r.ProductoId == productId)
                .OrderByDescending(r => r.CreatedAtUtc)
                .ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IReadOnlyList<ProductReview>> GetAllAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            var all = await ReadAllInternalAsync();
            return all
                .OrderByDescending(r => r.CreatedAtUtc)
                .ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<ReviewSummary> GetSummaryAsync(int productId)
    {
        await _semaphore.WaitAsync();
        try
        {
            var all = await ReadAllInternalAsync();
            return BuildSummary(productId, all.Where(r => r.ProductoId == productId));
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<Dictionary<int, ReviewSummary>> GetSummariesAsync(IEnumerable<int> productIds)
    {
        var idSet = productIds.Distinct().ToHashSet();
        await _semaphore.WaitAsync();
        try
        {
            var all = await ReadAllInternalAsync();
            var grouped = all
                .Where(r => idSet.Contains(r.ProductoId))
                .GroupBy(r => r.ProductoId)
                .ToDictionary(g => g.Key, g => BuildSummary(g.Key, g));

            foreach (var id in idSet)
            {
                if (!grouped.ContainsKey(id))
                {
                    grouped[id] = new ReviewSummary { ProductoId = id };
                }
            }

            return grouped;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<ProductReview?> GetByIdAsync(Guid reviewId)
    {
        await _semaphore.WaitAsync();
        try
        {
            var all = await ReadAllInternalAsync();
            return all.FirstOrDefault(r => r.Id == reviewId);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<ProductReview?> GetByUserAndProductAsync(string userId, int productId)
    {
        await _semaphore.WaitAsync();
        try
        {
            var all = await ReadAllInternalAsync();
            return all
                .Where(r => r.ProductoId == productId && r.UserId == userId)
                .OrderByDescending(r => r.CreatedAtUtc)
                .FirstOrDefault();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<(bool Success, string? ErrorMessage, ProductReview? Review)> AddAsync(ProductReview review, bool isAdmin)
    {
        var validation = ValidateInput(review.Rating, review.Comment);
        if (!validation.Valid)
        {
            return (false, validation.Error, null);
        }

        await _semaphore.WaitAsync();
        try
        {
            var all = await ReadAllInternalAsync();
            var now = DateTime.UtcNow;

            // Control de frecuencia (máximo 5 reseñas por usuario cada 10 minutos)
            var recentCount = all.Count(r =>
                r.UserId == review.UserId &&
                r.CreatedAtUtc >= now.Subtract(RateLimitWindow));

            if (recentCount >= MaxReviewsPerWindow)
            {
                return (false, "Has alcanzado el límite temporal de reseñas. Intenta nuevamente en unos minutos.", null);
            }

            // Un usuario normal solo puede publicar una reseña por producto
            if (!isAdmin && all.Any(r => r.ProductoId == review.ProductoId && r.UserId == review.UserId))
            {
                return (false, "Ya publicaste una reseña para este producto. Puedes editarla o eliminarla.", null);
            }

            var newReview = new ProductReview
            {
                Id = review.Id == Guid.Empty ? Guid.NewGuid() : review.Id,
                ProductoId = review.ProductoId,
                UserId = review.UserId,
                UserName = string.IsNullOrWhiteSpace(review.UserName) ? "Cliente LentSoft" : review.UserName.Trim(),
                Rating = review.Rating,
                Comment = validation.NormalizedComment,
                CreatedAtUtc = now,
                UpdatedAtUtc = null,
                IsAdminReview = isAdmin
            };

            all.Add(newReview);
            await WriteAllInternalAsync(all);
            return (true, null, newReview);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<(bool Success, string? ErrorMessage, ProductReview? Review)> UpdateAsync(Guid reviewId, string userId, bool isAdmin, int rating, string comment)
    {
        var validation = ValidateInput(rating, comment);
        if (!validation.Valid)
        {
            return (false, validation.Error, null);
        }

        await _semaphore.WaitAsync();
        try
        {
            var all = await ReadAllInternalAsync();
            var existing = all.FirstOrDefault(r => r.Id == reviewId);
            if (existing == null)
            {
                return (false, "La reseña no fue encontrada.", null);
            }

            if (existing.UserId != userId && !isAdmin)
            {
                return (false, "No tienes permiso para editar esta reseña.", null);
            }

            existing.Rating = rating;
            existing.Comment = validation.NormalizedComment;
            existing.UpdatedAtUtc = DateTime.UtcNow;

            await WriteAllInternalAsync(all);
            return (true, null, existing);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteAsync(Guid reviewId, string userId, bool isAdmin)
    {
        await _semaphore.WaitAsync();
        try
        {
            var all = await ReadAllInternalAsync();
            var existing = all.FirstOrDefault(r => r.Id == reviewId);
            if (existing == null)
            {
                return (false, "La reseña no fue encontrada.");
            }

            if (existing.UserId != userId && !isAdmin)
            {
                return (false, "No tienes permiso para eliminar esta reseña.");
            }

            all.Remove(existing);
            await WriteAllInternalAsync(all);
            return (true, null);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
