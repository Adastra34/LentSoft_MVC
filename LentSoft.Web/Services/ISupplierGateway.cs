namespace LentSoft.Web.Services;

/// <summary>
/// Ítem de catálogo retornado por la pasarela de integración con el proveedor externo.
/// </summary>
public class SupplierCatalogItemDto
{
    public string Sku { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public decimal PrecioCosto { get; set; }
    public int StockDisponible { get; set; }
    public int TiempoEntregaDias { get; set; }
}

public class SupplierCatalogResult
{
    public bool Success { get; set; }
    public string SupplierId { get; set; } = string.Empty;
    public string? Message { get; set; }
    public List<SupplierCatalogItemDto> Items { get; set; } = new();
}

public class SupplierOrderDispatchRequest
{
    public string SupplierId { get; set; } = string.Empty;
    public string NumeroPedidoInterno { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public string ProductoNombre { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public string? Notas { get; set; }
}

public class SupplierOrderDispatchResult
{
    public bool Success { get; set; }
    public string NumeroPedidoInterno { get; set; } = string.Empty;
    public string? NumeroPedidoProveedor { get; set; }
    public string? TrackingNumber { get; set; }
    public DateTime? FechaEstimadaEntrega { get; set; }
    public string? Message { get; set; }
}

public class SupplierOrderStatusResult
{
    public bool Success { get; set; }
    public string NumeroPedidoProveedor { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty; // Pendiente, EnPreparacion, Despachado, Entregado
    public string? TrackingNumber { get; set; }
    public DateTime? FechaEstimadaEntrega { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Gateway de integración externa con proveedores y fabricantes de lentes y monturas.
/// Permite consultar catálogos remotos en tiempo real, enviar órdenes de compra
/// y verificar el estado de despacho y seguimiento de envíos.
/// </summary>
public interface ISupplierGateway
{
    /// <summary>
    /// Consulta el catálogo de productos disponibles en el proveedor externo.
    /// </summary>
    Task<SupplierCatalogResult> ConsultarCatalogoAsync(string supplierId, string? categoria = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Envía un pedido u orden de compra al proveedor externo para su fabricación y despacho.
    /// </summary>
    Task<SupplierOrderDispatchResult> EnviarPedidoAsync(SupplierOrderDispatchRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta el estado actual de despacho y guía de seguimiento de un pedido en el proveedor.
    /// </summary>
    Task<SupplierOrderStatusResult> ConsultarEstadoPedidoAsync(string supplierId, string numeroPedidoProveedor, CancellationToken cancellationToken = default);
}

