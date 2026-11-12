using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Services;
using Xunit;

namespace LentSoft.Tests;

public class InvoiceServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LentSoftDbContext _context;
    private readonly InvoiceService _service;

    public InvoiceServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LentSoftDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new LentSoftDbContext(options);
        _context.Database.EnsureCreated();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Facturacion:ResolucionDianNumero", "18764000001" },
                { "Facturacion:RangoAutorizadoDesde", "SETP-1" },
                { "Facturacion:RangoAutorizadoHasta", "SETP-50000" },
                { "Facturacion:NitEmisor", "901234567-8" }
            })
            .Build();

        _service = new InvoiceService(_context, config);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private async Task<Order> CrearPedidoConProductosAsync(decimal total, decimal montoPagado, string estado = "pendiente")
    {
        var user = new User
        {
            Nombre = "Ana",
            Apellido = "Gomez",
            Email = $"ana_{Guid.NewGuid():N}@test.com",
            PasswordHash = "hash123",
            Role = "usuario",
            Telefono = "3201112233",
            NumeroDocumento = $"DOC_{Guid.NewGuid():N}"
        };
        _context.Users.Add(user);

        var product = new Product
        {
            Nombre = "Montura Test",
            Precio = total,
            PorcentajeIva = 19m,
            Stock = 20,
            Activo = true
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var order = new Order
        {
            UserId = user.Id,
            Total = total,
            MontoPagado = montoPagado,
            Estado = estado,
            EstadoPago = montoPagado >= total ? "pagado" : (montoPagado > 0 ? "abonado" : "pendiente"),
            Activo = true,
            FechaPedido = DateTime.UtcNow
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var orderItem = new OrderItem
        {
            OrderId = order.Id,
            ProductId = product.Id,
            Cantidad = 1,
            PrecioUnitario = total
        };
        _context.OrderItems.Add(orderItem);
        await _context.SaveChangesAsync();

        return order;
    }

    [Fact]
    public async Task CreateAsync_DobleFactura_LanzaInvalidOperationException()
    {
        // Arrange: Crear pedido y su primera factura
        var order = await CrearPedidoConProductosAsync(119000m, 119000m);
        var factura1 = new Invoice { OrderId = order.Id };
        var creada = await _service.CreateAsync(factura1);
        Assert.NotNull(creada);

        // Act & Assert: Intentar facturar el mismo pedido de nuevo
        var factura2 = new Invoice { OrderId = order.Id };
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateAsync(factura2));

        Assert.Contains("ya cuenta con una factura activa", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_EstadoSegunSaldo_AsignaCorrectamente()
    {
        // 1. Pedido con saldo 0 (totalmente pagado) -> Factura 'pagada'
        var orderPagado = await CrearPedidoConProductosAsync(100000m, 100000m);
        var fPagada = await _service.CreateAsync(new Invoice { OrderId = orderPagado.Id });
        Assert.Equal("pagada", fPagada.Estado);
        Assert.StartsWith("FAC-", fPagada.NumeroFactura);
        Assert.NotNull(fPagada.CUFE);

        // 2. Pedido con abono parcial -> Factura 'parcial'
        var orderParcial = await CrearPedidoConProductosAsync(100000m, 40000m);
        var fParcial = await _service.CreateAsync(new Invoice { OrderId = orderParcial.Id });
        Assert.Equal("parcial", fParcial.Estado);

        // 3. Pedido sin pagos -> Factura 'pendiente'
        var orderPendiente = await CrearPedidoConProductosAsync(100000m, 0m);
        var fPendiente = await _service.CreateAsync(new Invoice { OrderId = orderPendiente.Id });
        Assert.Equal("pendiente", fPendiente.Estado);
    }

    [Fact]
    public async Task GetOrdersAvailableForInvoicingAsync_ExcluyeFacturadosCanceladosEInactivos()
    {
        // Arrange:
        // Order 1: Válido y disponible
        var orderValido = await CrearPedidoConProductosAsync(50000m, 50000m);

        // Order 2: Ya facturado
        var orderFacturado = await CrearPedidoConProductosAsync(60000m, 60000m);
        await _service.CreateAsync(new Invoice { OrderId = orderFacturado.Id });

        // Order 3: Cancelado
        var orderCancelado = await CrearPedidoConProductosAsync(70000m, 0m, estado: "cancelado");

        // Order 4: Inactivo
        var orderInactivo = await CrearPedidoConProductosAsync(80000m, 0m);
        orderInactivo.Activo = false;
        await _context.SaveChangesAsync();

        // Act
        var disponibles = await _service.GetOrdersAvailableForInvoicingAsync();

        // Assert
        Assert.Contains(disponibles, o => o.Id == orderValido.Id);
        Assert.DoesNotContain(disponibles, o => o.Id == orderFacturado.Id);
        Assert.DoesNotContain(disponibles, o => o.Id == orderCancelado.Id);
        Assert.DoesNotContain(disponibles, o => o.Id == orderInactivo.Id);
    }
}
