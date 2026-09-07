using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using LentSoft.Web.Services;

namespace LentSoft.Web.Controllers.Api;

[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IMobileJwtService _jwtService;
    private readonly IPasswordResetTokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthApiController> _logger;

    public AuthApiController(
        IAuthService authService,
        IMobileJwtService jwtService,
        IPasswordResetTokenService tokenService,
        IEmailService emailService,
        ILogger<AuthApiController> logger)
    {
        _authService = authService;
        _jwtService = jwtService;
        _tokenService = tokenService;
        _emailService = emailService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginApiRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _authService.LoginAsync(request.Email.Trim(), request.Password);
        if (user == null || !user.Activo)
        {
            return Unauthorized(new { message = "Credenciales incorrectas o usuario inactivo." });
        }

        var token = _jwtService.GenerateToken(user);
        return Ok(new
        {
            token,
            user = new
            {
                user.Id,
                user.Nombre,
                user.Apellido,
                user.NombreCompleto,
                user.Email,
                user.Telefono,
                user.TipoDocumento,
                user.NumeroDocumento,
                user.Direccion,
                user.Role
            }
        });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterApiRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _authService.RegisterAsync(
            request.Nombre.Trim(),
            request.Apellido.Trim(),
            request.TipoDocumento.Trim(),
            request.NumeroDocumento.Trim(),
            request.Email.Trim().ToLower(),
            request.Telefono.Trim(),
            request.Password
        );

        if (user == null)
        {
            return BadRequest(new { message = "El correo electrónico o número de documento ya está registrado." });
        }

        var token = _jwtService.GenerateToken(user);
        return Ok(new
        {
            token,
            user = new
            {
                user.Id,
                user.Nombre,
                user.Apellido,
                user.NombreCompleto,
                user.Email,
                user.Telefono,
                user.TipoDocumento,
                user.NumeroDocumento,
                user.Role
            }
        });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordApiRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { message = "El correo electrónico es requerido." });

        var user = await _authService.GetUserByEmailAsync(request.Email.Trim());
        if (user != null)
        {
            try
            {
                var token = _tokenService.GenerateToken(user.Id, user.Email);
                var resetLink = Url.Action("ResetPassword", "Auth", new { token }, Request.Scheme) 
                    ?? $"{Request.Scheme}://{Request.Host}/Auth/ResetPassword?token={token}";
                await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar correo de recuperación a {Email}", request.Email);
            }
        }

        return Ok(new { success = true, message = "Si el correo está registrado, recibirás un enlace de recuperación en breve." });
    }
}

public class LoginApiRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class RegisterApiRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Apellido { get; set; } = string.Empty;

    [Required]
    public string TipoDocumento { get; set; } = "CC";

    [Required]
    public string NumeroDocumento { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Telefono { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;
}

public class ForgotPasswordApiRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
