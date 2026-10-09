using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace LentSoft.Web.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly FileStorageSettings _settings;

    public LocalFileStorageService(
        IWebHostEnvironment webHostEnvironment,
        IOptions<FileStorageSettings> options)
    {
        _webHostEnvironment = webHostEnvironment;
        _settings = options.Value;
    }

    public async Task<(bool Success, string? RelativePath, string? ErrorMessage)> SaveImageAsync(
        IFormFile file, 
        string subFolder, 
        string[] allowedExtensions, 
        long maxSizeBytes)
    {
        if (file == null || file.Length == 0)
        {
            return (false, null, "El archivo de imagen está vacío.");
        }

        if (file.Length > maxSizeBytes)
        {
            var maxMb = maxSizeBytes / (1024 * 1024);
            return (false, null, $"El tamaño de la imagen no debe superar los {maxMb} MB.");
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
        {
            var allowedList = string.Join(", ", allowedExtensions);
            return (false, null, $"Formato no permitido. Solo se aceptan archivos {allowedList}.");
        }

        try
        {
            var webRoot = _webHostEnvironment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var targetDir = Path.Combine(webRoot, _settings.BaseUploadPath, subFolder);
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            var uniqueFileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(targetDir, uniqueFileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativePath = $"/{_settings.BaseUploadPath}/{subFolder}/{uniqueFileName}".Replace("//", "/");
            return (true, relativePath, null);
        }
        catch (Exception ex)
        {
            return (false, null, $"Error al guardar el archivo en el almacenamiento: {ex.Message}");
        }
    }

    public void DeleteFile(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return;

        try
        {
            var normalized = relativePath.Replace('\\', '/').TrimStart('/');
            var webRoot = _webHostEnvironment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var fullPath = Path.Combine(webRoot, normalized.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
        catch
        {
            // Silently ignore delete failures on disk cleanup
        }
    }
}
