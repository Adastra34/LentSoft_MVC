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

    [JsonPropertyName("material")]
    public string? Material { get; set; }

    [JsonPropertyName("color")]
    public string? Color { get; set; }

    [JsonPropertyName("proteccion")]
    public string? Proteccion { get; set; }

    [JsonPropertyName("estilo")]
    public string? Estilo { get; set; }

    [JsonPropertyName("tamanio")]
    public string? Tamanio { get; set; }

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

    public string SafeImageUrl
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ImagenUrl))
            {
                return GetFallbackImage();
            }

            if (ImagenUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                ImagenUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return ImagenUrl;
            }

            if (ImagenUrl.StartsWith("/"))
            {
                string host = "localhost";
                if (DeviceInfo.Platform == DevicePlatform.Android)
                {
                    host = (DeviceInfo.DeviceType == DeviceType.Virtual) ? "10.0.2.2" : "172.16.6.121";
                }
                return $"http://{host}:5000{ImagenUrl}";
            }

            return GetFallbackImage();
        }
    }

    private string GetFallbackImage()
    {
        if (Nombre.Contains("Acuvue", StringComparison.OrdinalIgnoreCase) || Categoria.Contains("contacto", StringComparison.OrdinalIgnoreCase))
            return "https://images.unsplash.com/photo-1588776814546-1ffcf47267a5?auto=format&fit=crop&w=600&q=80";

        if (Nombre.Contains("Estuche", StringComparison.OrdinalIgnoreCase) || Categoria.Contains("accesorios", StringComparison.OrdinalIgnoreCase))
            return "https://images.unsplash.com/photo-1606107557195-0e29a4b5b4aa?auto=format&fit=crop&w=600&q=80";

        if (Nombre.Contains("Limpiador", StringComparison.OrdinalIgnoreCase) || Nombre.Contains("Líquido", StringComparison.OrdinalIgnoreCase))
            return "https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?auto=format&fit=crop&w=600&q=80";

        if (Categoria.Contains("sol", StringComparison.OrdinalIgnoreCase) || Nombre.Contains("Aviator", StringComparison.OrdinalIgnoreCase))
            return "https://images.unsplash.com/photo-1511499767150-a48a237f0083?auto=format&fit=crop&w=600&q=80";

        if (Nombre.Contains("Graduados", StringComparison.OrdinalIgnoreCase) || Categoria.Contains("monturas", StringComparison.OrdinalIgnoreCase))
            return "https://images.unsplash.com/photo-1591076482161-42ce6da69f67?auto=format&fit=crop&w=600&q=80";

        return "https://images.unsplash.com/photo-1591076482161-42ce6da69f67?auto=format&fit=crop&w=600&q=80";
    }

    public string DisplayNombre => Services.LocalizationService.Instance.TranslateProductTitle(Nombre);

    public string SafeDescripcion
    {
        get
        {
            if (Services.LocalizationService.Instance.CurrentLanguage == "EN")
            {
                return "Discover the exceptional quality and design of this product selected by LentSoft for your eye care.";
            }

            return !string.IsNullOrWhiteSpace(Descripcion)
                ? Descripcion
                : "Descubre la calidad y el diseño excepcional de este producto seleccionado por LentSoft para tu cuidado visual.";
        }
    }

    public string DisplayRatingStars
    {
        get
        {
            int fullStars = (int)Math.Floor(Rating);
            string stars = new string('★', fullStars);
            return stars.PadRight(5, '☆');
        }
    }

    public bool IsFavorite => Services.FavoriteService.Instance.IsFavorite(Id);
    public string FavoriteHeartIcon => IsFavorite ? "❤️" : "🤍";
}
