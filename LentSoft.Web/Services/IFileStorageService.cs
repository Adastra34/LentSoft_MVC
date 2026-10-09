using Microsoft.AspNetCore.Http;

namespace LentSoft.Web.Services;

public interface IFileStorageService
{
    Task<(bool Success, string? RelativePath, string? ErrorMessage)> SaveImageAsync(
        IFormFile file, 
        string subFolder, 
        string[] allowedExtensions, 
        long maxSizeBytes);

    void DeleteFile(string? relativePath);
}
