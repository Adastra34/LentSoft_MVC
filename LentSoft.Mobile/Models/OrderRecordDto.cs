using System.Globalization;

namespace LentSoft.Mobile.Models;

public class OrderRecordDto
{
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;
    public string Status { get; set; } = "En Proceso";
    public decimal Total { get; set; }
    public List<CartItemDto> Items { get; set; } = new();

    public string DisplayDate => Date.ToString("dd/MM/yyyy hh:mm tt");

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

    public int ItemsCount => Items.Sum(i => i.Cantidad);

    public string ItemsSummary
    {
        get
        {
            if (Items == null || Items.Count == 0) return "Sin productos";
            return string.Join(", ", Items.Select(i => $"{i.Cantidad}x {i.Product?.Nombre}"));
        }
    }
}
