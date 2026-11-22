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
            ["AddToCart"] = "Añadir al Carrito",
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
            ["ConfirmPayment"] = "Confirmar Pago",
            ["Back"] = "← Regresar",
            ["Total"] = "Total General",
            ["Products"] = "Productos",
            ["Shipping"] = "Envío",
            ["Free"] = "Gratis",
            ["AddedToCart"] = "Producto añadido al carrito",
            ["PaymentSuccess"] = "¡Pago realizado con éxito!",
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
            ["EmptyCartSubtext"] = "Explora la tienda y añade tus productos preferidos.",
            ["PrescriptionLoading"] = "Consultando tu fórmula médica...",
            ["NoPrescriptionTitle"] = "No tienes una fórmula médica registrada",
            ["NoPrescriptionSubtitle"] = "Agenda tu cita de optometría con nuestros especialistas para obtener tu prescripción personalizada.",
            ["ScheduleAppointment"] = "Agendar cita",
            ["Retry"] = "Reintentar",
            ["Close"] = "Cerrar",
            ["PrescriptionOfflineNotice"] = "Modo sin conexión - Mostrando última fórmula sincronizada."
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
            ["AddToCart"] = "Add to Cart",
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
            ["ConfirmPayment"] = "Confirm Payment",
            ["Back"] = "← Back",
            ["Total"] = "Grand Total",
            ["Products"] = "Products",
            ["Shipping"] = "Shipping",
            ["Free"] = "Free",
            ["AddedToCart"] = "Product added to cart",
            ["PaymentSuccess"] = "Payment successful!",
            ["AppointmentsTitle"] = "Schedule Eye Appointment",
            ["SelectService"] = "Select a service",
            ["ConfirmAppointment"] = "Confirm & Schedule Appointment",
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
            ["EmptyCartText"] = "Your cart is empty",
            ["EmptyCartSubtext"] = "Browse the store and add your favorite items.",
            ["PrescriptionLoading"] = "Loading your medical prescription...",
            ["NoPrescriptionTitle"] = "No medical prescription found",
            ["NoPrescriptionSubtitle"] = "Schedule an appointment with our eye specialists to obtain your prescription.",
            ["ScheduleAppointment"] = "Schedule appointment",
            ["Retry"] = "Retry",
            ["Close"] = "Close",
            ["PrescriptionOfflineNotice"] = "Offline mode - Showing last synchronized prescription."
        }
    };

    public string GetString(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return string.Empty;

        if (_dictionary.TryGetValue(_currentLanguage, out var langDict) &&
            langDict.TryGetValue(key, out var value))
        {
            return value;
        }

        if (_dictionary.TryGetValue("ES", out var fallbackDict) &&
            fallbackDict.TryGetValue(key, out var fallbackValue))
        {
            return fallbackValue;
        }

        return key;
    }

    public void SetLanguage(string lang)
    {
        if (string.IsNullOrWhiteSpace(lang)) return;

        string normalized = lang.Trim().ToUpperInvariant();
        if (normalized != "ES" && normalized != "EN") normalized = "ES";

        if (_currentLanguage != normalized)
        {
            _currentLanguage = normalized;
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public string TranslateProductTitle(string originalTitle)
    {
        if (string.IsNullOrWhiteSpace(originalTitle) || _currentLanguage == "ES")
            return originalTitle;

        if (originalTitle.Contains("Estuche", StringComparison.OrdinalIgnoreCase))
            return "Premium Eyewear Case";
        if (originalTitle.Contains("Líquido", StringComparison.OrdinalIgnoreCase) || originalTitle.Contains("Limpiador", StringComparison.OrdinalIgnoreCase))
            return "Lens Cleaning Solution 120ml";
        if (originalTitle.Contains("Acuvue", StringComparison.OrdinalIgnoreCase))
            return "Acuvue Contact Lenses (Pack x6)";
        if (originalTitle.Contains("Graduados", StringComparison.OrdinalIgnoreCase))
            return "Classic Prescription Glasses";
        if (originalTitle.Contains("Oakley", StringComparison.OrdinalIgnoreCase))
            return "Oakley Sport Eyewear Frame";
        if (originalTitle.Contains("Aviator", StringComparison.OrdinalIgnoreCase))
            return "Ray-Ban Aviator Sunglasses";

        return originalTitle;
    }
}
