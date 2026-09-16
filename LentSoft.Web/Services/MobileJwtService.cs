using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public class MobileJwtService : IMobileJwtService
{
    private readonly IConfiguration _configuration;

    public MobileJwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user)
    {
        var jwtSettings = _configuration.GetSection("MobileJwt");
        var secretKey = jwtSettings["SecretKey"] ?? "dev-secret-key-lentsoft-mobile-jwt-api-minimum-32-chars";
        var issuer = jwtSettings["Issuer"] ?? "LentSoft.Api";
        var audience = jwtSettings["Audience"] ?? "LentSoft.Mobile";
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
