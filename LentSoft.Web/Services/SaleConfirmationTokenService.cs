using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace LentSoft.Web.Services;

public class SaleConfirmationTokenService : ISaleConfirmationTokenService
{
    private readonly IConfiguration _configuration;

    public SaleConfirmationTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(int saleId)
    {
        var jwtSettings = _configuration.GetSection("SaleConfirmationJwt");
        var secretKey = jwtSettings["SecretKey"]!;
        var issuer = jwtSettings["Issuer"]!;
        var audience = jwtSettings["Audience"]!;
        var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "4320");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("saleId", saleId.ToString()),
            new Claim("purpose", "sale_confirmation"),
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

    public int? ValidateToken(string token)
    {
        var jwtSettings = _configuration.GetSection("SaleConfirmationJwt");
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
            if (purposeClaim != "sale_confirmation")
                return null;

            var saleIdClaim = principal.FindFirst("saleId")?.Value;
            if (saleIdClaim != null && int.TryParse(saleIdClaim, out var saleId))
                return saleId;

            return null;
        }
        catch
        {
            return null;
        }
    }
}
