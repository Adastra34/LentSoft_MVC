using LentSoft.Mobile.Models;

namespace LentSoft.Mobile.Services;

public interface ICartService
{
    IReadOnlyList<CartItemDto> Items { get; }
    int TotalItemsCount { get; }
    decimal TotalAmount { get; }
    string DisplayTotalAmount { get; }

    event EventHandler? CartChanged;

    void AddProduct(ProductDto product, int quantity = 1);
    void IncreaseQuantity(int productId);
    void DecreaseQuantity(int productId);
    void RemoveProduct(int productId);
    void Clear();
}
