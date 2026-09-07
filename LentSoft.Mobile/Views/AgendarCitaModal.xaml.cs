using LentSoft.Mobile.ViewModels;

namespace LentSoft.Mobile.Views;

public partial class AgendarCitaModal : ContentPage
{
    public AgendarCitaModal(AgendarCitaViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
