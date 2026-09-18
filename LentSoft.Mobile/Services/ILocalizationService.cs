namespace LentSoft.Mobile.Services;

public interface ILocalizationService
{
    string CurrentLanguage { get; }
    event EventHandler? LanguageChanged;

    void SetLanguage(string langCode);
    string GetString(string key);
}
