using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LentSoft.Web.Models.ViewModels;
using LentSoft.Web.Services;

namespace LentSoft.Web.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _authService;
    private readonly IPasswordResetTokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IPasswordResetTokenService tokenService,
        IEmailService emailService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _tokenService = tokenService;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Show login form — migrated from Views/login.html
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
    /// Process login — migrated from login.html form submit + js/auth.js login()
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

        var user = await _authService.LoginAsync(model.Email, model.Password);

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Credenciales inválidas. Intenta nuevamente.");
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        // Create authentication cookie with claims
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, string.IsNullOrEmpty(user.Apellido) ? user.Nombre : user.NombreCompleto),
            new(ClaimTypes.Email, user.Email),
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
    /// Show registration form — migrated from Views/registro.html
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
    /// Process registration — migrated from registro.html form submit + AuthController.js register()
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

        // Auto-login after registration
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
    /// Logout — migrated from js/auth.js AuthService.logout()
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
    //  PASSWORD RESET FLOW
    // ══════════════════════════════════════════

    /// <summary>
    /// Show forgot password form
    /// </summary>
    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    /// <summary>
    /// Process forgot password request — generates JWT token and sends email.
    /// Always shows success message regardless of whether email exists (security).
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
                var token = _tokenService.GenerateToken(user.Id, user.Email);
                var resetLink = Url.Action("ResetPassword", "Auth", new { token }, Request.Scheme)!;
                await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar correo de recuperación para {Email}", email);
                // Don't reveal the error to the user for security
            }
        }

        // Always show success (security: don't reveal if email exists)
        ViewBag.SuccessMessage = successMessage;
        return View();
    }

    /// <summary>
    /// Show reset password form (validates JWT token from URL)
    /// </summary>
    [HttpGet]
    public IActionResult ResetPassword(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            ViewBag.TokenValid = false;
            return View();
        }

        var userId = _tokenService.ValidateToken(token);
        ViewBag.TokenValid = userId != null;
        ViewBag.Token = token;
        return View();
    }

    /// <summary>
    /// Process password reset — validates token again, updates password hash
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

        // Validate passwords
        var isStrongPassword = System.Text.RegularExpressions.Regex.IsMatch(newPassword ?? "", @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$");
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

        // Update password
        var user = await _authService.GetUserByIdAsync(userId.Value);
        if (user == null)
        {
            ViewBag.TokenValid = false;
            return View();
        }

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
