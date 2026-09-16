using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LentSoft.Mobile.Models;
using LentSoft.Mobile.Services;

namespace LentSoft.Mobile.ViewModels;

public partial class FacturasViewModel : ObservableObject
{
    private readonly IApiService _apiService;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private bool _isEmpty;

    public ObservableCollection<InvoiceDto> Invoices { get; } = new();

    public FacturasViewModel(IApiService apiService)
    {
        _apiService = apiService;
    }

    [RelayCommand]
    public async Task LoadInvoicesAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            Invoices.Clear();

            var list = await _apiService.GetInvoicesAsync();
            foreach (var inv in list)
            {
                Invoices.Add(inv);
            }

            IsEmpty = Invoices.Count == 0;
        }
        catch
        {
            IsEmpty = true;
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task InvoiceSelectedAsync(InvoiceDto invoice)
    {
        if (invoice == null) return;

        await Shell.Current.DisplayAlert(
            $"Factura {invoice.NumeroFactura}",
            $"Fecha: {invoice.DisplayFecha}\nEstado: {invoice.Estado}\nTotal: {invoice.DisplayTotal}\nMétodo de pago: {invoice.MetodoPago ?? "Tarjeta"}",
            "Aceptar");
    }
}
