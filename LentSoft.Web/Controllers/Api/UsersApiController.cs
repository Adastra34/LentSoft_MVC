using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Services;

namespace LentSoft.Web.Controllers.Api;

[ApiController]
[Route("api/users")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class UsersApiController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAuthService _authService;
    private readonly LentSoftDbContext _context;
    private readonly ILogger<UsersApiController> _logger;

    public UsersApiController(
        IUserService userService,
        IAuthService authService,
        LentSoftDbContext context,
        ILogger<UsersApiController> logger)
    {
        _userService = userService;
        _authService = authService;
        _context = context;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idStr, out var id) ? id : 0;
    }

    [HttpGet("me/formula")]
    public async Task<IActionResult> GetMyFormula()
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        try
        {
            var formula = await _context.FormulasOpticas
                .AsNoTracking()
                .Include(f => f.Optometra)
                .Where(f => f.UserId == userId && f.Activo)
                .OrderByDescending(f => f.Fecha)
                .ThenByDescending(f => f.Id)
                .FirstOrDefaultAsync();

            Response.Headers["Cache-Control"] = "no-store";

            if (formula == null)
            {
                return Ok(new { hasFormula = false });
            }

            var diasParaVencer = (formula.Fecha.AddMonths(12).Date - DateTime.UtcNow.Date).Days;
            var fechaVenc = formula.Fecha.AddMonths(12).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

            return Ok(new
            {
                hasFormula = true,
                formulaId = formula.Id,
                fecha = formula.Fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                fechaVencimiento = fechaVenc,
                diasParaVencer,
                estado = formula.Estado,
                esferaOD = formula.EsferaOD,
                cilindroOD = formula.CilindroOD,
                ejeOD = formula.EjeOD,
                adicionOD = formula.AdicionOD,
                esferaOI = formula.EsferaOI,
                cilindroOI = formula.CilindroOI,
                ejeOI = formula.EjeOI,
                adicionOI = formula.AdicionOI,
                tipoLente = formula.TipoLente,
                distanciaPupilar = FormattedDistanciaPupilar(formula.DistanciaPupilar),
                observaciones = formula.Observaciones,
                optometraNombre = GetOptometraNombre(formula.Optometra)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar la fórmula óptica del usuario {UserId}", userId);
            return StatusCode(500, new { message = "No se pudo consultar la fórmula óptica." });
        }
    }

    private static string? FormattedDistanciaPupilar(string? dp)
    {
        if (string.IsNullOrWhiteSpace(dp)) return null;
        var trimmed = dp.Trim();
        if (trimmed.EndsWith("mm", StringComparison.OrdinalIgnoreCase))
            return trimmed;
        if (decimal.TryParse(trimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out _) || int.TryParse(trimmed, out _))
            return $"{trimmed} mm";
        return trimmed;
    }

    private static string? GetOptometraNombre(User? optometra)
    {
        if (optometra == null) return null;
        var nombre = optometra.NombreCompleto;
        if (string.IsNullOrWhiteSpace(nombre))
            nombre = $"{optometra.Nombre} {optometra.Apellido}".Trim();
        if (string.IsNullOrWhiteSpace(nombre))
            return null;
        return nombre.StartsWith("Dr", StringComparison.OrdinalIgnoreCase) ? nombre : $"Dr(a). {nombre}";
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var user = await _userService.GetByIdAsync(userId);
        if (user == null || !user.Activo)
            return NotFound(new { message = "Usuario no encontrado." });

        return Ok(new
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
            user.EPS,
            user.Genero,
            user.FechaNacimiento,
            user.Role
        });
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateCurrentUser([FromBody] UpdateUserApiRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var user = await _userService.GetByIdAsync(userId);
        if (user == null || !user.Activo)
            return NotFound(new { message = "Usuario no encontrado." });

        user.Nombre = request.Nombre.Trim();
        if (!string.IsNullOrWhiteSpace(request.Apellido))
            user.Apellido = request.Apellido.Trim();
        if (!string.IsNullOrWhiteSpace(request.Telefono))
            user.Telefono = request.Telefono.Trim();
        if (request.Direccion != null)
            user.Direccion = request.Direccion.Trim();

        await _authService.UpdateUserAsync(user);

        return Ok(new
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
            user.Role,
            Message = "Perfil actualizado exitosamente."
        });
    }
}

public class UpdateUserApiRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Nombre { get; set; } = string.Empty;

    public string? Apellido { get; set; }

    public string? Telefono { get; set; }

    public string? Direccion { get; set; }
}
