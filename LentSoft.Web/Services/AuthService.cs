using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public class AuthService : IAuthService
{
    private readonly LentSoftDbContext _context;

    public AuthService(LentSoftDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Authenticate user with email and password.
    /// Migrated from js/auth.js AuthService.login()
    /// </summary>
    public async Task<User?> LoginAsync(string email, string password)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;

        return user;
    }

    /// <summary>
    /// Register a new user with full identity fields.
    /// </summary>
    public async Task<User?> RegisterAsync(string nombre, string apellido, string tipoDocumento, string numeroDocumento, string email, string telefono, string password)
    {
        // Check if email already exists
        var existingEmail = await _context.Users
            .AnyAsync(u => u.Email == email);

        if (existingEmail)
            return null;

        // Check if document number already exists
        var existingDoc = await _context.Users
            .AnyAsync(u => u.NumeroDocumento == numeroDocumento);

        if (existingDoc)
            return null;

        var user = new User
        {
            Nombre = nombre,
            Apellido = apellido,
            TipoDocumento = tipoDocumento,
            NumeroDocumento = numeroDocumento,
            Email = email,
            Telefono = telefono,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = "usuario",
            FechaRegistro = DateTime.UtcNow
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

    public async Task<User?> GetUserByIdAsync(int id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);
    }

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
