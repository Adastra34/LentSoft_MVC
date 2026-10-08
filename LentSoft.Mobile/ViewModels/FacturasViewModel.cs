using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LentSoft.Mobile.Models;
using LentSoft.Mobile.Services;

namespace LentSoft.Mobile.ViewModels;

public partial class FacturasViewModel : ObservableObject
{
    private readonly IApiService _apiService;
    private readonly IInvoiceService _invoiceService;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private bool _isEmpty;

    [ObservableProperty]
    private InvoiceDto? _selectedInvoice;

    [ObservableProperty]
    private bool _isDetailModalVisible;

    public ObservableCollection<InvoiceDto> Invoices { get; } = new();

    public FacturasViewModel(IApiService apiService, IInvoiceService invoiceService)
    {
        _apiService = apiService;
        _invoiceService = invoiceService;

        _invoiceService.InvoicesChanged += OnInvoicesChanged;
    }

    private void OnInvoicesChanged(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _ = LoadInvoicesAsync();
        });
    }

    [RelayCommand]
    public async Task LoadInvoicesAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            Invoices.Clear();

            var apiList = await _apiService.GetInvoicesAsync();
            if (apiList != null && apiList.Count > 0)
            {
                _invoiceService.SyncInvoices(apiList);
            }

            var allInvoices = _invoiceService.Invoices;
            foreach (var inv in allInvoices)
            {
                Invoices.Add(inv);
            }

            IsEmpty = Invoices.Count == 0;
        }
        catch
        {
            IsEmpty = Invoices.Count == 0;
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private void InvoiceSelected(InvoiceDto invoice)
    {
        if (invoice == null) return;
        SelectedInvoice = invoice;
        IsDetailModalVisible = true;
    }

    [RelayCommand]
    private void CloseDetailModal()
    {
        IsDetailModalVisible = false;
        SelectedInvoice = null;
    }
}
