using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

/// <summary>
/// Servicio de generación de tokens JWT para la autenticación de la aplicación móvil (MAUI).
/// Valida de forma estricta que la clave secreta y la configuración existan sin recurrir a valores por defecto inseguros.
/// </summary>
public class MobileJwtService : IMobileJwtService
{
    private readonly IConfiguration _configuration;

    public MobileJwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Genera un token JWT para el usuario autenticado en la API móvil.
    /// </summary>
    /// <param name="user">Entidad del usuario con sus claims.</param>
    /// <returns>Token JWT firmado.</returns>
    public string GenerateToken(User user)
    {
        var jwtSettings = _configuration.GetSection("MobileJwt");
        var secretKey = jwtSettings["SecretKey"];

        if (string.IsNullOrEmpty(secretKey) ||
            secretKey == "CONFIGURAR_EN_USER_SECRETS_O_VARIABLE_DE_ENTORNO" ||
            secretKey.Length < 32)
        {
            throw new InvalidOperationException("La clave secreta para MobileJwt no está configurada o tiene menos de 32 caracteres.");
        }

        var issuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("El emisor (Issuer) para MobileJwt no está configurado.");
        var audience = jwtSettings["Audience"] ?? throw new InvalidOperationException("La audiencia (Audience) para MobileJwt no está configurada.");
        var expirationDays = int.TryParse(jwtSettings["ExpirationDays"], out var days) ? days : 30;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new("userId", user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, string.IsNullOrWhiteSpace(user.Apellido) ? user.Nombre : user.NombreCompleto),
            new(ClaimTypes.Role, user.Role),
            new("purpose", "mobile_auth"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(expirationDays),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
