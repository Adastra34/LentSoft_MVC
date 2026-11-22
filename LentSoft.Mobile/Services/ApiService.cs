using System.Net;
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
            var response = await _httpClient.PostAsJsonAsync(ApiEndpoints.Login, new
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
            var response = await _httpClient.PostAsJsonAsync(ApiEndpoints.Register, new
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
            var response = await _httpClient.PostAsJsonAsync(ApiEndpoints.ForgotPassword, new { email = email.Trim() });
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
            var url = ApiEndpoints.Products;
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
            return await _httpClient.GetFromJsonAsync<ProductDto>($"{ApiEndpoints.Products}/{id}");
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
            var invoices = await _httpClient.GetFromJsonAsync<List<InvoiceDto>>(ApiEndpoints.Invoices);
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
            var response = await _httpClient.GetFromJsonAsync<AppointmentsResponseDto>(ApiEndpoints.Appointments);
            return response ?? new AppointmentsResponseDto();
        }
        catch
        {
            return new AppointmentsResponseDto();
        }
    }

    public async Task<AppointmentDto?> CreateAppointmentAsync(string servicio, DateTime fechaHora, string? notas)
    {
        var response = await _httpClient.PostAsJsonAsync(ApiEndpoints.Appointments, new
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

    public async Task<bool> CancelAppointmentAsync(int id)
    {
        var response = await _httpClient.PutAsync($"{ApiEndpoints.Appointments}/{id}/cancel", null);
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

    public async Task<AppointmentDto?> RescheduleAppointmentAsync(int id, DateTime nuevaFechaHora)
    {
        var response = await _httpClient.PutAsJsonAsync($"{ApiEndpoints.Appointments}/{id}/reschedule", new
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

    public async Task<UserDto?> GetProfileAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<UserDto>(ApiEndpoints.Profile);
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
            var response = await _httpClient.PutAsJsonAsync(ApiEndpoints.Profile, new
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

    public async Task<FormulaOpticaDto> GetFormulaAsync(CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync(ApiEndpoints.Formula, ct);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException("Sesión expirada o no autorizada. Por favor inicia sesión nuevamente.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(ct);
            string errorMsg = "No fue posible consultar tu fórmula médica en este momento.";
            try
            {
                using var doc = JsonDocument.Parse(errorContent);
                if (doc.RootElement.TryGetProperty("message", out var msgProp))
                {
                    var parsed = msgProp.GetString();
                    if (!string.IsNullOrWhiteSpace(parsed)) errorMsg = parsed;
                }
            }
            catch { }

            throw new InvalidOperationException(errorMsg);
        }

        var formula = await response.Content.ReadFromJsonAsync<FormulaOpticaDto>(cancellationToken: ct);
        return formula ?? new FormulaOpticaDto { HasFormula = false, Message = "No se recibió información de prescripción." };
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

            var response = await _httpClient.PostAsJsonAsync(ApiEndpoints.Chat, new
            {
                mensaje = userText,
                historial = apiHistory
            });

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return "Has alcanzado el límite de consultas del asistente inteligente. Por favor espera un momento antes de volver a consultar.";
            }

            if (response.IsSuccessStatusCode)
            {
                using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
                if (doc.RootElement.TryGetProperty("respuesta", out var respProp))
                {
                    var txt = respProp.GetString();
                    if (!string.IsNullOrWhiteSpace(txt)) return txt;
                }
            }

            return "En este momento el servicio del asistente no pudo procesar tu respuesta. Por favor intenta más tarde.";
        }
        catch (HttpRequestException)
        {
            return "No se pudo conectar con el asistente de salud visual. Verifica tu conexión a internet.";
        }
        catch (Exception)
        {
            return "Ocurrió un error al consultar el asistente virtual. Por favor intenta de nuevo.";
        }
    }
}
