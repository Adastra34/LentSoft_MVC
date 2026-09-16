using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LentSoft.Mobile.Models;
using LentSoft.Mobile.Services;

namespace LentSoft.Mobile.ViewModels;

public partial class CitasViewModel : ObservableObject
{
    private readonly IApiService _apiService;

    [ObservableProperty]
    private AppointmentDto? _proximaCita;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NoProximaCita))]
    private bool _hasProximaCita;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NoHistorial))]
    private bool _hasHistorial;

    public bool NoProximaCita => !HasProximaCita;
    public bool NoHistorial => !HasHistorial;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isRefreshing;

    public ObservableCollection<AppointmentDto> Historial { get; } = new();

    public CitasViewModel(IApiService apiService)
    {
        _apiService = apiService;
    }

    [RelayCommand]
    public async Task LoadAppointmentsAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            Historial.Clear();

            var response = await _apiService.GetAppointmentsAsync();
            ProximaCita = response.ProximaCita;
            HasProximaCita = ProximaCita != null;

            foreach (var cita in response.Historial)
            {
                Historial.Add(cita);
            }

            HasHistorial = Historial.Count > 0;
        }
        catch
        {
            HasProximaCita = false;
            HasHistorial = false;
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task OpenAgendarModalAsync()
    {
        await Shell.Current.GoToAsync("agendar_cita");
    }

    [RelayCommand]
    private async Task ReprogramarProximaCitaAsync()
    {
        if (ProximaCita != null)
        {
            await ReprogramarCitaAsync(ProximaCita);
        }
    }

    [RelayCommand]
    private async Task CancelarProximaCitaAsync()
    {
        if (ProximaCita != null)
        {
            await CancelarCitaAsync(ProximaCita);
        }
    }

    [RelayCommand]
    public async Task ReprogramarCitaAsync(AppointmentDto? appointment)
    {
        if (appointment == null) return;

        if (!appointment.CanManage)
        {
            await Shell.Current.DisplayAlert("Aviso", "Esta cita ya no puede ser modificada.", "Aceptar");
            return;
        }

        await Shell.Current.GoToAsync($"agendar_cita?citaId={appointment.Id}&servicio={Uri.EscapeDataString(appointment.Servicio)}");
    }

    [RelayCommand]
    public async Task CancelarCitaAsync(AppointmentDto? appointment)
    {
        if (appointment == null) return;

        if (!appointment.CanManage)
        {
            await Shell.Current.DisplayAlert("Aviso", "Esta cita ya no puede ser cancelada.", "Aceptar");
            return;
        }

        bool confirm = await Shell.Current.DisplayAlert(
            "Cancelar Cita",
            $"¿Estás seguro de que deseas cancelar tu cita de \"{appointment.Servicio}\" programada para el {appointment.DisplayFechaCorta}?",
            "Sí, cancelar",
            "No");

        if (!confirm) return;

        try
        {
            IsBusy = true;
            await _apiService.CancelAppointmentAsync(appointment.Id);

            await Shell.Current.DisplayAlert(
                "Cita Cancelada",
                "Tu cita ha sido cancelada exitosamente.",
                "Aceptar");

            await LoadAppointmentsAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert(
                "Error",
                $"No se pudo cancelar la cita: {ex.Message}",
                "Aceptar");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AppointmentSelectedAsync(AppointmentDto? appointment)
    {
        if (appointment == null) return;

        if (appointment.CanManage)
        {
            string action = await Shell.Current.DisplayActionSheet(
                $"{appointment.Servicio} ({appointment.Estado})",
                "Cerrar",
                null,
                "🔄 Reprogramar cita",
                "❌ Cancelar cita",
                "👁️ Ver detalles");

            if (action == "🔄 Reprogramar cita")
            {
                await ReprogramarCitaAsync(appointment);
            }
            else if (action == "❌ Cancelar cita")
            {
                await CancelarCitaAsync(appointment);
            }
            else if (action == "👁️ Ver detalles")
            {
                await MostrarDetallesAsync(appointment);
            }
        }
        else
        {
            await MostrarDetallesAsync(appointment);
        }
    }

    private async Task MostrarDetallesAsync(AppointmentDto appointment)
    {
        await Shell.Current.DisplayAlert(
            appointment.Servicio,
            $"Fecha: {appointment.DisplayFechaHora}\nEstado: {appointment.Estado}\nEspecialista: {appointment.OptometraNombre}\nNotas: {appointment.Notas ?? "Ninguna"}",
            "Cerrar");
    }
}
