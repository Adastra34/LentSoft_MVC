using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LentSoft.Mobile.Models;
using LentSoft.Mobile.Services;

namespace LentSoft.Mobile.ViewModels;

public partial class PerfilViewModel : ObservableObject
{
    private readonly IApiService _apiService;
    private readonly IAuthStorageService _storageService;
    private readonly IFavoriteService _favService;
    private readonly IOrderHistoryService _orderService;
    private readonly ILocalizationService _locService;

    [ObservableProperty]
    private UserDto? _user;

    [ObservableProperty]
    private string _nombreCompleto = "Usuario LentSoft";

    [ObservableProperty]
    private string _email = "usuario@lentsoft.com";

    [ObservableProperty]
    private string _initials = "LS";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isFavoritesModalVisible;

    [ObservableProperty]
    private bool _isOrdersModalVisible;

    [ObservableProperty]
    private bool _isFormulaModalVisible;

    [ObservableProperty]
    private string _currentLanguageDisplay = "🌐 ES";

    // Dynamic Localized UI String Properties
    [ObservableProperty] private string _myFavoritesText = "Mis favoritos";
    [ObservableProperty] private string _myOrdersText = "Mis pedidos";
    [ObservableProperty] private string _opticPrescriptionText = "Fórmula óptica";
    [ObservableProperty] private string _myDataText = "Mis datos";
    [ObservableProperty] private string _addressesText = "Direcciones";
    [ObservableProperty] private string _languageText = "Idioma / Language";
    [ObservableProperty] private string _securityText = "Seguridad";
    [ObservableProperty] private string _notificationsText = "Notificaciones";
    [ObservableProperty] private string _logoutText = "Cerrar sesión";

    public ObservableCollection<ProductDto> Favorites { get; } = new();
    public ObservableCollection<OrderRecordDto> Orders { get; } = new();

    public PerfilViewModel(
        IApiService apiService,
        IAuthStorageService storageService,
        IFavoriteService favService,
        IOrderHistoryService orderService,
        ILocalizationService locService)
    {
        _apiService = apiService;
        _storageService = storageService;
        _favService = favService;
        _orderService = orderService;
        _locService = locService;

        _favService.FavoritesChanged += OnFavoritesChanged;
        _locService.LanguageChanged += OnLanguageChanged;

        RefreshLanguageDisplay();
        RefreshFavorites();
        RefreshOrders();
    }

    private void OnFavoritesChanged(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(RefreshFavorites);
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(RefreshLanguageDisplay);
    }

    private void RefreshLanguageDisplay()
    {
        CurrentLanguageDisplay = _locService.CurrentLanguage == "ES" ? "🌐 ES" : "🌐 EN";

        MyFavoritesText = _locService.GetString("MyFavorites");
        MyOrdersText = _locService.GetString("MyOrders");
        OpticPrescriptionText = _locService.GetString("OpticPrescription");
        MyDataText = _locService.GetString("MyData");
        AddressesText = _locService.GetString("Addresses");
        LanguageText = _locService.GetString("Language");
        SecurityText = _locService.GetString("Security");
        NotificationsText = _locService.GetString("Notifications");
        LogoutText = _locService.GetString("Logout");

        RefreshFavorites();
    }

    public void RefreshFavorites()
    {
        Favorites.Clear();
        foreach (var p in _favService.Favorites)
        {
            Favorites.Add(p);
        }
    }

    public void RefreshOrders()
    {
        Orders.Clear();
        foreach (var o in _orderService.Orders)
        {
            Orders.Add(o);
        }
    }

    [RelayCommand]
    public async Task LoadProfileAsync()
    {
        try
        {
            IsBusy = true;
            RefreshFavorites();
            RefreshOrders();

            var cached = await _storageService.GetUserAsync();
            if (cached != null)
            {
                ApplyUser(cached);
            }

            var remote = await _apiService.GetProfileAsync();
            if (remote != null)
            {
                ApplyUser(remote);
            }
        }
        catch { }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyUser(UserDto u)
    {
        User = u;
        NombreCompleto = !string.IsNullOrWhiteSpace(u.NombreCompleto) ? u.NombreCompleto : $"{u.Nombre} {u.Apellido}".Trim();
        Email = u.Email;
        Initials = u.Initials;
    }

    [RelayCommand]
    private async Task MenuItemSelectedAsync(string menuItem)
    {
        switch (menuItem)
        {
            case "Mis favoritos":
                RefreshFavorites();
                IsFavoritesModalVisible = true;
                break;

            case "Mis pedidos":
                RefreshOrders();
                IsOrdersModalVisible = true;
                break;

            case "Mis datos":
                await Shell.Current.DisplayAlert(
                    _locService.CurrentLanguage == "ES" ? "Mis Datos Personales" : "My Personal Data",
                    $"Nombre: {NombreCompleto}\nEmail: {Email}\nTeléfono: {User?.Telefono ?? "No registrado"}\nDocumento: {User?.TipoDocumento} {User?.NumeroDocumento}",
                    "OK");
                break;

            case "Direcciones":
                await Shell.Current.DisplayAlert(
                    _locService.CurrentLanguage == "ES" ? "Mis Direcciones" : "My Addresses",
                    $"Dirección de entrega predeterminada:\n{User?.Direccion ?? "Calle 45 # 12 - 34, Bogotá D.C."}",
                    "OK");
                break;

            case "Fórmula óptica":
                IsFormulaModalVisible = true;
                break;

            case "Seguridad":
                await Shell.Current.DisplayAlert(
                    _locService.CurrentLanguage == "ES" ? "Seguridad y Contraseña" : "Security & Password",
                    "Tu cuenta cuenta con autenticación segura por Token JWT.\nPuedes cambiar tu clave en el portal web o solicitando restablecimiento.",
                    "OK");
                break;

            case "Notificaciones":
                await Shell.Current.DisplayAlert(
                    _locService.CurrentLanguage == "ES" ? "Notificaciones" : "Notifications",
                    "Notificaciones de citas activadas.\nRecordatorios automáticos vía correo electrónico activos.",
                    "OK");
                break;

            case "Idioma":
                ToggleLanguage();
                break;
        }
    }

    [RelayCommand]
    private void ToggleLanguage()
    {
        string nextLang = _locService.CurrentLanguage == "ES" ? "EN" : "ES";
        _locService.SetLanguage(nextLang);
    }

    [RelayCommand]
    private void RemoveFavorite(ProductDto product)
    {
        if (product == null) return;
        _favService.ToggleFavorite(product);
        RefreshFavorites();
    }

    [RelayCommand]
    private void CloseFavorites()
    {
        IsFavoritesModalVisible = false;
    }

    [RelayCommand]
    private void CloseOrders()
    {
        IsOrdersModalVisible = false;
    }

    [RelayCommand]
    private void CloseFormula()
    {
        IsFormulaModalVisible = false;
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        bool confirm = await Shell.Current.DisplayAlert(
            _locService.CurrentLanguage == "ES" ? "Cerrar sesión" : "Log out",
            _locService.CurrentLanguage == "ES" ? "¿Estás seguro de que deseas salir de LentSoft?" : "Are you sure you want to log out of LentSoft?",
            _locService.CurrentLanguage == "ES" ? "Sí, salir" : "Yes, log out",
            _locService.CurrentLanguage == "ES" ? "Cancelar" : "Cancel");

        if (confirm)
        {
            await _storageService.ClearAsync();
            await Shell.Current.GoToAsync("//login");
        }
    }
}
