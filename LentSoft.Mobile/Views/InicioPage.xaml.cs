using LentSoft.Mobile.ViewModels;

namespace LentSoft.Mobile.Views;

public partial class InicioPage : ContentPage
{
    private readonly InicioViewModel _viewModel;

    public InicioPage(InicioViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDataCommand.ExecuteAsync(null);
    }
}
