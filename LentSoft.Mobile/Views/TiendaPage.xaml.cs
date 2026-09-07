using LentSoft.Mobile.ViewModels;

namespace LentSoft.Mobile.Views;

public partial class TiendaPage : ContentPage
{
    private readonly TiendaViewModel _viewModel;

    public TiendaPage(TiendaViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel.Products.Count == 0)
        {
            await _viewModel.LoadProductsCommand.ExecuteAsync(null);
        }
    }
}
