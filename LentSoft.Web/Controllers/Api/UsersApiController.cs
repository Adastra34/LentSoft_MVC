using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LentSoft.Web.Services;

namespace LentSoft.Web.Controllers.Api;

[ApiController]
[Route("api/users")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class UsersApiController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAuthService _authService;

    public UsersApiController(IUserService userService, IAuthService authService)
    {
        _userService = userService;
        _authService = authService;
    }

    private int GetCurrentUserId()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idStr, out var id) ? id : 0;
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
