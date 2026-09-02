namespace LentSoft.Web.Services;

/// <summary>
/// DTO que representa un mensaje individual en el historial de conversación.
/// </summary>
public record ChatMessageDto(string Role, string Content);

/// <summary>
/// DTO para la petición entrante desde el cliente al endpoint /Chat/Ask.
/// </summary>
public record ChatRequestDto(string Mensaje, List<ChatMessageDto> Historial);

/// <summary>
/// Servicio de agente de chat con IA para Morgana.
/// </summary>
public interface IChatAgentService
{
    /// <summary>
    /// Envía el mensaje del usuario (con historial de conversación) al modelo de IA
    /// y devuelve la respuesta generada.
    /// </summary>
    /// <param name="userMessage">Mensaje actual del usuario.</param>
    /// <param name="history">Historial corto de la conversación (mensajes previos).</param>
    /// <param name="userName">Nombre del usuario autenticado, o null si es visitante.</param>
    /// <returns>Texto de respuesta del modelo de IA.</returns>
    Task<string> GetResponseAsync(string userMessage, List<ChatMessageDto> history, string? userName);
}
