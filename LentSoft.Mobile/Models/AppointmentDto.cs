using System.Globalization;
using System.Text.Json.Serialization;

namespace LentSoft.Mobile.Models;

public class AppointmentDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("servicio")]
    public string Servicio { get; set; } = string.Empty;

    [JsonPropertyName("fechaHora")]
    public DateTime FechaHora { get; set; }

    [JsonPropertyName("estado")]
    public string Estado { get; set; } = "Pendiente";

    [JsonPropertyName("estadoRaw")]
    public string EstadoRaw { get; set; } = "pendiente";

    [JsonPropertyName("notas")]
    public string? Notas { get; set; }

    [JsonPropertyName("optometraNombre")]
    public string OptometraNombre { get; set; } = "Optómetra de turno";

    public bool IsAsistio => EstadoRaw.Equals("completada", StringComparison.OrdinalIgnoreCase) || Estado.Equals("Asistió", StringComparison.OrdinalIgnoreCase);
    public bool IsCancelada => EstadoRaw.Equals("cancelada", StringComparison.OrdinalIgnoreCase) || Estado.Equals("Cancelada", StringComparison.OrdinalIgnoreCase);
    public bool CanManage => EstadoRaw.Equals("pendiente", StringComparison.OrdinalIgnoreCase) || EstadoRaw.Equals("confirmada", StringComparison.OrdinalIgnoreCase);

    public string BadgeBackgroundColor => IsCancelada ? "#FEE2E2" : (IsAsistio ? "#DCFCE7" : "#FEF3C7");
    public string BadgeTextColor => IsCancelada ? "#DC2626" : (IsAsistio ? "#2FBF71" : "#D97706");

    public string DisplayFechaHora => FechaHora.ToLocalTime().ToString("dddd, dd MMMM yyyy - hh:mm tt", new CultureInfo("es-CO"));
    public string DisplayFechaCorta => FechaHora.ToLocalTime().ToString("dd MMM yyyy, hh:mm tt", new CultureInfo("es-CO"));
}

public class AppointmentsResponseDto
{
    [JsonPropertyName("proximaCita")]
    public AppointmentDto? ProximaCita { get; set; }

    [JsonPropertyName("historial")]
    public List<AppointmentDto> Historial { get; set; } = new();
}

public class CreateAppointmentRequestDto
{
    public string Servicio { get; set; } = string.Empty;
    public DateTime FechaHora { get; set; }
    public string? Notas { get; set; }
}
