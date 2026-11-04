using System.IO;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using LentSoft.Web.Services;
using Xunit;

namespace LentSoft.Tests;

public class FileStorageServiceTests
{
    private class FakeWebHostEnv : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = Path.Combine(Path.GetTempPath(), "LentSoft_Test_WebRoot_" + Guid.NewGuid().ToString("N"));
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "LentSoft.Tests";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    [Fact]
    public async Task SaveImageAsync_SavesFileToConfigurablePath()
    {
        // Arrange
        var env = new FakeWebHostEnv();
        var customFolder = "custom_uploads";
        var options = Options.Create(new FileStorageSettings { BaseUploadPath = customFolder });
        IFileStorage storage = new LocalFileStorageService(env, options);

        var content = "fake image content";
        var bytes = Encoding.UTF8.GetBytes(content);
        using var stream = new MemoryStream(bytes);
        var formFile = new FormFile(stream, 0, bytes.Length, "Data", "test-product.png");

        // Act
        var result = await storage.SaveImageAsync(formFile, "productos", new[] { ".png", ".jpg" }, 5 * 1024 * 1024);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.RelativePath);
        Assert.Contains($"/{customFolder}/productos/", result.RelativePath);

        // Cleanup
        storage.DeleteFile(result.RelativePath);
        if (Directory.Exists(env.WebRootPath))
        {
            Directory.Delete(env.WebRootPath, true);
        }
    }

    [Fact]
    public async Task SaveImageAsync_RejectsInvalidExtension()
    {
        // Arrange
        var env = new FakeWebHostEnv();
        var options = Options.Create(new FileStorageSettings { BaseUploadPath = "uploads" });
        IFileStorage storage = new LocalFileStorageService(env, options);

        var content = "malicious script";
        var bytes = Encoding.UTF8.GetBytes(content);
        using var stream = new MemoryStream(bytes);
        var formFile = new FormFile(stream, 0, bytes.Length, "Data", "malicious.exe");

        // Act
        var result = await storage.SaveImageAsync(formFile, "productos", new[] { ".png", ".jpg" }, 5 * 1024 * 1024);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Formato no permitido", result.ErrorMessage);
    }
}

