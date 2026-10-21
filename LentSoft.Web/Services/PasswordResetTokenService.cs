using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace LentSoft.Web.Services;

/// <summary>
/// Implementación del servicio de tokens JWT de recuperación de contraseña de un solo uso.
/// No requiere tablas adicionales en base de datos: incrusta en el token un claim "pwdfp"
/// con los primeros 16 caracteres hexadecimales del SHA256 del hash de contraseña actual.
/// Al cambiar la contraseña, cualquier token previo queda inmediatamente invalidado.
/// </summary>
public class PasswordResetTokenService : IPasswordResetTokenService
{
    private readonly IConfiguration _configuration;

    public PasswordResetTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Calcula los primeros 16 caracteres hexadecimales en minúsculas del SHA256 del hash de contraseña.
    /// </summary>
    private static string ComputePasswordFingerprint(string passwordHash)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(passwordHash ?? string.Empty);
        var hashBytes = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant().Substring(0, 16);
    }

    /// <inheritdoc />
    public string GenerateToken(int userId, string email, string passwordHash)
    {
        var jwtSettings = _configuration.GetSection("PasswordResetJwt");
        var secretKey = jwtSettings["SecretKey"]!;
        var issuer = jwtSettings["Issuer"]!;
        var audience = jwtSettings["Audience"]!;
        var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "30");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var fingerprint = ComputePasswordFingerprint(passwordHash);

        var claims = new[]
        {
            new Claim("userId", userId.ToString()),
            new Claim("email", email),
            new Claim("purpose", "password_reset"),
            new Claim("pwdfp", fingerprint),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <inheritdoc />
    public int? ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var jwtSettings = _configuration.GetSection("PasswordResetJwt");
        var secretKey = jwtSettings["SecretKey"]!;
        var issuer = jwtSettings["Issuer"]!;
        var audience = jwtSettings["Audience"]!;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, validationParameters, out _);

            var purposeClaim = principal.FindFirst("purpose")?.Value;
            if (purposeClaim != "password_reset")
                return null;

            var userIdClaim = principal.FindFirst("userId")?.Value;
            if (userIdClaim != null && int.TryParse(userIdClaim, out var userId))
                return userId;

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public bool MatchesCurrentPassword(string token, string currentPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(currentPasswordHash))
            return false;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token))
                return false;

            var jwtToken = handler.ReadJwtToken(token);
            var tokenFingerprint = jwtToken.Claims.FirstOrDefault(c => c.Type == "pwdfp")?.Value;

            if (string.IsNullOrWhiteSpace(tokenFingerprint))
                return false;

            var currentFingerprint = ComputePasswordFingerprint(currentPasswordHash);
            return string.Equals(tokenFingerprint, currentFingerprint, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
