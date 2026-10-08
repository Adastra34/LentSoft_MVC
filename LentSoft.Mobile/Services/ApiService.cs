using System.Net.Http.Json;
using System.Text.Json;
using LentSoft.Mobile.Models;

namespace LentSoft.Mobile.Services;

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly IAuthStorageService _storageService;

    public ApiService(HttpClient httpClient, IAuthStorageService storageService)
    {
        _httpClient = httpClient;
        _storageService = storageService;
    }

    public async Task<AuthResultDto> LoginAsync(string email, string password)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", new
            {
                email = email.Trim(),
                password
            });

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                string errorMsg = "Credenciales incorrectas o error en el inicio de sesión.";
                try
                {
                    using var doc = JsonDocument.Parse(errorContent);
                    if (doc.RootElement.TryGetProperty("message", out var msgProp))
                        errorMsg = msgProp.GetString() ?? errorMsg;
                }
                catch { }

                return new AuthResultDto { Message = errorMsg };
            }

            var result = await response.Content.ReadFromJsonAsync<AuthResultDto>();
            if (result != null && !string.IsNullOrWhiteSpace(result.Token))
            {
                await _storageService.SaveTokenAsync(result.Token);
                if (result.User != null)
                {
                    await _storageService.SaveUserAsync(result.User);
                }
            }

            return result ?? new AuthResultDto { Message = "Respuesta vacía del servidor." };
        }
        catch (Exception ex)
        {
            return new AuthResultDto { Message = ex.Message };
        }
    }

    public async Task<AuthResultDto> RegisterAsync(
        string nombre, 
        string apellido, 
        string tipoDocumento, 
        string numeroDocumento, 
        string email, 
        string telefono, 
        string password)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/register", new
            {
                nombre = nombre.Trim(),
                apellido = apellido.Trim(),
                tipoDocumento = tipoDocumento.Trim(),
                numeroDocumento = numeroDocumento.Trim(),
                email = email.Trim().ToLower(),
                telefono = telefono.Trim(),
                password
            });

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                string errorMsg = "No se pudo registrar el usuario.";
                try
                {
                    using var doc = JsonDocument.Parse(errorContent);
                    if (doc.RootElement.TryGetProperty("message", out var msgProp))
                        errorMsg = msgProp.GetString() ?? errorMsg;
                }
                catch { }

                return new AuthResultDto { Message = errorMsg };
            }

            var result = await response.Content.ReadFromJsonAsync<AuthResultDto>();
            if (result != null && !string.IsNullOrWhiteSpace(result.Token))
            {
                await _storageService.SaveTokenAsync(result.Token);
                if (result.User != null)
                {
                    await _storageService.SaveUserAsync(result.User);
                }
            }

            return result ?? new AuthResultDto { Message = "Registro exitoso pero no se obtuvo respuesta del servidor." };
        }
        catch (Exception ex)
        {
            return new AuthResultDto { Message = ex.Message };
        }
    }

    public async Task<bool> ForgotPasswordAsync(string email)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/forgot-password", new { email = email.Trim() });
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<ProductDto>> GetProductsAsync(string? categoria = null, string? search = null)
    {
        try
        {
            var url = "api/products";
            var queryParams = new List<string>();
            if (!string.IsNullOrWhiteSpace(categoria) && !categoria.Equals("todos", StringComparison.OrdinalIgnoreCase))
            {
                queryParams.Add($"categoria={Uri.EscapeDataString(categoria)}");
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                queryParams.Add($"search={Uri.EscapeDataString(search)}");
            }

            if (queryParams.Any())
            {
                url += "?" + string.Join("&", queryParams);
            }

            var products = await _httpClient.GetFromJsonAsync<List<ProductDto>>(url);
            return products ?? new List<ProductDto>();
        }
        catch
        {
            return new List<ProductDto>();
        }
    }

    public async Task<ProductDto?> GetProductByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ProductDto>($"api/products/{id}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<InvoiceDto>> GetInvoicesAsync()
    {
        try
        {
            var invoices = await _httpClient.GetFromJsonAsync<List<InvoiceDto>>("api/invoices");
            return invoices ?? new List<InvoiceDto>();
        }
        catch
        {
            return new List<InvoiceDto>();
        }
    }

    public async Task<AppointmentsResponseDto> GetAppointmentsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<AppointmentsResponseDto>("api/appointments");
            return response ?? new AppointmentsResponseDto();
        }
        catch
        {
            return new AppointmentsResponseDto();
        }
    }

    public async Task<AppointmentDto?> CreateAppointmentAsync(string servicio, DateTime fechaHora, string? notas)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/appointments", new
            {
                servicio = servicio.Trim(),
                fechaHora,
                notas = notas?.Trim()
            });

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                string errorMsg = "Error al agendar la cita.";
                try
                {
                    using var doc = JsonDocument.Parse(errorContent);
                    if (doc.RootElement.TryGetProperty("message", out var msgProp))
                        errorMsg = msgProp.GetString() ?? errorMsg;
                }
                catch { }

                throw new InvalidOperationException(errorMsg);
            }

            return await response.Content.ReadFromJsonAsync<AppointmentDto>();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<bool> CancelAppointmentAsync(int id)
    {
        try
        {
            var response = await _httpClient.PutAsync($"api/appointments/{id}/cancel", null);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                string errorMsg = "Error al cancelar la cita.";
                try
                {
                    using var doc = JsonDocument.Parse(errorContent);
                    if (doc.RootElement.TryGetProperty("message", out var msgProp))
                        errorMsg = msgProp.GetString() ?? errorMsg;
                }
                catch { }

                throw new InvalidOperationException(errorMsg);
            }

            return true;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<AppointmentDto?> RescheduleAppointmentAsync(int id, DateTime nuevaFechaHora)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/appointments/{id}/reschedule", new
            {
                nuevaFechaHora
            });

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                string errorMsg = "Error al reprogramar la cita.";
                try
                {
                    using var doc = JsonDocument.Parse(errorContent);
                    if (doc.RootElement.TryGetProperty("message", out var msgProp))
                        errorMsg = msgProp.GetString() ?? errorMsg;
                }
                catch { }

                throw new InvalidOperationException(errorMsg);
            }

            return await response.Content.ReadFromJsonAsync<AppointmentDto>();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<UserDto?> GetProfileAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<UserDto>("api/users/me");
        }
        catch
        {
            return await _storageService.GetUserAsync();
        }
    }

    public async Task<UserDto?> UpdateProfileAsync(string nombre, string? apellido, string? telefono, string? direccion)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync("api/users/me", new
            {
                nombre = nombre.Trim(),
                apellido = apellido?.Trim(),
                telefono = telefono?.Trim(),
                direccion = direccion?.Trim()
            });

            if (response.IsSuccessStatusCode)
            {
                var user = await response.Content.ReadFromJsonAsync<UserDto>();
                if (user != null)
                {
                    await _storageService.SaveUserAsync(user);
                    return user;
                }
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<string> AskChatBotAsync(string mensaje, List<ChatMessageDto>? history = null)
    {
        if (string.IsNullOrWhiteSpace(mensaje)) return "Por favor escribe una consulta.";

        string userText = mensaje.Trim();

        try
        {
            var apiHistory = history?
                .TakeLast(6)
                .Select(m => new { role = m.IsUser ? "user" : "assistant", content = m.Message })
                .ToList() ?? new();

            var response = await _httpClient.PostAsJsonAsync("Chat/Ask", new
            {
                mensaje = userText,
                historial = apiHistory
            });

            if (response.IsSuccessStatusCode)
            {
                using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
                if (doc.RootElement.TryGetProperty("respuesta", out var respProp))
                {
                    var txt = respProp.GetString();
                    if (!string.IsNullOrWhiteSpace(txt)) return txt;
                }
            }
        }
        catch { }

        // Comprehensive Smart Assistant Engine for Mobile
        string lower = userText.ToLower(new System.Globalization.CultureInfo("es-CO"));

        if (lower.Contains("producto") || lower.Contains("catalogo") || lower.Contains("ofrecen") || lower.Contains("tienen") || lower.Contains("venden") || lower.Contains("poseen"))
        {
            return "En LentSoft contamos con un catálogo completo de salud visual:\n\n" +
                   "👓 Monturas Graduadas: Diseños Classic, Oakley Sport y gafas de sol elegantes.\n" +
                   "👁️ Lentes de Contacto: Marcas reconocidas como Acuvue (diarios y de uso mensual).\n" +
                   "🧼 Accesorios y Limpieza: Estuches Premium y líquido limpiador para el cuidado de tus lentes.\n\n" +
                   "¿Deseas detalles o precios sobre alguna categoría en especial?";
        }

        if (lower.Contains("precio") || lower.Contains("cuanto cuesta") || lower.Contains("valor") || lower.Contains("costo"))
        {
            return "Nuestros precios varían según el producto y categoría:\n\n" +
                   "• Estuche Premium: $99.000 COP\n" +
                   "• Líquido Limpiador: $120.000 COP\n" +
                   "• Lentes de Contacto Acuvue: $399.000 COP\n" +
                   "• Lentes Graduados Classic: $1.200.000 COP\n" +
                   "• Montura Oakley Sport: $1.800.000 COP\n" +
                   "• Lentes Ray-Ban Aviator: $2.500.000 COP\n\n" +
                   "¡Todos incluyen envío gratis y opción de pago simulado en la app!";
        }

        if (lower.Contains("cita") || lower.Contains("optometra") || lower.Contains("examen") || lower.Contains("agendar") || lower.Contains("doctor"))
        {
            return "Puedes agendar tu cita de valoración visual con nuestros optómetras directamente en la pestaña 'Citas' 🗓️. Podrás seleccionar la fecha, hora y especialista de tu preferencia.";
        }

        if (lower.Contains("envio") || lower.Contains("entrega") || lower.Contains("domicilio") || lower.Contains("despacho"))
        {
            return "🚚 ¡El envío es 100% GRATIS a nivel nacional! Tu pedido llegará directamente a tu domicilio en un plazo estimado de 3 a 5 días hábiles.";
        }

        if (lower.Contains("garantia") || lower.Contains("devolucion") || lower.Contains("calidad"))
        {
            return "🛡️ Todas nuestras monturas y lentes cuentan con 12 meses de garantía contra defectos de fabricación. Además, nuestros materiales están avalados por profesionales de la salud visual.";
        }

        if (lower.Contains("contacto") || lower.Contains("acuvue"))
        {
            return "👁️ Ofrecemos lentes de contacto de marcas líderes como Acuvue. Disponibles para corrección de miopía, astigmatismo y uso diario con máxima hidratación.";
        }

        if (lower.Contains("sol") || lower.Contains("ray ban") || lower.Contains("ray-ban") || lower.Contains("aviator") || lower.Contains("gafas"))
        {
            return "🕶️ En la categoría 'Sol' encontrarás modelos emblemáticos como los Ray-Ban Aviator y monturas deportivas con 100% de protección UV400.";
        }

        if (lower.Contains("hola") || lower.Contains("buenas") || lower.Contains("saludos"))
        {
            return "¡Hola! 👋 Qué gusto saludarte. Soy Morgana, tu asesora de óptica en LentSoft. ¿En qué te puedo asesorar hoy? (Productos, Citas, Envíos o Garantías)";
        }

        return $"Entiendo tu consulta sobre \"{userText}\". Puedo orientarte sobre nuestro catálogo de monturas, lentes de contacto, agendamiento de citas médicas o estado de envíos. ¿Sobre qué área te gustaría saber más?";
    }
}
