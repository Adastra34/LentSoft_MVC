using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

/// <summary>
/// Servicio de autenticación y gestión de identidad de usuarios.
/// Implementa la verificación de credenciales con BCrypt, normalización estricta de correos
/// y restricción de inicio de sesión exclusivamente para usuarios activos.
/// </summary>
public class AuthService : IAuthService
{
    private readonly LentSoftDbContext _context;

    public AuthService(LentSoftDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Autentica a un usuario verificando su correo normalizado y contraseña hasheada.
    /// Rechaza cualquier usuario inactivo (<c>Activo == false</c>) retornando <c>null</c>.
    /// </summary>
    /// <param name="email">Correo electrónico del usuario.</param>
    /// <param name="password">Contraseña en texto claro.</param>
    /// <returns>La instancia del <see cref="User"/> activo si las credenciales son correctas; de lo contrario <c>null</c>.</returns>
    public async Task<User?> LoginAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
            return null;

        var normalizedEmail = email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail && u.Activo);

        if (user == null)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;

        return user;
    }

    /// <summary>
    /// Registra un nuevo usuario en la base de datos asegurando que el rol asignado sea siempre "usuario".
    /// Normaliza el correo electrónico a minúsculas y valida que ni el email ni el documento existan previamente.
    /// </summary>
    /// <param name="nombre">Nombre de pila.</param>
    /// <param name="apellido">Apellidos.</param>
    /// <param name="tipoDocumento">Tipo de identificación (ej. CC, TI, CE).</param>
    /// <param name="numeroDocumento">Número de identificación.</param>
    /// <param name="email">Correo electrónico (se normaliza con Trim y ToLowerInvariant).</param>
    /// <param name="telefono">Teléfono de contacto.</param>
    /// <param name="password">Contraseña que será hasheada con BCrypt.</param>
    /// <returns>El nuevo <see cref="User"/> creado o <c>null</c> si ya existe un registro con el mismo correo o documento.</returns>
    public async Task<User?> RegisterAsync(string nombre, string apellido, string tipoDocumento, string numeroDocumento, string email, string telefono, string password)
    {
        var normalizedEmail = email?.Trim().ToLowerInvariant() ?? string.Empty;
        var normalizedDoc = numeroDocumento?.Trim() ?? string.Empty;

        // Comprobar si el correo ya existe
        var existingEmail = await _context.Users
            .AnyAsync(u => u.Email == normalizedEmail);

        if (existingEmail)
            return null;

        // Comprobar si el número de documento ya existe
        var existingDoc = await _context.Users
            .AnyAsync(u => u.NumeroDocumento == normalizedDoc);

        if (existingDoc)
            return null;

        var user = new User
        {
            Nombre = nombre?.Trim() ?? string.Empty,
            Apellido = apellido?.Trim() ?? string.Empty,
            TipoDocumento = string.IsNullOrWhiteSpace(tipoDocumento) ? "CC" : tipoDocumento.Trim(),
            NumeroDocumento = normalizedDoc,
            Email = normalizedEmail,
            Telefono = telefono?.Trim() ?? string.Empty,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = "usuario",
            FechaRegistro = DateTime.UtcNow,
            Activo = true
        };

        try
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException("El registro de usuario fue modificado concurrentemente.", ex);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("No se pudo completar el registro del usuario debido a una restricción de base de datos.", ex);
        }
    }

    /// <summary>
    /// Busca un usuario por su identificador primario.
    /// </summary>
    /// <param name="id">Identificador numérico del usuario.</param>
    /// <returns>La entidad <see cref="User"/> o <c>null</c> si no existe.</returns>
    public async Task<User?> GetUserByIdAsync(int id)
    {
        return await _context.Users.FindAsync(id);
    }

    /// <summary>
    /// Busca un usuario por su correo electrónico aplicando normalización con Trim y ToLowerInvariant.
    /// </summary>
    /// <param name="email">Correo electrónico a consultar.</param>
    /// <returns>La entidad <see cref="User"/> encontrada o <c>null</c> si no existe.</returns>
    public async Task<User?> GetUserByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var normalizedEmail = email.Trim().ToLowerInvariant();

        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail);
    }

    /// <summary>
    /// Persiste los cambios de un usuario en la base de datos.
    /// </summary>
    /// <param name="user">Entidad con los cambios a guardar.</param>
    public async Task UpdateUserAsync(User user)
    {
        try
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException("El registro de usuario fue modificado por otro proceso.", ex);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("No se pudieron guardar los cambios del usuario debido a una restricción de datos.", ex);
        }
    }
}
