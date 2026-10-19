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
    private readonly PagoVentaService _pagoService;
    private readonly PasarelaPagoService _pasarelaService;
    private readonly User _testUser;

    // Tarjeta válida Luhn (Visa, termina en 7 -> Aprobada)
    private const string TarjetaValidaAprobada = "4532000000000017";
    // Tarjeta válida Luhn (Visa, termina en 9 -> Rechazada por simulación de fondos insuficientes)
    private const string TarjetaValidaRechazadaTerminadaEn9 = "4532000000000009";
    // Tarjeta que NO cumple el algoritmo de Luhn
    private const string TarjetaInvalidaLuhn = "4532000000000018";

    public PasarelaPagoServiceTests()
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
            Nombre = "Maria",
            Apellido = "Lopez",
            TipoDocumento = "CC",
            NumeroDocumento = "87654321",
            Email = "marialopez@lentsoft.test",
            PasswordHash = "hash123",
            Role = "usuario"
        };
        _context.Users.Add(_testUser);
        _context.SaveChanges();

        _pagoService = new PagoVentaService(_context);
        _pasarelaService = new PasarelaPagoService(_context, _pagoService);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public void ValidarAlgoritmoLuhn_LuhnValidoEInvalido()
    {
        // Act & Assert
        Assert.True(_pasarelaService.ValidarAlgoritmoLuhn(TarjetaValidaAprobada));
        Assert.True(_pasarelaService.ValidarAlgoritmoLuhn(TarjetaValidaRechazadaTerminadaEn9));
        Assert.False(_pasarelaService.ValidarAlgoritmoLuhn(TarjetaInvalidaLuhn));
        Assert.False(_pasarelaService.ValidarAlgoritmoLuhn("12345"));
    }

    [Fact]
    public async Task ProcesarPagoTarjeta_LuhnInvalido_RetornaRechazoPorLuhn()
    {
        // Arrange
        var dto = new TarjetaPagoDTO
        {
            NumeroTarjeta = TarjetaInvalidaLuhn,
            NombreTitular = "CARLOS PEREZ",
            FechaExpiracion = "12/29",
            Cvv = "123",
            Monto = 50000m
        };

        // Act
        var resultado = await _pasarelaService.ProcesarPagoTarjetaAsync(dto);

        // Assert
        Assert.False(resultado.Exitoso);
        Assert.Contains("Luhn", resultado.Mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcesarPagoTarjeta_LuhnValido_RetornaExitoso()
    {
        // Arrange
        var dto = new TarjetaPagoDTO
        {
            NumeroTarjeta = TarjetaValidaAprobada,
            NombreTitular = "CARLOS PEREZ",
            FechaExpiracion = "12/29",
            Cvv = "123",
            Monto = 50000m
        };

        // Act
        var resultado = await _pasarelaService.ProcesarPagoTarjetaAsync(dto);

        // Assert
        Assert.True(resultado.Exitoso);
        Assert.NotNull(resultado.Transaccion);
        Assert.Equal("Aprobada", resultado.Transaccion.Estado);
        Assert.Equal("Visa", resultado.Transaccion.MarcaTarjeta);
        Assert.Equal("0017", resultado.Transaccion.UltimosDigitosTarjeta);
        Assert.NotNull(resultado.Transaccion.CodigoAutorizacion);
    }

    [Fact]
    public async Task ProcesarPagoTarjeta_TarjetaTerminadaEn9_EsRechazadaPorSimulacion()
    {
        // Arrange
        var dto = new TarjetaPagoDTO
        {
            NumeroTarjeta = TarjetaValidaRechazadaTerminadaEn9,
            NombreTitular = "MARIA GOMEZ",
            FechaExpiracion = "10/30",
            Cvv = "456",
            Monto = 75000m
        };

        // Act
        var resultado = await _pasarelaService.ProcesarPagoTarjetaAsync(dto);

        // Assert
        Assert.False(resultado.Exitoso);
        Assert.NotNull(resultado.Transaccion);
        Assert.Equal("Rechazada", resultado.Transaccion.Estado);
        Assert.Contains("rechazada por el banco emisor", resultado.Mensaje, StringComparison.OrdinalIgnoreCase);
        Assert.Null(resultado.Transaccion.CodigoAutorizacion);
    }

    [Fact]
    public async Task ProcesarPagoTarjeta_TarjetaVencida_RetornaErrorDeVencimiento()
    {
        // Arrange (año pasado: 01/22)
        var dto = new TarjetaPagoDTO
        {
            NumeroTarjeta = TarjetaValidaAprobada,
            NombreTitular = "CARLOS PEREZ",
            FechaExpiracion = "01/22",
            Cvv = "123",
            Monto = 50000m
        };

        // Act
        var resultado = await _pasarelaService.ProcesarPagoTarjetaAsync(dto);

        // Assert
        Assert.False(resultado.Exitoso);
        Assert.Contains("vencida", resultado.Mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcesarPagoTarjeta_CvvInvalido_RetornaErrorDeCvv()
    {
        // Arrange (CVV con solo 2 dígitos o letras)
        var dto = new TarjetaPagoDTO
        {
            NumeroTarjeta = TarjetaValidaAprobada,
            NombreTitular = "CARLOS PEREZ",
            FechaExpiracion = "12/29",
            Cvv = "12",
            Monto = 50000m
        };

        // Act
        var resultado = await _pasarelaService.ProcesarPagoTarjetaAsync(dto);

        // Assert
        Assert.False(resultado.Exitoso);
        Assert.Contains("CVV inválido", resultado.Mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcesarPagoTarjeta_ConVentaId_MontoMayorAlSaldo_RetornaErrorAntesDeCobrar()
    {
        // Arrange
        var order = new Order
        {
            UserId = _testUser.Id,
            Total = 100000m,
            MontoPagado = 50000m,
            Estado = "pendiente",
            EstadoPago = "abonado",
            Activo = true,
            FechaPedido = DateTime.UtcNow
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var dto = new TarjetaPagoDTO
        {
            VentaId = order.Id,
            NumeroTarjeta = TarjetaValidaAprobada,
            NombreTitular = "CARLOS PEREZ",
            FechaExpiracion = "12/29",
            Cvv = "123",
            Monto = 60000m // Saldo pendiente = 50000
        };

        // Act
        var resultado = await _pasarelaService.ProcesarPagoTarjetaAsync(dto);

        // Assert
        Assert.False(resultado.Exitoso);
        Assert.Contains("supera el saldo pendiente", resultado.Mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcesarPagoTarjeta_ConVentaId_VentaCancelada_RetornaErrorAntesDeCobrar()
    {
        // Arrange
        var order = new Order
        {
            UserId = _testUser.Id,
            Total = 100000m,
            MontoPagado = 0m,
            Estado = "cancelado",
            EstadoPago = "cancelado",
            Activo = true,
            FechaPedido = DateTime.UtcNow
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var dto = new TarjetaPagoDTO
        {
            VentaId = order.Id,
            NumeroTarjeta = TarjetaValidaAprobada,
            NombreTitular = "CARLOS PEREZ",
            FechaExpiracion = "12/29",
            Cvv = "123",
            Monto = 50000m
        };

        // Act
        var resultado = await _pasarelaService.ProcesarPagoTarjetaAsync(dto);

        // Assert
        Assert.False(resultado.Exitoso);
        Assert.Contains("cancelada", resultado.Mensaje, StringComparison.OrdinalIgnoreCase);
    }
}
