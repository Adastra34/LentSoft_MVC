using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Services;
using Xunit;

namespace LentSoft.Tests;

public class SalesOrderServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LentSoftDbContext _context;
    private readonly SalesOrderService _salesOrderService;

    public SalesOrderServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LentSoftDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new LentSoftDbContext(options);
        _context.Database.EnsureCreated();

        _salesOrderService = new SalesOrderService(_context, NullLogger<SalesOrderService>.Instance);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task CrearPedido_StockSuficiente_DescuentaStockYRegistraMovimientoYAuditoria()
    {
        // Arrange
        var product = new Product
        {
            Nombre = "Montura Ray-Ban Aviator",
            Precio = 350000m,
            Stock = 10,
            Activo = true
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var pedido = new SalesOrder
        {
            NumeroPedido = "PED-2026-0001",
            ClienteNombre = "Juan Perez",
            ProductoId = product.Id,
            Cantidad = 3,
            PrecioUnitario = 9999m // Debería recalcularse al precio real del producto
        };

        // Act
        var resultado = await _salesOrderService.CreateAsync(pedido, "test.user");

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(350000m, resultado.PrecioUnitario);
        Assert.Equal(1050000m, resultado.Total);

        var productoActualizado = await _context.Products.FindAsync(product.Id);
        Assert.Equal(7, productoActualizado!.Stock);

        // Movimiento de salida registrado
        var movimiento = await _context.InventoryMovements.FirstOrDefaultAsync(m => m.ProductId == product.Id);
        Assert.NotNull(movimiento);
        Assert.Equal("Salida", movimiento.Tipo);
        Assert.Equal(3, movimiento.Cantidad);

        // Auditoría registrada
        var auditoria = await _context.AuditoriasVentas.FirstOrDefaultAsync(a => a.TipoEntidad == "SalesOrder" && a.EntidadId == resultado.Id);
        Assert.NotNull(auditoria);
        Assert.Equal("CreacionPedidoVenta", auditoria.Accion);
    }

    [Fact]
    public async Task CrearPedido_StockInsuficiente_LanzaInvalidOperationException()
    {
        // Arrange
        var product = new Product
        {
            Nombre = "Lente de Contacto Acuvue",
            Precio = 120000m,
            Stock = 2,
            Activo = true
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var pedido = new SalesOrder
        {
            NumeroPedido = "PED-2026-0002",
            ClienteNombre = "Maria Lopez",
            ProductoId = product.Id,
            Cantidad = 5 // Mayor que el stock disponible
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _salesOrderService.CreateAsync(pedido, "test.user"));

        Assert.Contains("Stock insuficiente", ex.Message);

        var productoActualizado = await _context.Products.FindAsync(product.Id);
        Assert.Equal(2, productoActualizado!.Stock);
    }

    [Fact]
    public async Task CrearPedido_ProductoInexistente_LanzaInvalidOperationException()
    {
        // Arrange
        var pedido = new SalesOrder
        {
            NumeroPedido = "PED-2026-0003",
            ClienteNombre = "Carlos Ruiz",
            ProductoId = 99999, // No existe
            Cantidad = 1
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _salesOrderService.CreateAsync(pedido, "test.user"));
    }

    [Fact]
    public async Task EditarPedido_IncrementaCantidad_AjustaStockConDiferencia()
    {
        // Arrange
        var product = new Product
        {
            Nombre = "Gafas de Sol Oakley",
            Precio = 200000m,
            Stock = 10,
            Activo = true
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var pedido = new SalesOrder
        {
            NumeroPedido = "PED-2026-0004",
            ClienteNombre = "Andrea Gómez",
            ProductoId = product.Id,
            Cantidad = 2
        };
        var creado = await _salesOrderService.CreateAsync(pedido, "test.user");
        Assert.Equal(8, (await _context.Products.FindAsync(product.Id))!.Stock);

        // Act - Modificar cantidad de 2 a 5 (+3 requeridos)
        creado.Cantidad = 5;
        await _salesOrderService.EditAsync(creado, "test.user");

        // Assert
        var productoActualizado = await _context.Products.FindAsync(product.Id);
        Assert.Equal(5, productoActualizado!.Stock); // 8 - 3 = 5
        Assert.Equal(1000000m, creado.Total);
    }

    [Fact]
    public async Task EliminarPedido_RestauraStockEnInventario()
    {
        // Arrange
        var product = new Product
        {
            Nombre = "Estuche Rígido",
            Precio = 30000m,
            Stock = 15,
            Activo = true
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var pedido = new SalesOrder
        {
            NumeroPedido = "PED-2026-0005",
            ClienteNombre = "Felipe Rios",
            ProductoId = product.Id,
            Cantidad = 4
        };
        var creado = await _salesOrderService.CreateAsync(pedido, "test.user");
        Assert.Equal(11, (await _context.Products.FindAsync(product.Id))!.Stock);

        // Act
        var eliminado = await _salesOrderService.DeleteAsync(creado.Id, "test.user");

        // Assert
        Assert.True(eliminado);
        var productoActualizado = await _context.Products.FindAsync(product.Id);
        Assert.Equal(15, productoActualizado!.Stock); // 11 + 4 = 15
    }
}
