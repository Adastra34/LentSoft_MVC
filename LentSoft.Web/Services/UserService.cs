using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Models.ViewModels;

namespace LentSoft.Web.Services;

/// <summary>
/// Servicio de administración de usuarios y clientes.
/// Implementa la gestión de perfiles, contraseñas y operaciones de clientes,
/// aplicando las reglas de negocio de protección del administrador (RF-M07-03) y no auto-desactivación.
/// </summary>
public class UserService : IUserService
{
    private readonly LentSoftDbContext _context;

    public UserService(LentSoftDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Obtiene la lista completa de usuarios ordenada por nombre.
    /// Utiliza AsNoTracking para optimizar el rendimiento al tratarse de una consulta de lectura.
    /// </summary>
    /// <param name="includeInactive">Indica si se deben retornar también los usuarios inactivos.</param>
    /// <returns>Lista de usuarios.</returns>
    public async Task<List<User>> GetAllAsync(bool includeInactive = false)
    {
        var query = _context.Users.AsNoTracking().AsQueryable();
        if (!includeInactive)
        {
            query = query.Where(u => u.Activo);
        }

        return await query
            .OrderBy(u => u.Nombre)
            .ToListAsync();
    }

    /// <summary>
    /// Obtiene un usuario por su identificador primario.
    /// </summary>
    /// <param name="id">Identificador del usuario.</param>
    /// <returns>Entidad de usuario o <c>null</c> si no existe.</returns>
    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users.FindAsync(id);
    }

    /// <summary>
    /// Actualiza el nombre y teléfono del perfil de un usuario.
    /// </summary>
    /// <param name="id">Identificador del usuario.</param>
    /// <param name="nombre">Nombre a actualizar.</param>
    /// <param name="telefono">Teléfono a actualizar.</param>
    /// <returns>La entidad de usuario actualizada o <c>null</c> si no se encontró.</returns>
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

    /// <summary>
    /// Cambia la contraseña de un usuario previa validación de su contraseña actual.
    /// </summary>
    /// <param name="id">Identificador del usuario.</param>
    /// <param name="currentPassword">Contraseña actual.</param>
    /// <param name="newPassword">Nueva contraseña.</param>
    /// <returns><c>true</c> si la contraseña actual coincide y se guardó la nueva; de lo contrario <c>false</c>.</returns>
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

    /// <summary>
    /// Desactiva lógicamente al usuario especificado (<c>Activo = false</c>).
    /// </summary>
    /// <param name="id">Identificador del usuario a desactivar.</param>
    /// <returns><c>true</c> si se desactivó; <c>false</c> si no se encontró el usuario.</returns>
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

    /// <summary>
    /// Reactiva un usuario previamente desactivado en el sistema (<c>Activo = true</c>).
    /// </summary>
    /// <param name="id">Identificador del usuario.</param>
    /// <returns><c>true</c> si se reactivó correctamente; de lo contrario <c>false</c>.</returns>
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

    /// <summary>
    /// Registra un nuevo cliente desde el panel de administración.
    /// Valida que el correo no esté duplicado y que, en caso de suministrarse contraseña, esta cumpla
    /// con los estándares de fortaleza (8+ caracteres, mayúscula, minúscula, número y especial).
    /// </summary>
    /// <param name="model">Datos del cliente a crear.</param>
    /// <returns>Resultado con el usuario registrado o el mensaje de error de validación.</returns>
    public async Task<(bool Success, string? ErrorMessage, User? User, bool IsPasswordAutoGenerated)> CreateClientAsync(ClientFormViewModel model)
    {
        var normalizedEmail = model.Email?.Trim().ToLowerInvariant() ?? string.Empty;

        if (await _context.Users.AnyAsync(u => u.Email == normalizedEmail))
        {
            return (false, "El correo electrónico ya está registrado.", null, false);
        }

        var isAutoGenerated = string.IsNullOrWhiteSpace(model.Password);
        string passwordToHash;

        if (isAutoGenerated)
        {
            passwordToHash = Guid.NewGuid().ToString("N");
        }
        else
        {
            var isStrongPassword = Regex.IsMatch(
                model.Password!,
                @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$");

            if (!isStrongPassword)
            {
                return (false, "La contraseña debe tener al menos 8 caracteres e incluir mayúsculas, minúsculas, números y un carácter especial.", null, false);
            }

            passwordToHash = model.Password!;
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(passwordToHash);

        var user = new User
        {
            Nombre = model.Nombre.Trim(),
            Apellido = string.IsNullOrWhiteSpace(model.Apellido) ? "" : model.Apellido.Trim(),
            Email = normalizedEmail,
            Telefono = model.Telefono?.Trim(),
            TipoDocumento = string.IsNullOrWhiteSpace(model.TipoDocumento) ? "CC" : model.TipoDocumento,
            NumeroDocumento = string.IsNullOrWhiteSpace(model.NumeroDocumento) ? Guid.NewGuid().ToString("N").Substring(0, 10) : model.NumeroDocumento.Trim(),
            PasswordHash = passwordHash,
            Role = "usuario",
            FechaRegistro = DateTime.UtcNow,
            Activo = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return (true, null, user, isAutoGenerated);
    }

    /// <summary>
    /// Actualiza los datos de un cliente existente comprobando la unicidad del nuevo correo respecto a otros usuarios.
    /// </summary>
    /// <param name="model">Modelo con la información actualizada.</param>
    /// <returns>Resultado de la operación.</returns>
    public async Task<(bool Success, string? ErrorMessage)> UpdateClientAsync(ClientFormViewModel model)
    {
        var user = await _context.Users.FindAsync(model.Id);
        if (user == null)
        {
            return (false, "Cliente no encontrado.");
        }

        var normalizedEmail = model.Email?.Trim().ToLowerInvariant() ?? string.Empty;

        if (await _context.Users.AnyAsync(u => u.Email == normalizedEmail && u.Id != model.Id))
        {
            return (false, "El correo electrónico ya pertenece a otro usuario.");
        }

        user.Nombre = model.Nombre.Trim();
        user.Apellido = string.IsNullOrWhiteSpace(model.Apellido) ? "" : model.Apellido.Trim();
        user.Email = normalizedEmail;
        user.Telefono = model.Telefono?.Trim();

        await _context.SaveChangesAsync();
        return (true, null);
    }

    /// <summary>
    /// Desactiva a un usuario aplicando las reglas críticas de seguridad:
    /// 1. Un administrador no puede desactivar su propia cuenta.
    /// 2. Regla RF-M07-03: No se puede desactivar al único administrador activo restante en el sistema.
    /// </summary>
    /// <param name="id">Identificador del usuario a desactivar.</param>
    /// <param name="currentUserId">Identificador del usuario administrador autenticado.</param>
    /// <returns>Resultado indicando éxito o mensaje descriptivo de la regla infringida.</returns>
    public async Task<(bool Success, string? ErrorMessage)> DeactivateAsync(int id, int currentUserId)
    {
        if (id == currentUserId && currentUserId > 0)
        {
            return (false, "No puedes desactivar tu propia cuenta.");
        }

        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return (false, "Usuario no encontrado.");
        }

        // Regla de Negocio RF-M07-03: No permitir desactivar al único usuario con rol "admin" activo restante
        if (string.Equals(user.Role, "admin", StringComparison.OrdinalIgnoreCase))
        {
            var activeAdminCount = await _context.Users.CountAsync(u => u.Role.ToLower() == "admin" && u.Activo);
            if (activeAdminCount <= 1)
            {
                return (false, "No se puede eliminar el único usuario administrador restante del sistema.");
            }
        }

        user.Activo = false;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        return (true, null);
    }
}
