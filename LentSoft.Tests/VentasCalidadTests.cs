using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using LentSoft.Web.Data;
using LentSoft.Web.Models.DTOs;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Services;
using Xunit;

namespace LentSoft.Tests;

public class VentasCalidadTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LentSoftDbContext _context;
    private readonly IMemoryCache _memoryCache;
    private readonly IInventoryService _inventoryService;
    private readonly PagoVentaService _pagoService;
    private readonly PasarelaPagoService _pasarelaService;
    private readonly VentasService _ventasService;
    private readonly User _testUser;

    private const string TarjetaValidaAprobada = "4532000000000017";
    private const string TarjetaInvalidaLuhn = "4532000000000018";

    public VentasCalidadTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LentSoftDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new LentSoftDbContext(options);
        _context.Database.EnsureCreated();

        _memoryCache = new MemoryCache(new MemoryCacheOptions());

        var invSettings = Options.Create(new InventorySettings
        {
            LowStockThreshold = 5,
            MaxStock = 500
        });
        _inventoryService = new InventoryService(_context, invSettings, new Microsoft.AspNetCore.Http.HttpContextAccessor());

        _pagoService = new PagoVentaService(
            _context,
            _memoryCache,
            NullLogger<PagoVentaService>.Instance,
            NullLoggerFactory.Instance);

        _pasarelaService = new PasarelaPagoService(
            _context,
            _pagoService,
            _memoryCache,
            NullLogger<PasarelaPagoService>.Instance,
            NullLoggerFactory.Instance);

        var invoiceService = new InvoiceService(_context);
        var tokenService = new FakeTokenService();
        var emailService = new FakeEmailService();

        _ventasService = new VentasService(
            _context,
            invoiceService,
            tokenService,
            emailService,
            NullLogger<VentasService>.Instance,
            _inventoryService,
            NullLoggerFactory.Instance);

        _testUser = new User
        {
            Nombre = "Cliente",
            Apellido = "Calidad",
            TipoDocumento = "CC",
            NumeroDocumento = "900123456",
            Email = "calidad@lentsoft.test",
            PasswordHash = "hash123",
            Role = "usuario"
        };
        _context.Users.Add(_testUser);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _memoryCache.Dispose();
        _context.Dispose();
        _connection.Dispose();
    }

    // ── 1. Rollback de transacción cuando ocurre un error a mitad del flujo ──
    [Fact]
    public async Task ProcesarPagoTarjeta_FalloEnRegistrarAbono_HaceRollbackDeTransaccion()
    {
        var order = new Order
        {
            UserId = _testUser.Id,
            Total = 100000m,
            MontoPagado = 0m,
            Estado = "pendiente",
            EstadoPago = "pendiente",
            Activo = true,
            FechaPedido = DateTime.UtcNow
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Servicio de pagos simulado que lanza excepción a mitad del flujo después de insertar la TransaccionPasarela
        var failingPagoService = new FailingPagoVentaService();
        var pasarelaConFallo = new PasarelaPagoService(
            _context,
            failingPagoService,
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<PasarelaPagoService>.Instance,
            NullLoggerFactory.Instance);

        var dto = new TarjetaPagoDTO
        {
            VentaId = order.Id,
            NumeroTarjeta = TarjetaValidaAprobada,
            NombreTitular = "CLIENTE PRUEBA",
            FechaExpiracion = "12/29",
            Cvv = "123",
            Monto = 50000m,
            Responsable = "vendedor_test"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            pasarelaConFallo.ProcesarPagoTarjetaAsync(dto));

        // Verificar que se hizo rollback completo: no quedó ninguna TransaccionPago ni PagoVenta huérfano
        var transaccionesCount = await _context.TransaccionesPagos.AsNoTracking().CountAsync();
        var pagosCount = await _context.PagosVentas.AsNoTracking().CountAsync();
        var orderActual = await _context.Orders.AsNoTracking().FirstAsync(o => o.Id == order.Id);

        Assert.Equal(0, transaccionesCount);
        Assert.Equal(0, pagosCount);
        Assert.Equal(0m, orderActual.MontoPagado);
    }

    // ── 2. Rechazo de cobro duplicado dentro de la ventana de 30 segundos ──
    [Fact]
    public async Task RegistrarAbono_CobroDuplicadoEnVentanaDe30Segundos_LanzaExcepcion()
    {
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

        // Primer abono exitoso
        var primerPago = await _pagoService.RegistrarAbonoAsync(order.Id, 50000m, "Efectivo", "Vendedor1");
        Assert.NotNull(primerPago);

        // Segundo abono idéntico inmediato (mismo ventaId, monto y método de pago dentro de 30s)
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _pagoService.RegistrarAbonoAsync(order.Id, 50000m, "Efectivo", "Vendedor1"));

        Assert.Contains("30 segundos", ex.Message, StringComparison.OrdinalIgnoreCase);

        // Un abono con monto distinto sí debe permitirse
        var pagoDistintoMonto = await _pagoService.RegistrarAbonoAsync(order.Id, 30000m, "Efectivo", "Vendedor1");
        Assert.NotNull(pagoDistintoMonto);
    }

    [Fact]
    public async Task ProcesarPagoTarjeta_CobroDuplicadoEnVentanaDe30Segundos_RechazaSegundoIntento()
    {
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

        var dto = new TarjetaPagoDTO
        {
            VentaId = order.Id,
            NumeroTarjeta = TarjetaValidaAprobada,
            NombreTitular = "CARLOS PEREZ",
            FechaExpiracion = "12/29",
            Cvv = "123",
            Monto = 60000m,
            Responsable = "vendedor1"
        };

        var res1 = await _pasarelaService.ProcesarPagoTarjetaAsync(dto);
        Assert.True(res1.Exitoso);

        var res2 = await _pasarelaService.ProcesarPagoTarjetaAsync(dto);
        Assert.False(res2.Exitoso);
        Assert.Contains("30 segundos", res2.Mensaje, StringComparison.OrdinalIgnoreCase);
    }

    // ── 3. Creación de SalesOrder con producto inexistente y con stock insuficiente ──
    [Fact]
    public async Task CrearPedidoVenta_ProductoInexistenteOStockInsuficiente_RechazaYNoDescuentaOtroProducto()
    {
        var productoDefault = new Product
        {
            Nombre = "Montura Test Inicial",
            Sku = "SKU-DEF-001",
            Categoria = "monturas",
            Precio = 120000m,
            Stock = 10,
            StockMinimo = 2,
            Activo = true
        };
        _context.Products.Add(productoDefault);
        await _context.SaveChangesAsync();

        // Caso A: Producto inexistente (ProductId = 9999)
        var resInexistente = await _ventasService.CrearPedidoVentaAsync(new CrearPedidoVentaDTO
        {
            NumeroPedido = "PED-VENTA-INEX",
            ClienteNombre = "Laura Gómez",
            ProductId = 9999,
            Cantidad = 2,
            Estado = "pendiente"
        }, "vendedor_test");

        Assert.False(resInexistente.Exito);
        Assert.Contains("no existe", resInexistente.Mensaje, StringComparison.OrdinalIgnoreCase);

        // Verificar que el producto existente ("Montura Test Inicial") NO sufrió descuento de stock por defecto
        var prodTrasInexistente = await _context.Products.AsNoTracking().FirstAsync(p => p.Id == productoDefault.Id);
        Assert.Equal(10, prodTrasInexistente.Stock);

        // Caso B: Stock insuficiente (solicita 25 cuando hay 10)
        var resSinStock = await _ventasService.CrearPedidoVentaAsync(new CrearPedidoVentaDTO
        {
            NumeroPedido = "PED-VENTA-SINSTOCK",
            ClienteNombre = "Laura Gómez",
            ProductId = productoDefault.Id,
            Cantidad = 25,
            Estado = "pendiente"
        }, "vendedor_test");

        Assert.False(resSinStock.Exito);
        Assert.Contains("Stock insuficiente", resSinStock.Mensaje, StringComparison.OrdinalIgnoreCase);

        var prodTrasSinStock = await _context.Products.AsNoTracking().FirstAsync(p => p.Id == productoDefault.Id);
        Assert.Equal(10, prodTrasSinStock.Stock);
    }

    // ── 4. Descuento de stock al crear SalesOrder y devolución al cancelar/eliminar con coincidencia única ──
    [Fact]
    public async Task SalesOrder_CicloCompleto_DescuentaStockAlCrearYDevuelveAlEliminarConCoincidenciaUnica()
    {
        var producto = new Product
        {
            Nombre = "Lente Progresivo Digital HD",
            Sku = "SKU-PROG-01",
            Categoria = "lentes-graduados",
            Precio = 250000m,
            PrecioDescuento = 200000m,
            Stock = 15,
            StockMinimo = 3,
            Activo = true
        };
        _context.Products.Add(producto);
        await _context.SaveChangesAsync();

        // 1. Crear SalesOrder (debe recalcular precio en servidor usando PrecioDescuento = 200000 y descontar 3 unidades)
        var resCrear = await _ventasService.CrearPedidoVentaAsync(new CrearPedidoVentaDTO
        {
            NumeroPedido = "PED-VENTA-001",
            ClienteNombre = "Andrés Murcia",
            ProductId = producto.Id,
            Cantidad = 3,
            Estado = "pendiente",
            Notas = "Entrega prioritaria"
        }, "vendedor_test");

        Assert.True(resCrear.Exito);
        Assert.NotNull(resCrear.PedidoId);

        var pedidoGuardado = await _context.SalesOrders.AsNoTracking().FirstAsync(s => s.Id == resCrear.PedidoId.Value);
        Assert.Equal(200000m, pedidoGuardado.PrecioUnitario);
        Assert.Equal(600000m, pedidoGuardado.Total);

        var prodDescontado = await _context.Products.AsNoTracking().FirstAsync(p => p.Id == producto.Id);
        Assert.Equal(12, prodDescontado.Stock);

        // 2. Eliminar SalesOrder: como el nombre del producto tiene coincidencia exacta y única, devuelve las 3 unidades
        var resEliminar = await _ventasService.EliminarPedidoVentaAsync(resCrear.PedidoId.Value, "vendedor_test");
        Assert.True(resEliminar.Exito);
        Assert.Null(resEliminar.Advertencia);

        var prodRestaurado = await _context.Products.AsNoTracking().FirstAsync(p => p.Id == producto.Id);
        Assert.Equal(15, prodRestaurado.Stock);
    }

    [Fact]
    public async Task EliminarPedidoVenta_RegistroHistoricoSinCoincidenciaUnica_NoTocaStockDeOtroProducto()
    {
        var prodExistente = new Product
        {
            Nombre = "Montura Titanio",
            Sku = "SKU-TIT-01",
            Categoria = "monturas",
            Precio = 180000m,
            Stock = 20,
            Activo = true
        };
        _context.Products.Add(prodExistente);

        // Insertar un SalesOrder histórico cuyo ProductoNombre no existe en Products
        var pedidoHistorico = new SalesOrder
        {
            NumeroPedido = "PED-HIST-99",
            ClienteNombre = "Cliente Histórico",
            ProductoNombre = "Producto Descontinuado Antiguo",
            Cantidad = 4,
            PrecioUnitario = 90000m,
            Total = 360000m,
            Estado = "pendiente",
            Fecha = DateTime.UtcNow
        };
        _context.SalesOrders.Add(pedidoHistorico);
        await _context.SaveChangesAsync();

        var resEliminar = await _ventasService.EliminarPedidoVentaAsync(pedidoHistorico.Id, "vendedor_test");
        Assert.True(resEliminar.Exito);
        Assert.NotNull(resEliminar.Advertencia);

        // El stock del otro producto jamás debe ser alterado
        var prodIntacto = await _context.Products.AsNoTracking().FirstAsync(p => p.Id == prodExistente.Id);
        Assert.Equal(20, prodIntacto.Stock);
    }

    // ── 5. Manejo de DbUpdateConcurrencyException ──
    [Fact]
    public async Task CrearPedidoVenta_DbUpdateConcurrencyException_RetornaMensajeClaroDeReintento()
    {
        var options = new DbContextOptionsBuilder<LentSoftDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var concurrencyContext = new ConcurrencyThrowingDbContext(options);
        var prod = new Product
        {
            Nombre = "Gafa Sol Polarizada",
            Sku = "SKU-SOL-01",
            Categoria = "lentes-sol",
            Precio = 150000m,
            Stock = 10,
            Activo = true
        };
        _context.Products.Add(prod);
        await _context.SaveChangesAsync();

        var serviceConConcurrencia = new VentasService(
            concurrencyContext,
            new InvoiceService(concurrencyContext),
            new FakeTokenService(),
            new FakeEmailService(),
            NullLogger<VentasService>.Instance,
            null,
            NullLoggerFactory.Instance);

        var resultado = await serviceConConcurrencia.CrearPedidoVentaAsync(new CrearPedidoVentaDTO
        {
            NumeroPedido = "PED-CONC-01",
            ClienteNombre = "Pedro Concurrencia",
            ProductId = prod.Id,
            Cantidad = 1,
            Estado = "pendiente"
        }, "vendedor_test");

        Assert.False(resultado.Exito);
        Assert.Contains("simultáneamente", resultado.Mensaje, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("intente de nuevo", resultado.Mensaje, StringComparison.OrdinalIgnoreCase);
    }

    // ── 6. Rate limiting de intentos fallidos de pago con tarjeta (máx 5 en 10 min) ──
    [Fact]
    public async Task ProcesarPagoTarjeta_CincoIntentosFallidos_BloqueaSextoIntentoPorRateLimiting()
    {
        var dtoInvalido = new TarjetaPagoDTO
        {
            NumeroTarjeta = TarjetaInvalidaLuhn,
            NombreTitular = "USUARIO BLOQUEO",
            FechaExpiracion = "12/29",
            Cvv = "123",
            Monto = 40000m,
            Responsable = "usuario_atacante"
        };

        for (int i = 0; i < 5; i++)
        {
            var intento = await _pasarelaService.ProcesarPagoTarjetaAsync(dtoInvalido);
            Assert.False(intento.Exitoso);
        }

        // El 6to intento, incluso con tarjeta válida, debe ser bloqueado por rate limiting
        var dtoValido = new TarjetaPagoDTO
        {
            NumeroTarjeta = TarjetaValidaAprobada,
            NombreTitular = "USUARIO BLOQUEO",
            FechaExpiracion = "12/29",
            Cvv = "123",
            Monto = 40000m,
            Responsable = "usuario_atacante"
        };

        var sextoIntento = await _pasarelaService.ProcesarPagoTarjetaAsync(dtoValido);
        Assert.False(sextoIntento.Exitoso);
        Assert.Contains("5 intentos fallidos", sextoIntento.Mensaje, StringComparison.OrdinalIgnoreCase);
    }

    // ── 7. Exportación CSV de todas las ventas filtradas sin truncamiento ──
    [Fact]
    public async Task ExportarVentasFiltradasAsync_RetornaTodosLosRegistrosFiltradosSinPaginacion()
    {
        for (int i = 1; i <= 15; i++)
        {
            _context.Orders.Add(new Order
            {
                UserId = _testUser.Id,
                Total = 50000m * i,
                MontoPagado = i % 2 == 0 ? 50000m * i : 0m,
                Estado = i % 2 == 0 ? "pagado" : "pendiente",
                EstadoPago = i % 2 == 0 ? "pagado" : "pendiente",
                Activo = true,
                FechaPedido = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        await _context.SaveChangesAsync();

        var todasPagadas = await _ventasService.ExportarVentasFiltradasAsync(new DataTablesRequestDTO
        {
            Estado = "pagado"
        });
        Assert.Equal(7, todasPagadas.Count);
        Assert.All(todasPagadas, v => Assert.Equal("pagado", v.Estado));
    }

    // ── Helpers para pruebas de resiliencia ──
    private sealed class FailingPagoVentaService : IPagoVentaService
    {
        public Task<PagoVenta> RegistrarAbonoAsync(int ventaId, decimal monto, string metodoPago, string? responsable = null)
        {
            throw new InvalidOperationException("Error simulado a mitad del registro de abono.");
        }

        public Task<List<PagoVenta>> ObtenerAbonosPorVentaAsync(int ventaId) =>
            Task.FromResult(new List<PagoVenta>());

        public Task<PagoVenta?> ObtenerPagoPorIdAsync(int id) =>
            Task.FromResult<PagoVenta?>(null);
    }

    private sealed class ConcurrencyThrowingDbContext : LentSoftDbContext
    {
        public ConcurrencyThrowingDbContext(DbContextOptions<LentSoftDbContext> options) : base(options) { }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new DbUpdateConcurrencyException("Conflicto de concurrencia simulado en prueba.");
        }
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
}

