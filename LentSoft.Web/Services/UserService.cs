using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

/// <summary>
/// User service — migrated from Controllers/UserController.js
/// </summary>
public class UserService : IUserService
{
    private readonly LentSoftDbContext _context;

    public UserService(LentSoftDbContext context)
    {
        _context = context;
    }

    public async Task<List<User>> GetAllAsync()
    {
        return await _context.Users
            .OrderBy(u => u.Nombre)
            .ToListAsync();
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<User?> UpdateProfileAsync(int id, string nombre, string? telefono)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return null;

        user.Nombre = nombre;
        user.Telefono = telefono;

        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<bool> ChangePasswordAsync(int id, string currentPassword, string newPassword)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return false;

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return false;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return true;
    }
}
