namespace LentSoft.Mobile.Services;

public class LocalizationService : ILocalizationService
{
    private static LocalizationService? _instance;
    public static LocalizationService Instance => _instance ??= new LocalizationService();

    private string _currentLanguage = "ES";
    public string CurrentLanguage => _currentLanguage;

    public event EventHandler? LanguageChanged;

    public LocalizationService()
    {
        _instance = this;
    }

    private readonly Dictionary<string, Dictionary<string, string>> _dictionary = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ES"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["ShopTitle"] = "Tienda Óptica",
            ["SearchPlaceholder"] = "Buscar monturas, lentes...",
            ["All"] = "Todas",
            ["Frames"] = "Monturas",
            ["ContactLenses"] = "Contacto",
            ["Sun"] = "Sol",
            ["Accessories"] = "Accesorios",
            ["AddToCart"] = "🛒 Añadir al Carrito",
            ["CloseDetail"] = "Cerrar Detalle",
            ["Description"] = "Descripción",
            ["WarrantyTitle"] = "Garantía incluida",
            ["WarrantyDesc"] = "Cobertura de 12 meses ante defectos de fábrica en todas tus monturas.",
            ["ShippingTitle"] = "Envío gratis",
            ["ShippingDesc"] = "Recibe tu pedido a domicilio en un plazo de 3 a 5 días hábiles sin costo.",
            ["QualityTitle"] = "Calidad garantizada",
            ["QualityDesc"] = "Materiales duraderos, cómodos y avalados por profesionales de la salud visual.",
            ["InStock"] = "en stock",
            ["CartTitle"] = "Tu Carrito de Compras",
            ["CheckoutButton"] = "Proceder al pago =",
            ["PaymentTitle"] = "Método de Pago Simulado",
            ["CardNumber"] = "Número de Tarjeta",
            ["CardHolder"] = "Nombre en la Tarjeta",
            ["Expiry"] = "Fecha de Expiración",
            ["Cvv"] = "CVV",
            ["Address"] = "Dirección de Envío",
            ["ConfirmPayment"] = "Confirmar Pago 💳",
            ["Back"] = "← Regresar",
            ["Total"] = "Total General",
            ["Products"] = "Productos",
            ["Shipping"] = "Envío",
            ["Free"] = "Gratis",
            ["AddedToCart"] = "🛒 Producto añadido al carrito",
            ["PaymentSuccess"] = "💳 ¡Pago realizado con éxito!",
            ["AppointmentsTitle"] = "Agendar Cita Médica",
            ["SelectService"] = "Selecciona un servicio",
            ["ConfirmAppointment"] = "Confirmar y Agendar Cita",
            ["MyProfile"] = "Mi Perfil",
            ["MyFavorites"] = "Mis Favoritos",
            ["MyOrders"] = "Mis Pedidos",
            ["OpticPrescription"] = "Fórmula óptica",
            ["MyData"] = "Mis datos",
            ["Addresses"] = "Direcciones",
            ["Language"] = "Idioma / Language",
            ["Security"] = "Seguridad",
            ["Notifications"] = "Notificaciones",
            ["Logout"] = "Cerrar sesión",
            ["NoProductsFound"] = "No se encontraron productos",
            ["TryAnotherCategory"] = "Prueba con otra categoría o búsqueda.",
            ["EmptyCartText"] = "Tu carrito está vacío",
            ["EmptyCartSubtext"] = "Explora la tienda y añade tus productos preferidos."
        },
        ["EN"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["ShopTitle"] = "Optical Store",
            ["SearchPlaceholder"] = "Search frames, lenses...",
            ["All"] = "All",
            ["Frames"] = "Frames",
            ["ContactLenses"] = "Contact",
            ["Sun"] = "Sun",
            ["Accessories"] = "Accessories",
            ["AddToCart"] = "🛒 Add to Cart",
            ["CloseDetail"] = "Close Detail",
            ["Description"] = "Description",
            ["WarrantyTitle"] = "Warranty Included",
            ["WarrantyDesc"] = "12-month coverage for manufacturing defects on all your frames.",
            ["ShippingTitle"] = "Free Shipping",
            ["ShippingDesc"] = "Receive your order at home within 3 to 5 business days at no cost.",
            ["QualityTitle"] = "Guaranteed Quality",
            ["QualityDesc"] = "Durable, comfortable materials approved by eye care professionals.",
            ["InStock"] = "in stock",
            ["CartTitle"] = "Your Shopping Cart",
            ["CheckoutButton"] = "Proceed to Checkout =",
            ["PaymentTitle"] = "Simulated Payment Method",
            ["CardNumber"] = "Card Number",
            ["CardHolder"] = "Name on Card",
            ["Expiry"] = "Expiration Date",
            ["Cvv"] = "CVV",
            ["Address"] = "Shipping Address",
            ["ConfirmPayment"] = "Confirm Payment 💳",
            ["Back"] = "← Back",
            ["Total"] = "Grand Total",
            ["Products"] = "Products",
            ["Shipping"] = "Shipping",
            ["Free"] = "Free",
            ["AddedToCart"] = "🛒 Item added to cart",
            ["PaymentSuccess"] = "💳 Payment confirmed successfully!",
            ["AppointmentsTitle"] = "Schedule Medical Appointment",
            ["SelectService"] = "Select a service",
            ["ConfirmAppointment"] = "Confirm and Schedule Appointment",
            ["MyProfile"] = "My Profile",
            ["MyFavorites"] = "My Favorites",
            ["MyOrders"] = "My Orders",
            ["OpticPrescription"] = "Optical Prescription",
            ["MyData"] = "My Personal Data",
            ["Addresses"] = "Addresses",
            ["Language"] = "Language / Idioma",
            ["Security"] = "Security",
            ["Notifications"] = "Notifications",
            ["Logout"] = "Log out",
            ["NoProductsFound"] = "No products found",
            ["TryAnotherCategory"] = "Try another category or search query.",
            ["EmptyCartText"] = "Your shopping cart is empty",
            ["EmptyCartSubtext"] = "Browse the store and add your favorite products."
        }
    };

    public void SetLanguage(string langCode)
    {
        if (string.IsNullOrWhiteSpace(langCode)) return;
        var upper = langCode.Trim().ToUpper();

        if (upper == "ES" || upper == "EN")
        {
            _currentLanguage = upper;
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public string GetString(string key)
    {
        if (_dictionary.TryGetValue(_currentLanguage, out var langDict) &&
            langDict.TryGetValue(key, out var value))
        {
            return value;
        }

        return key;
    }

    public string TranslateProductTitle(string originalTitle)
    {
        if (string.IsNullOrWhiteSpace(originalTitle)) return string.Empty;
        if (_currentLanguage != "EN") return originalTitle;

        if (originalTitle == "Lentes Graduados Classic") return "Classic Prescription Lenses";
        if (originalTitle == "Lentes Ray-Ban Aviator") return "Ray-Ban Aviator Sunglasses";
        if (originalTitle == "Líquido Limpiador") return "Lens Cleaning Solution";
        if (originalTitle == "Montura Oakley Sport") return "Oakley Sport Frame";
        if (originalTitle == "Gafas de Sol Elegantes") return "Elegant Sunglasses";
        if (originalTitle == "Lentes de Contacto Diarios") return "Daily Contact Lenses";

        string result = originalTitle;
        if (result.StartsWith("Montura ", StringComparison.OrdinalIgnoreCase))
            result = "Frame " + result.Substring(8);
        if (result.StartsWith("Lentes de ", StringComparison.OrdinalIgnoreCase))
            result = "Lenses for " + result.Substring(10);
        if (result.StartsWith("Gafas de ", StringComparison.OrdinalIgnoreCase))
            result = "Glasses for " + result.Substring(9);

        return result;
    }
}
