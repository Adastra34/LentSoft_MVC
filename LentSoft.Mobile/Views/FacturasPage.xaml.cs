using LentSoft.Mobile.ViewModels;

namespace LentSoft.Mobile.Views;

public partial class FacturasPage : ContentPage
{
    private readonly FacturasViewModel _viewModel;

    public FacturasPage(FacturasViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadInvoicesCommand.ExecuteAsync(null);
    }
}
