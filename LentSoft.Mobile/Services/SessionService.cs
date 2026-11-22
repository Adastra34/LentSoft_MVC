namespace LentSoft.Mobile.Services;

public class SessionService : ISessionService
{
    private readonly IAuthStorageService _authStorage;
    private readonly ICartService _cartService;
    private readonly IFavoriteService _favoriteService;
    private readonly IOrderHistoryService _orderHistoryService;
    private readonly IInvoiceService _invoiceService;

    public SessionService(
        IAuthStorageService authStorage,
        ICartService cartService,
        IFavoriteService favoriteService,
        IOrderHistoryService orderHistoryService,
        IInvoiceService invoiceService)
    {
        _authStorage = authStorage;
        _cartService = cartService;
        _favoriteService = favoriteService;
        _orderHistoryService = orderHistoryService;
        _invoiceService = invoiceService;
    }

    public async Task EndSessionAsync()
    {
        try
        {
            _cartService.Clear();
            _favoriteService.Clear();
            _orderHistoryService.Clear();
            _invoiceService.Clear();
            await _authStorage.ClearAsync();
        }
        catch { }

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                if (Shell.Current != null)
                {
                    await Shell.Current.GoToAsync("//login");
                }
            }
            catch { }
        });
    }
}
