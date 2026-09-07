using System.Text.Json;
using LentSoft.Mobile.Models;

namespace LentSoft.Mobile.Services;

public class AuthStorageService : IAuthStorageService
{
    private const string TokenKey = "lentsoft_jwt_token";
    private const string UserKey = "lentsoft_user_info";

    public async Task<string?> GetTokenAsync()
    {
        try
        {
            return await SecureStorage.Default.GetAsync(TokenKey);
        }
        catch
        {
            return Preferences.Default.Get(TokenKey, null as string);
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
            Preferences.Default.Set(TokenKey, token);
        }
    }

    public async Task<UserDto?> GetUserAsync()
    {
        try
        {
            var json = await SecureStorage.Default.GetAsync(UserKey);
            if (string.IsNullOrWhiteSpace(json))
            {
                json = Preferences.Default.Get(UserKey, null as string);
            }

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
        var json = JsonSerializer.Serialize(user);
        try
        {
            await SecureStorage.Default.SetAsync(UserKey, json);
        }
        catch
        {
            Preferences.Default.Set(UserKey, json);
        }
    }

    public Task ClearAsync()
    {
        try
        {
            SecureStorage.Default.Remove(TokenKey);
            SecureStorage.Default.Remove(UserKey);
        }
        catch { }

        Preferences.Default.Remove(TokenKey);
        Preferences.Default.Remove(UserKey);
        return Task.CompletedTask;
    }

    public async Task<bool> HasValidTokenAsync()
    {
        var token = await GetTokenAsync();
        return !string.IsNullOrWhiteSpace(token);
    }
}
