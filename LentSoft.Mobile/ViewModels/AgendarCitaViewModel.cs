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
    private DateTime _minimumDate = DateTime.Today;

    [ObservableProperty]
    private DateTime _maximumDate = DateTime.Today.AddMonths(6);

    [ObservableProperty]
    private DateTime _selectedDate;

    [ObservableProperty]
    private TimeSpan _selectedTime;

    [ObservableProperty]
    private string? _notas;

    [ObservableProperty]
    private string? _dateValidationHint;

    [ObservableProperty]
    private string _dateHintColor = "#77708A";

    [ObservableProperty]
    private string? _timeValidationHint;

    [ObservableProperty]
    private string _timeHintColor = "#77708A";

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
        InitializeDates();
    }

    private void InitializeDates()
    {
        MinimumDate = DateTime.Today;
        MaximumDate = DateTime.Today.AddMonths(6);

        var now = DateTime.Now;
        // Si hoy es domingo o ya pasaron las 5:00 PM, pasar al siguiente día hábil
        if (now.DayOfWeek == DayOfWeek.Sunday || now.Hour >= 17)
        {
            var next = now.Date.AddDays(1);
            while (next.DayOfWeek == DayOfWeek.Sunday)
            {
                next = next.AddDays(1);
            }
            SelectedDate = next;
            SelectedTime = new TimeSpan(10, 0, 0); // 10:00 AM
        }
        else
        {
            SelectedDate = now.Date;
            int nextHour = Math.Max(8, now.Hour + 1);
            if (nextHour >= 18) nextHour = 17;
            SelectedTime = new TimeSpan(nextHour, 0, 0);
        }

        UpdateValidationHints();
    }

    partial void OnSelectedDateChanged(DateTime value)
    {
        UpdateValidationHints();
    }

    partial void OnSelectedTimeChanged(TimeSpan value)
    {
        UpdateValidationHints();
    }

    private void UpdateValidationHints()
    {
        var now = DateTime.Now;

        // Validación de Fecha
        if (SelectedDate.Date < DateTime.Today)
        {
            DateValidationHint = "❌ No puedes seleccionar una fecha anterior al día de hoy.";
            DateHintColor = "#DC2626";
        }
        else if (SelectedDate.DayOfWeek == DayOfWeek.Sunday)
        {
            DateValidationHint = "❌ Los domingos no hay atención (Atendemos de Lunes a Sábado).";
            DateHintColor = "#DC2626";
        }
        else if (SelectedDate.Date == DateTime.Today && now.Hour >= 18)
        {
            DateValidationHint = "❌ La clínica ya cerró por el día de hoy (horario hasta 6:00 p.m.).";
            DateHintColor = "#DC2626";
        }
        else if (SelectedDate.Date == DateTime.Today)
        {
            DateValidationHint = $"📅 Cita para hoy (debe ser posterior a las {now:hh:mm tt})";
            DateHintColor = "#D97706";
        }
        else
        {
            DateValidationHint = $"✔️ {SelectedDate:dddd, dd MMMM yyyy}";
            DateHintColor = "#2FBF71";
        }

        // Validación de Hora
        if (SelectedTime.Hours < 8 || SelectedTime.Hours > 18 || (SelectedTime.Hours == 18 && SelectedTime.Minutes > 0))
        {
            TimeValidationHint = "❌ Horario fuera de servicio (Atención: 8:00 a.m. a 6:00 p.m.)";
            TimeHintColor = "#DC2626";
        }
        else if (SelectedDate.Date == DateTime.Today && SelectedTime <= now.TimeOfDay)
        {
            TimeValidationHint = $"❌ Hora ya pasada hoy. Debe ser posterior a las {now:hh:mm tt}.";
            TimeHintColor = "#DC2626";
        }
        else
        {
            TimeValidationHint = "✔️ Horario disponible para atención";
            TimeHintColor = "#2FBF71";
        }
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
        UpdateValidationHints();
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

        ErrorMessage = null;
        var now = DateTime.Now;

        // 1. Validar servicio
        if (string.IsNullOrWhiteSpace(SelectedServicio))
        {
            ErrorMessage = "Por favor selecciona un servicio.";
            await Shell.Current.DisplayAlert("Validación", ErrorMessage, "Entendido");
            return;
        }

        // 2. Validar que la fecha no sea anterior a hoy
        if (SelectedDate.Date < DateTime.Today)
        {
            ErrorMessage = "No puedes agendar citas en fechas pasadas. Selecciona una fecha a partir de hoy.";
            await Shell.Current.DisplayAlert("Fecha no válida", ErrorMessage, "Entendido");
            return;
        }

        // 3. Validar domingo
        if (SelectedDate.DayOfWeek == DayOfWeek.Sunday)
        {
            ErrorMessage = "La clínica atiende de lunes a sábado. No es posible agendar citas los domingos.";
            await Shell.Current.DisplayAlert("Día no hábil", ErrorMessage, "Entendido");
            return;
        }

        // 4. Validar si hoy ya cerró
        if (SelectedDate.Date == DateTime.Today && now.Hour >= 18)
        {
            ErrorMessage = "La clínica ya cerró su jornada por el día de hoy (horario hasta las 6:00 p.m.). Por favor selecciona una fecha a partir de mañana.";
            await Shell.Current.DisplayAlert("Clínica cerrada hoy", ErrorMessage, "Entendido");
            return;
        }

        // 5. Validar horario de atención (8:00 a.m. a 6:00 p.m.)
        if (SelectedTime.Hours < 8 || SelectedTime.Hours > 18 || (SelectedTime.Hours == 18 && SelectedTime.Minutes > 0))
        {
            ErrorMessage = "El horario de atención es únicamente de lunes a sábado, entre 8:00 a.m. y 6:00 p.m.";
            await Shell.Current.DisplayAlert("Horario no permitido", ErrorMessage, "Entendido");
            return;
        }

        // 6. Validar que si es hoy, la hora sea estrictamente posterior a la hora actual
        var fechaHora = SelectedDate.Date + SelectedTime;
        if (fechaHora <= now)
        {
            ErrorMessage = $"La hora seleccionada ({SelectedTime:hh\\:mm}) ya pasó el día de hoy. Debe ser posterior a las {now:hh:mm tt}.";
            await Shell.Current.DisplayAlert("Hora no válida", ErrorMessage, "Entendido");
            return;
        }

        // 7. Validar longitud de notas
        if (!string.IsNullOrWhiteSpace(Notas) && Notas.Length > 500)
        {
            ErrorMessage = "El motivo de consulta u observaciones no puede superar los 500 caracteres.";
            await Shell.Current.DisplayAlert("Validación de notas", ErrorMessage, "Entendido");
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
            await Shell.Current.DisplayAlert("Error", ex.Message, "Aceptar");
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
