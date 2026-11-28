using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using LentSoft.Web.Controllers;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Models.ViewModels;
using LentSoft.Web.Services;
using Xunit;
using Xunit.Abstractions;

namespace LentSoft.Tests;

public class MisFacturasTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly SqliteConnection _connection;
    private readonly LentSoftDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly InvoiceService _invoiceService;
    private readonly PdfInvoiceService _pdfService;
    private readonly IMemoryCache _memoryCache;
    private readonly User _usuarioA;
    private readonly User _usuarioB;

    public MisFacturasTests(ITestOutputHelper output)
    {
        _output = output;
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LentSoftDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new LentSoftDbContext(options);
        _context.Database.EnsureCreated();

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Facturacion:ResolucionDianNumero"] = "18764028920000",
                ["Facturacion:RangoAutorizadoDesde"] = "FAC-2026-0001",
                ["Facturacion:RangoAutorizadoHasta"] = "FAC-2026-9999",
                ["Facturacion:NitEmisor"] = "9001234567"
            })
            .Build();

        _invoiceService = new InvoiceService(_context, _configuration);
        _pdfService = new PdfInvoiceService(_configuration);
        _memoryCache = new MemoryCache(new MemoryCacheOptions());

        _usuarioA = new User
        {
            Nombre = "Laura",
            Apellido = "Méndez",
            TipoDocumento = "CC",
            NumeroDocumento = "1020304050",
            Email = "laura.mendez@lentsoft.test",
            PasswordHash = "hashA",
            Role = "usuario",
            Activo = true
        };

        _usuarioB = new User
        {
            Nombre = "Carlos",
            Apellido = "Ruiz",
            TipoDocumento = "CC",
            NumeroDocumento = "1090807060",
            Email = "carlos.ruiz@lentsoft.test",
            PasswordHash = "hashB",
            Role = "usuario",
            Activo = true
        };

        _context.Users.AddRange(_usuarioA, _usuarioB);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _memoryCache.Dispose();
        _context.Dispose();
        _connection.Dispose();
    }

    private MisFacturasController CrearMisFacturasController(
        int userId,
        LentSoftDbContext? db = null,
        IInvoiceService? invoiceService = null,
        IPdfInvoiceService? pdfService = null,
        IMemoryCache? cache = null)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, "usuario")
            }, "TestAuth"))
        };

        return new MisFacturasController(
            db ?? _context,
            invoiceService ?? _invoiceService,
            pdfService ?? _pdfService,
            NullLogger<MisFacturasController>.Instance,
            cache ?? _memoryCache)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    [Fact]
    public async Task ConfirmarPago_CompraExitosa_GeneraFacturaAutomaticamenteConTotalCorrecto()
    {
        var product = new Product
        {
            Nombre = "Montura Titanium Pro",
            Precio = 350000m,
            PorcentajeIva = 19m,
            StockBodega = 10,
            Stock = 10,
            Activo = true
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var cart = new Cart
        {
            Id = 99,
            UserId = _usuarioA.Id,
            CartItems = new List<CartItem>
            {
                new CartItem
                {
                    ProductId = product.Id,
                    Product = product,
                    Cantidad = 2,
                    PrecioUnitario = product.Precio
                }
            }
        };

        var cartService = new StubCartService(cart);
        var pasarelaService = new StubPasarelaPagoService();
        var inventoryService = new InventoryService(
            _context,
            Options.Create(new InventorySettings { LowStockThreshold = 5, MaxStock = 1000 }),
            new HttpContextAccessor());

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, _usuarioA.Id.ToString())
            }, "TestAuth"))
        };

        var controller = new CheckoutController(
            cartService,
            _context,
            pasarelaService,
            inventoryService,
            _invoiceService,
            NullLogger<CheckoutController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new StubTempDataProvider())
        };

        var model = new CheckoutViewModel
        {
            NumeroTarjeta = "4111 1111 1111 1111",
            NombreTarjeta = "Laura Mendez",
            Expiracion = "12/30",
            Cvv = "123",
            DireccionEnvio = "Cra 10 #20-30"
        };

        var result = await controller.ConfirmarPago(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Confirmacion", redirect.ActionName);

        var order = await _context.Orders.AsNoTracking()
            .Where(o => o.UserId == _usuarioA.Id)
            .OrderByDescending(o => o.Id)
            .FirstOrDefaultAsync();
        Assert.NotNull(order);
        Assert.Equal(700000m, order!.Total);

        var factura = await _context.Invoices.AsNoTracking()
            .FirstOrDefaultAsync(i => i.OrderId == order.Id && i.Activo);
        Assert.NotNull(factura);
        Assert.Equal("pagada", factura!.Estado);
        Assert.Equal(700000m, factura.Total);
        Assert.False(string.IsNullOrWhiteSpace(factura.NumeroFactura));
        Assert.False(string.IsNullOrWhiteSpace(factura.CUFE));
        Assert.NotNull(factura.FechaPago);
    }

    [Fact]
    public async Task ConfirmarPago_SiFallaFacturacion_NoCancelaLaCompraYRegistraError()
    {
        var product = new Product
        {
            Nombre = "Gafa Sol Polarizada",
            Precio = 180000m,
            PorcentajeIva = 19m,
            StockBodega = 5,
            Stock = 5,
            Activo = true
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var cart = new Cart
        {
            Id = 100,
            UserId = _usuarioA.Id,
            CartItems = new List<CartItem>
            {
                new CartItem
                {
                    ProductId = product.Id,
                    Product = product,
                    Cantidad = 1,
                    PrecioUnitario = product.Precio
                }
            }
        };

        var inventoryService = new InventoryService(
            _context,
            Options.Create(new InventorySettings { LowStockThreshold = 2, MaxStock = 1000 }),
            new HttpContextAccessor());

        var recordingLogger = new RecordingLogger<CheckoutController>();
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, _usuarioA.Id.ToString())
            }, "TestAuth"))
        };

        var controller = new CheckoutController(
            new StubCartService(cart),
            _context,
            new StubPasarelaPagoService(),
            inventoryService,
            new ThrowingInvoiceService(),
            recordingLogger)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new StubTempDataProvider())
        };

        var model = new CheckoutViewModel
        {
            NumeroTarjeta = "4111 1111 1111 1111",
            NombreTarjeta = "Laura Mendez",
            Expiracion = "12/30",
            Cvv = "123",
            DireccionEnvio = "Calle 50 #10-20"
        };

        var result = await controller.ConfirmarPago(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Confirmacion", redirect.ActionName);
        Assert.True(await _context.Orders.AnyAsync(o => o.UserId == _usuarioA.Id && o.Total == 180000m));
        Assert.True(recordingLogger.ErrorLogged);
    }

    [Fact]
    public async Task CreateAsync_VeinteFacturasSeguidas_TienenNumerosDistintos()
    {
        var numeros = new List<string>();

        for (int i = 0; i < 20; i++)
        {
            var order = new Order
            {
                UserId = _usuarioA.Id,
                Total = 100000m + (i * 1000m),
                MontoPagado = 100000m + (i * 1000m),
                Estado = "pagado",
                EstadoPago = "pagado",
                Activo = true,
                FechaPedido = DateTime.UtcNow.AddMinutes(-i)
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var invoice = await _invoiceService.CreateAsync(new Invoice { OrderId = order.Id });
            numeros.Add(invoice.NumeroFactura);
        }

        Assert.Equal(20, numeros.Count);
        Assert.Equal(20, numeros.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public async Task CreateAsync_EnParaleloConDbContextPorTarea_TienenNumerosDistintos()
    {
        var tempDbPath = Path.Combine(Path.GetTempPath(), $"lentsoft_facturas_{Guid.NewGuid():N}.db");
        try
        {
            var connString = $"Data Source={tempDbPath}";
            var baseOptions = new DbContextOptionsBuilder<LentSoftDbContext>()
                .UseSqlite(connString)
                .Options;

            var orderIds = new List<int>();
            using (var setupCtx = new LentSoftDbContext(baseOptions))
            {
                setupCtx.Database.EnsureCreated();
                setupCtx.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;");

                var user = new User
                {
                    Nombre = "Paralelo",
                    Apellido = "Test",
                    TipoDocumento = "CC",
                    NumeroDocumento = "5544332211",
                    Email = "paralelo@lentsoft.test",
                    PasswordHash = "hash",
                    Role = "usuario",
                    Activo = true
                };
                setupCtx.Users.Add(user);
                await setupCtx.SaveChangesAsync();

                for (int i = 0; i < 4; i++)
                {
                    var o = new Order
                    {
                        UserId = user.Id,
                        Total = 150000m,
                        MontoPagado = 150000m,
                        Estado = "pagado",
                        EstadoPago = "pagado",
                        Activo = true,
                        FechaPedido = DateTime.UtcNow
                    };
                    setupCtx.Orders.Add(o);
                    await setupCtx.SaveChangesAsync();
                    orderIds.Add(o.Id);
                }
            }

            var tasks = orderIds.Select(orderId => Task.Run(async () =>
            {
                var taskOptions = new DbContextOptionsBuilder<LentSoftDbContext>()
                    .UseSqlite(connString)
                    .Options;

                using var taskCtx = new LentSoftDbContext(taskOptions);
                taskCtx.Database.ExecuteSqlRaw("PRAGMA busy_timeout=5000;");
                var service = new InvoiceService(taskCtx, _configuration);
                var created = await service.CreateAsync(new Invoice { OrderId = orderId });
                return created.NumeroFactura;
            })).ToArray();

            var resultados = await Task.WhenAll(tasks);

            Assert.Equal(4, resultados.Length);
            Assert.Equal(4, resultados.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(tempDbPath))
            {
                try { File.Delete(tempDbPath); } catch { }
            }
            var walPath = tempDbPath + "-wal";
            var shmPath = tempDbPath + "-shm";
            if (File.Exists(walPath)) { try { File.Delete(walPath); } catch { } }
            if (File.Exists(shmPath)) { try { File.Delete(shmPath); } catch { } }
        }
    }

    [Fact]
    public async Task DetalleYDescargar_UsuarioNoPropietario_RetornaNotFound()
    {
        var product = new Product { Nombre = "Lentes UV", Precio = 200000m, PorcentajeIva = 19m, Stock = 10, Activo = true };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var orderA = new Order
        {
            UserId = _usuarioA.Id,
            Total = 200000m,
            MontoPagado = 200000m,
            Estado = "pagado",
            EstadoPago = "pagado",
            Activo = true,
            FechaPedido = DateTime.UtcNow,
            OrderItems = new List<OrderItem>
            {
                new OrderItem { ProductId = product.Id, Cantidad = 1, PrecioUnitario = 200000m }
            }
        };
        _context.Orders.Add(orderA);
        await _context.SaveChangesAsync();

        var invoiceA = await _invoiceService.CreateAsync(new Invoice { OrderId = orderA.Id });

        // Usuario B intenta acceder o descargar la factura de Usuario A -> NotFound
        var controllerB = CrearMisFacturasController(_usuarioB.Id);
        var resDetalleB = await controllerB.Detalle(invoiceA.Id);
        var resDescargarB = await controllerB.Descargar(invoiceA.Id);

        Assert.IsType<NotFoundResult>(resDetalleB);
        Assert.IsType<NotFoundResult>(resDescargarB);

        // Usuario A sí puede ver y descargar su propia factura
        var controllerA = CrearMisFacturasController(_usuarioA.Id);
        var resDetalleA = Assert.IsType<PartialViewResult>(await controllerA.Detalle(invoiceA.Id));
        var detalleVm = Assert.IsType<FacturaDetalleViewModel>(resDetalleA.Model);
        Assert.Equal(invoiceA.NumeroFactura, detalleVm.NumeroFactura);

        var resDescargarA = Assert.IsType<FileContentResult>(await controllerA.Descargar(invoiceA.Id));
        Assert.Equal("application/pdf", resDescargarA.ContentType);
        Assert.Equal($"Factura-{invoiceA.NumeroFactura}.pdf", resDescargarA.FileDownloadName);
        Assert.True(resDescargarA.FileContents.Length > 100);
    }

    [Fact]
    public async Task Lista_BackfillIdempotente_NoDuplicaFacturasEnLlamadasRepetidas()
    {
        var ordPagado1 = new Order
        {
            UserId = _usuarioA.Id,
            Total = 120000m,
            MontoPagado = 120000m,
            Estado = "entregado",
            EstadoPago = "pagado",
            Activo = true,
            FechaPedido = DateTime.UtcNow.AddDays(-2)
        };
        var ordPagado2 = new Order
        {
            UserId = _usuarioA.Id,
            Total = 250000m,
            MontoPagado = 250000m,
            Estado = "pagado",
            EstadoPago = "pagado",
            Activo = true,
            FechaPedido = DateTime.UtcNow.AddDays(-1)
        };
        var ordPendiente = new Order
        {
            UserId = _usuarioA.Id,
            Total = 90000m,
            MontoPagado = 0m,
            Estado = "pendiente",
            EstadoPago = "pendiente",
            Activo = true,
            FechaPedido = DateTime.UtcNow
        };
        _context.Orders.AddRange(ordPagado1, ordPagado2, ordPendiente);
        await _context.SaveChangesAsync();

        var controller = CrearMisFacturasController(_usuarioA.Id);

        var res1 = Assert.IsType<PartialViewResult>(await controller.Lista(1, 5));
        var vm1 = Assert.IsType<MisFacturasListaViewModel>(res1.Model);
        Assert.Equal(2, vm1.TotalCount);
        Assert.Equal(2, vm1.Facturas.Select(f => f.NumeroFactura).Distinct().Count());

        // Segunda llamada: no debe duplicar ni fallar
        var res2 = Assert.IsType<PartialViewResult>(await controller.Lista(1, 5));
        var vm2 = Assert.IsType<MisFacturasListaViewModel>(res2.Model);
        Assert.Equal(2, vm2.TotalCount);

        var totalFacturasEnBd = await _context.Invoices.CountAsync(i =>
            (i.OrderId == ordPagado1.Id || i.OrderId == ordPagado2.Id) && i.Activo);
        Assert.Equal(2, totalFacturasEnBd);
        Assert.False(await _context.Invoices.AnyAsync(i => i.OrderId == ordPendiente.Id));
    }

    [Fact]
    public async Task Rendimiento_200FacturasYPrimeraGeneracionPdfEnFrio_RespondeEnMenosDe5Segundos()
    {
        var product = new Product { Nombre = "Lente Pro", Precio = 300000m, PorcentajeIva = 19m, Stock = 500, Activo = true };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var orders = new List<Order>(200);
        for (int i = 0; i < 200; i++)
        {
            orders.Add(new Order
            {
                UserId = _usuarioA.Id,
                Total = 300000m,
                MontoPagado = 300000m,
                Estado = "pagado",
                EstadoPago = "pagado",
                Activo = true,
                FechaPedido = DateTime.UtcNow.AddMinutes(-i),
                OrderItems = new List<OrderItem>
                {
                    new OrderItem { ProductId = product.Id, Cantidad = 1, PrecioUnitario = 300000m }
                }
            });
        }
        _context.Orders.AddRange(orders);
        await _context.SaveChangesAsync();

        var invoices = new List<Invoice>(200);
        for (int i = 0; i < 200; i++)
        {
            invoices.Add(new Invoice
            {
                OrderId = orders[i].Id,
                NumeroFactura = $"FAC-2026-{(i + 1):D4}",
                Subtotal = 252100.84m,
                Impuestos = 47899.16m,
                Total = 300000m,
                Estado = "pagada",
                FechaEmision = DateTime.UtcNow.AddMinutes(-i),
                FechaPago = DateTime.UtcNow.AddMinutes(-i),
                MetodoPago = "Tarjeta Visa (****1111)",
                CUFE = $"cufe-test-{i + 1:D4}",
                Activo = true
            });
        }
        _context.Invoices.AddRange(invoices);
        await _context.SaveChangesAsync();

        var targetInvoice = await _context.Invoices
            .AsNoTracking()
            .Include(i => i.Order).ThenInclude(o => o!.User)
            .Include(i => i.Order).ThenInclude(o => o!.OrderItems).ThenInclude(oi => oi.Product)
            .FirstAsync(i => i.Id == invoices[0].Id);

        // Medición de primera generación PDF en frío
        var coldPdfService = new PdfInvoiceService(_configuration);
        var swColdPdf = Stopwatch.StartNew();
        var coldBytes = coldPdfService.GenerateInvoicePdf(targetInvoice);
        swColdPdf.Stop();
        _output.WriteLine($"Primera generación PDF en frío: {swColdPdf.ElapsedMilliseconds} ms (supera 3s: {swColdPdf.ElapsedMilliseconds > 3000})");
        Assert.True(coldBytes.Length > 100);
        Assert.True(swColdPdf.ElapsedMilliseconds < 5000, $"PDF en frío tardó {swColdPdf.ElapsedMilliseconds} ms");

        // Medición Lista con 200 facturas
        var controller = CrearMisFacturasController(_usuarioA.Id, pdfService: coldPdfService);
        var swLista = Stopwatch.StartNew();
        var listaResult = Assert.IsType<PartialViewResult>(await controller.Lista(1, 5));
        swLista.Stop();
        var listaVm = Assert.IsType<MisFacturasListaViewModel>(listaResult.Model);
        _output.WriteLine($"Lista (200 facturas): {swLista.ElapsedMilliseconds} ms");
        Assert.Equal(200, listaVm.TotalCount);
        Assert.Equal(5, listaVm.Facturas.Count);
        Assert.True(swLista.ElapsedMilliseconds < 5000, $"Lista tardó {swLista.ElapsedMilliseconds} ms");

        // Medición Detalle
        var swDetalle = Stopwatch.StartNew();
        var detalleResult = Assert.IsType<PartialViewResult>(await controller.Detalle(targetInvoice.Id));
        swDetalle.Stop();
        _output.WriteLine($"Detalle (200 facturas): {swDetalle.ElapsedMilliseconds} ms");
        Assert.IsType<FacturaDetalleViewModel>(detalleResult.Model);
        Assert.True(controller.Response.Headers.ContainsKey("Server-Timing"));
        Assert.True(swDetalle.ElapsedMilliseconds < 5000, $"Detalle tardó {swDetalle.ElapsedMilliseconds} ms");

        // Medición Descargar PDF
        var swDescargar = Stopwatch.StartNew();
        var descargarResult = Assert.IsType<FileContentResult>(await controller.Descargar(targetInvoice.Id));
        swDescargar.Stop();
        _output.WriteLine($"Descargar PDF: {swDescargar.ElapsedMilliseconds} ms");
        Assert.Equal("application/pdf", descargarResult.ContentType);
        Assert.True(controller.Response.Headers.ContainsKey("Server-Timing"));
        Assert.True(swDescargar.ElapsedMilliseconds < 5000, $"Descargar tardó {swDescargar.ElapsedMilliseconds} ms");
    }

    private sealed class StubCartService : ICartService
    {
        private readonly Cart? _cart;
        public StubCartService(Cart? cart) => _cart = cart;
        public Task<CartItem> AddToCartAsync(int userId, int productId, int cantidad) => throw new NotImplementedException();
        public Task<Cart?> GetCartAsync(int userId) => Task.FromResult(_cart);
        public Task<CartItem?> UpdateQuantityAsync(int userId, int productId, int cantidad) => throw new NotImplementedException();
        public Task<bool> RemoveItemAsync(int userId, int productId) => throw new NotImplementedException();
        public Task<bool> ClearCartAsync(int userId) => Task.FromResult(true);
    }

    private sealed class StubPasarelaPagoService : IPasarelaPagoService
    {
        public Task<ResultadoTransaccionDTO> ProcesarPagoTarjetaAsync(TarjetaPagoDTO dto) =>
            Task.FromResult(new ResultadoTransaccionDTO { Exitoso = true, Mensaje = "Pago aprobado" });

        public bool ValidarAlgoritmoLuhn(string numeroTarjeta) => true;
        public string DetectarMarcaTarjeta(string numeroTarjeta) => "Visa";
    }

    private sealed class ThrowingInvoiceService : IInvoiceService
    {
        public Task<(List<Invoice> Items, int TotalCount)> GetAllAsync(string? searchTerm, int page, int pageSize) => throw new NotImplementedException();
        public Task<Invoice?> GetByIdAsync(int id) => throw new NotImplementedException();
        public Task<Invoice> CreateAsync(Invoice invoice) => throw new InvalidOperationException("Error simulado al facturar");
        public Task<Invoice?> UpdateAsync(Invoice invoice) => throw new NotImplementedException();
        public Task<bool> DeleteAsync(int id) => throw new NotImplementedException();
        public Task<List<Order>> GetOrdersAvailableForInvoicingAsync() => throw new NotImplementedException();
    }

    private sealed class StubTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public bool ErrorLogged { get; private set; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel >= LogLevel.Error)
            {
                ErrorLogged = true;
            }
        }
    }
}
