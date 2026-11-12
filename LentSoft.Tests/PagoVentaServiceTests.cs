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
    private readonly PagoVentaService _service;

    public PagoVentaServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LentSoftDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new LentSoftDbContext(options);
        _context.Database.EnsureCreated();

        _service = new PagoVentaService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private async Task<Order> CrearPedidoPruebaAsync(decimal total, string estado = "pendiente", string estadoPago = "pendiente")
    {
        var user = new User
        {
            Nombre = "Juan",
            Apellido = "Perez",
            Email = $"juan_{Guid.NewGuid():N}@test.com",
            PasswordHash = "hash123",
            Role = "usuario",
            Telefono = "3001112233",
            NumeroDocumento = $"DOC_{Guid.NewGuid():N}"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var order = new Order
        {
            UserId = user.Id,
            Total = total,
            Estado = estado,
            EstadoPago = estadoPago,
            MontoPagado = 0m,
            Activo = true,
            FechaPedido = DateTime.UtcNow
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return order;
    }

    [Fact]
    public async Task RegistrarAbono_MontoMayorAlSaldo_LanzaInvalidOperationException()
    {
        // Arrange
        var order = await CrearPedidoPruebaAsync(100000m);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.RegistrarAbonoAsync(order.Id, 120000m, "Efectivo", "Cajero 1"));

        Assert.Contains("supera el saldo pendiente", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegistrarAbono_VentaCancelada_LanzaInvalidOperationException()
    {
        // Arrange
        var order = await CrearPedidoPruebaAsync(100000m, estado: "cancelado");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.RegistrarAbonoAsync(order.Id, 50000m, "Efectivo", "Cajero 1"));

        Assert.Contains("cancelada", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegistrarAbono_AbonosCompletos_CambiaEstadoAPagado()
    {
        // Arrange
        var order = await CrearPedidoPruebaAsync(100000m);

        // Act 1: Primer abono parcial
        var abono1 = await _service.RegistrarAbonoAsync(order.Id, 40000m, "Efectivo", "Cajero 1");
        Assert.NotNull(abono1);
        Assert.StartsWith("REC-", abono1.NumeroComprobante);

        var orderAbonado = await _context.Orders.FindAsync(order.Id);
        Assert.NotNull(orderAbonado);
        Assert.Equal("abonado", orderAbonado.EstadoPago);
        Assert.Equal(40000m, orderAbonado.MontoPagado);
        Assert.Equal("pendiente", orderAbonado.Estado);

        // Act 2: Segundo abono que completa el total
        var abono2 = await _service.RegistrarAbonoAsync(order.Id, 60000m, "Transferencia", "Cajero 2");
        Assert.NotNull(abono2);

        // Assert final
        var orderPagado = await _context.Orders.FindAsync(order.Id);
        Assert.NotNull(orderPagado);
        Assert.Equal("pagado", orderPagado.EstadoPago);
        Assert.Equal("pagado", orderPagado.Estado);
        Assert.Equal(100000m, orderPagado.MontoPagado);
        Assert.Equal(0m, orderPagado.SaldoPendiente);
    }
}
