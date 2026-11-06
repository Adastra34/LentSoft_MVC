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
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using LentSoft.Web.Controllers;
using LentSoft.Web.Data;
using LentSoft.Web.Models.DTOs;
using LentSoft.Web.Models.Entities;
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
        public Task SendPasswordResetEmailAsync(string toEmail, string resetLink) => Task.CompletedTask;
        public Task SendSaleConfirmationEmailAsync(string toEmail, Order order, string confirmLink) => Task.CompletedTask;
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

    private OptometraController CreateController(int optometraId = 3, IVentasService? ventasService = null, bool isAjax = false)
    {
        var controller = new OptometraController(_context, null, ventasService);
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

    // ── (1) CreateExamen rechaza fecha anterior a hoy ──
    [Fact]
    public async Task CreateExamen_RechazaFechaAnteriorAHoy_EstableceErrorYNoGuarda()
    {
        // Arrange
        var controller = CreateController();
        var examenAyer = new ExamenVisual
        {
            UserId = 2,
            Fecha = DateTime.Today.AddDays(-1),
            TipoExamen = "Refracción General",
            Diagnostico = "Miopía",
            Activo = true
        };

        // Act
        var result = await controller.CreateExamen(examenAyer) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName);
        Assert.Equal("examenes", result.RouteValues?["section"]);
        Assert.Equal("La fecha del examen no puede ser anterior a hoy.", controller.TempData["ErrorMessage"]?.ToString());

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

    // ── (3) EditExamen rechaza fecha anterior a hoy ──
    [Fact]
    public async Task EditExamen_RechazaFechaAnteriorAHoy_NoModificaRegistroYEstableceError()
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
            Fecha = DateTime.Today.AddDays(-3), // Fecha inválida
            TipoExamen = "Control Modificado",
            Diagnostico = "Diagnóstico Modificado"
        };

        // Act
        var result = await controller.EditExamen(examenEditadoInvalido) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName);
        Assert.Equal("examenes", result.RouteValues?["section"]);
        Assert.Equal("La fecha del examen no puede ser anterior a hoy.", controller.TempData["ErrorMessage"]?.ToString());

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
}
