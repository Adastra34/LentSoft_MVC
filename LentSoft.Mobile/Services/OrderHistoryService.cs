using LentSoft.Mobile.Models;

namespace LentSoft.Mobile.Services;

public class OrderHistoryService : IOrderHistoryService
{
    private readonly List<OrderRecordDto> _orders = new();

    public IReadOnlyList<OrderRecordDto> Orders => _orders.AsReadOnly();

    public event EventHandler? OrdersChanged;

    public OrderHistoryService()
    {
        // Las órdenes inician vacías y se registran al completar compras reales
    }

    public void AddOrder(OrderRecordDto order)
    {
        if (order == null) return;
        _orders.Insert(0, order);
        OrdersChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        _orders.Clear();
        OrdersChanged?.Invoke(this, EventArgs.Empty);
    }
}
