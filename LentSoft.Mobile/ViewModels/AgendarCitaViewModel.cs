using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LentSoft.Mobile.Services;

namespace LentSoft.Mobile.ViewModels;

[QueryProperty(nameof(CitaIdStr), "citaId")]
[QueryProperty(nameof(ServicioParam), "servicio")]
public partial class AgendarCitaViewModel : ObservableObject
{
    private readonly IApiService _apiService;

    public ObservableCollection<string> Servicios { get; } = new()
    {
        "Examen visual general",
        "Control de lentes de contacto",
        "Adaptación de montura",
        "Urgencia optométrica",
        "Terapia visual"
    };

    [ObservableProperty]
    private string? _citaIdStr;

    [ObservableProperty]
    private string? _servicioParam;

    [ObservableProperty]
    private int _citaId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotRescheduling))]
    private bool _isRescheduling;

    public bool IsNotRescheduling => !IsRescheduling;

    [ObservableProperty]
    private string _pageTitle = "Agendar Cita Médica";

    [ObservableProperty]
    private string _pageSubtitle = "Selecciona el servicio y fecha deseada";

    [ObservableProperty]
    private string _confirmButtonText = "Confirmar y Agendar Cita";

    [ObservableProperty]
    private string _selectedServicio = "Examen visual general";

    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today.AddDays(1);

    [ObservableProperty]
    private TimeSpan _selectedTime = new(10, 0, 0); // 10:00 AM

    [ObservableProperty]
    private string? _notas;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool IsNotBusy => !IsBusy;

    public AgendarCitaViewModel(IApiService apiService)
    {
        _apiService = apiService;
    }

    partial void OnCitaIdStrChanged(string? value)
    {
        if (int.TryParse(value, out int id) && id > 0)
        {
            CitaId = id;
            IsRescheduling = true;
            PageTitle = "Reprogramar Cita";
            PageSubtitle = "Selecciona la nueva fecha y hora para tu atención";
            ConfirmButtonText = "Confirmar Nueva Fecha";
        }
        else
        {
            CitaId = 0;
            IsRescheduling = false;
            PageTitle = "Agendar Cita Médica";
            PageSubtitle = "Selecciona el servicio y fecha deseada";
            ConfirmButtonText = "Confirmar y Agendar Cita";
        }
    }

    partial void OnServicioParamChanged(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            SelectedServicio = Uri.UnescapeDataString(value);
        }
    }

    [RelayCommand]
    private async Task ConfirmarCitaAsync()
    {
        if (IsBusy) return;

        if (string.IsNullOrWhiteSpace(SelectedServicio))
        {
            ErrorMessage = "Por favor selecciona un servicio.";
            return;
        }

        var fechaHora = SelectedDate.Date + SelectedTime;

        if (fechaHora <= DateTime.Now)
        {
            ErrorMessage = "La cita debe ser programada para una fecha y hora futura.";
            return;
        }

        if (fechaHora.DayOfWeek == DayOfWeek.Sunday)
        {
            ErrorMessage = "La clínica atiende de lunes a sábado.";
            return;
        }

        if (SelectedTime.Hours < 8 || SelectedTime.Hours >= 18)
        {
            ErrorMessage = "El horario de atención es de 8:00 a.m. a 6:00 p.m.";
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            if (IsRescheduling && CitaId > 0)
            {
                var result = await _apiService.RescheduleAppointmentAsync(CitaId, fechaHora);

                await Shell.Current.DisplayAlert(
                    "¡Cita Reprogramada!",
                    $"Tu cita ha sido reprogramada exitosamente para el {fechaHora:dd/MM/yyyy hh:mm tt}.",
                    "Aceptar");
            }
            else
            {
                var result = await _apiService.CreateAppointmentAsync(SelectedServicio, fechaHora, Notas);

                await Shell.Current.DisplayAlert(
                    "¡Cita Agendada!",
                    $"Tu cita para {SelectedServicio} fue programada exitosamente para el {fechaHora:dd/MM/yyyy hh:mm tt}.",
                    "Aceptar");
            }

            CitaIdStr = null;
            CitaId = 0;
            IsRescheduling = false;

            await Shell.Current.GoToAsync("..");
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
    private async Task CancelarAsync()
    {
        CitaIdStr = null;
        CitaId = 0;
        IsRescheduling = false;
        await Shell.Current.GoToAsync("..");
    }
}
