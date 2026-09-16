using LentSoft.Mobile.Models;

namespace LentSoft.Mobile.Services;

public interface IOrderHistoryService
{
    IReadOnlyList<OrderRecordDto> Orders { get; }
    event EventHandler? OrdersChanged;

    void AddOrder(OrderRecordDto order);
}
