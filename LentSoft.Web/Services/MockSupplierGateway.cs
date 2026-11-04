namespace LentSoft.Web.Services;

/// <summary>
/// Implementación simulada (Mock) de la pasarela de integración externa con proveedores (ISupplierGateway).
/// Proporciona datos simulados y respuestas determinísticas para desarrollo, pruebas unitarias
/// e integraciones B2B ópticas sin necesidad de credenciales activas de proveedores como Luxottica, Safilo o Essilor.
/// </summary>
public class MockSupplierGateway : ISupplierGateway
{
    private readonly ILogger<MockSupplierGateway>? _logger;

    public MockSupplierGateway(ILogger<MockSupplierGateway>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<SupplierCatalogResult> ConsultarCatalogoAsync(string supplierId, string? categoria = null, CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("MockSupplierGateway: Consultando catálogo para proveedor {SupplierId}, categoría {Categoria}", supplierId, categoria);

        if (string.IsNullOrWhiteSpace(supplierId))
        {
            return Task.FromResult(new SupplierCatalogResult
            {
                Success = false,
                SupplierId = supplierId,
                Message = "El identificador del proveedor es requerido."
            });
        }

        // Catálogo simulado según proveedor
        var items = new List<SupplierCatalogItemDto>
        {
            new() { Sku = "EXT-RB-001", Nombre = "Montura Ray-Ban Aviator Classic", Marca = "Ray-Ban", Categoria = "lentes-sol", PrecioCosto = 320000m, StockDisponible = 45, TiempoEntregaDias = 3 },
            new() { Sku = "EXT-RB-002", Nombre = "Montura Ray-Ban Wayfarer Original", Marca = "Ray-Ban", Categoria = "monturas", PrecioCosto = 280000m, StockDisponible = 30, TiempoEntregaDias = 3 },
            new() { Sku = "EXT-OK-101", Nombre = "Gafas Oakley Holbrook Sport", Marca = "Oakley", Categoria = "lentes-sol", PrecioCosto = 350000m, StockDisponible = 20, TiempoEntregaDias = 5 },
            new() { Sku = "EXT-AC-201", Nombre = "Lentes de Contacto Acuvue Oasys (Caja x6)", Marca = "Acuvue", Categoria = "lentes-contacto", PrecioCosto = 110000m, StockDisponible = 150, TiempoEntregaDias = 2 },
            new() { Sku = "EXT-ES-301", Nombre = "Lente Oftálmico Digital Antirreflejo Crizal", Marca = "Essilor", Categoria = "lentes-graduados", PrecioCosto = 190000m, StockDisponible = 80, TiempoEntregaDias = 4 }
        };

        if (!string.IsNullOrWhiteSpace(categoria))
        {
            items = items.FindAll(x => x.Categoria.Contains(categoria, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult(new SupplierCatalogResult
        {
            Success = true,
            SupplierId = supplierId,
            Items = items,
            Message = $"Catálogo obtenido exitosamente con {items.Count} ítems disponibles."
        });
    }

    /// <inheritdoc />
    public Task<SupplierOrderDispatchResult> EnviarPedidoAsync(SupplierOrderDispatchRequest request, CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("MockSupplierGateway: Enviando orden {NumeroPedidoInterno} a proveedor {SupplierId}", request.NumeroPedidoInterno, request.SupplierId);

        if (string.IsNullOrWhiteSpace(request.SupplierId) || request.Cantidad <= 0)
        {
            return Task.FromResult(new SupplierOrderDispatchResult
            {
                Success = false,
                NumeroPedidoInterno = request.NumeroPedidoInterno,
                Message = "Datos de pedido inválidos o cantidad menor o igual a 0."
            });
        }

        var hash = Math.Abs((request.NumeroPedidoInterno + request.SupplierId).GetHashCode());
        var numProv = $"B2B-PROV-{hash % 100000:D5}";
        var tracking = $"TRK-CO-{hash % 1000000:D6}";
        var fechaEstimada = DateTime.UtcNow.AddDays(4);

        return Task.FromResult(new SupplierOrderDispatchResult
        {
            Success = true,
            NumeroPedidoInterno = request.NumeroPedidoInterno,
            NumeroPedidoProveedor = numProv,
            TrackingNumber = tracking,
            FechaEstimadaEntrega = fechaEstimada,
            Message = $"Orden confirmada por el proveedor. Despacho programado para {fechaEstimada:yyyy-MM-dd}."
        });
    }

    /// <inheritdoc />
    public Task<SupplierOrderStatusResult> ConsultarEstadoPedidoAsync(string supplierId, string numeroPedidoProveedor, CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("MockSupplierGateway: Consultando estado del pedido {NumeroPedidoProveedor}", numeroPedidoProveedor);

        if (string.IsNullOrWhiteSpace(numeroPedidoProveedor))
        {
            return Task.FromResult(new SupplierOrderStatusResult
            {
                Success = false,
                NumeroPedidoProveedor = numeroPedidoProveedor,
                Message = "El número de pedido de proveedor es requerido."
            });
        }

        return Task.FromResult(new SupplierOrderStatusResult
        {
            Success = true,
            NumeroPedidoProveedor = numeroPedidoProveedor,
            Estado = "EnPreparacion",
            TrackingNumber = $"TRK-{numeroPedidoProveedor}",
            FechaEstimadaEntrega = DateTime.UtcNow.AddDays(3),
            Message = "El pedido se encuentra en proceso de manufactura y embalaje en laboratorio."
        });
    }
}

