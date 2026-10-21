using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Services;
using Xunit;

namespace LentSoft.Tests;

public class AuthServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LentSoftDbContext _context;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LentSoftDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new LentSoftDbContext(options);
        _context.Database.EnsureCreated();

        _authService = new AuthService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task LoginAsync_CredencialesCorrectas_RetornaUsuario()
    {
        // Arrange
        var rawPassword = "PasswordSeguro123*";
        var user = new User
        {
            Nombre = "Carlos",
            Apellido = "Pérez",
            TipoDocumento = "CC",
            NumeroDocumento = "5544332211",
            Email = "carlos.perez@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(rawPassword),
            Role = "usuario",
            Activo = true,
            FechaRegistro = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _authService.LoginAsync("carlos.perez@test.com", rawPassword);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("carlos.perez@test.com", result.Email);
    }

    [Fact]
    public async Task LoginAsync_PasswordIncorrecto_RetornaNull()
    {
        // Arrange
        var user = new User
        {
            Nombre = "Laura",
            Apellido = "Gómez",
            TipoDocumento = "CC",
            NumeroDocumento = "5544332212",
            Email = "laura.gomez@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!"),
            Role = "usuario",
            Activo = true,
            FechaRegistro = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _authService.LoginAsync("laura.gomez@test.com", "WrongPassword999!");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_UsuarioInactivo_RetornaNull()
    {
        // Arrange: Usuario con credenciales correctas pero inactivo
        var rawPassword = "PasswordValido123!";
        var user = new User
        {
            Nombre = "Inactivo",
            Apellido = "Prueba",
            TipoDocumento = "CC",
            NumeroDocumento = "5544332213",
            Email = "inactivo@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(rawPassword),
            Role = "usuario",
            Activo = false,
            FechaRegistro = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _authService.LoginAsync("inactivo@test.com", rawPassword);

        // Assert: Debe ser rechazado aunque la contraseña coincida
        Assert.Null(result);
    }

    [Fact]
    public async Task RegisterAsync_EmailDuplicado_RetornaNull()
    {
        // Arrange
        await _authService.RegisterAsync("Primero", "User", "CC", "77001122", "existente@test.com", "3001112233", "Pass123456*");

        // Act: Intento registrar con el mismo correo pero diferente mayúsculas/minúsculas
        var result = await _authService.RegisterAsync("Segundo", "User", "CC", "77001123", "EXISTENTE@TEST.COM", "3001112234", "Pass123456*");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RegisterAsync_DocumentoDuplicado_RetornaNull()
    {
        // Arrange
        await _authService.RegisterAsync("Persona", "Uno", "CC", "99887766", "persona1@test.com", "3001112235", "Pass123456*");

        // Act: Correo distinto pero mismo número de documento
        var result = await _authService.RegisterAsync("Persona", "Dos", "CC", "99887766", "persona2@test.com", "3001112236", "Pass123456*");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RegisterAsync_EmailSeNormalizaAMinusculas()
    {
        // Act
        var result = await _authService.RegisterAsync(
            "Elena", "Ramos", "CC", "66554433", "  Elena.Ramos@LENTSOFT.TEST  ", "3009998877", "ClaveSegura123!");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("elena.ramos@lentsoft.test", result.Email);

        // Verificar que el login funciona buscando en minúsculas
        var loginResult = await _authService.LoginAsync("elena.ramos@lentsoft.test", "ClaveSegura123!");
        Assert.NotNull(loginResult);
    }

    [Fact]
    public async Task RegisterAsync_RolAsignadoEsSiempreUsuario()
    {
        // Act
        var result = await _authService.RegisterAsync(
            "David", "Ruiz", "CC", "44332211", "david.ruiz@test.com", "3112223344", "Password123!");

        // Assert: El rol debe ser forzado a "usuario"
        Assert.NotNull(result);
        Assert.Equal("usuario", result.Role);
    }
}
