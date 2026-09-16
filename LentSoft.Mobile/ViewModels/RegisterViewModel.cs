using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LentSoft.Mobile.Services;

namespace LentSoft.Mobile.ViewModels;

public partial class RegisterViewModel : ObservableObject
{
    private readonly IApiService _apiService;

    [ObservableProperty]
    private string _nombre = string.Empty;

    [ObservableProperty]
    private string _apellido = string.Empty;

    [ObservableProperty]
    private string _tipoDocumento = "CC";

    [ObservableProperty]
    private string _numeroDocumento = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _telefono = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool IsNotBusy => !IsBusy;

    public RegisterViewModel(IApiService apiService)
    {
        _apiService = apiService;
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (IsBusy) return;

        if (string.IsNullOrWhiteSpace(Nombre) || string.IsNullOrWhiteSpace(Apellido) ||
            string.IsNullOrWhiteSpace(NumeroDocumento) || string.IsNullOrWhiteSpace(Email) ||
            string.IsNullOrWhiteSpace(Telefono) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Todos los campos son obligatorios.";
            return;
        }

        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Las contraseñas no coinciden.";
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var result = await _apiService.RegisterAsync(
                Nombre,
                Apellido,
                TipoDocumento,
                NumeroDocumento,
                Email,
                Telefono,
                Password);

            if (!string.IsNullOrWhiteSpace(result.Token))
            {
                await Shell.Current.GoToAsync("//main/inicio");
            }
            else
            {
                ErrorMessage = result.Message ?? "Error al registrar la cuenta.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task BackToLoginAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
