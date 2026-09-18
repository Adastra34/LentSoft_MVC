using LentSoft.Mobile.Models;

namespace LentSoft.Mobile.Services;

public class OrderHistoryService : IOrderHistoryService
{
    private readonly List<OrderRecordDto> _orders = new();

    public IReadOnlyList<OrderRecordDto> Orders => _orders.AsReadOnly();

    public event EventHandler? OrdersChanged;

    public OrderHistoryService()
    {
        // Seed default initial order for illustration
        _orders.Add(new OrderRecordDto
        {
            OrderNumber = "ORD-2026-8819",
            Date = DateTime.Now.AddDays(-2),
            Status = "En camino",
            Total = 700000m,
            Items = new List<CartItemDto>
            {
                new CartItemDto
                {
                    Product = new ProductDto
                    {
                        Id = 1,
                        Nombre = "Lentes Ray-Ban Aviator",
                        Marca = "Ray-Ban",
                        PrecioFinal = 700000m
                    },
                    Cantidad = 1
                }
            }
        });
    }

    public void AddOrder(OrderRecordDto order)
    {
        if (order == null) return;
        _orders.Insert(0, order);
        OrdersChanged?.Invoke(this, EventArgs.Empty);
    }
}
