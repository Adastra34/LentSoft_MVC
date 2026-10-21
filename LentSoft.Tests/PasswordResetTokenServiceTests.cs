using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using LentSoft.Web.Services;
using Xunit;

namespace LentSoft.Tests;

public class PasswordResetTokenServiceTests
{
    private const string SecretKey = "test-secret-key-password-reset-minimum-32-chars";
    private const string Issuer = "LentSoft.Web";
    private const string Audience = "LentSoft.PasswordReset";

    private readonly IConfiguration _configuration;
    private readonly PasswordResetTokenService _tokenService;

    public PasswordResetTokenServiceTests()
    {
        var configData = new Dictionary<string, string?>
        {
            { "PasswordResetJwt:SecretKey", SecretKey },
            { "PasswordResetJwt:Issuer", Issuer },
            { "PasswordResetJwt:Audience", Audience },
            { "PasswordResetJwt:ExpirationMinutes", "30" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _tokenService = new PasswordResetTokenService(_configuration);
    }

    [Fact]
    public void ValidateToken_TokenValido_RetornaUserId()
    {
        // Arrange
        var userId = 42;
        var email = "recuperar@test.com";
        var passwordHash = "$2a$11$hashOriginalDePrueba123";

        // Act
        var token = _tokenService.GenerateToken(userId, email, passwordHash);
        var validatedUserId = _tokenService.ValidateToken(token);
        var matchesPassword = _tokenService.MatchesCurrentPassword(token, passwordHash);

        // Assert
        Assert.Equal(userId, validatedUserId);
        Assert.True(matchesPassword);
    }

    [Fact]
    public void ValidateToken_TokenExpirado_RetornaNull()
    {
        // Arrange: Crear manualmente un token vencido con la misma firma
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("userId", "15"),
            new Claim("email", "expirado@test.com"),
            new Claim("purpose", "password_reset"),
            new Claim("pwdfp", "a1b2c3d4e5f60718")
        };

        var expiredJwt = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(-10),
            signingCredentials: credentials
        );

        var expiredTokenString = new JwtSecurityTokenHandler().WriteToken(expiredJwt);

        // Act
        var result = _tokenService.ValidateToken(expiredTokenString);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ValidateToken_TokenDeOtroProposito_RetornaNull()
    {
        // Arrange: Crear token con purpose distinto a "password_reset"
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("userId", "20"),
            new Claim("email", "otro@test.com"),
            new Claim("purpose", "login_mobile")
        };

        var wrongPurposeJwt = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials
        );

        var wrongTokenString = new JwtSecurityTokenHandler().WriteToken(wrongPurposeJwt);

        // Act
        var result = _tokenService.ValidateToken(wrongTokenString);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void MatchesCurrentPassword_TokenDejaDeSerValidoTrasCambiarPassword()
    {
        // Arrange
        var userId = 50;
        var email = "usuario.singleuse@test.com";
        var originalHash = "$2a$11$hashOriginal123456789";
        var nuevoHash = "$2a$11$hashNuevoModificado987654321";

        // Act 1: Generar token con la contraseña vigente
        var token = _tokenService.GenerateToken(userId, email, originalHash);

        // Assert inicial: El token coincide con el hash original
        Assert.True(_tokenService.MatchesCurrentPassword(token, originalHash));

        // Act 2: Al cambiar la contraseña en la base de datos (nuevo hash)
        var matchesDespuesDelCambio = _tokenService.MatchesCurrentPassword(token, nuevoHash);

        // Assert: El token queda automáticamente invalidado (un solo uso sin base de datos)
        Assert.False(matchesDespuesDelCambio);
    }
}
