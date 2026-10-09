using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using LentSoft.Web.Data;
using LentSoft.Web.Models;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Services;
using Xunit;

namespace LentSoft.Tests;

public class VentasBusinessValidationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LentSoftDbContext _context;
    private readonly PagoVentaService _pagoVentaService;
    private readonly InvoiceService _invoiceService;
    private readonly PasarelaPagoService _pasarelaService;

    public VentasBusinessValidationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LentSoftDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new LentSoftDbContext(options);
        _context.Database.EnsureCreated();

        _pagoVentaService = new PagoVentaService(_context, NullLogger<PagoVentaService>.Instance);

        var dianOptions = Options.Create(new DianSettings
        {
            ResolucionNumero = "18764028920000",
            RangoDesde = "FAC-2026-0001",
            RangoHasta = "FAC-2026-9999"
        });
        _invoiceService = new InvoiceService(_context, dianOptions, NullLogger<InvoiceService>.Instance);

        _pasarelaService = new PasarelaPagoService(_context, _pagoVentaService, NullLogger<PasarelaPagoService>.Instance);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task RegistrarAbono_AbonoMayorAlSaldoPendiente_LanzaInvalidOperationException()
    {
        // Arrange
        var user = new User { Nombre = "Cliente", Apellido = "Test", Email = "cliente@test.com", PasswordHash = "hash" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var order = new Order
        {
            UserId = user.Id,
            Total = 200000m,
            Estado = "pendiente",
            FechaPedido = DateTime.UtcNow,
            Activo = true
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Registrar un abono inicial de 150.000 (saldo pendiente = 50.000)
        await _pagoVentaService.RegistrarAbonoAsync(order.Id, 150000m, "Efectivo", "cajero1");

        // Act & Assert - Intentar abonar 60.000 (mayor a 50.000)
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _pagoVentaService.RegistrarAbonoAsync(order.Id, 60000m, "Efectivo", "cajero1"));

        Assert.Contains("supera el saldo pendiente", ex.Message);
    }

    [Fact]
    public async Task RegistrarAbono_OrdenCancelada_LanzaInvalidOperationException()
    {
        // Arrange
        var user = new User { Nombre = "Cliente", Apellido = "Cancelado", Email = "canc@test.com", PasswordHash = "hash" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var order = new Order
        {
            UserId = user.Id,
            Total = 100000m,
            Estado = "cancelado",
            FechaPedido = DateTime.UtcNow,
            Activo = true
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _pagoVentaService.RegistrarAbonoAsync(order.Id, 50000m, "Efectivo", "cajero1"));

        Assert.Contains("venta cancelada", ex.Message);
    }

    [Fact]
    public async Task CrearFactura_DobleFacturacion_LanzaInvalidOperationException()
    {
        // Arrange
        var user = new User { Nombre = "Cliente", Apellido = "Factura", Email = "fac@test.com", PasswordHash = "hash" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var order = new Order
        {
            UserId = user.Id,
            Total = 300000m,
            Estado = "entregado",
            FechaPedido = DateTime.UtcNow,
            Activo = true
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var factura1 = new Invoice
        {
            OrderId = order.Id,
            Total = 300000m,
            Estado = "pendiente"
        };
        await _invoiceService.CreateAsync(factura1);

        // Act & Assert - Intentar crear segunda factura para el mismo pedido
        var factura2 = new Invoice
        {
            OrderId = order.Id,
            Total = 300000m,
            Estado = "pendiente"
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _invoiceService.CreateAsync(factura2));

        Assert.Contains("ya cuenta con una factura activa", ex.Message);
    }

    [Fact]
    public async Task CrearFactura_CalculaEstadoParcial_CuandoHayAbonoIncompleto()
    {
        // Arrange
        var user = new User { Nombre = "Cliente", Apellido = "Parcial", Email = "parcial@test.com", PasswordHash = "hash" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var order = new Order
        {
            UserId = user.Id,
            Total = 500000m,
            Estado = "pendiente",
            FechaPedido = DateTime.UtcNow,
            Activo = true
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Registrar abono de 200.000
        await _pagoVentaService.RegistrarAbonoAsync(order.Id, 200000m, "Transferencia", "cajero");

        var factura = new Invoice
        {
            OrderId = order.Id,
            Total = 500000m,
            Estado = "pendiente"
        };

        // Act
        var creada = await _invoiceService.CreateAsync(factura);

        // Assert - Debe detectar abono y clasificar como 'parcial'
        Assert.Equal("parcial", creada.Estado.ToLower());
        Assert.StartsWith("FAC-", creada.NumeroFactura);
        Assert.False(string.IsNullOrEmpty(creada.CUFE));
    }

    [Fact]
    public async Task PasarelaTarjeta_NumeroInvalidoLuhn_RechazaTransaccion()
    {
        // Arrange
        var dto = new TarjetaPagoDTO
        {
            VentaId = 1,
            Monto = 50000m,
            NumeroTarjeta = "4111111111111112", // Falla checksum Luhn
            NombreTitular = "PEDRO PEREZ",
            FechaExpiracion = "12/28",
            Cvv = "123",
            ClaveIdempotencia = "test_luhn_fail"
        };

        // Act
        var resultado = await _pasarelaService.ProcesarPagoTarjetaAsync(dto);

        // Assert
        Assert.False(resultado.Exitoso);
        Assert.Contains("Luhn", resultado.Mensaje);
    }

    [Fact]
    public async Task PasarelaTarjeta_MismaClaveIdempotencia_RetornaTransaccionPreviaSinDuplicar()
    {
        // Arrange
        var user = new User { Nombre = "Cliente", Apellido = "Idem", Email = "idem@test.com", PasswordHash = "hash" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var order = new Order
        {
            UserId = user.Id,
            Total = 100000m,
            Estado = "pendiente",
            FechaPedido = DateTime.UtcNow,
            Activo = true
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var dto = new TarjetaPagoDTO
        {
            VentaId = order.Id,
            Monto = 50000m,
            NumeroTarjeta = "4532015112830366", // Tarjeta válida Visa sandbox
            NombreTitular = "ANA TORRES",
            FechaExpiracion = "12/29",
            Cvv = "456",
            ClaveIdempotencia = "idem_key_unique_123"
        };

        // Act 1
        var primerResultado = await _pasarelaService.ProcesarPagoTarjetaAsync(dto);

        // Act 2 - Reintentar con exactamente la misma clave de idempotencia
        var segundoResultado = await _pasarelaService.ProcesarPagoTarjetaAsync(dto);

        // Assert
        Assert.True(primerResultado.Exitoso);
        Assert.True(segundoResultado.Exitoso);
        Assert.Contains("previamente procesada", segundoResultado.Mensaje);

        // Verificar que solo se guardó 1 transacción en la base de datos
        var totalTransacciones = await _context.TransaccionesPagos
            .CountAsync(t => t.ClaveIdempotencia == "idem_key_unique_123");
        Assert.Equal(1, totalTransacciones);
    }
}
