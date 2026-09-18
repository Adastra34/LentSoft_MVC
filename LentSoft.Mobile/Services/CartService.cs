using System.Globalization;
using LentSoft.Mobile.Models;

namespace LentSoft.Mobile.Services;

public class CartService : ICartService
{
    private readonly List<CartItemDto> _items = new();

    public IReadOnlyList<CartItemDto> Items => _items.AsReadOnly();

    public int TotalItemsCount => _items.Sum(i => i.Cantidad);

    public decimal TotalAmount => _items.Sum(i => i.Subtotal);

    public string DisplayTotalAmount
    {
        get
        {
            var culture = new CultureInfo("es-CO");
            culture.NumberFormat.CurrencySymbol = "$ ";
            culture.NumberFormat.CurrencyDecimalDigits = 0;
            culture.NumberFormat.CurrencyGroupSeparator = ".";
            return TotalAmount.ToString("C0", culture) + " COP";
        }
    }

    public event EventHandler? CartChanged;

    public void AddProduct(ProductDto product, int quantity = 1)
    {
        if (product == null) return;

        var existing = _items.FirstOrDefault(i => i.Product.Id == product.Id);
        if (existing != null)
        {
            existing.Cantidad += quantity;
        }
        else
        {
            _items.Add(new CartItemDto
            {
                Product = product,
                Cantidad = quantity
            });
        }

        CartChanged?.Invoke(this, EventArgs.Empty);
    }

    public void IncreaseQuantity(int productId)
    {
        var existing = _items.FirstOrDefault(i => i.Product.Id == productId);
        if (existing != null)
        {
            existing.Cantidad++;
            CartChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void DecreaseQuantity(int productId)
    {
        var existing = _items.FirstOrDefault(i => i.Product.Id == productId);
        if (existing != null)
        {
            existing.Cantidad--;
            if (existing.Cantidad <= 0)
            {
                _items.Remove(existing);
            }
            CartChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void RemoveProduct(int productId)
    {
        var existing = _items.FirstOrDefault(i => i.Product.Id == productId);
        if (existing != null)
        {
            _items.Remove(existing);
            CartChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Clear()
    {
        _items.Clear();
        CartChanged?.Invoke(this, EventArgs.Empty);
    }
}
