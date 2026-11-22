using System.Globalization;
using System.Text.Json.Serialization;

namespace LentSoft.Mobile.Models;

public class InvoiceDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("numeroFactura")]
    public string NumeroFactura { get; set; } = string.Empty;

    [JsonPropertyName("orderId")]
    public int OrderId { get; set; }

    [JsonPropertyName("subtotal")]
    public decimal Subtotal { get; set; }

    [JsonPropertyName("impuestos")]
    public decimal Impuestos { get; set; }

    [JsonPropertyName("total")]
    public decimal Total { get; set; }

    [JsonPropertyName("estado")]
    public string Estado { get; set; } = "Pendiente";

    [JsonPropertyName("estadoRaw")]
    public string EstadoRaw { get; set; } = "pendiente";

    [JsonPropertyName("fechaEmision")]
    public DateTime FechaEmision { get; set; }

    [JsonPropertyName("fechaPago")]
    public DateTime? FechaPago { get; set; }

    [JsonPropertyName("metodoPago")]
    public string? MetodoPago { get; set; }

    [JsonPropertyName("itemsSummary")]
    public string? ItemsSummary { get; set; }

    public bool IsPagada => EstadoRaw.Equals("pagada", StringComparison.OrdinalIgnoreCase);
    public bool IsParcial => EstadoRaw.Equals("parcial", StringComparison.OrdinalIgnoreCase);
    public bool IsCancelada => EstadoRaw.Equals("cancelada", StringComparison.OrdinalIgnoreCase) || EstadoRaw.Equals("anulada", StringComparison.OrdinalIgnoreCase);

    public string BadgeBackgroundColor
    {
        get
        {
            if (IsPagada) return "#DCFCE7";
            if (IsParcial) return "#DBEAFE";
            if (IsCancelada) return "#FEE2E2";
            return "#FEF3C7"; // Pendiente
        }
    }

    public string BadgeTextColor
    {
        get
        {
            if (IsPagada) return "#03543F";
            if (IsParcial) return "#1E40AF";
            if (IsCancelada) return "#DC2626";
            return "#D97706"; // Pendiente
        }
    }

    public string DisplayTotal
    {
        get
        {
            var culture = new CultureInfo("es-CO");
            culture.NumberFormat.CurrencySymbol = "$ ";
            culture.NumberFormat.CurrencyDecimalDigits = 0;
            culture.NumberFormat.CurrencyGroupSeparator = ".";
            return Total.ToString("C0", culture) + " COP";
        }
    }

    public string DisplayFechaEmision => FechaEmision.ToString("dd/MM/yyyy HH:mm", new CultureInfo("es-CO"));
}
