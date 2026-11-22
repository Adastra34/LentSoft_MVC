using LentSoft.Mobile.Models;

namespace LentSoft.Mobile.Services;

public interface IAuthStorageService
{
    Task<string?> GetTokenAsync();
    Task SaveTokenAsync(string token);
    Task<UserDto?> GetUserAsync();
    Task SaveUserAsync(UserDto user);
    Task ClearAsync();
    Task<bool> HasValidTokenAsync();
    Task SaveFormulaAsync(FormulaOpticaDto formula);
    Task<FormulaOpticaDto?> GetCachedFormulaAsync();
}
