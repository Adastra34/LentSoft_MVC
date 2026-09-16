using LentSoft.Mobile.Models;

namespace LentSoft.Mobile.Services;

public interface IApiService
{
    Task<AuthResultDto> LoginAsync(string email, string password);
    Task<AuthResultDto> RegisterAsync(string nombre, string apellido, string tipoDocumento, string numeroDocumento, string email, string telefono, string password);
    Task<bool> ForgotPasswordAsync(string email);
    Task<List<ProductDto>> GetProductsAsync(string? categoria = null, string? search = null);
    Task<ProductDto?> GetProductByIdAsync(int id);
    Task<List<InvoiceDto>> GetInvoicesAsync();
    Task<AppointmentsResponseDto> GetAppointmentsAsync();
    Task<AppointmentDto?> CreateAppointmentAsync(string servicio, DateTime fechaHora, string? notas);
    Task<bool> CancelAppointmentAsync(int id);
    Task<AppointmentDto?> RescheduleAppointmentAsync(int id, DateTime nuevaFechaHora);
    Task<UserDto?> GetProfileAsync();
    Task<UserDto?> UpdateProfileAsync(string nombre, string? apellido, string? telefono, string? direccion);
}
