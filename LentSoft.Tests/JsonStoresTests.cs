using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using LentSoft.Web.Models.FrameModels;
using LentSoft.Web.Models.Reviews;
using LentSoft.Web.Services.FrameModels;
using LentSoft.Web.Services.Reviews;
using Xunit;

namespace LentSoft.Tests;

public class JsonStoresTests : IDisposable
{
    private readonly string _tempDir;

    public JsonStoresTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"lentsoft_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }

    [Fact]
    public async Task JsonReviewStore_CrudYReglasNegocioFuncionan()
    {
        var path = Path.Combine(_tempDir, "reviews.json");
        var store = new JsonReviewStore(path, NullLogger<JsonReviewStore>.Instance);

        // Agregar reseña válida de usuario normal
        var r1 = await store.AddAsync(new ProductReview
        {
            ProductoId = 10,
            UserId = "1",
            UserName = "Ana",
            Rating = 5,
            Comment = "Excelente montura, muy liviana y cómoda."
        }, isAdmin: false);
        Assert.True(r1.Success);
        Assert.NotNull(r1.Review);

        // Usuario normal no puede duplicar reseña en el mismo producto
        var dup = await store.AddAsync(new ProductReview
        {
            ProductoId = 10,
            UserId = "1",
            UserName = "Ana",
            Rating = 4,
            Comment = "Intentando duplicar reseña en el mismo producto."
        }, isAdmin: false);
        Assert.False(dup.Success);

        // Admin puede publicar reseña oficial (IsAdminReview = true)
        var rAdmin = await store.AddAsync(new ProductReview
        {
            ProductoId = 10,
            UserId = "99",
            UserName = "Admin LentSoft",
            Rating = 4,
            Comment = "Recomendada por nuestros especialistas en óptica."
        }, isAdmin: true);
        Assert.True(rAdmin.Success);
        Assert.True(rAdmin.Review!.IsAdminReview);

        // Resumen promedio (5 + 4) / 2 = 4.5
        var summary = await store.GetSummaryAsync(10);
        Assert.Equal(2, summary.TotalReviews);
        Assert.Equal(4.5, summary.AverageRating);

        // Usuario normal no puede eliminar reseña ajena
        var forbiddenDelete = await store.DeleteAsync(rAdmin.Review.Id, "1", isAdmin: false);
        Assert.False(forbiddenDelete.Success);

        // Admin sí puede eliminar cualquier reseña
        var adminDelete = await store.DeleteAsync(r1.Review!.Id, "99", isAdmin: true);
        Assert.True(adminDelete.Success);
    }

    [Fact]
    public async Task JsonReviewStore_ArchivoCorruptoNoRompeAplicacion()
    {
        var path = Path.Combine(_tempDir, "corrupt_reviews.json");
        await File.WriteAllTextAsync(path, "{ json_corrupto ::: ");

        var store = new JsonReviewStore(path, NullLogger<JsonReviewStore>.Instance);
        var list = await store.GetAllAsync();
        Assert.Empty(list);
    }

    [Fact]
    public async Task JsonFrameModelStore_GuardaYObtieneManifiestoYDestacado()
    {
        var path = Path.Combine(_tempDir, "frame-models.json");
        var store = new JsonFrameModelStore(path, NullLogger<JsonFrameModelStore>.Instance);

        await store.SetFeaturedProductIdAsync(42);
        await store.SaveModelAsync(new FrameModelEntry
        {
            ProductId = 42,
            ModelPath = "/models/frames/test.glb",
            ModelType = "glb",
            Scale = 1.25
        });

        var manifest = await store.GetManifestAsync();
        Assert.Equal(42, manifest.FeaturedProductId);
        Assert.Single(manifest.Models);
        Assert.Equal("glb", manifest.Models[0].ModelType);

        var deleted = await store.DeleteModelAsync(42);
        Assert.True(deleted);
        Assert.Null(await store.GetByProductAsync(42));
    }
}
