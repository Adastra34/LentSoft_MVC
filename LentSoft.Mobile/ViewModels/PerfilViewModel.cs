using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LentSoft.Mobile.Models;
using LentSoft.Mobile.Services;

namespace LentSoft.Mobile.ViewModels;

public partial class PerfilViewModel : ObservableObject
{
    private readonly IApiService _apiService;
    private readonly IAuthStorageService _storageService;

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

    public PerfilViewModel(IApiService apiService, IAuthStorageService storageService)
    {
        _apiService = apiService;
        _storageService = storageService;
    }

    [RelayCommand]
    public async Task LoadProfileAsync()
    {
        try
        {
            IsBusy = true;
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
            case "Mis datos":
                await Shell.Current.DisplayAlert(
                    "Mis Datos Personales",
                    $"Nombre: {NombreCompleto}\nEmail: {Email}\nTeléfono: {User?.Telefono ?? "No registrado"}\nDocumento: {User?.TipoDocumento} {User?.NumeroDocumento}",
                    "Cerrar");
                break;

            case "Direcciones":
                await Shell.Current.DisplayAlert(
                    "Mis Direcciones",
                    $"Dirección de entrega predeterminada:\n{User?.Direccion ?? "Calle 123 # 45 - 67, Bogotá D.C."}",
                    "Cerrar");
                break;

            case "Fórmula óptica":
                await Shell.Current.DisplayAlert(
                    "Fórmula Óptica Registrada",
                    "Esfera: OD -1.50, OI -1.75\nCilindro: OD -0.50, OI -0.25\nEje: OD 180°, OI 175°\nÚltimo control: Hace 3 meses (Dr. Ana Gómez)",
                    "Cerrar");
                break;

            case "Seguridad":
                await Shell.Current.DisplayAlert(
                    "Seguridad y Contraseña",
                    "Tu cuenta cuenta con autenticación segura por Token JWT.\nPuedes cambiar tu clave en el portal web o solicitando restablecimiento.",
                    "Cerrar");
                break;

            case "Notificaciones":
                await Shell.Current.DisplayAlert(
                    "Notificaciones",
                    "Notificaciones de citas activadas.\nRecordatorios automáticos vía correo electrónico activos.",
                    "Cerrar");
                break;
        }
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        bool confirm = await Shell.Current.DisplayAlert(
            "Cerrar sesión",
            "¿Estás seguro de que deseas salir de LentSoft?",
            "Sí, salir",
            "Cancelar");

        if (confirm)
        {
            await _storageService.ClearAsync();
            await Shell.Current.GoToAsync("//login");
        }
    }
}
