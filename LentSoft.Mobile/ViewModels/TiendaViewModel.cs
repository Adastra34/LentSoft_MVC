using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LentSoft.Mobile.Models;
using LentSoft.Mobile.Services;

namespace LentSoft.Mobile.ViewModels;

public partial class TiendaViewModel : ObservableObject
{
    private readonly IApiService _apiService;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedCategory = "Todas";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isRefreshing;

    public ObservableCollection<ProductDto> Products { get; } = new();

    public ObservableCollection<string> Categories { get; } = new()
    {
        "Todas",
        "Monturas",
        "Contacto",
        "Sol",
        "Accesorios"
    };

    public TiendaViewModel(IApiService apiService)
    {
        _apiService = apiService;
    }

    [RelayCommand]
    public async Task LoadProductsAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            Products.Clear();

            string? catQuery = SelectedCategory.Equals("Todas", StringComparison.OrdinalIgnoreCase) ? null : SelectedCategory;
            var list = await _apiService.GetProductsAsync(catQuery, SearchText);

            foreach (var p in list)
            {
                Products.Add(p);
            }
        }
        catch
        {
            // Handle error gracefully
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task SelectCategoryAsync(string category)
    {
        if (SelectedCategory == category) return;

        SelectedCategory = category;
        await LoadProductsAsync();
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        await LoadProductsAsync();
    }

    [RelayCommand]
    private async Task ProductSelectedAsync(ProductDto product)
    {
        if (product == null) return;

        await Shell.Current.DisplayAlert(
            product.Nombre,
            $"{product.Descripcion ?? "Producto de alta calidad LentSoft"}\n\nMarca: {product.Marca ?? "LentSoft"}\nPrecio: {product.DisplayPrice}\nStock disponible: {product.Stock} unidades",
            "Cerrar");
    }
}
