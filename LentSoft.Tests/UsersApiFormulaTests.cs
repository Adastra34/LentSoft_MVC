using System;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using LentSoft.Web.Controllers.Api;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Services;
using Xunit;

namespace LentSoft.Tests;

public class UsersApiFormulaTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LentSoftDbContext _context;
    private readonly IUserService _userService;
    private readonly IAuthService _authService;

    public UsersApiFormulaTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LentSoftDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new LentSoftDbContext(options);
        _context.Database.EnsureCreated();

        _userService = new UserService(_context);
        _authService = new AuthService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private UsersApiController CreateController(int userId = 0)
    {
        var controller = new UsersApiController(
            _userService,
            _authService,
            _context,
            NullLogger<UsersApiController>.Instance);

        var httpContext = new DefaultHttpContext();
        if (userId > 0)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, $"user{userId}@lentsoft.com")
            }, "TestAuth");
            httpContext.User = new ClaimsPrincipal(identity);
        }
        else
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        return controller;
    }

    [Fact]
    public async Task GetMyFormula_SinUsuarioAutenticado_RetornaUnauthorized()
    {
        // Arrange
        var controller = CreateController(userId: 0);

        // Act
        var result = await controller.GetMyFormula();

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetMyFormula_UsuarioSinFormula_RetornaHasFormulaFalse()
    {
        // Arrange
        var user = new User
        {
            Nombre = "Sin",
            Apellido = "Formula",
            TipoDocumento = "CC",
            NumeroDocumento = $"DOC_SF_{Guid.NewGuid():N}",
            Email = "sinformula@test.com",
            PasswordHash = "hash",
            Role = "usuario",
            Activo = true,
            FechaRegistro = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var controller = CreateController(user.Id);

        // Act
        var result = await controller.GetMyFormula();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("hasFormula", out var hasProp));
        Assert.False(hasProp.GetBoolean());
    }

    [Fact]
    public async Task GetMyFormula_UsuarioConFormulaActiva_RetornaDatosCompletosYFormateados()
    {
        // Arrange
        var optometra = new User
        {
            Nombre = "Valeria",
            Apellido = "Ríos",
            TipoDocumento = "CC",
            NumeroDocumento = $"DOC_OPT_{Guid.NewGuid():N}",
            Email = "valeria.optometra@test.com",
            PasswordHash = "hash",
            Role = "optometra",
            Activo = true,
            FechaRegistro = DateTime.UtcNow
        };
        _context.Users.Add(optometra);
        await _context.SaveChangesAsync();

        var user = new User
        {
            Nombre = "Camilo",
            Apellido = "Daza",
            TipoDocumento = "CC",
            NumeroDocumento = $"DOC_USR_{Guid.NewGuid():N}",
            Email = "camilo.daza@test.com",
            PasswordHash = "hash",
            Role = "usuario",
            Activo = true,
            FechaRegistro = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var formula = new FormulaOptica
        {
            UserId = user.Id,
            OptometraId = optometra.Id,
            Fecha = DateTime.UtcNow.AddMonths(-1),
            EsferaOD = "-1.75",
            CilindroOD = "-0.50",
            EjeOD = "180",
            AdicionOD = "+0.50",
            EsferaOI = "-2.00",
            CilindroOI = "-0.75",
            EjeOI = "175",
            AdicionOI = "+0.50",
            DistanciaPupilar = "63",
            TipoLente = "Monofocal",
            Observaciones = "Uso permanente frente a pantallas",
            Activo = true
        };
        _context.FormulasOpticas.Add(formula);
        await _context.SaveChangesAsync();

        var controller = CreateController(user.Id);

        // Act
        var result = await controller.GetMyFormula();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("hasFormula").GetBoolean());
        Assert.Equal("-1.75", root.GetProperty("esferaOD").GetString());
        Assert.Equal("-0.50", root.GetProperty("cilindroOD").GetString());
        Assert.Equal("180", root.GetProperty("ejeOD").GetString());
        Assert.Equal("-2.00", root.GetProperty("esferaOI").GetString());
        Assert.Equal("-0.75", root.GetProperty("cilindroOI").GetString());
        Assert.Equal("175", root.GetProperty("ejeOI").GetString());
        Assert.Equal("63 mm", root.GetProperty("distanciaPupilar").GetString());
        Assert.Equal("Dr(a). Valeria Ríos", root.GetProperty("optometraNombre").GetString());
        Assert.True(root.GetProperty("diasParaVencer").GetInt32() > 0);
    }

    [Fact]
    public async Task GetMyFormula_MultiplesFormulas_RetornaLaMasReciente()
    {
        // Arrange
        var user = new User
        {
            Nombre = "Elena",
            Apellido = "Gómez",
            TipoDocumento = "CC",
            NumeroDocumento = $"DOC_EL_{Guid.NewGuid():N}",
            Email = "elena.gomez@test.com",
            PasswordHash = "hash",
            Role = "usuario",
            Activo = true,
            FechaRegistro = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var formulaAntigua = new FormulaOptica
        {
            UserId = user.Id,
            OptometraId = 1,
            Fecha = DateTime.UtcNow.AddMonths(-6),
            EsferaOD = "-1.00",
            CilindroOD = "0.00",
            EjeOD = "0",
            EsferaOI = "-1.00",
            CilindroOI = "0.00",
            EjeOI = "0",
            DistanciaPupilar = "60 mm",
            TipoLente = "Monofocal",
            Activo = true
        };

        var formulaReciente = new FormulaOptica
        {
            UserId = user.Id,
            OptometraId = 1,
            Fecha = DateTime.UtcNow.AddDays(-5),
            EsferaOD = "-2.25",
            CilindroOD = "-0.50",
            EjeOD = "90",
            EsferaOI = "-2.50",
            CilindroOI = "-0.50",
            EjeOI = "85",
            DistanciaPupilar = "61 mm",
            TipoLente = "Monofocal",
            Activo = true
        };

        _context.FormulasOpticas.AddRange(formulaAntigua, formulaReciente);
        await _context.SaveChangesAsync();

        var controller = CreateController(user.Id);

        // Act
        var result = await controller.GetMyFormula();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("hasFormula").GetBoolean());
        Assert.Equal("-2.25", root.GetProperty("esferaOD").GetString());
        Assert.Equal("61 mm", root.GetProperty("distanciaPupilar").GetString());
    }

    [Fact]
    public async Task GetMyFormula_FormulaInactiva_RetornaHasFormulaFalse()
    {
        // Arrange
        var user = new User
        {
            Nombre = "Daniel",
            Apellido = "Ruiz",
            TipoDocumento = "CC",
            NumeroDocumento = $"DOC_DR_{Guid.NewGuid():N}",
            Email = "daniel.ruiz@test.com",
            PasswordHash = "hash",
            Role = "usuario",
            Activo = true,
            FechaRegistro = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var formulaInactiva = new FormulaOptica
        {
            UserId = user.Id,
            OptometraId = 1,
            Fecha = DateTime.UtcNow.AddDays(-1),
            EsferaOD = "-1.50",
            CilindroOD = "-0.50",
            EjeOD = "180",
            EsferaOI = "-1.50",
            CilindroOI = "-0.50",
            EjeOI = "180",
            DistanciaPupilar = "62",
            TipoLente = "Monofocal",
            Activo = false // Marcada inactiva
        };

        _context.FormulasOpticas.Add(formulaInactiva);
        await _context.SaveChangesAsync();

        var controller = CreateController(user.Id);

        // Act
        var result = await controller.GetMyFormula();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.GetProperty("hasFormula").GetBoolean());
    }

    [Fact]
    public void GetMyFormula_HeaderCacheControl_EsNoStore()
    {
        // Arrange
        var controller = CreateController(userId: 1);

        // Act
        _ = controller.GetMyFormula();

        // Assert
        Assert.Equal("no-store", controller.Response.Headers["Cache-Control"]);
    }
}
