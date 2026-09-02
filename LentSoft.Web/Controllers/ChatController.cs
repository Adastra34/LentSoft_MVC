using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using LentSoft.Web.Services;

namespace LentSoft.Web.Controllers;

public class ChatController : Controller
{
    private readonly IChatAgentService _chatAgentService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatAgentService chatAgentService, ILogger<ChatController> logger)
    {
        _chatAgentService = chatAgentService;
        _logger = logger;
    }

    /// <summary>
    /// Endpoint POST para que Morgana procese un mensaje del usuario vía IA.
    /// No requiere autenticación (funciona para visitantes).
    /// </summary>
    [HttpPost]
    [EnableRateLimiting("chat")]
    public async Task<IActionResult> Ask([FromBody] ChatRequestDto request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Mensaje))
        {
            return BadRequest(new { respuesta = "El mensaje no puede estar vacío." });
        }

        // Si el usuario está autenticado, pasar su nombre para personalizar la respuesta
        string? userName = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            userName = User.FindFirst(ClaimTypes.Name)?.Value;
        }

        var historial = request.Historial ?? new List<ChatMessageDto>();

        try
        {
            var respuesta = await _chatAgentService.GetResponseAsync(request.Mensaje, historial, userName);
            return Json(new { respuesta });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar mensaje de chat: {Mensaje}", request.Mensaje);
            return StatusCode(500, new { respuesta = "Lo siento, ocurrió un error al procesar tu mensaje. Por favor intenta de nuevo en unos momentos. 🙁" });
        }
    }
}
