using System.Text.Json;
using LentSoft.Mobile.Models;

namespace LentSoft.Mobile.Services;

public class AuthStorageService : IAuthStorageService
{
    private const string TokenKey = "lentsoft_jwt_token";
    private const string UserKey = "lentsoft_user_info";
    private const string FormulaKeyPrefix = "lentsoft_formula_";

    public async Task<string?> GetTokenAsync()
    {
        try
        {
            return await SecureStorage.Default.GetAsync(TokenKey);
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveTokenAsync(string token)
    {
        try
        {
            await SecureStorage.Default.SetAsync(TokenKey, token);
        }
        catch
        {
            // Logging without sensitive data if needed
        }
    }

    public async Task<UserDto?> GetUserAsync()
    {
        try
        {
            var json = await SecureStorage.Default.GetAsync(UserKey);
            if (string.IsNullOrWhiteSpace(json)) return null;
            return JsonSerializer.Deserialize<UserDto>(json);
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveUserAsync(UserDto user)
    {
        try
        {
            var json = JsonSerializer.Serialize(user);
            await SecureStorage.Default.SetAsync(UserKey, json);
        }
        catch
        {
        }
    }

    public async Task SaveFormulaAsync(FormulaOpticaDto formula)
    {
        try
        {
            var user = await GetUserAsync();
            var userSuffix = user != null && user.Id > 0 ? user.Id.ToString() : "current";
            var key = FormulaKeyPrefix + userSuffix;
            var json = JsonSerializer.Serialize(formula);
            await SecureStorage.Default.SetAsync(key, json);
        }
        catch
        {
        }
    }

    public async Task<FormulaOpticaDto?> GetCachedFormulaAsync()
    {
        try
        {
            var user = await GetUserAsync();
            var userSuffix = user != null && user.Id > 0 ? user.Id.ToString() : "current";
            var key = FormulaKeyPrefix + userSuffix;
            var json = await SecureStorage.Default.GetAsync(key);
            if (string.IsNullOrWhiteSpace(json)) return null;
            return JsonSerializer.Deserialize<FormulaOpticaDto>(json);
        }
        catch
        {
            return null;
        }
    }

    public async Task ClearAsync()
    {
        try
        {
            var user = await GetUserAsync();
            if (user != null && user.Id > 0)
            {
                SecureStorage.Default.Remove(FormulaKeyPrefix + user.Id);
            }
            SecureStorage.Default.Remove(FormulaKeyPrefix + "current");
            SecureStorage.Default.Remove(TokenKey);
            SecureStorage.Default.Remove(UserKey);
        }
        catch { }

        // Limpiar también preferencias por si existían registros residuales previos
        try
        {
            Preferences.Default.Remove(TokenKey);
            Preferences.Default.Remove(UserKey);
        }
        catch { }
    }

    public async Task<bool> HasValidTokenAsync()
    {
        var token = await GetTokenAsync();
        if (string.IsNullOrWhiteSpace(token)) return false;

        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3) return false;

            var payload = parts[1];
            // Decodificar Base64Url
            string padded = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=')
                                   .Replace('-', '+')
                                   .Replace('_', '/');

            var bytes = Convert.FromBase64String(padded);
            using var doc = JsonDocument.Parse(bytes);

            if (doc.RootElement.TryGetProperty("exp", out var expProp))
            {
                long expSeconds = expProp.GetInt64();
                var expDate = DateTimeOffset.FromUnixTimeSeconds(expSeconds);
                return expDate > DateTimeOffset.UtcNow;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
