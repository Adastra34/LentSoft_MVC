using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Services;
using Xunit;

namespace LentSoft.Tests;

public class PagoVentaServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LentSoftDbContext _context;
    private readonly PagoVentaService _pagoService;
    private readonly User _testUser;

    public PagoVentaServiceTests()
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
            Nombre = "Juan",
            Apellido = "Pérez",
            TipoDocumento = "CC",
            NumeroDocumento = "12345678",
            Email = "testuser@lentsoft.test",
            PasswordHash = "hash123",
            Role = "usuario"
        };
        _context.Users.Add(_testUser);
        _context.SaveChanges();

        _pagoService = new PagoVentaService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task RegistrarAbono_AbonoMayorAlSaldo_ThrowsInvalidOperationException()
    {
        // Arrange
        var order = new Order
        {
            UserId = _testUser.Id,
            Total = 100000m,
            MontoPagado = 20000m,
            Estado = "pendiente",
            EstadoPago = "abonado",
            Activo = true,
            FechaPedido = DateTime.UtcNow
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Saldo actual = 100000 - 20000 = 80000. Intentar abonar 90000
        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _pagoService.RegistrarAbonoAsync(order.Id, 90000m, "Efectivo", "Vendedor"));

        Assert.Contains("supera el saldo pendiente", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegistrarAbono_VentaCancelada_ThrowsInvalidOperationException()
    {
        // Arrange
        var order = new Order
        {
            UserId = _testUser.Id,
            Total = 150000m,
            MontoPagado = 0m,
            Estado = "cancelado",
            EstadoPago = "cancelado",
            Activo = true,
            FechaPedido = DateTime.UtcNow
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _pagoService.RegistrarAbonoAsync(order.Id, 50000m, "Efectivo", "Vendedor"));

        Assert.Contains("cancelada", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegistrarAbono_AbonoCompleto_CambiaEstadoAPagado()
    {
        // Arrange
        var order = new Order
        {
            UserId = _testUser.Id,
            Total = 200000m,
            MontoPagado = 0m,
            Estado = "pendiente",
            EstadoPago = "pendiente",
            Activo = true,
            FechaPedido = DateTime.UtcNow
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Act - Abonar el total restante
        var pago = await _pagoService.RegistrarAbonoAsync(order.Id, 200000m, "Efectivo", "Vendedor");

        // Assert
        Assert.NotNull(pago);
        Assert.StartsWith($"REC-{DateTime.UtcNow.Year}-", pago.NumeroComprobante);
        Assert.Equal(200000m, pago.Monto);

        var updatedOrder = await _context.Orders.FindAsync(order.Id);
        Assert.NotNull(updatedOrder);
        Assert.Equal("pagado", updatedOrder.EstadoPago);
        Assert.Equal("pagado", updatedOrder.Estado);
        Assert.Equal(200000m, updatedOrder.MontoPagado);
    }

    [Fact]
    public async Task RegistrarAbono_GeneraConsecutivoUnicoYConsecutivo()
    {
        // Arrange
        var order = new Order
        {
            UserId = _testUser.Id,
            Total = 300000m,
            MontoPagado = 0m,
            Estado = "pendiente",
            EstadoPago = "pendiente",
            Activo = true,
            FechaPedido = DateTime.UtcNow
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Act - Dos abonos parciales sucesivos
        var pago1 = await _pagoService.RegistrarAbonoAsync(order.Id, 50000m, "Efectivo", "Vendedor");
        var pago2 = await _pagoService.RegistrarAbonoAsync(order.Id, 50000m, "Transferencia", "Vendedor");

        // Assert
        Assert.NotEqual(pago1.NumeroComprobante, pago2.NumeroComprobante);
        Assert.StartsWith($"REC-{DateTime.UtcNow.Year}-0001", pago1.NumeroComprobante);
        Assert.StartsWith($"REC-{DateTime.UtcNow.Year}-0002", pago2.NumeroComprobante);
    }
}
