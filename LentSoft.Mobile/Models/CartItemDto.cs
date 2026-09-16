using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LentSoft.Mobile.Models;

public partial class CartItemDto : ObservableObject
{
    [ObservableProperty]
    private ProductDto _product = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Subtotal))]
    [NotifyPropertyChangedFor(nameof(DisplaySubtotal))]
    private int _cantidad = 1;

    public decimal Subtotal => Product.PrecioFinal * Cantidad;

    public string DisplaySubtotal
    {
        get
        {
            var culture = new CultureInfo("es-CO");
            culture.NumberFormat.CurrencySymbol = "$ ";
            culture.NumberFormat.CurrencyDecimalDigits = 0;
            culture.NumberFormat.CurrencyGroupSeparator = ".";
            return Subtotal.ToString("C0", culture) + " COP";
        }
    }

    public string DisplayUnitPrice
    {
        get
        {
            var culture = new CultureInfo("es-CO");
            culture.NumberFormat.CurrencySymbol = "$ ";
            culture.NumberFormat.CurrencyDecimalDigits = 0;
            culture.NumberFormat.CurrencyGroupSeparator = ".";
            return Product.PrecioFinal.ToString("C0", culture);
        }
    }
}
