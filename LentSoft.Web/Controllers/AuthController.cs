using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LentSoft.Web.Models.ViewModels;
using LentSoft.Web.Services;

namespace LentSoft.Web.Controllers;

/// <summary>
/// Controlador web de autenticación, registro y recuperación de contraseña.
/// </summary>
public class AuthController : Controller
{
    private readonly IAuthService _authService;
    private readonly IPasswordResetTokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly ILoginAttemptService _loginAttemptService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IPasswordResetTokenService tokenService,
        IEmailService emailService,
        ILoginAttemptService loginAttemptService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _tokenService = tokenService;
        _emailService = emailService;
        _loginAttemptService = loginAttemptService;
        _logger = logger;
    }

    /// <summary>
    /// Muestra el formulario de inicio de sesión.
    /// Si el usuario ya está autenticado, lo redirige a su panel correspondiente según su rol.
    /// </summary>
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectBasedOnRole();
        }
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    /// <summary>
    /// Procesa las credenciales de inicio de sesión aplicando protección de bloqueo temporal por intentos fallidos.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var clientKey = $"{model.Email?.Trim().ToLowerInvariant()}:{ip}";

        if (_loginAttemptService.IsLockedOut(clientKey, out var remaining))
        {
            var minutes = Math.Max(1, (int)Math.Ceiling(remaining.TotalMinutes));
            ModelState.AddModelError(string.Empty, $"Demasiados intentos fallidos. Intenta de nuevo en {minutes} minuto(s).");
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        var user = await _authService.LoginAsync(model.Email, model.Password);

        if (user == null)
        {
            _loginAttemptService.RegisterFailure(clientKey);

            if (_loginAttemptService.IsLockedOut(clientKey, out var rem))
            {
                _logger.LogWarning("Bloqueo temporal activado por demasiados intentos fallidos para el correo {Email} desde la IP {IpAddress}.", model.Email?.Trim().ToLowerInvariant(), ip);
                var minutes = Math.Max(1, (int)Math.Ceiling(rem.TotalMinutes));
                ModelState.AddModelError(string.Empty, $"Demasiados intentos fallidos. Intenta de nuevo en {minutes} minuto(s).");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Credenciales inválidas. Intenta nuevamente.");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        // Inicio de sesión exitoso: reiniciar contador de fallos
        _loginAttemptService.Reset(clientKey);

        // Crear cookie de autenticación con claims del usuario
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, string.IsNullOrEmpty(user.Apellido) ? user.Nombre : user.NombreCompleto),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Role, user.Role)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
            });

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectBasedOnRole(user.Role);
    }

    /// <summary>
    /// Muestra el formulario de registro de nuevos clientes.
    /// </summary>
    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectBasedOnRole();
        }
        return View();
    }

    /// <summary>
    /// Procesa el registro de clientes garantizando que el rol asignado sea siempre "usuario".
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _authService.RegisterAsync(
            model.Nombre,
            model.Apellido,
            model.TipoDocumento,
            model.NumeroDocumento,
            model.Email,
            model.Telefono,
            model.Password);

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Error al crear la cuenta. El email o el número de documento ya están registrados.");
            return View(model);
        }

        // Auto-login posterior al registro exitoso
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.NombreCompleto),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal);

        TempData["SuccessMessage"] = "Cuenta creada exitosamente.";
        return RedirectToAction("Usuario", "Dashboard");
    }

    /// <summary>
    /// Cierra la sesión activa del usuario y elimina la cookie de autenticación.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    // ══════════════════════════════════════════
    //  FLUJO DE RECUPERACIÓN DE CONTRASEÑA
    // ══════════════════════════════════════════

    /// <summary>
    /// Muestra la vista para solicitar el enlace de recuperación de contraseña.
    /// </summary>
    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    /// <summary>
    /// Genera y envía por correo el token de recuperación de contraseña de un solo uso con huella digital (pwdfp).
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        const string successMessage = "Si el correo está registrado, recibirás un enlace de recuperación en breve.";

        if (string.IsNullOrWhiteSpace(email))
        {
            ViewBag.ErrorMessage = "Por favor ingresa un correo electrónico.";
            return View();
        }

        var user = await _authService.GetUserByEmailAsync(email.Trim());

        if (user != null)
        {
            try
            {
                var token = _tokenService.GenerateToken(user.Id, user.Email, user.PasswordHash);
                var resetLink = Url.Action("ResetPassword", "Auth", new { token }, Request.Scheme)!;
                await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar correo de recuperación para {Email}", email);
            }
        }

        ViewBag.SuccessMessage = successMessage;
        return View();
    }

    /// <summary>
    /// Muestra el formulario para ingresar la nueva contraseña, validando el token y su fingerprint.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ResetPassword(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            ViewBag.TokenValid = false;
            return View();
        }

        var userId = _tokenService.ValidateToken(token);
        if (userId == null)
        {
            ViewBag.TokenValid = false;
            return View();
        }

        var user = await _authService.GetUserByIdAsync(userId.Value);
        if (user == null || !_tokenService.MatchesCurrentPassword(token, user.PasswordHash))
        {
            ViewBag.TokenValid = false;
            return View();
        }

        ViewBag.TokenValid = true;
        ViewBag.Token = token;
        return View();
    }

    /// <summary>
    /// Procesa el cambio de contraseña garantizando el token de un solo uso.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string token, string newPassword, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            ViewBag.TokenValid = false;
            return View();
        }

        var userId = _tokenService.ValidateToken(token);
        if (userId == null)
        {
            ViewBag.TokenValid = false;
            return View();
        }

        var user = await _authService.GetUserByIdAsync(userId.Value);
        if (user == null || !_tokenService.MatchesCurrentPassword(token, user.PasswordHash))
        {
            ViewBag.TokenValid = false;
            return View();
        }

        // Validar fortaleza de la nueva contraseña
        var isStrongPassword = System.Text.RegularExpressions.Regex.IsMatch(
            newPassword ?? string.Empty,
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$");

        if (!isStrongPassword)
        {
            ViewBag.TokenValid = true;
            ViewBag.Token = token;
            ViewBag.ErrorMessage = "La contraseña debe tener al menos 8 caracteres e incluir mayúsculas, minúsculas, números y un carácter especial.";
            return View();
        }

        if (newPassword != confirmPassword)
        {
            ViewBag.TokenValid = true;
            ViewBag.Token = token;
            ViewBag.ErrorMessage = "Las contraseñas no coinciden.";
            return View();
        }

        // Actualizar contraseña: el nuevo hash invalidará automáticamente el token para futuros intentos
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _authService.UpdateUserAsync(user);

        TempData["SuccessMessage"] = "Tu contraseña ha sido restablecida exitosamente. Inicia sesión con tu nueva contraseña.";
        return RedirectToAction("Login");
    }

    private IActionResult RedirectBasedOnRole(string? role = null)
    {
        role ??= User.FindFirstValue(ClaimTypes.Role);
        return role switch
        {
            "admin" => RedirectToAction("Admin", "Dashboard"),
            "optometra" => RedirectToAction("Index", "Optometra"),
            "ventas" => RedirectToAction("Index", "Ventas"),
            _ => RedirectToAction("Usuario", "Dashboard")
        };
    }
}
