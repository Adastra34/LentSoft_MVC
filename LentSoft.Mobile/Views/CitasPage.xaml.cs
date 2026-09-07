using LentSoft.Mobile.ViewModels;

namespace LentSoft.Mobile.Views;

public partial class CitasPage : ContentPage
{
    private readonly CitasViewModel _viewModel;

    public CitasPage(CitasViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAppointmentsCommand.ExecuteAsync(null);
    }
}
