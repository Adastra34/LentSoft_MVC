using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LentSoft.Web.Services;

public class ChatAgentService : IChatAgentService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ChatAgentService> _logger;

    private const string SystemPrompt = @"Eres Morgana, la asistente virtual de LentSoft, una óptica digital colombiana.

SOBRE LENTSOFT:
- Vendemos gafas formuladas, gafas de sol y lentes de contacto.
- Ofrecemos una función de prueba virtual de monturas con cámara web.
- Los clientes pueden agendar citas con optómetras desde su dashboard.
- El registro y la navegación de la tienda son gratuitos.
- Los filtros de la tienda permiten buscar por categoría, marca y precio.
- Para agregar al carrito, guardar favoritos o agendar citas se necesita cuenta.
- La recuperación de contraseña se hace desde el formulario de login.

TU PERSONALIDAD:
- Eres cercana, cordial y profesional.
- Usas emojis con moderación para ser amigable (1-2 por respuesta máximo).
- Respondes en español de forma clara y concisa.
- Si el usuario saluda, responde con calidez y ofrece ayuda.

RESTRICCIONES IMPORTANTES:
- NO inventes precios, stock ni disponibilidad de citas si no los tienes confirmados.
- Si te preguntan por precios o disponibilidad específica, sugiere amablemente que consulten la tienda o agenden una cita.
- NO proporciones información médica ni diagnósticos visuales.
- Si no sabes algo, dilo honestamente y sugiere contactar soporte o explorar la tienda.
- Mantén tus respuestas breves (máximo 2-3 párrafos cortos).";

    public ChatAgentService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<ChatAgentService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> GetResponseAsync(string userMessage, List<ChatMessageDto> history, string? userName)
    {
        var apiKey = _configuration["Gemini:ApiKey"]!;
        var client = _httpClientFactory.CreateClient("Gemini");

        // Construir el system prompt personalizado
        var systemPromptFinal = SystemPrompt;
        if (!string.IsNullOrEmpty(userName))
        {
            systemPromptFinal += $"\n\nEl usuario autenticado se llama {userName}. Puedes personalizar tu saludo usando su nombre.";
        }

        // Construir el array de contenidos (historial + mensaje actual)
        var contents = new List<GeminiContent>();

        foreach (var msg in history)
        {
            var role = msg.Role == "assistant" ? "model" : "user";
            contents.Add(new GeminiContent
            {
                Role = role,
                Parts = new List<GeminiPart> { new() { Text = msg.Content } }
            });
        }

        // Agregar el mensaje actual del usuario
        contents.Add(new GeminiContent
        {
            Role = "user",
            Parts = new List<GeminiPart> { new() { Text = userMessage } }
        });

        var requestBody = new GeminiRequest
        {
            SystemInstruction = new GeminiContent
            {
                Parts = new List<GeminiPart> { new() { Text = systemPromptFinal } }
            },
            Contents = contents,
            GenerationConfig = new GeminiGenerationConfig
            {
                MaxOutputTokens = 512,
                Temperature = 0.7
            }
        };

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var jsonContent = JsonSerializer.Serialize(requestBody, jsonOptions);
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.6-flash:generateContent?key={apiKey}";

        try
        {
            var response = await client.PostAsync(url, httpContent);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Error de Gemini API — Status: {Status}, Body: {Body}", response.StatusCode, responseBody);
                throw new HttpRequestException($"Gemini API respondió con código {(int)response.StatusCode}");
            }

            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var text = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

            if (string.IsNullOrEmpty(text))
            {
                _logger.LogWarning("Gemini API devolvió una respuesta vacía. Body: {Body}", responseBody);
                return "Lo siento, no pude generar una respuesta en este momento. ¿Podrías intentar de nuevo? 🤔";
            }

            return text;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout al llamar a Gemini API");
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error HTTP al llamar a Gemini API");
            throw;
        }
    }

    // ── DTOs internos para serialización/deserialización de Gemini API ──

    private class GeminiRequest
    {
        [JsonPropertyName("system_instruction")]
        public GeminiContent? SystemInstruction { get; set; }

        public List<GeminiContent> Contents { get; set; } = new();

        [JsonPropertyName("generationConfig")]
        public GeminiGenerationConfig? GenerationConfig { get; set; }
    }

    private class GeminiContent
    {
        public string? Role { get; set; }
        public List<GeminiPart> Parts { get; set; } = new();
    }

    private class GeminiPart
    {
        public string Text { get; set; } = string.Empty;
    }

    private class GeminiGenerationConfig
    {
        public int MaxOutputTokens { get; set; }
        public double Temperature { get; set; }
    }

    private class GeminiResponse
    {
        public List<GeminiCandidate>? Candidates { get; set; }
    }

    private class GeminiCandidate
    {
        public GeminiContent? Content { get; set; }
    }
}
