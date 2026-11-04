using LentSoft.Web.Services;
using Xunit;

namespace LentSoft.Tests;

public class SupplierGatewayTests
{
    [Fact]
    public async Task ConsultarCatalogoAsync_ReturnsCatalogItems()
    {
        // Arrange
        ISupplierGateway gateway = new MockSupplierGateway();

        // Act
        var result = await gateway.ConsultarCatalogoAsync("PROV001");

        // Assert
        Assert.True(result.Success);
        Assert.NotEmpty(result.Items);
        Assert.Contains(result.Items, i => i.Marca == "Ray-Ban");
    }

    [Fact]
    public async Task ConsultarCatalogoAsync_FiltersByCategoria()
    {
        // Arrange
        ISupplierGateway gateway = new MockSupplierGateway();

        // Act
        var result = await gateway.ConsultarCatalogoAsync("PROV001", "contacto");

        // Assert
        Assert.True(result.Success);
        Assert.All(result.Items, i => Assert.Contains("contacto", i.Categoria, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task EnviarPedidoAsync_ReturnsTrackingNumberAndEstimatedDate()
    {
        // Arrange
        ISupplierGateway gateway = new MockSupplierGateway();
        var request = new SupplierOrderDispatchRequest
        {
            SupplierId = "PROV001",
            NumeroPedidoInterno = "PED-2026-001",
            ProductSku = "EXT-RB-001",
            ProductoNombre = "Montura Ray-Ban Aviator",
            Cantidad = 15,
            PrecioUnitario = 320000m
        };

        // Act
        var result = await gateway.EnviarPedidoAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.NumeroPedidoProveedor);
        Assert.NotNull(result.TrackingNumber);
        Assert.NotNull(result.FechaEstimadaEntrega);
        Assert.StartsWith("B2B-PROV-", result.NumeroPedidoProveedor);
        Assert.StartsWith("TRK-CO-", result.TrackingNumber);
    }

    [Fact]
    public async Task ConsultarEstadoPedidoAsync_ReturnsCurrentStatus()
    {
        // Arrange
        ISupplierGateway gateway = new MockSupplierGateway();

        // Act
        var result = await gateway.ConsultarEstadoPedidoAsync("PROV001", "B2B-PROV-12345");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("B2B-PROV-12345", result.NumeroPedidoProveedor);
        Assert.False(string.IsNullOrWhiteSpace(result.Estado));
    }
}

