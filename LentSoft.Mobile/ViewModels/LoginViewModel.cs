using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LentSoft.Mobile.Services;

namespace LentSoft.Mobile.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IApiService _apiService;

    [ObservableProperty]
    private string _email = "user@lentsoft.com";

    [ObservableProperty]
    private string _password = "user123";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool IsNotBusy => !IsBusy;

    public LoginViewModel(IApiService apiService)
    {
        _apiService = apiService;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsBusy) return;

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Por favor ingresa tu correo y contraseña.";
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var result = await _apiService.LoginAsync(Email.Trim(), Password);
            if (!string.IsNullOrWhiteSpace(result.Token))
            {
                await Shell.Current.GoToAsync("//main/inicio");
            }
            else
            {
                ErrorMessage = result.Message ?? "Error al iniciar sesión.";
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
    private async Task GoToRegisterAsync()
    {
        await Shell.Current.GoToAsync("register");
    }

    [RelayCommand]
    private async Task ForgotPasswordAsync()
    {
        string? emailInput = await Shell.Current.DisplayPromptAsync(
            "Recuperar contraseña",
            "Ingresa tu correo electrónico para recibir el enlace de restablecimiento:",
            "Enviar",
            "Cancelar",
            "correo@ejemplo.com",
            keyboard: Keyboard.Email);

        if (!string.IsNullOrWhiteSpace(emailInput))
        {
            IsBusy = true;
            var sent = await _apiService.ForgotPasswordAsync(emailInput.Trim());
            IsBusy = false;
            await Shell.Current.DisplayAlert(
                "Recuperación de contraseña",
                "Si el correo está registrado, recibirás un enlace de recuperación en breve.",
                "Entendido");
        }
    }
}
