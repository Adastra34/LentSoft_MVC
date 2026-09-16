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
}
