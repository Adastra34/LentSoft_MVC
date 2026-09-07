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

    public bool IsPagada => EstadoRaw.Equals("pagada", StringComparison.OrdinalIgnoreCase);

    public string BadgeBackgroundColor => IsPagada ? "#DCFCE7" : "#FEF3C7";
    public string BadgeTextColor => IsPagada ? "#2FBF71" : "#D97706";

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

    public string DisplayFecha => FechaEmision.ToLocalTime().ToString("dd MMM yyyy", new CultureInfo("es-CO"));
}
