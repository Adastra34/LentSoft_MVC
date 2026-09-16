using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LentSoft.Mobile.Models;
using LentSoft.Mobile.Services;

namespace LentSoft.Mobile.ViewModels;

public partial class InicioViewModel : ObservableObject
{
    private readonly IApiService _apiService;
    private readonly IAuthStorageService _storageService;

    [ObservableProperty]
    private string _userName = "Usuario";

    [ObservableProperty]
    private string _greeting = "Hola, Usuario";

    [ObservableProperty]
    private string _currentDate = string.Empty;

    [ObservableProperty]
    private AppointmentDto? _proximaCita;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NoProximaCita))]
    private bool _hasProximaCita;

    [ObservableProperty]
    private InvoiceDto? _ultimaFactura;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NoUltimaFactura))]
    private bool _hasUltimaFactura;

    public bool NoProximaCita => !HasProximaCita;
    public bool NoUltimaFactura => !HasUltimaFactura;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isRefreshing;

    public InicioViewModel(IApiService apiService, IAuthStorageService storageService)
    {
        _apiService = apiService;
        _storageService = storageService;

        var culture = new CultureInfo("es-CO");
        CurrentDate = DateTime.Now.ToString("dddd, dd 'de' MMMM", culture);
        CurrentDate = char.ToUpper(CurrentDate[0]) + CurrentDate.Substring(1);
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            var user = await _storageService.GetUserAsync();
            if (user != null)
            {
                UserName = user.Nombre;
                Greeting = $"Hola, {user.Nombre}";
            }

            // Cargar Citas
            var appResponse = await _apiService.GetAppointmentsAsync();
            ProximaCita = appResponse.ProximaCita;
            HasProximaCita = ProximaCita != null;

            // Cargar Facturas
            var invoices = await _apiService.GetInvoicesAsync();
            UltimaFactura = invoices.FirstOrDefault();
            HasUltimaFactura = UltimaFactura != null;
        }
        catch (Exception)
        {
            // Silently fallback to offline/cached state
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task GoToTiendaAsync()
    {
        await Shell.Current.GoToAsync("//main/tienda");
    }

    [RelayCommand]
    private async Task GoToCitasAsync()
    {
        await Shell.Current.GoToAsync("//main/citas");
    }

    [RelayCommand]
    private async Task GoToFacturasAsync()
    {
        await Shell.Current.GoToAsync("//main/facturas");
    }
}
