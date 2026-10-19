using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Services;
using Xunit;

namespace LentSoft.Tests;

public class InvoiceServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LentSoftDbContext _context;
    private readonly InvoiceService _invoiceService;
    private readonly User _testUser;

    public InvoiceServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LentSoftDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new LentSoftDbContext(options);
        _context.Database.EnsureCreated();

        _testUser = new User
        {
            Nombre = "Ana",
            Apellido = "Torres",
            TipoDocumento = "CC",
            NumeroDocumento = "11223344",
            Email = "anatorres@lentsoft.test",
            PasswordHash = "hash123",
            Role = "usuario"
        };
        _context.Users.Add(_testUser);
        _context.SaveChanges();

        _invoiceService = new InvoiceService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task CreateAsync_DobleFactura_ThrowsInvalidOperationException()
    {
        // Arrange
        var product = new Product { Nombre = "Montura Test", Precio = 100000m, Stock = 10, PorcentajeIva = 19, Activo = true };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var order = new Order
        {
            UserId = _testUser.Id,
            Total = 119000m,
            MontoPagado = 119000m,
            Estado = "pagado",
            EstadoPago = "pagado",
            Activo = true,
            FechaPedido = DateTime.UtcNow,
            OrderItems = new List<OrderItem>
            {
                new OrderItem { ProductId = product.Id, Cantidad = 1, PrecioUnitario = 100000m }
            }
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Primera factura exitosa
        var primeraFactura = new Invoice
        {
            OrderId = order.Id,
            FechaEmision = DateTime.UtcNow,
            Activo = true
        };
        await _invoiceService.CreateAsync(primeraFactura);

        // Act & Assert - Intentar crear segunda factura sobre el mismo pedido
        var segundaFactura = new Invoice
        {
            OrderId = order.Id,
            FechaEmision = DateTime.UtcNow,
            Activo = true
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _invoiceService.CreateAsync(segundaFactura));

        Assert.Contains("ya cuenta con una factura activa", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(100000, 100000, "pagada")] // Saldo 0 -> Pagada
    [InlineData(100000, 40000, "parcial")]  // Abono parcial -> Parcial
    [InlineData(100000, 0, "pendiente")]   // Sin abonos -> Pendiente
    public async Task CreateAsync_CalculaEstadoSegunSaldoDelPedido(decimal total, decimal montoPagado, string estadoEsperado)
    {
        // Arrange
        var product = new Product { Nombre = "Lente Test", Precio = total, Stock = 10, PorcentajeIva = 0, Activo = true };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var order = new Order
        {
            UserId = _testUser.Id,
            Total = total,
            MontoPagado = montoPagado,
            Estado = montoPagado >= total ? "pagado" : "pendiente",
            EstadoPago = montoPagado >= total ? "pagado" : (montoPagado > 0 ? "abonado" : "pendiente"),
            Activo = true,
            FechaPedido = DateTime.UtcNow,
            OrderItems = new List<OrderItem>
            {
                new OrderItem { ProductId = product.Id, Cantidad = 1, PrecioUnitario = total }
            }
        };

        if (montoPagado > 0)
        {
            order.Pagos = new List<PagoVenta>
            {
                new PagoVenta { Monto = montoPagado, MetodoPago = "Efectivo", NumeroComprobante = "REC-2026-TEST" }
            };
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var invoice = new Invoice
        {
            OrderId = order.Id,
            FechaEmision = DateTime.UtcNow,
            Activo = true
        };

        // Act
        var created = await _invoiceService.CreateAsync(invoice);

        // Assert
        Assert.NotNull(created);
        Assert.Equal(estadoEsperado, created.Estado.ToLower());
    }

    [Fact]
    public async Task GetOrdersAvailableForInvoicingAsync_SoloDevuelvePedidosActivosNoCanceladosYSinFacturaActiva()
    {
        // Arrange
        // Pedido 1: Válido (activo, pendiente, sin factura) -> Debe aparecer
        var o1 = new Order { UserId = _testUser.Id, Total = 50000m, Estado = "pendiente", Activo = true, FechaPedido = DateTime.UtcNow.AddHours(-1) };
        // Pedido 2: Cancelado -> NO debe aparecer
        var o2 = new Order { UserId = _testUser.Id, Total = 70000m, Estado = "cancelado", Activo = true, FechaPedido = DateTime.UtcNow.AddHours(-2) };
        // Pedido 3: Inactivo -> NO debe aparecer
        var o3 = new Order { UserId = _testUser.Id, Total = 80000m, Estado = "pendiente", Activo = false, FechaPedido = DateTime.UtcNow.AddHours(-3) };
        // Pedido 4: Con factura activa -> NO debe aparecer
        var o4 = new Order { UserId = _testUser.Id, Total = 90000m, Estado = "pendiente", Activo = true, FechaPedido = DateTime.UtcNow.AddHours(-4) };

        _context.Orders.AddRange(o1, o2, o3, o4);
        await _context.SaveChangesAsync();

        // Agregar factura activa a o4
        _context.Invoices.Add(new Invoice
        {
            OrderId = o4.Id,
            NumeroFactura = "FAC-2026-0099",
            Estado = "pendiente",
            Activo = true,
            FechaEmision = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var disponibles = await _invoiceService.GetOrdersAvailableForInvoicingAsync();

        // Assert
        Assert.Contains(disponibles, o => o.Id == o1.Id);
        Assert.DoesNotContain(disponibles, o => o.Id == o2.Id);
        Assert.DoesNotContain(disponibles, o => o.Id == o3.Id);
        Assert.DoesNotContain(disponibles, o => o.Id == o4.Id);
    }
}
