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

    public async Task<List<User>> GetAllAsync(bool includeInactive = false)
    {
        var query = _context.Users.AsQueryable();
        if (!includeInactive)
        {
            query = query.Where(u => u.Activo);
        }

        return await query
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

        try
        {
            await _context.SaveChangesAsync();
            return user;
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("No se pudo actualizar el perfil debido a un error de base de datos.", ex);
        }
    }

    public async Task<bool> ChangePasswordAsync(int id, string currentPassword, string newPassword)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return false;

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("No se pudo cambiar la contraseña debido a una restricción de base de datos.", ex);
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return false;

        user.Activo = false;
        _context.Users.Update(user);

        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("No se pudo desactivar el usuario porque está vinculado a otros registros del sistema.", ex);
        }
    }

    public async Task<bool> ReactivateAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return false;

        user.Activo = true;
        _context.Users.Update(user);

        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("No se pudo reactivar el usuario debido a un error de base de datos.", ex);
        }
    }
}
