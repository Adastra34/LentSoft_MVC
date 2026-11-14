using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using LentSoft.Web.Controllers;
using LentSoft.Web.Data;
using LentSoft.Web.Models.DTOs;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Models.ViewModels;
using LentSoft.Web.Services;
using Xunit;

namespace LentSoft.Tests;

public class OptometraModuloTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LentSoftDbContext _context;

    public OptometraModuloTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LentSoftDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new LentSoftDbContext(options);
        _context.Database.EnsureCreated();

        // EnsureCreated() ya siembra por defecto:
        // User Id = 1 (admin)
        // User Id = 2 (usuario / paciente)
        // User Id = 3 (optometra)
        // User Id = 4 (ventas)

        // Agregamos un segundo paciente (Id = 5) para pruebas comparativas
        if (!_context.Users.Any(u => u.Id == 5))
        {
            _context.Users.Add(new User
            {
                Id = 5,
                Nombre = "Segundo",
                Apellido = "Paciente",
                Email = "paciente2@test.com",
                Role = "usuario",
                TipoDocumento = "CC",
                NumeroDocumento = "1000000005",
                PasswordHash = "hash",
                FechaRegistro = DateTime.UtcNow,
                Activo = true
            });
            _context.SaveChanges();
        }
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private class FakeTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private sealed class FakeTokenService : ISaleConfirmationTokenService
    {
        public string GenerateToken(int saleId) => $"token-{saleId}";
        public int? ValidateToken(string token) => 1;
    }

    private sealed class FakeEmailService : IEmailService
    {
        public string? UltimoEmailEnviado { get; private set; }
        public string? UltimoEnlaceEnviado { get; private set; }

        public Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            UltimoEmailEnviado = toEmail;
            UltimoEnlaceEnviado = resetLink;
            return Task.CompletedTask;
        }

        public Task SendSaleConfirmationEmailAsync(string toEmail, Order order, string confirmLink) => Task.CompletedTask;
    }

    private sealed class FakePasswordResetTokenService : IPasswordResetTokenService
    {
        public string GenerateToken(int userId, string email, string passwordHash) => $"token-reset-{userId}";
        public int? ValidateToken(string token) => 1;
        public bool MatchesCurrentPassword(string token, string currentPasswordHash) => true;
    }

    private sealed class FakeLogger<T> : ILogger<T>
    {
        public List<string> LoggedErrors { get; } = new();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel >= LogLevel.Error)
            {
                LoggedErrors.Add(formatter(state, exception));
            }
        }
    }

    private IVentasService CreateVentasService()
    {
        var invSettings = Options.Create(new InventorySettings
        {
            LowStockThreshold = 5,
            MaxStock = 500
        });
        var inventoryService = new InventoryService(_context, invSettings, new HttpContextAccessor());
        var invoiceService = new InvoiceService(_context);
        return new VentasService(
            _context,
            invoiceService,
            new FakeTokenService(),
            new FakeEmailService(),
            NullLogger<VentasService>.Instance,
            inventoryService,
            NullLoggerFactory.Instance);
    }

    private OptometraController CreateController(
        int optometraId = 3, 
        IVentasService? ventasService = null, 
        bool isAjax = false,
        IPasswordResetTokenService? tokenService = null,
        IEmailService? emailService = null,
        ILogger<OptometraController>? logger = null)
    {
        var controller = new OptometraController(_context, null, ventasService, tokenService, emailService, logger);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, optometraId.ToString()),
            new Claim(ClaimTypes.Role, "optometra"),
            new Claim(ClaimTypes.Name, "María García")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };
        if (isAjax)
        {
            httpContext.Request.Headers["X-Requested-With"] = "XMLHttpRequest";
        }
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        controller.TempData = new TempDataDictionary(httpContext, new FakeTempDataProvider());
        return controller;
    }

    // ── (1a) CreateExamen permite fecha pasada ──
    [Fact]
    public async Task CreateExamen_FechaPasada_PermiteRegistroYGuarda()
    {
        // Arrange
        var controller = CreateController();
        var examenAyer = new ExamenVisual
        {
            UserId = 2,
            Fecha = DateTime.Today.AddDays(-5), // Fecha pasada válida
            TipoExamen = "Refracción General",
            Diagnostico = "Miopía Previa",
            Activo = true
        };

        // Act
        var result = await controller.CreateExamen(examenAyer) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName);
        Assert.Equal("examenes", result.RouteValues?["section"]);
        Assert.Equal("Examen visual registrado exitosamente.", controller.TempData["SuccessMessage"]?.ToString());

        var examenEnDb = await _context.ExamenesVisuales.FirstOrDefaultAsync(e => e.UserId == 2);
        Assert.NotNull(examenEnDb);
        Assert.Equal("Miopía Previa", examenEnDb.Diagnostico);
    }

    // ── (1b) CreateExamen rechaza fecha futura ──
    [Fact]
    public async Task CreateExamen_RechazaFechaFutura_EstableceErrorYNoGuarda()
    {
        // Arrange
        var controller = CreateController();
        var examenFuturo = new ExamenVisual
        {
            UserId = 2,
            Fecha = DateTime.Today.AddDays(2), // Fecha futura inválida
            TipoExamen = "Refracción General",
            Diagnostico = "Miopía Futura",
            Activo = true
        };

        // Act
        var result = await controller.CreateExamen(examenFuturo) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName);
        Assert.Equal("examenes", result.RouteValues?["section"]);
        Assert.Equal("La fecha del examen no puede ser una fecha futura.", controller.TempData["ErrorMessage"]?.ToString());

        var examenEnDb = await _context.ExamenesVisuales.FirstOrDefaultAsync(e => e.UserId == 2);
        Assert.Null(examenEnDb);
    }

    // ── (2) CreateExamen crea automáticamente una entrada en HistorialesClinicos con los 6 campos de refracción copiados ──
    [Fact]
    public async Task CreateExamen_FechaValida_CreaExamenYEntradaAutomaticaEnHistorialConSeisCamposRefraccion()
    {
        // Arrange
        var controller = CreateController();
        var nuevoExamen = new ExamenVisual
        {
            UserId = 2,
            Fecha = DateTime.Today,
            TipoExamen = "Examen Completo Refracción",
            Diagnostico = "Astigmatismo Miópico",
            Tratamiento = "Uso de lentes formulados",
            Observaciones = "Control en 12 meses",
            EsferaOD = "-1.75",
            CilindroOD = "-0.75",
            EjeOD = "180",
            EsferaOI = "-1.50",
            CilindroOI = "-0.50",
            EjeOI = "175",
            Activo = true
        };

        // Act
        var result = await controller.CreateExamen(nuevoExamen) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName);
        Assert.Equal("examenes", result.RouteValues?["section"]);
        Assert.Equal("Examen visual registrado exitosamente.", controller.TempData["SuccessMessage"]?.ToString());

        // Verificar persistencia del examen
        var examenEnDb = await _context.ExamenesVisuales.FirstOrDefaultAsync(e => e.UserId == 2);
        Assert.NotNull(examenEnDb);
        Assert.Equal("Astigmatismo Miópico", examenEnDb.Diagnostico);

        // Verificar creación automática en HistorialesClinicos con los 6 campos de refracción copiados
        var historialEnDb = await _context.HistorialesClinicos.FirstOrDefaultAsync(h => h.UserId == 2);
        Assert.NotNull(historialEnDb);
        Assert.Equal(nuevoExamen.UserId, historialEnDb.UserId);
        Assert.Equal(3, historialEnDb.OptometraId);
        Assert.Equal("-1.75", historialEnDb.EsferaOD);
        Assert.Equal("-0.75", historialEnDb.CilindroOD);
        Assert.Equal("180", historialEnDb.EjeOD);
        Assert.Equal("-1.50", historialEnDb.EsferaOI);
        Assert.Equal("-0.50", historialEnDb.CilindroOI);
        Assert.Equal("175", historialEnDb.EjeOI);
    }

    // ── (3a) EditExamen permite fecha pasada ──
    [Fact]
    public async Task EditExamen_FechaPasada_PermiteModificacionYGuarda()
    {
        // Arrange
        var controller = CreateController();
        var examenExistente = new ExamenVisual
        {
            UserId = 2,
            OptometraId = 3,
            Fecha = DateTime.Today,
            TipoExamen = "Control",
            Diagnostico = "Diagnóstico Inicial",
            Activo = true
        };
        _context.ExamenesVisuales.Add(examenExistente);
        await _context.SaveChangesAsync();

        var examenEditadoValido = new ExamenVisual
        {
            Id = examenExistente.Id,
            UserId = 2,
            Fecha = DateTime.Today.AddDays(-7), // Fecha pasada válida
            TipoExamen = "Control Modificado",
            Diagnostico = "Diagnóstico Histórico Modificado"
        };

        // Act
        var result = await controller.EditExamen(examenEditadoValido) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName);
        Assert.Equal("examenes", result.RouteValues?["section"]);
        Assert.Equal("Examen visual actualizado exitosamente.", controller.TempData["SuccessMessage"]?.ToString());

        var examenEnDb = await _context.ExamenesVisuales.FindAsync(examenExistente.Id);
        Assert.NotNull(examenEnDb);
        Assert.Equal("Diagnóstico Histórico Modificado", examenEnDb.Diagnostico);
    }

    // ── (3b) EditExamen rechaza fecha futura ──
    [Fact]
    public async Task EditExamen_RechazaFechaFutura_NoModificaRegistroYEstableceError()
    {
        // Arrange
        var controller = CreateController();
        var examenExistente = new ExamenVisual
        {
            UserId = 2,
            OptometraId = 3,
            Fecha = DateTime.Today,
            TipoExamen = "Control",
            Diagnostico = "Diagnóstico Inicial",
            Activo = true
        };
        _context.ExamenesVisuales.Add(examenExistente);
        await _context.SaveChangesAsync();

        var examenEditadoInvalido = new ExamenVisual
        {
            Id = examenExistente.Id,
            UserId = 2,
            Fecha = DateTime.Today.AddDays(3), // Fecha futura inválida
            TipoExamen = "Control Modificado",
            Diagnostico = "Diagnóstico Modificado"
        };

        // Act
        var result = await controller.EditExamen(examenEditadoInvalido) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName);
        Assert.Equal("examenes", result.RouteValues?["section"]);
        Assert.Equal("La fecha del examen no puede ser una fecha futura.", controller.TempData["ErrorMessage"]?.ToString());

        var examenEnDb = await _context.ExamenesVisuales.FindAsync(examenExistente.Id);
        Assert.NotNull(examenEnDb);
        Assert.Equal("Diagnóstico Inicial", examenEnDb.Diagnostico); // No se modificó
    }

    // ── (4) GetUltimoExamenPorPaciente devuelve el último examen del paciente correcto y encontrado = false si no hay ninguno ──
    [Fact]
    public async Task GetUltimoExamenPorPaciente_DevuelveUltimoExamenOEncontradoFalso()
    {
        // Arrange
        var controller = CreateController();

        // Paciente 2 tiene 2 exámenes en distintas fechas
        var examenAntiguo = new ExamenVisual
        {
            UserId = 2,
            OptometraId = 3,
            Fecha = DateTime.UtcNow.AddDays(-15),
            TipoExamen = "Examen Antiguo",
            EsferaOD = "-1.00",
            Activo = true
        };
        var examenReciente = new ExamenVisual
        {
            UserId = 2,
            OptometraId = 3,
            Fecha = DateTime.UtcNow.AddDays(-1),
            TipoExamen = "Examen Reciente",
            EsferaOD = "-2.50",
            Activo = true
        };
        _context.ExamenesVisuales.AddRange(examenAntiguo, examenReciente);
        await _context.SaveChangesAsync();

        // Act 1: Paciente 2 (con exámenes)
        var resultConExamen = await controller.GetUltimoExamenPorPaciente(2) as JsonResult;
        Assert.NotNull(resultConExamen);

        // Deserialización robusta del tipo anónimo mediante System.Text.Json
        var jsonConExamen = JsonSerializer.Serialize(resultConExamen.Value);
        using var docConExamen = JsonDocument.Parse(jsonConExamen);
        var rootConExamen = docConExamen.RootElement;

        Assert.True(rootConExamen.GetProperty("encontrado").GetBoolean());
        var examenProp = rootConExamen.GetProperty("examen");
        Assert.Equal(examenReciente.Id, examenProp.GetProperty("id").GetInt32());
        Assert.Equal("-2.50", examenProp.GetProperty("esferaOD").GetString());

        // Act 2: Paciente 5 (sin ningún examen)
        var resultSinExamen = await controller.GetUltimoExamenPorPaciente(5) as JsonResult;
        Assert.NotNull(resultSinExamen);

        var jsonSinExamen = JsonSerializer.Serialize(resultSinExamen.Value);
        using var docSinExamen = JsonDocument.Parse(jsonSinExamen);
        var rootSinExamen = docSinExamen.RootElement;

        Assert.False(rootSinExamen.GetProperty("encontrado").GetBoolean());
    }

    // ── (5) CreateFormula y EditFormula: caso válido y caso con paciente/modelo inválido ──
    [Fact]
    public async Task CreateFormula_CasoValido_GuardaEnBaseDeDatosYRedirige()
    {
        // Arrange
        var controller = CreateController();
        var formula = new FormulaOptica
        {
            UserId = 2,
            Fecha = DateTime.Today,
            TipoLente = "Monofocal Antirreflejo",
            EsferaOD = "-1.50",
            CilindroOD = "-0.50",
            EjeOD = "90",
            EsferaOI = "-1.25",
            CilindroOI = "-0.25",
            EjeOI = "85",
            Activo = true
        };

        // Act
        var result = await controller.CreateFormula(formula) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName);
        Assert.Equal("formulas", result.RouteValues?["section"]);
        Assert.Equal("Fórmula óptica creada exitosamente.", controller.TempData["SuccessMessage"]?.ToString());

        var formulaEnDb = await _context.FormulasOpticas.FirstOrDefaultAsync(f => f.UserId == 2);
        Assert.NotNull(formulaEnDb);
        Assert.Equal("Monofocal Antirreflejo", formulaEnDb.TipoLente);
    }

    [Fact]
    public async Task CreateFormula_ModeloInvalido_RechazaYEstableceError()
    {
        // Arrange
        var controller = CreateController();
        controller.ModelState.AddModelError("TipoLente", "El tipo de lente es obligatorio.");
        var formulaInvalida = new FormulaOptica
        {
            UserId = 2,
            Fecha = DateTime.Today
        };

        // Act
        var result = await controller.CreateFormula(formulaInvalida) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName);
        Assert.Equal("formulas", result.RouteValues?["section"]);
        Assert.Equal("El tipo de lente es obligatorio.", controller.TempData["ErrorMessage"]?.ToString());

        var formulaEnDb = await _context.FormulasOpticas.FirstOrDefaultAsync(f => f.UserId == 2);
        Assert.Null(formulaEnDb);
    }

    [Fact]
    public async Task EditFormula_CasoValido_ActualizaEnBaseDeDatos()
    {
        // Arrange
        var controller = CreateController();
        var formulaExistente = new FormulaOptica
        {
            UserId = 2,
            OptometraId = 3,
            Fecha = DateTime.Today,
            TipoLente = "Monofocal",
            EsferaOD = "-1.00",
            Activo = true
        };
        _context.FormulasOpticas.Add(formulaExistente);
        await _context.SaveChangesAsync();

        var edicion = new FormulaOptica
        {
            Id = formulaExistente.Id,
            UserId = 2,
            Fecha = DateTime.Today,
            TipoLente = "Progresivo Digital",
            EsferaOD = "-2.25",
            DistanciaPupilar = "62mm"
        };

        // Act
        var result = await controller.EditFormula(edicion) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName);
        Assert.Equal("formulas", result.RouteValues?["section"]);
        Assert.Equal("Fórmula óptica actualizada exitosamente.", controller.TempData["SuccessMessage"]?.ToString());

        var formulaEnDb = await _context.FormulasOpticas.FindAsync(formulaExistente.Id);
        Assert.NotNull(formulaEnDb);
        Assert.Equal("Progresivo Digital", formulaEnDb.TipoLente);
        Assert.Equal("-2.25", formulaEnDb.EsferaOD);
    }

    [Fact]
    public async Task EditFormula_IdInexistente_EstableceError()
    {
        // Arrange
        var controller = CreateController();
        var edicionInexistente = new FormulaOptica
        {
            Id = 99999,
            UserId = 2,
            Fecha = DateTime.Today,
            TipoLente = "Progresivo"
        };

        // Act
        var result = await controller.EditFormula(edicionInexistente) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName);
        Assert.Equal("formulas", result.RouteValues?["section"]);
        Assert.Equal("No se encontró el registro a editar.", controller.TempData["ErrorMessage"]?.ToString());
    }

    // ── (6) Acceso restringido por rol: OptometraController tiene [Authorize(Roles = "optometra")] ──
    [Fact]
    public void OptometraController_TieneAtributoAuthorizeConRolOptometra()
    {
        // Arrange & Act
        var authorizeAttr = typeof(OptometraController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .FirstOrDefault() as AuthorizeAttribute;

        // Assert
        Assert.NotNull(authorizeAttr);
        Assert.Equal("optometra", authorizeAttr.Roles);
    }

    // ── (7) Pruebas de generación de Pedido de Venta desde Fórmula Óptica ──

    [Fact]
    public async Task CrearPedidoDesdeFormula_FormulaInexistente_RetornaError()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.CrearPedidoDesdeFormula(formulaId: 99999, productId: 1, cantidad: 1) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName);
        Assert.Equal("formulas", result.RouteValues?["section"]);
        Assert.Equal("La fórmula óptica no fue encontrada o se encuentra inactiva.", controller.TempData["ErrorMessage"]?.ToString());
    }

    [Fact]
    public async Task CrearPedidoDesdeFormula_FormulaVencida_RetornaErrorClaro()
    {
        // Arrange
        var formulaVencida = new FormulaOptica
        {
            UserId = 2,
            OptometraId = 3,
            Fecha = DateTime.UtcNow.AddMonths(-15), // Más de 12 meses => Vencida
            TipoLente = "Monofocal",
            Activo = true
        };
        _context.FormulasOpticas.Add(formulaVencida);
        await _context.SaveChangesAsync();

        var controller = CreateController();

        // Act
        var result = await controller.CrearPedidoDesdeFormula(formulaId: formulaVencida.Id, productId: 1, cantidad: 1) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("La fórmula está vencida; no se puede generar el pedido.", controller.TempData["ErrorMessage"]?.ToString());
    }

    [Fact]
    public async Task CrearPedidoDesdeFormula_StockInsuficiente_RetornaMensajeStockInsuficiente()
    {
        // Arrange
        var producto = new Product
        {
            Nombre = "Montura Stock Limitado",
            Descripcion = "Prueba stock",
            Precio = 100000,
            Stock = 2,
            Categoria = "Monturas",
            Activo = true
        };
        _context.Products.Add(producto);

        var formula = new FormulaOptica
        {
            UserId = 2,
            OptometraId = 3,
            Fecha = DateTime.UtcNow,
            TipoLente = "Monofocal",
            Activo = true
        };
        _context.FormulasOpticas.Add(formula);
        await _context.SaveChangesAsync();

        var ventasService = CreateVentasService();
        var controller = CreateController(ventasService: ventasService);

        // Act (solicita cantidad 5 > stock 2)
        var result = await controller.CrearPedidoDesdeFormula(formulaId: formula.Id, productId: producto.Id, cantidad: 5, ventasService: ventasService) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        var errorMsg = controller.TempData["ErrorMessage"]?.ToString();
        Assert.NotNull(errorMsg);
        Assert.Contains("Stock insuficiente", errorMsg);
    }

    [Fact]
    public async Task CrearPedidoDesdeFormula_PedidoDuplicadoActivo_RetornaAvisoPedidoExistente()
    {
        // Arrange
        var formula = new FormulaOptica
        {
            UserId = 2,
            OptometraId = 3,
            Fecha = DateTime.UtcNow,
            TipoLente = "Monofocal",
            Activo = true
        };
        _context.FormulasOpticas.Add(formula);
        await _context.SaveChangesAsync();

        var pedidoExistente = new SalesOrder
        {
            NumeroPedido = "PV-2026-999",
            ClienteNombre = "Paciente Test",
            ProductId = 1,
            Cantidad = 1,
            PrecioUnitario = 100000,
            Total = 100000,
            Estado = "Pendiente",
            FormulaOpticaId = formula.Id,
            Activo = true,
            Fecha = DateTime.UtcNow
        };
        _context.SalesOrders.Add(pedidoExistente);
        await _context.SaveChangesAsync();

        var controller = CreateController();

        // Act
        var result = await controller.CrearPedidoDesdeFormula(formulaId: formula.Id, productId: 1, cantidad: 1) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        var warningMsg = controller.TempData["WarningMessage"]?.ToString();
        Assert.NotNull(warningMsg);
        Assert.Contains("Ya existe un pedido para esta fórmula", warningMsg);
        Assert.Contains("PV-2026-999", warningMsg);
    }

    [Fact]
    public async Task CrearPedidoDesdeFormula_Exito_CreaPedidoVinculadoConEstadoPendienteYNotasFormateadas()
    {
        // Arrange
        var producto = new Product
        {
            Nombre = "Lente Antirreflejo Test",
            Descripcion = "Lente de prueba",
            Precio = 120000,
            Stock = 10,
            Categoria = "Lentes",
            Activo = true
        };
        _context.Products.Add(producto);

        var formula = new FormulaOptica
        {
            UserId = 2,
            OptometraId = 3,
            Fecha = DateTime.UtcNow,
            TipoLente = "Monofocal",
            Activo = true
        };
        _context.FormulasOpticas.Add(formula);
        await _context.SaveChangesAsync();

        var ventasService = CreateVentasService();
        var controller = CreateController(ventasService: ventasService, isAjax: true);

        // Act
        var result = await controller.CrearPedidoDesdeFormula(
            formulaId: formula.Id,
            productId: producto.Id,
            cantidad: 2,
            notas: "Filtro azul solicitado por el paciente",
            ventasService: ventasService) as JsonResult;

        // Assert
        Assert.NotNull(result);
        var jsonVal = result.Value;
        var successProp = jsonVal?.GetType().GetProperty("success")?.GetValue(jsonVal) as bool?;
        Assert.True(successProp);

        var pedidoDb = await _context.SalesOrders
            .FirstOrDefaultAsync(o => o.FormulaOpticaId == formula.Id && o.Activo);

        Assert.NotNull(pedidoDb);
        Assert.Equal("Pendiente", pedidoDb.Estado);
        Assert.Equal(formula.Id, pedidoDb.FormulaOpticaId);
        Assert.Equal(2, pedidoDb.Cantidad);
        Assert.Contains($"Fórmula #FOR-{formula.Id:D4}", pedidoDb.Notas);
        Assert.Contains("Filtro azul solicitado por el paciente", pedidoDb.Notas);

        // El stock del producto debe haberse descontado en 2
        var prodDb = await _context.Products.FindAsync(producto.Id);
        Assert.NotNull(prodDb);
        Assert.Equal(8, prodDb.Stock);
    }

    [Fact]
    public async Task CrearPedidoVenta_FlujoNormalSinFormula_CreaPedidoConFormulaOpticaIdNulo()
    {
        // Arrange
        var producto = new Product
        {
            Nombre = "Gafas de Sol Clásicas",
            Descripcion = "Gafas de sol",
            Precio = 80000,
            Stock = 5,
            Categoria = "Gafas",
            Activo = true
        };
        _context.Products.Add(producto);
        await _context.SaveChangesAsync();

        var ventasService = CreateVentasService();
        var dto = new CrearPedidoVentaDTO
        {
            ClienteNombre = "Cliente Mostrador",
            ProductId = producto.Id,
            Cantidad = 1,
            Estado = "Pendiente",
            Notas = "Pedido desde portal de ventas"
        };

        // Act
        var resultado = await ventasService.CrearPedidoVentaAsync(dto, "Vendedor");

        // Assert
        Assert.True(resultado.Exito);
        var pedido = await _context.SalesOrders.FindAsync(resultado.PedidoId);
        Assert.NotNull(pedido);
        Assert.Null(pedido.FormulaOpticaId);
    }

    // ── TANDA 1: Línea de Tiempo del Paciente ──
    [Fact]
    public async Task HistorialCompleto_RedirigeAIndexConSeccionHistorialYPacienteTimelineId()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.HistorialCompleto(2) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName);
        Assert.Equal("historial", result.RouteValues?["section"]);
        Assert.Equal(2, result.RouteValues?["pacienteTimelineId"]);
    }

    [Fact]
    public async Task Index_ConPacienteTimelineId_CargaLineaDeTiempoOrdenadaDescendentePorFecha()
    {
        // Arrange
        var controller = CreateController();

        var fechaAntigua = DateTime.UtcNow.AddDays(-10);
        var fechaMedia = DateTime.UtcNow.AddDays(-5);
        var fechaReciente = DateTime.UtcNow.AddDays(-1);

        var historial = new HistorialClinico
        {
            UserId = 2,
            OptometraId = 3,
            Fecha = fechaAntigua,
            Diagnostico = "Miopía inicial",
            Observaciones = "Control primer semestre",
            Activo = true
        };

        var formula = new FormulaOptica
        {
            UserId = 2,
            OptometraId = 3,
            Fecha = fechaMedia,
            TipoLente = "Monofocal antireflejo",
            Observaciones = "Uso permanente",
            Activo = true
        };

        var examen = new ExamenVisual
        {
            UserId = 2,
            OptometraId = 3,
            Fecha = fechaReciente,
            TipoExamen = "Control de agudeza visual",
            Resultado = "Estable con lentes",
            Activo = true
        };

        _context.HistorialesClinicos.Add(historial);
        _context.FormulasOpticas.Add(formula);
        _context.ExamenesVisuales.Add(examen);
        await _context.SaveChangesAsync();

        // Act
        var result = await controller.Index(section: "historial", pacienteTimelineId: 2) as ViewResult;

        // Assert
        Assert.NotNull(result);
        var model = Assert.IsType<DashboardOptometraViewModel>(result.Model);
        Assert.Equal(2, model.PacienteTimelineId);
        Assert.NotNull(model.PacienteTimeline);
        Assert.Equal(2, model.PacienteTimeline.Id);

        Assert.Equal(3, model.LineaDeTiempo.Count);
        // Debe estar ordenado descendentemente por Fecha: más reciente primero
        Assert.Equal("Examen", model.LineaDeTiempo[0].TipoRegistro);
        Assert.Equal("Control de agudeza visual", model.LineaDeTiempo[0].Descripcion);

        Assert.Equal("Formula", model.LineaDeTiempo[1].TipoRegistro);
        Assert.Equal("Monofocal antireflejo", model.LineaDeTiempo[1].Descripcion);

        Assert.Equal("Historial", model.LineaDeTiempo[2].TipoRegistro);
        Assert.Equal("Miopía inicial", model.LineaDeTiempo[2].Descripcion);

        Assert.True(model.LineaDeTiempo[0].Fecha >= model.LineaDeTiempo[1].Fecha);
        Assert.True(model.LineaDeTiempo[1].Fecha >= model.LineaDeTiempo[2].Fecha);
    }

    // ── TANDA 1: CreatePaciente con Contraseña Aleatoria Segura y Enlace de Definición ──
    [Fact]
    public async Task CreatePaciente_GeneraContrasenaAleatoriaSegura_HashNoEsUser123YEnviaEmail()
    {
        // Arrange
        var fakeEmail = new FakeEmailService();
        var fakeToken = new FakePasswordResetTokenService();
        var controller = CreateController(tokenService: fakeToken, emailService: fakeEmail);

        var nuevoPaciente = new PacienteFormViewModel
        {
            Nombre = "Andrés",
            Apellido = "Mejía",
            Email = "andres.mejia@test.com",
            NumeroDocumento = "1099887766",
            Telefono = "3159988776",
            TipoDocumento = "CC"
        };

        // Act
        var result = await controller.CreatePaciente(nuevoPaciente) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName);
        Assert.Equal("pacientes", result.RouteValues?["section"]);

        var pacienteEnDb = await _context.Users.FirstOrDefaultAsync(u => u.Email == "andres.mejia@test.com");
        Assert.NotNull(pacienteEnDb);

        // 1. Verificar que el hash NO corresponde a la contraseña fija "user123"
        Assert.False(BCrypt.Net.BCrypt.Verify("user123", pacienteEnDb.PasswordHash));

        // 2. Verificar que es un hash válido de BCrypt
        Assert.StartsWith("$2", pacienteEnDb.PasswordHash);

        // 3. Verificar que se envió el correo con el enlace de definición de contraseña
        Assert.Equal("andres.mejia@test.com", fakeEmail.UltimoEmailEnviado);
        Assert.NotNull(fakeEmail.UltimoEnlaceEnviado);
        Assert.Contains("token-reset-", fakeEmail.UltimoEnlaceEnviado);
        Assert.Contains("ResetPassword", fakeEmail.UltimoEnlaceEnviado);
    }

    [Fact]
    public async Task EditPaciente_CorreoDuplicadoEnOtroPaciente_RechazaYNoModifica()
    {
        // Arrange
        var controller = CreateController();
        var model = new PacienteFormViewModel
        {
            Id = 2,
            Nombre = "Usuario",
            Apellido = "Demo",
            Email = "paciente2@test.com", // pertenece al usuario 5
            NumeroDocumento = "1000000002",
            Telefono = "3000000000"
        };

        // Act
        var result = await controller.EditPaciente(model) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName);
        Assert.Equal("pacientes", result.RouteValues?["section"]);
        Assert.Equal("El correo ya está registrado.", controller.TempData["ErrorMessage"]);

        var pacienteDb = await _context.Users.FindAsync(2);
        Assert.NotNull(pacienteDb);
        Assert.Equal("user@lentsoft.com", pacienteDb.Email);
    }

    [Fact]
    public async Task EditPaciente_DocumentoDuplicadoEnOtroPaciente_RechazaYNoModifica()
    {
        // Arrange
        var controller = CreateController();
        var model = new PacienteFormViewModel
        {
            Id = 2,
            Nombre = "Usuario",
            Apellido = "Demo",
            Email = "user@lentsoft.com",
            NumeroDocumento = "1000000005", // pertenece al usuario 5
            Telefono = "3000000000"
        };

        // Act
        var result = await controller.EditPaciente(model) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName);
        Assert.Equal("pacientes", result.RouteValues?["section"]);
        Assert.Equal("El número de documento ya está registrado.", controller.TempData["ErrorMessage"]);

        var pacienteDb = await _context.Users.FindAsync(2);
        Assert.NotNull(pacienteDb);
        Assert.Equal("1000000002", pacienteDb.NumeroDocumento);
    }

    [Fact]
    public async Task EditPaciente_MismoCorreoYDocumentoDelPropioPaciente_PermiteActualizacion()
    {
        // Arrange
        var controller = CreateController();
        var model = new PacienteFormViewModel
        {
            Id = 2,
            Nombre = "Usuario",
            Apellido = "Modificado",
            Email = "user@lentsoft.com", // su propio correo
            NumeroDocumento = "1000000002", // su propio documento
            Telefono = "3119998888"
        };

        // Act
        var result = await controller.EditPaciente(model) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName);
        Assert.Equal("pacientes", result.RouteValues?["section"]);
        Assert.Equal("Datos del paciente actualizados exitosamente.", controller.TempData["SuccessMessage"]);

        var pacienteDb = await _context.Users.FindAsync(2);
        Assert.NotNull(pacienteDb);
        Assert.Equal("Modificado", pacienteDb.Apellido);
        Assert.Equal("3119998888", pacienteDb.Telefono);
    }

    [Fact]
    public async Task UpdateAppointmentStatus_EstadoAtendida_RechazaPorqueNoEsEstadoValido()
    {
        // Arrange
        var controller = CreateController();

        // Act - Intentar cambiar estado a "atendida" (no existe en Appointment.EstadosValidos)
        var result = await controller.UpdateAppointmentStatus(1, "atendida") as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName);
        Assert.Equal("citas", result.RouteValues?["section"]);
        Assert.Equal("Estado no válido. Los valores permitidos son: pendiente, confirmada, completada, cancelada.", controller.TempData["ErrorMessage"]);

        var citaDb = await _context.Appointments.FindAsync(1);
        Assert.NotNull(citaDb);
        Assert.NotEqual("atendida", citaDb.Estado);
    }

    [Fact]
    public async Task DeleteAppointment_ExcepcionEnBaseDeDatos_MuestraMensajeGenericoYRegistraEnLogger()
    {
        // Arrange
        var fakeLogger = new FakeLogger<OptometraController>();
        var controller = CreateController(logger: fakeLogger);

        // Cerramos la conexión para forzar una excepción en SaveChangesAsync
        _connection.Close();

        // Act
        var result = await controller.DeleteAppointment(1) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName);
        Assert.Equal("citas", result.RouteValues?["section"]);

        // El mensaje debe ser genérico y amigable, NO contener detalles técnicos ni 'ex.Message'
        var errorMsg = controller.TempData["ErrorMessage"]?.ToString();
        Assert.NotNull(errorMsg);
        Assert.Equal("Error al eliminar la cita.", errorMsg);
        Assert.DoesNotContain("SQLite", errorMsg);
        Assert.DoesNotContain("Exception", errorMsg);

        // El logger sí debe registrar el error técnico
        Assert.NotEmpty(fakeLogger.LoggedErrors);
        Assert.Contains(fakeLogger.LoggedErrors, msg => msg.Contains("Error al eliminar la cita"));
    }

    [Fact]
    public async Task CreateAppointment_FechaPasada_RechazaYNoGuarda()
    {
        // Arrange
        var controller = CreateController();
        var model = new AppointmentFormViewModel
        {
            UserId = 2,
            Servicio = "Examen visual completo",
            FechaHora = DateTime.Today.AddDays(-1).AddHours(10),
            Estado = "pendiente"
        };

        // Act
        var result = await controller.CreateAppointment(model) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName);
        Assert.Equal("citas", result.RouteValues?["section"]);
        Assert.Equal("La fecha y hora de la cita debe ser futura.", controller.TempData["ErrorMessage"]);

        var citaDb = await _context.Appointments.FirstOrDefaultAsync(a => a.UserId == 2 && a.FechaHora == DateTime.SpecifyKind(model.FechaHora, DateTimeKind.Utc));
        Assert.Null(citaDb);
    }

    [Fact]
    public async Task CreateAppointment_FechaFuturaHorarioLaboral_GuardaExitosamente()
    {
        // Arrange
        var controller = CreateController();

        var fechaFutura = DateTime.Today.AddDays(7);
        while (fechaFutura.DayOfWeek == DayOfWeek.Sunday)
        {
            fechaFutura = fechaFutura.AddDays(1);
        }
        fechaFutura = fechaFutura.Date.AddHours(10);

        var model = new AppointmentFormViewModel
        {
            UserId = 2,
            Servicio = "Examen de control",
            FechaHora = fechaFutura,
            Notas = "Cita de prueba futura",
            Estado = "pendiente"
        };

        // Act
        var result = await controller.CreateAppointment(model) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName);
        Assert.Equal("citas", result.RouteValues?["section"]);
        Assert.Equal("Cita programada exitosamente.", controller.TempData["SuccessMessage"]);

        var citaDb = await _context.Appointments.FirstOrDefaultAsync(a => a.UserId == 2 && a.Servicio == "Examen de control");
        Assert.NotNull(citaDb);
        Assert.Equal("pendiente", citaDb.Estado);
    }

    [Fact]
    public async Task CreateFormula_ConAdicionODyOI_GuardaCorrectamente()
    {
        // Arrange
        var controller = CreateController();
        var formula = new FormulaOptica
        {
            UserId = 2,
            Fecha = DateTime.Today,
            TipoLente = "Progresivo Digital",
            EsferaOD = "-1.50",
            CilindroOD = "-0.50",
            EjeOD = "90",
            EsferaOI = "-1.25",
            CilindroOI = "-0.25",
            EjeOI = "85",
            AdicionOD = "+1.50",
            AdicionOI = "+1.75",
            Observaciones = "Adición para visión cercana",
            Activo = true
        };

        // Act
        var result = await controller.CreateFormula(formula) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName);
        Assert.Equal("formulas", result.RouteValues?["section"]);
        Assert.Equal("Fórmula óptica creada exitosamente.", controller.TempData["SuccessMessage"]);

        var formulaEnDb = await _context.FormulasOpticas.FirstOrDefaultAsync(f => f.UserId == 2 && f.TipoLente == "Progresivo Digital");
        Assert.NotNull(formulaEnDb);
        Assert.Equal("+1.50", formulaEnDb.AdicionOD);
        Assert.Equal("+1.75", formulaEnDb.AdicionOI);
    }

    [Fact]
    public async Task EditFormula_ActualizaAdicionODyOI_PersisteCambios()
    {
        // Arrange
        var controller = CreateController();
        var formulaExistente = new FormulaOptica
        {
            UserId = 2,
            OptometraId = 3,
            Fecha = DateTime.Today,
            TipoLente = "Bifocal",
            EsferaOD = "-1.00",
            CilindroOD = "0.00",
            EjeOD = "0",
            EsferaOI = "-1.00",
            CilindroOI = "0.00",
            EjeOI = "0",
            AdicionOD = "+1.00",
            AdicionOI = "+1.00",
            Activo = true
        };
        _context.FormulasOpticas.Add(formulaExistente);
        await _context.SaveChangesAsync();

        var edicion = new FormulaOptica
        {
            Id = formulaExistente.Id,
            UserId = 2,
            Fecha = DateTime.Today,
            TipoLente = "Bifocal Ejecutivo",
            EsferaOD = "-1.25",
            CilindroOD = "-0.25",
            EjeOD = "10",
            EsferaOI = "-1.25",
            CilindroOI = "-0.25",
            EjeOI = "15",
            AdicionOD = "+2.00",
            AdicionOI = "+2.25",
            DistanciaPupilar = "64mm"
        };

        // Act
        var result = await controller.EditFormula(edicion) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName);
        Assert.Equal("formulas", result.RouteValues?["section"]);
        Assert.Equal("Fórmula óptica actualizada exitosamente.", controller.TempData["SuccessMessage"]);

        var formulaEnDb = await _context.FormulasOpticas.FindAsync(formulaExistente.Id);
        Assert.NotNull(formulaEnDb);
        Assert.Equal("+2.00", formulaEnDb.AdicionOD);
        Assert.Equal("+2.25", formulaEnDb.AdicionOI);
        Assert.Equal("Bifocal Ejecutivo", formulaEnDb.TipoLente);
    }

    [Fact]
    public void PdfRecetaService_GeneraRecetaConAdicion_RetornaPdfValido()
    {
        // Arrange
        var service = new PdfRecetaService();
        var formula = new FormulaOptica
        {
            Id = 42,
            UserId = 2,
            OptometraId = 3,
            Fecha = DateTime.Today,
            TipoLente = "Progresivo Antirreflejo",
            EsferaOD = "-2.00",
            CilindroOD = "-0.50",
            EjeOD = "180",
            AdicionOD = "+1.75",
            EsferaOI = "-1.75",
            CilindroOI = "-0.75",
            EjeOI = "170",
            AdicionOI = "+1.75",
            DistanciaPupilar = "63",
            Observaciones = "Uso permanente con filtro azul",
            User = new User
            {
                Id = 2,
                Nombre = "Carlos",
                Apellido = "Prueba",
                TipoDocumento = "CC",
                NumeroDocumento = "100200300"
            },
            Optometra = new User
            {
                Id = 3,
                Nombre = "Dra. Laura",
                Apellido = "Optómetra",
                RegistroMedico = "RM-998877"
            }
        };

        // Act
        var pdfBytes = service.GenerateRecetaPdf(formula);

        // Assert
        Assert.NotNull(pdfBytes);
        Assert.NotEmpty(pdfBytes);
        Assert.True(pdfBytes.Length > 4);
        Assert.Equal((byte)'%', pdfBytes[0]);
        Assert.Equal((byte)'P', pdfBytes[1]);
        Assert.Equal((byte)'D', pdfBytes[2]);
        Assert.Equal((byte)'F', pdfBytes[3]);
    }

    [Fact]
    public async Task DeletePaciente_CasoValido_DesactivaLogicamentePaciente()
    {
        // Arrange
        var controller = CreateController();

        // Act - Eliminar paciente 5
        var result = await controller.DeletePaciente(5) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName);
        Assert.Equal("pacientes", result.RouteValues?["section"]);
        Assert.Equal("Paciente desactivado (Inactivo) exitosamente.", controller.TempData["SuccessMessage"]);

        var pacienteDb = await _context.Users.FindAsync(5);
        Assert.NotNull(pacienteDb);
        Assert.False(pacienteDb.Activo);
        Assert.Equal("Inactivo", pacienteDb.EstadoPaciente);
    }

    [Fact]
    public async Task CreateHistorial_CasoValido_GuardaDiagnosticoYTratamiento()
    {
        // Arrange
        var controller = CreateController();
        var nuevoHistorial = new HistorialClinico
        {
            UserId = 2,
            Fecha = DateTime.Today,
            Diagnostico = "Miopía leve y astigmatismo",
            Tratamiento = "Uso de lentes formulados permanentes",
            Antecedentes = "Madre con glaucoma",
            ExamenesRealizados = "Agudeza visual y tonometría",
            Observaciones = "Control en 12 meses",
            EsferaOD = "-1.50",
            CilindroOD = "-0.50",
            EjeOD = "90",
            EsferaOI = "-1.25",
            CilindroOI = "-0.25",
            EjeOI = "85",
            Activo = true
        };

        // Act
        var result = await controller.CreateHistorial(nuevoHistorial) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName);
        Assert.Equal("historial", result.RouteValues?["section"]);
        Assert.Equal("Historial clínico creado exitosamente.", controller.TempData["SuccessMessage"]);

        var enDb = await _context.HistorialesClinicos.FirstOrDefaultAsync(h => h.UserId == 2 && h.Diagnostico == "Miopía leve y astigmatismo");
        Assert.NotNull(enDb);
        Assert.Equal("Uso de lentes formulados permanentes", enDb.Tratamiento);
        Assert.Equal("-1.50", enDb.EsferaOD);
    }

    [Fact]
    public async Task DeleteHistorial_CasoValido_DesactivaLogicamenteHistorial()
    {
        // Arrange
        var controller = CreateController();
        var historial = new HistorialClinico
        {
            UserId = 2,
            OptometraId = 3,
            Fecha = DateTime.Today,
            Diagnostico = "Para eliminar",
            Tratamiento = "Ninguno",
            Activo = true
        };
        _context.HistorialesClinicos.Add(historial);
        await _context.SaveChangesAsync();

        // Act
        var result = await controller.DeleteHistorial(historial.Id) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName);
        Assert.Equal("historial", result.RouteValues?["section"]);
        Assert.Equal("Historial clínico eliminado exitosamente.", controller.TempData["SuccessMessage"]);

        var enDb = await _context.HistorialesClinicos.FindAsync(historial.Id);
        Assert.NotNull(enDb);
        Assert.False(enDb.Activo);
    }

    [Fact]
    public async Task CreateAppointment_CruceConOtraCitaMismoOptometraYHorario_RechazaPorDisponibilidad()
    {
        // Arrange
        var controller = CreateController(optometraId: 3);

        var fechaFutura = DateTime.Today.AddDays(7);
        while (fechaFutura.DayOfWeek == DayOfWeek.Sunday) fechaFutura = fechaFutura.AddDays(1);
        fechaFutura = fechaFutura.Date.AddHours(11);

        var citaExistente = new Appointment
        {
            UserId = 2,
            OptometraId = 3,
            Servicio = "Examen de vista",
            FechaHora = DateTime.SpecifyKind(fechaFutura, DateTimeKind.Utc),
            Estado = "confirmada",
            Activo = true
        };
        _context.Appointments.Add(citaExistente);
        await _context.SaveChangesAsync();

        var modelCruce = new AppointmentFormViewModel
        {
            UserId = 5,
            Servicio = "Control visual",
            FechaHora = fechaFutura,
            Estado = "pendiente"
        };
        var result = await controller.CreateAppointment(modelCruce) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName);
        Assert.Equal("citas", result.RouteValues?["section"]);
        Assert.Equal("El optómetra ya tiene una cita agendada en ese horario. Por favor elige otro horario.", controller.TempData["ErrorMessage"]);

        var citasCount = await _context.Appointments.CountAsync(a => a.OptometraId == 3 && a.FechaHora == DateTime.SpecifyKind(fechaFutura, DateTimeKind.Utc) && a.Activo);
        Assert.Equal(1, citasCount);
    }

    [Fact]
    public async Task DeleteAppointment_PermiteReutilizarHorarioParaNuevaCita()
    {
        // Arrange
        var controller = CreateController(optometraId: 3);

        var fechaFutura = DateTime.Today.AddDays(8);
        while (fechaFutura.DayOfWeek == DayOfWeek.Sunday) fechaFutura = fechaFutura.AddDays(1);
        fechaFutura = fechaFutura.Date.AddHours(14);

        var citaInicial = new Appointment
        {
            UserId = 2,
            OptometraId = 3,
            Servicio = "Cita a cancelar",
            FechaHora = DateTime.SpecifyKind(fechaFutura, DateTimeKind.Utc),
            Estado = "confirmada",
            Activo = true
        };
        _context.Appointments.Add(citaInicial);
        await _context.SaveChangesAsync();

        // 1. Eliminar cita existente
        var resultDelete = await controller.DeleteAppointment(citaInicial.Id) as RedirectToActionResult;
        Assert.NotNull(resultDelete);
        Assert.Equal("Cita eliminada exitosamente.", controller.TempData["SuccessMessage"]);

        // 2. Agendar nueva cita en el horario que quedó liberado
        var nuevaCitaModel = new AppointmentFormViewModel
        {
            UserId = 5,
            Servicio = "Nueva cita reutilizando horario",
            FechaHora = fechaFutura,
            Estado = "pendiente"
        };
        var resultCreate = await controller.CreateAppointment(nuevaCitaModel) as RedirectToActionResult;

        // Assert
        Assert.NotNull(resultCreate);
        Assert.Equal("Cita programada exitosamente.", controller.TempData["SuccessMessage"]);

        var nuevaCitaEnDb = await _context.Appointments.FirstOrDefaultAsync(a => a.UserId == 5 && a.FechaHora == DateTime.SpecifyKind(fechaFutura, DateTimeKind.Utc) && a.Activo);
        Assert.NotNull(nuevaCitaEnDb);
        Assert.Equal("Nueva cita reutilizando horario", nuevaCitaEnDb.Servicio);
    }

    [Fact]
    public async Task EditAppointment_ReprogramarACitaOcupada_RechazaPorCruce()
    {
        // Arrange
        var controller = CreateController(optometraId: 3);

        var fechaBase = DateTime.Today.AddDays(9);
        while (fechaBase.DayOfWeek == DayOfWeek.Sunday) fechaBase = fechaBase.AddDays(1);
        var hora1 = fechaBase.Date.AddHours(10);
        var hora2 = fechaBase.Date.AddHours(11);

        var cita1 = new Appointment
        {
            UserId = 2,
            OptometraId = 3,
            Servicio = "Cita 1",
            FechaHora = DateTime.SpecifyKind(hora1, DateTimeKind.Utc),
            Estado = "confirmada",
            Activo = true
        };
        var cita2 = new Appointment
        {
            UserId = 5,
            OptometraId = 3,
            Servicio = "Cita 2",
            FechaHora = DateTime.SpecifyKind(hora2, DateTimeKind.Utc),
            Estado = "pendiente",
            Activo = true
        };
        _context.Appointments.AddRange(cita1, cita2);
        await _context.SaveChangesAsync();

        // Act - Intentar reprogramar cita2 al horario ya ocupado por cita1
        var editModel = new AppointmentFormViewModel
        {
            Id = cita2.Id,
            UserId = 5,
            Servicio = "Cita 2 reprogramada",
            FechaHora = hora1,
            Estado = "pendiente"
        };
        var result = await controller.EditAppointment(editModel) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("El optómetra ya tiene una cita agendada en ese horario. Por favor elige otro horario.", controller.TempData["ErrorMessage"]);

        var cita2Db = await _context.Appointments.FindAsync(cita2.Id);
        Assert.NotNull(cita2Db);
        Assert.Equal(DateTime.SpecifyKind(hora2, DateTimeKind.Utc), cita2Db.FechaHora);
    }

    [Fact]
    public async Task CreateFormula_SinAdicionYSinDistanciaPupilar_GuardaValoresNulosCorrectamente()
    {
        // Arrange
        var controller = CreateController();
        var formulaMonofocal = new FormulaOptica
        {
            UserId = 2,
            Fecha = DateTime.Today,
            TipoLente = "Monofocal Estándar",
            EsferaOD = "-1.00",
            CilindroOD = "0.00",
            EjeOD = "0",
            EsferaOI = "-1.00",
            CilindroOI = "0.00",
            EjeOI = "0",
            AdicionOD = null,
            AdicionOI = null,
            DistanciaPupilar = null,
            Activo = true
        };

        // Act
        var result = await controller.CreateFormula(formulaMonofocal) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Fórmula óptica creada exitosamente.", controller.TempData["SuccessMessage"]);

        var enDb = await _context.FormulasOpticas.FirstOrDefaultAsync(f => f.UserId == 2 && f.TipoLente == "Monofocal Estándar");
        Assert.NotNull(enDb);
        Assert.Null(enDb.AdicionOD);
        Assert.Null(enDb.AdicionOI);
        Assert.Null(enDb.DistanciaPupilar);
    }

    [Fact]
    public async Task OptometraController_TieneAuthorizeRolOptometra_NoPermiteOtrosRoles()
    {
        // 1. Verificación de metadatos del controlador
        var authorizeAttr = typeof(OptometraController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorizeAttr);
        Assert.Equal("optometra", authorizeAttr.Roles);

        var allowAnonymousMethods = typeof(OptometraController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttribute<AllowAnonymousAttribute>() != null)
            .ToList();
        Assert.Empty(allowAnonymousMethods);

        // 2. Verificación de comportamiento en runtime con el motor de autorización real de ASP.NET Core
        var services = new ServiceCollection();
        services.AddAuthorizationCore();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();
        var authService = serviceProvider.GetRequiredService<IAuthorizationService>();

        var authorizeData = typeof(OptometraController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<IAuthorizeData>();
        var policyProvider = new DefaultAuthorizationPolicyProvider(Options.Create(new AuthorizationOptions()));
        var policy = await AuthorizationPolicy.CombineAsync(policyProvider, authorizeData);
        Assert.NotNull(policy);

        // Usuario anónimo (sin sesión) -> Debe ser rechazado
        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
        var resultAnon = await authService.AuthorizeAsync(anonymousUser, policy);
        Assert.False(resultAnon.Succeeded, "Un usuario sin sesión debe ser rechazado por la política de autorización.");

        // Usuario con rol 'usuario' (paciente) -> Debe ser rechazado
        var pacienteUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "2"),
            new Claim(ClaimTypes.Role, "usuario")
        }, "TestAuth"));
        var resultPaciente = await authService.AuthorizeAsync(pacienteUser, policy);
        Assert.False(resultPaciente.Succeeded, "Un usuario con rol 'usuario' no debe ser autorizado.");

        // Usuario con rol 'ventas' -> Debe ser rechazado
        var ventasUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "4"),
            new Claim(ClaimTypes.Role, "ventas")
        }, "TestAuth"));
        var resultVentas = await authService.AuthorizeAsync(ventasUser, policy);
        Assert.False(resultVentas.Succeeded, "Un usuario con rol 'ventas' no debe ser autorizado.");

        // Usuario con rol legítimo 'optometra' -> Debe ser autorizado
        var optoUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "3"),
            new Claim(ClaimTypes.Role, "optometra")
        }, "TestAuth"));
        var resultOpto = await authService.AuthorizeAsync(optoUser, policy);
        Assert.True(resultOpto.Succeeded, "Un usuario con rol 'optometra' debe ser autorizado exitosamente.");
    }

    [Fact]
    public async Task DashboardUsuario_AislamientoDatosClinicos_PacienteNoPuedeVerDatosDeOtroPaciente()
    {
        // Arrange: Se siembran datos clínicos pertenecientes al paciente 2 y confidenciales al paciente 5
        var historialPaciente2 = new HistorialClinico
        {
            UserId = 2,
            OptometraId = 3,
            Fecha = DateTime.Today,
            Diagnostico = "Diagnóstico legítimo del paciente 2",
            Tratamiento = "Tratamiento paciente 2",
            Activo = true
        };
        var formulaPaciente2 = new FormulaOptica
        {
            UserId = 2,
            OptometraId = 3,
            Fecha = DateTime.Today,
            TipoLente = "Monofocal",
            EsferaOD = "-1.00",
            CilindroOD = "0.00",
            EjeOD = "0",
            EsferaOI = "-1.00",
            CilindroOI = "0.00",
            EjeOI = "0",
            Activo = true
        };
        var historialPaciente5 = new HistorialClinico
        {
            UserId = 5,
            OptometraId = 3,
            Fecha = DateTime.Today,
            Diagnostico = "Diagnóstico estrictamente confidencial del paciente 5",
            Tratamiento = "Tratamiento confidencial 5",
            Activo = true
        };
        var formulaPaciente5 = new FormulaOptica
        {
            UserId = 5,
            OptometraId = 3,
            Fecha = DateTime.Today,
            TipoLente = "Fórmula confidencial del paciente 5",
            EsferaOD = "-3.00",
            CilindroOD = "0.00",
            EjeOD = "0",
            EsferaOI = "-3.00",
            CilindroOI = "0.00",
            EjeOI = "0",
            Activo = true
        };
        _context.HistorialesClinicos.AddRange(historialPaciente2, historialPaciente5);
        _context.FormulasOpticas.AddRange(formulaPaciente2, formulaPaciente5);
        await _context.SaveChangesAsync();

        // Se instancia el DashboardController real autenticado con la identidad del Paciente 2
        var dashboardController = new DashboardController(
            null!,
            null!,
            new UserService(_context),
            new OrderService(_context),
            new FavoriteService(_context),
            null!,
            null!,
            NullLogger<DashboardController>.Instance,
            _context,
            Options.Create(new InventorySettings()));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "2"),
            new Claim(ClaimTypes.Role, "usuario"),
            new Claim(ClaimTypes.Name, "Usuario Demo")
        };
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
        };
        dashboardController.ControllerContext = new ControllerContext { HttpContext = httpContext };
        dashboardController.TempData = new TempDataDictionary(httpContext, new FakeTempDataProvider());

        // Act: El paciente 2 ejecuta la acción real del controlador DashboardController.Usuario
        var actionResult = await dashboardController.Usuario("historial") as ViewResult;

        // Assert: Validar aislamiento real en el ViewModel producido por el controlador
        Assert.NotNull(actionResult);
        var viewModel = actionResult.Model as DashboardUsuarioViewModel;
        Assert.NotNull(viewModel);

        // 1. Debe contener los datos clínicos propios del paciente 2
        Assert.Contains(viewModel.Historiales, h => h.UserId == 2 && h.Diagnostico == "Diagnóstico legítimo del paciente 2");
        Assert.Contains(viewModel.Formulas, f => f.UserId == 2 && f.TipoLente == "Monofocal");

        // 2. Debe aislar por completo los datos del paciente 5
        Assert.DoesNotContain(viewModel.Historiales, h => h.UserId == 5);
        Assert.DoesNotContain(viewModel.Formulas, f => f.UserId == 5);
        Assert.DoesNotContain(viewModel.Historiales, h => h.Diagnostico == "Diagnóstico estrictamente confidencial del paciente 5");
        Assert.DoesNotContain(viewModel.Formulas, f => f.TipoLente == "Fórmula confidencial del paciente 5");
        Assert.All(viewModel.Historiales, h => Assert.Equal(2, h.UserId));
        Assert.All(viewModel.Formulas, f => Assert.Equal(2, f.UserId));
    }
}
