using System.Globalization;
using System.Text.Json.Serialization;

namespace LentSoft.Mobile.Models;

public class ProductDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("descripcion")]
    public string? Descripcion { get; set; }

    [JsonPropertyName("precio")]
    public decimal Precio { get; set; }

    [JsonPropertyName("precioDescuento")]
    public decimal? PrecioDescuento { get; set; }

    [JsonPropertyName("precioFinal")]
    public decimal PrecioFinal { get; set; }

    [JsonPropertyName("descuentoPorcentaje")]
    public int DescuentoPorcentaje { get; set; }

    [JsonPropertyName("categoria")]
    public string Categoria { get; set; } = string.Empty;

    [JsonPropertyName("marca")]
    public string? Marca { get; set; }

    [JsonPropertyName("stock")]
    public int Stock { get; set; }

    [JsonPropertyName("imagenUrl")]
    public string? ImagenUrl { get; set; }

    [JsonPropertyName("rating")]
    public decimal Rating { get; set; } = 4.8m;

    [JsonPropertyName("reviewCount")]
    public int ReviewCount { get; set; } = 12;

    [JsonPropertyName("esDestacado")]
    public bool EsDestacado { get; set; }

    public string DisplayPrice
    {
        get
        {
            var culture = new CultureInfo("es-CO");
            culture.NumberFormat.CurrencySymbol = "$ ";
            culture.NumberFormat.CurrencyDecimalDigits = 0;
            culture.NumberFormat.CurrencyGroupSeparator = ".";
            return PrecioFinal.ToString("C0", culture) + " COP";
        }
    }

    public string DisplayRegularPrice
    {
        get
        {
            var culture = new CultureInfo("es-CO");
            culture.NumberFormat.CurrencySymbol = "$ ";
            culture.NumberFormat.CurrencyDecimalDigits = 0;
            culture.NumberFormat.CurrencyGroupSeparator = ".";
            return Precio.ToString("C0", culture);
        }
    }

    public bool HasDiscount => DescuentoPorcentaje > 0 && PrecioDescuento.HasValue;

    public string SafeImageUrl => !string.IsNullOrWhiteSpace(ImagenUrl) ? ImagenUrl : "brand_lens.svg";
}
