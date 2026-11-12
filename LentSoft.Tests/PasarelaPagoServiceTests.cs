using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Services;
using Xunit;

namespace LentSoft.Tests;

public class PasarelaPagoServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LentSoftDbContext _context;
    private readonly PagoVentaService _pagoVentaService;
    private readonly PasarelaPagoService _service;

    public PasarelaPagoServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LentSoftDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new LentSoftDbContext(options);
        _context.Database.EnsureCreated();

        _pagoVentaService = new PagoVentaService(_context);
        _service = new PasarelaPagoService(_context, _pagoVentaService);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task ProcesarPagoTarjeta_LuhnValido_RetornaExitoso()
    {
        // Arrange: Tarjeta Visa válida con algoritmo de Luhn (no termina en 9)
        var dto = new TarjetaPagoDTO
        {
            NumeroTarjeta = "4532 0151 1283 0366",
            NombreTitular = "Carlos Santana",
            FechaExpiracion = "12/30",
            Cvv = "789",
            Monto = 50000m
        };

        // Act
        var result = await _service.ProcesarPagoTarjetaAsync(dto);

        // Assert
        Assert.True(result.Exitoso);
        Assert.NotNull(result.Transaccion);
        Assert.Equal("Aprobada", result.Transaccion.Estado);
        Assert.Equal("Visa", result.Transaccion.MarcaTarjeta);
        Assert.Equal("0366", result.Transaccion.UltimosDigitosTarjeta);
    }

    [Fact]
    public async Task ProcesarPagoTarjeta_LuhnInvalido_RetornaFallido()
    {
        // Arrange: Tarjeta con dígito de verificación corrupto
        var dto = new TarjetaPagoDTO
        {
            NumeroTarjeta = "4532 0151 1283 0367",
            NombreTitular = "Carlos Santana",
            FechaExpiracion = "12/30",
            Cvv = "789",
            Monto = 50000m
        };

        // Act
        var result = await _service.ProcesarPagoTarjetaAsync(dto);

        // Assert
        Assert.False(result.Exitoso);
        Assert.Contains("Luhn", result.Mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcesarPagoTarjeta_TarjetaTerminadaEn9_EsRechazadaPorSimulador()
    {
        // Arrange: Tarjeta con Luhn válido que termina en 9 (simulación de rechazo)
        var dto = new TarjetaPagoDTO
        {
            NumeroTarjeta = "4532 0151 1283 0499",
            NombreTitular = "Carlos Santana",
            FechaExpiracion = "12/30",
            Cvv = "789",
            Monto = 50000m
        };

        // Act
        var result = await _service.ProcesarPagoTarjetaAsync(dto);

        // Assert
        Assert.False(result.Exitoso);
        Assert.NotNull(result.Transaccion);
        Assert.Equal("Rechazada", result.Transaccion.Estado);
        Assert.Contains("rechazada", result.Mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcesarPagoTarjeta_TarjetaVencida_RetornaFallido()
    {
        // Arrange: Fecha en el pasado
        var dto = new TarjetaPagoDTO
        {
            NumeroTarjeta = "4532 0151 1283 0366",
            NombreTitular = "Carlos Santana",
            FechaExpiracion = "01/20",
            Cvv = "789",
            Monto = 50000m
        };

        // Act
        var result = await _service.ProcesarPagoTarjetaAsync(dto);

        // Assert
        Assert.False(result.Exitoso);
        Assert.Contains("vencida", result.Mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcesarPagoTarjeta_CvvInvalido_RetornaFallido()
    {
        // Arrange: CVV con formato inválido (2 dígitos o no numérico)
        var dto = new TarjetaPagoDTO
        {
            NumeroTarjeta = "4532 0151 1283 0366",
            NombreTitular = "Carlos Santana",
            FechaExpiracion = "12/30",
            Cvv = "1A",
            Monto = 50000m
        };

        // Act
        var result = await _service.ProcesarPagoTarjetaAsync(dto);

        // Assert
        Assert.False(result.Exitoso);
        Assert.Contains("CVV", result.Mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcesarPagoTarjeta_ConVenta_MontoMayorAlSaldo_RetornaFallido()
    {
        // Arrange: Pedido con total 100.000
        var user = new User
        {
            Nombre = "Maria",
            Apellido = "Lopez",
            Email = $"maria_{Guid.NewGuid():N}@test.com",
            PasswordHash = "hash123",
            Role = "usuario",
            Telefono = "3109998877"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var order = new Order
        {
            UserId = user.Id,
            Total = 100000m,
            Estado = "pendiente",
            EstadoPago = "pendiente",
            MontoPagado = 0m,
            Activo = true
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var dto = new TarjetaPagoDTO
        {
            VentaId = order.Id,
            NumeroTarjeta = "4532 0151 1283 0366",
            NombreTitular = "Maria Lopez",
            FechaExpiracion = "12/30",
            Cvv = "123",
            Monto = 150000m
        };

        // Act
        var result = await _service.ProcesarPagoTarjetaAsync(dto);

        // Assert
        Assert.False(result.Exitoso);
        Assert.Contains("saldo pendiente", result.Mensaje, StringComparison.OrdinalIgnoreCase);
    }
}
