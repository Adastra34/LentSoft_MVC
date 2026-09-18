using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LentSoft.Mobile.Models;
using LentSoft.Mobile.Services;

namespace LentSoft.Mobile.ViewModels;

public partial class TiendaViewModel : ObservableObject
{
    private readonly IApiService _apiService;
    private readonly ICartService _cartService;
    private readonly ILocalizationService _locService;
    private readonly IFavoriteService _favService;
    private readonly IOrderHistoryService _orderService;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedCategory = "Todas";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private ProductDto? _selectedProduct;

    [ObservableProperty]
    private bool _isDetailModalVisible;

    [ObservableProperty]
    private bool _isCartModalVisible;

    [ObservableProperty]
    private bool _isCartListStepVisible = true;

    [ObservableProperty]
    private bool _isCheckoutStepVisible = false;

    [ObservableProperty]
    private string _cardNumber = "4532 8901 2345 6789";

    [ObservableProperty]
    private string _cardHolderName = "Juan Pérez";

    [ObservableProperty]
    private string _cardExpiry = "12/28";

    [ObservableProperty]
    private string _cardCvv = "888";

    [ObservableProperty]
    private string _shippingAddress = "Calle 45 # 12-34, Bogotá";

    [ObservableProperty]
    private int _cartItemCount;

    [ObservableProperty]
    private string _cartTotalDisplay = "$ 0 COP";

    [ObservableProperty]
    private bool _isToastVisible;

    [ObservableProperty]
    private string _toastMessage = "🛒 Producto añadido al carrito";

    [ObservableProperty]
    private string _currentLanguageDisplay = "🌐 ES";

    [ObservableProperty]
    private string _selectedProductFavHeart = "🤍";

    // Dynamic Localized UI String Properties
    [ObservableProperty] private string _shopTitle = "Tienda Óptica";
    [ObservableProperty] private string _searchPlaceholderText = "Buscar monturas, lentes...";
    [ObservableProperty] private string _categoryAllText = "Todas";
    [ObservableProperty] private string _categoryFramesText = "Monturas";
    [ObservableProperty] private string _categoryContactText = "Contacto";
    [ObservableProperty] private string _categorySunText = "Sol";
    [ObservableProperty] private string _categoryAccText = "Accesorios";
    [ObservableProperty] private string _addToCartText = "🛒 Añadir al Carrito";
    [ObservableProperty] private string _cartTitleText = "Tu Carrito de Compras";
    [ObservableProperty] private string _checkoutButtonText = "Proceder al pago =";
    [ObservableProperty] private string _paymentTitleText = "Método de Pago Simulado";
    [ObservableProperty] private string _cardNumberText = "Número de Tarjeta";
    [ObservableProperty] private string _cardHolderText = "Nombre en la Tarjeta";
    [ObservableProperty] private string _cardExpiryText = "Fecha de Expiración";
    [ObservableProperty] private string _cardCvvText = "CVV";
    [ObservableProperty] private string _shippingAddressText = "Dirección de Envío";
    [ObservableProperty] private string _confirmPaymentText = "Confirmar Pago 💳";
    [ObservableProperty] private string _descriptionText = "Descripción";
    [ObservableProperty] private string _warrantyTitle = "Garantía incluida";
    [ObservableProperty] private string _warrantyDesc = "Cobertura de 12 meses ante defectos de fábrica en todas tus monturas.";
    [ObservableProperty] private string _shippingTitle = "Envío gratis";
    [ObservableProperty] private string _shippingDesc = "Recibe tu pedido a domicilio en un plazo de 3 a 5 días hábiles sin costo.";
    [ObservableProperty] private string _qualityTitle = "Calidad garantizada";
    [ObservableProperty] private string _qualityDesc = "Materiales duraderos, cómodos y avalados por profesionales de la salud visual.";
    [ObservableProperty] private string _noProductsText = "No se encontraron productos";
    [ObservableProperty] private string _noProductsSubtext = "Prueba con otra categoría o búsqueda.";
    [ObservableProperty] private string _emptyCartTitle = "Tu carrito está vacío";
    [ObservableProperty] private string _emptyCartSubtitle = "Explora la tienda y añade tus productos preferidos.";
    [ObservableProperty] private string _backText = "← Regresar";

    public ObservableCollection<ProductDto> Products { get; } = new();
    public ObservableCollection<CartItemDto> CartItems { get; } = new();

    public ObservableCollection<string> Categories { get; } = new()
    {
        "Todas",
        "Monturas",
        "Contacto",
        "Sol",
        "Accesorios"
    };

    public TiendaViewModel(
        IApiService apiService,
        ICartService cartService,
        ILocalizationService locService,
        IFavoriteService favService,
        IOrderHistoryService orderService)
    {
        _apiService = apiService;
        _cartService = cartService;
        _locService = locService;
        _favService = favService;
        _orderService = orderService;

        _cartService.CartChanged += OnCartChanged;
        _locService.LanguageChanged += OnLanguageChanged;
        _favService.FavoritesChanged += OnFavoritesChanged;

        RefreshCartState();
        RefreshLanguageDisplay();
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            RefreshLanguageDisplay();
        });
    }

    private void OnFavoritesChanged(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (SelectedProduct != null)
            {
                SelectedProductFavHeart = _favService.IsFavorite(SelectedProduct.Id) ? "❤️" : "🤍";
            }
        });
    }

    private void RefreshLanguageDisplay()
    {
        CurrentLanguageDisplay = _locService.CurrentLanguage == "ES" ? "🌐 ES" : "🌐 EN";

        ShopTitle = _locService.GetString("ShopTitle");
        SearchPlaceholderText = _locService.GetString("SearchPlaceholder");
        CategoryAllText = _locService.GetString("All");
        CategoryFramesText = _locService.GetString("Frames");
        CategoryContactText = _locService.GetString("ContactLenses");
        CategorySunText = _locService.GetString("Sun");
        CategoryAccText = _locService.GetString("Accessories");
        AddToCartText = _locService.GetString("AddToCart");
        CartTitleText = _locService.GetString("CartTitle");
        CheckoutButtonText = _locService.GetString("CheckoutButton");
        PaymentTitleText = _locService.GetString("PaymentTitle");
        CardNumberText = _locService.GetString("CardNumber");
        CardHolderText = _locService.GetString("CardHolder");
        CardExpiryText = _locService.GetString("Expiry");
        CardCvvText = _locService.GetString("Cvv");
        ShippingAddressText = _locService.GetString("Address");
        ConfirmPaymentText = _locService.GetString("ConfirmPayment");
        DescriptionText = _locService.GetString("Description");
        WarrantyTitle = _locService.GetString("WarrantyTitle");
        WarrantyDesc = _locService.GetString("WarrantyDesc");
        ShippingTitle = _locService.GetString("ShippingTitle");
        ShippingDesc = _locService.GetString("ShippingDesc");
        QualityTitle = _locService.GetString("QualityTitle");
        QualityDesc = _locService.GetString("QualityDesc");
        NoProductsText = _locService.GetString("NoProductsFound");
        NoProductsSubtext = _locService.GetString("TryAnotherCategory");
        EmptyCartTitle = _locService.GetString("EmptyCartText");
        EmptyCartSubtitle = _locService.GetString("EmptyCartSubtext");
        BackText = _locService.GetString("Back");

        if (Products.Count > 0)
        {
            var currentList = Products.ToList();
            Products.Clear();
            foreach (var p in currentList)
            {
                Products.Add(p);
            }
        }
    }

    [RelayCommand]
    private void ToggleLanguage()
    {
        string nextLang = _locService.CurrentLanguage == "ES" ? "EN" : "ES";
        _locService.SetLanguage(nextLang);
        ShowGreenToast(nextLang == "ES" ? "🌐 Idioma: Español (ES)" : "🌐 Language: English (EN)");
    }

    private void OnCartChanged(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(RefreshCartState);
    }

    private void RefreshCartState()
    {
        CartItemCount = _cartService.TotalItemsCount;
        CartTotalDisplay = _cartService.DisplayTotalAmount;

        var currentItems = _cartService.Items.ToList();
        
        for (int i = CartItems.Count - 1; i >= 0; i--)
        {
            if (!currentItems.Any(ci => ci.Product.Id == CartItems[i].Product.Id))
            {
                CartItems.RemoveAt(i);
            }
        }

        foreach (var item in currentItems)
        {
            var existing = CartItems.FirstOrDefault(ci => ci.Product.Id == item.Product.Id);
            if (existing == null)
            {
                CartItems.Add(item);
            }
            else
            {
                existing.Cantidad = item.Cantidad;
            }
        }
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
    private void ProductSelected(ProductDto product)
    {
        if (product == null) return;
        SelectedProduct = product;
        SelectedProductFavHeart = _favService.IsFavorite(product.Id) ? "❤️" : "🤍";
        IsDetailModalVisible = true;
    }

    [RelayCommand]
    private void CloseDetailModal()
    {
        IsDetailModalVisible = false;
        SelectedProduct = null;
    }

    [RelayCommand]
    private void ToggleFavorite(ProductDto? product)
    {
        var target = product ?? SelectedProduct;
        if (target == null) return;

        _favService.ToggleFavorite(target);
        bool isFav = _favService.IsFavorite(target.Id);
        SelectedProductFavHeart = isFav ? "❤️" : "🤍";

        ShowGreenToast(isFav ? $"❤️ {target.DisplayNombre} {_locService.GetString("MyFavorites")}" : $"🤍 {target.DisplayNombre}");
    }

    [RelayCommand]
    private void AddToCart(ProductDto? product)
    {
        var targetProduct = product ?? SelectedProduct;
        if (targetProduct == null) return;

        _cartService.AddProduct(targetProduct);
        CloseDetailModal();

        ShowGreenToast(_locService.GetString("AddedToCart"));
    }

    private void ShowGreenToast(string message)
    {
        ToastMessage = message;
        IsToastVisible = true;

        Task.Run(async () =>
        {
            await Task.Delay(2500);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsToastVisible = false;
            });
        });
    }

    [RelayCommand]
    private void OpenCartModal()
    {
        IsCartListStepVisible = true;
        IsCheckoutStepVisible = false;
        IsCartModalVisible = true;
    }

    [RelayCommand]
    private void CloseCartModal()
    {
        IsCartModalVisible = false;
        IsCartListStepVisible = true;
        IsCheckoutStepVisible = false;
    }

    [RelayCommand]
    private void IncreaseCartItem(int productId)
    {
        _cartService.IncreaseQuantity(productId);
    }

    [RelayCommand]
    private void DecreaseCartItem(int productId)
    {
        _cartService.DecreaseQuantity(productId);
    }

    [RelayCommand]
    private void RemoveCartItem(int productId)
    {
        _cartService.RemoveProduct(productId);
    }

    [RelayCommand]
    private async Task ProceedToCheckoutAsync()
    {
        if (_cartService.TotalItemsCount == 0)
        {
            await Shell.Current.DisplayAlert("Carrito Vacío", "No tienes productos en tu carrito para realizar el pago.", "OK");
            return;
        }

        IsCartListStepVisible = false;
        IsCheckoutStepVisible = true;
    }

    [RelayCommand]
    private void BackToCartList()
    {
        IsCheckoutStepVisible = false;
        IsCartListStepVisible = true;
    }

    [RelayCommand]
    private async Task ConfirmPaymentAsync()
    {
        if (string.IsNullOrWhiteSpace(ShippingAddress))
        {
            await Shell.Current.DisplayAlert("Campo Requerido", "Por favor ingresa la dirección completa de envío.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(CardHolderName))
        {
            await Shell.Current.DisplayAlert("Campo Requerido", "Por favor ingresa el nombre en la tarjeta.", "OK");
            return;
        }

        string totalPurchased = CartTotalDisplay;
        var cartSnapshot = _cartService.Items.ToList();

        // Record order in order history
        _orderService.AddOrder(new OrderRecordDto
        {
            OrderNumber = $"ORD-2026-{Random.Shared.Next(1000, 9999)}",
            Date = DateTime.Now,
            Status = _locService.CurrentLanguage == "ES" ? "En camino" : "Shipped",
            Total = _cartService.TotalAmount,
            Items = cartSnapshot
        });

        _cartService.Clear();
        CloseCartModal();

        ShowGreenToast(_locService.GetString("PaymentSuccess"));
    }
}
