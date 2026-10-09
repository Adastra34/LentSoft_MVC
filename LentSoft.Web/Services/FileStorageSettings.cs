namespace LentSoft.Web.Services;

public class FileStorageSettings
{
    public const string SectionName = "FileStorageSettings";

    public string BaseUploadPath { get; set; } = "uploads";
}
