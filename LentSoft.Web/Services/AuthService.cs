using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models;
using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public class AuthService : IAuthService
{
    private readonly LentSoftDbContext _context;

    private const int MaxIntentosFallidos = 5;
    private const int MinutosBloqueo = 15;

    public AuthService(LentSoftDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Authenticate user with email and password.
    /// Includes brute-force protection: locks account for 15 minutes after 5 consecutive failed attempts.
    /// </summary>
    public async Task<LoginAttemptResult> LoginAsync(string email, string password)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);

        // Usuario no existe — devolver credenciales inválidas (sin revelar que el email no existe)
        if (user == null)
            return new LoginAttemptResult { Result = LoginResult.InvalidCredentials };

        // Verificar si la cuenta está bloqueada temporalmente
        if (user.BloqueadoHasta.HasValue && user.BloqueadoHasta.Value > DateTime.UtcNow)
        {
            var tiempoRestante = user.BloqueadoHasta.Value - DateTime.UtcNow;
            return new LoginAttemptResult
            {
                Result = LoginResult.AccountLocked,
                TiempoRestanteBloqueo = tiempoRestante
            };
        }

        // Contraseña incorrecta
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            user.IntentosFallidos++;

            if (user.IntentosFallidos >= MaxIntentosFallidos)
            {
                user.BloqueadoHasta = DateTime.UtcNow.AddMinutes(MinutosBloqueo);
                user.IntentosFallidos = 0;
            }

            await _context.SaveChangesAsync();
            return new LoginAttemptResult { Result = LoginResult.InvalidCredentials };
        }

        // Login exitoso — limpiar historial de intentos fallidos
        user.IntentosFallidos = 0;
        user.BloqueadoHasta = null;
        await _context.SaveChangesAsync();

        return new LoginAttemptResult
        {
            Result = LoginResult.Success,
            User = user
        };
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

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
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
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }
}
