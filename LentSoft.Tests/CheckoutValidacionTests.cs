using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Controllers;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Models.ViewModels;
using LentSoft.Web.Services;
using Xunit;

namespace LentSoft.Tests;

public class CheckoutValidacionTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LentSoftDbContext _context;

    public CheckoutValidacionTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LentSoftDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new LentSoftDbContext(options);
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private class FakeCartService : ICartService
    {
        public Cart? CartToReturn { get; set; }
        public Task<CartItem> AddToCartAsync(int userId, int productId, int cantidad) => throw new NotImplementedException();
        public Task<Cart?> GetCartAsync(int userId) => Task.FromResult(CartToReturn);
        public Task<CartItem?> UpdateQuantityAsync(int userId, int productId, int cantidad) => throw new NotImplementedException();
        public Task<bool> RemoveItemAsync(int userId, int productId) => throw new NotImplementedException();
        public Task<bool> ClearCartAsync(int userId) => Task.FromResult(true);
    }

    private class FakePasarelaPagoService : IPasarelaPagoService
    {
        public int LlamadasProcesarPago { get; private set; }
        public TarjetaPagoDTO? UltimoDtoRecibido { get; private set; }
        public ResultadoTransaccionDTO ResultadoAConfigurar { get; set; } = new() { Exitoso = true };

        public Task<ResultadoTransaccionDTO> ProcesarPagoTarjetaAsync(TarjetaPagoDTO dto)
        {
            LlamadasProcesarPago++;
            UltimoDtoRecibido = dto;
            return Task.FromResult(ResultadoAConfigurar);
        }

        public bool ValidarAlgoritmoLuhn(string numeroTarjeta)
        {
            if (string.IsNullOrWhiteSpace(numeroTarjeta)) return false;

            int sum = 0;
            bool alternate = false;
            for (int i = numeroTarjeta.Length - 1; i >= 0; i--)
            {
                int digit = numeroTarjeta[i] - '0';
                if (alternate)
                {
                    digit *= 2;
                    if (digit > 9) digit -= 9;
                }
                sum += digit;
                alternate = !alternate;
            }
            return sum % 10 == 0;
        }

        public string DetectarMarcaTarjeta(string numeroTarjeta) => "Visa";
    }

    private class FakeInventoryService : IInventoryService
    {
        public int MaxStock => 1000;
        public int LowStockThreshold => 5;
        public Task<Product> AumentarStockAsync(int productId, int cantidad, string motivo, string? responsable = null, string? nota = null, string? referencia = null, string? ubicacion = null) => throw new NotImplementedException();
        public Task<Product> DisminuirStockAsync(int productId, int cantidad, string motivo, string? responsable = null, string? nota = null, string? referencia = null, string? ubicacion = null) => throw new NotImplementedException();
        public Task<SupplierOrder> ProcesarRecepcionPedidoProveedorAsync(int supplierOrderId, string? responsable = null) => throw new NotImplementedException();
        public Task<SupplierOrder> ProcesarRecepcionParcialPedidoProveedorAsync(int supplierOrderId, int cantidadRecibida, string? responsable = null) => throw new NotImplementedException();
        public Task<SalesOrder> ProcesarPedidoVentaAsync(SalesOrder order, int? productId = null, string? sku = null, string? responsable = null) => throw new NotImplementedException();
        public Task RegistrarMovimientoAsync(int productId, string tipo, int cantidad, string? responsable = null, string? motivo = null, string? nota = null, string? referencia = null) => throw new NotImplementedException();
    }

    private class FakeTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private CheckoutController CrearController(
        FakeCartService cartService,
        FakePasarelaPagoService pasarelaService,
        int userId = 42)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            }, "TestAuth"))
        };

        var controller = new CheckoutController(cartService, _context, pasarelaService, new FakeInventoryService())
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new FakeTempDataProvider())
        };

        return controller;
    }

    private static Cart CrearCarritoConStock(int stockDisponible, int cantidadSolicitada, string nombreProducto = "Montura Ray-Ban")
    {
        var product = new Product
        {
            Id = 1,
            Nombre = nombreProducto,
            Precio = 250000m,
            Stock = stockDisponible,
            Activo = true
        };

        return new Cart
        {
            Id = 1,
            UserId = 42,
            CartItems = new List<CartItem>
            {
                new CartItem
                {
                    Id = 1,
                    CartId = 1,
                    ProductId = product.Id,
                    Product = product,
                    Cantidad = cantidadSolicitada,
                    PrecioUnitario = product.Precio
                }
            }
        };
    }

    [Fact]
    public async Task ConfirmarPago_LuhnInvalido_AgregaErrorDeCampoYNoLlamaPasarela()
    {
        var cartService = new FakeCartService { CartToReturn = CrearCarritoConStock(10, 1) };
        var pasarelaService = new FakePasarelaPagoService();
        var controller = CrearController(cartService, pasarelaService);

        var model = new CheckoutViewModel
        {
            NumeroTarjeta = "1379 7946 3684 3647",
            NombreTarjeta = "Juan Perez",
            Expiracion = "12/30",
            Cvv = "123",
            DireccionEnvio = "Calle 123 #45-67"
        };

        var result = await controller.ConfirmarPago(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", viewResult.ViewName);
        Assert.Equal(0, pasarelaService.LlamadasProcesarPago);
        Assert.True(controller.ModelState.ContainsKey(nameof(CheckoutViewModel.NumeroTarjeta)));
        var error = controller.ModelState[nameof(CheckoutViewModel.NumeroTarjeta)]!.Errors.Single().ErrorMessage;
        Assert.Equal("El número de tarjeta no es válido. Revisa que esté completo y escrito correctamente.", error);
        Assert.DoesNotContain("Luhn", error, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pasarela", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConfirmarPago_TarjetaVencida_AgregaErrorDeCampoYNoLlamaPasarela()
    {
        var cartService = new FakeCartService { CartToReturn = CrearCarritoConStock(10, 1) };
        var pasarelaService = new FakePasarelaPagoService();
        var controller = CrearController(cartService, pasarelaService);

        var model = new CheckoutViewModel
        {
            NumeroTarjeta = "4111 1111 1111 1111",
            NombreTarjeta = "Juan Perez",
            Expiracion = "01/22",
            Cvv = "123",
            DireccionEnvio = "Calle 123 #45-67"
        };

        var result = await controller.ConfirmarPago(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", viewResult.ViewName);
        Assert.Equal(0, pasarelaService.LlamadasProcesarPago);
        Assert.True(controller.ModelState.ContainsKey(nameof(CheckoutViewModel.Expiracion)));
        var error = controller.ModelState[nameof(CheckoutViewModel.Expiracion)]!.Errors.Single().ErrorMessage;
        Assert.Equal("La tarjeta está vencida. Usa una tarjeta con fecha vigente.", error);
    }

    [Fact]
    public async Task ConfirmarPago_StockInsuficiente_NoCobraNiCreaOrden()
    {
        var cartService = new FakeCartService { CartToReturn = CrearCarritoConStock(2, 5, "Lentes Aviador") };
        var pasarelaService = new FakePasarelaPagoService();
        var controller = CrearController(cartService, pasarelaService);
        var ordenesIniciales = await _context.Orders.CountAsync();

        var model = new CheckoutViewModel
        {
            NumeroTarjeta = "4111 1111 1111 1111",
            NombreTarjeta = "Juan Perez",
            Expiracion = "12/30",
            Cvv = "123",
            DireccionEnvio = "Calle 123 #45-67"
        };

        var result = await controller.ConfirmarPago(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", viewResult.ViewName);
        Assert.Equal(0, pasarelaService.LlamadasProcesarPago);
        Assert.Equal(ordenesIniciales, await _context.Orders.CountAsync());
        var error = controller.ModelState[""]!.Errors.Single().ErrorMessage;
        Assert.Equal("No hay stock suficiente de Lentes Aviador. Disponible: 2.", error);
    }

    [Fact]
    public async Task ConfirmarPago_RechazoBanco_MuestraMensajeAmigableSinDuplicarToastYResponsablePorUsuario()
    {
        var cartService = new FakeCartService { CartToReturn = CrearCarritoConStock(10, 1) };
        var pasarelaService = new FakePasarelaPagoService
        {
            ResultadoAConfigurar = new ResultadoTransaccionDTO
            {
                Exitoso = false,
                Mensaje = "Transacción rechazada por el banco emisor (Fondos Insuficientes / Tarjeta de prueba rechazada)."
            }
        };
        var controller = CrearController(cartService, pasarelaService, userId: 77);

        var model = new CheckoutViewModel
        {
            NumeroTarjeta = "4000 0000 0000 0119",
            NombreTarjeta = "Juan Perez",
            Expiracion = "12/30",
            Cvv = "123",
            DireccionEnvio = "Calle 123 #45-67"
        };

        var result = await controller.ConfirmarPago(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", viewResult.ViewName);
        Assert.Equal(1, pasarelaService.LlamadasProcesarPago);
        Assert.Equal("Cliente #77 (Checkout Web)", pasarelaService.UltimoDtoRecibido?.Responsable);
        Assert.False(controller.TempData.ContainsKey("ErrorMessage"));
        var error = controller.ModelState[""]!.Errors.Single().ErrorMessage;
        Assert.Equal("Tu banco rechazó el pago. Intenta con otra tarjeta.", error);
    }
}
