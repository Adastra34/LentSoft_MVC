using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public interface IUserService
{
    Task<List<User>> GetAllAsync(bool includeInactive = false);
    Task<User?> GetByIdAsync(int id);
    Task<User?> UpdateProfileAsync(int id, string nombre, string? telefono);
    Task<bool> ChangePasswordAsync(int id, string currentPassword, string newPassword);
    Task<bool> DeleteAsync(int id);
    Task<bool> ReactivateAsync(int id);
}
