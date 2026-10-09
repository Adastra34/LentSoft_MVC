using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Services;
using Xunit;

namespace LentSoft.Tests;

public class InventoryServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LentSoftDbContext _context;
    private readonly InventoryService _inventoryService;
    private readonly InventorySettings _settings;

    public InventoryServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LentSoftDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new LentSoftDbContext(options);
        _context.Database.EnsureCreated();

        _settings = new InventorySettings { MaxStock = 85, LowStockThreshold = 10 };
        var optionsWrapper = Options.Create(_settings);
        var httpContextAccessor = new HttpContextAccessor();

        _inventoryService = new InventoryService(_context, optionsWrapper, httpContextAccessor);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task DisminuirStock_SalidaMayorAlStock_ThrowsInvalidOperationException()
    {
        // Arrange
        var product = new Product
        {
            Nombre = "Lentes Prueba Salida",
            Precio = 100000m,
            Stock = 5,
            Activo = true
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _inventoryService.DisminuirStockAsync(product.Id, 10, InventoryConstants.MovementTypes.Salida));

        Assert.Contains("Stock insuficiente", ex.Message);

        // Verify stock was not altered in DB
        var dbProduct = await _context.Products.FindAsync(product.Id);
        Assert.NotNull(dbProduct);
        Assert.Equal(5, dbProduct.Stock);

        // Verify no movement was saved
        var movements = await _context.InventoryMovements.Where(m => m.ProductId == product.Id).ToListAsync();
        Assert.Empty(movements);
    }

    [Fact]
    public async Task AumentarStock_EntradaQueExcedeElMaximo_ThrowsInvalidOperationException()
    {
        // Arrange
        var product = new Product
        {
            Nombre = "Lentes Prueba Maximo",
            Precio = 150000m,
            Stock = 80,
            Activo = true
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Act & Assert (80 + 10 = 90 > 85)
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _inventoryService.AumentarStockAsync(product.Id, 10, InventoryConstants.MovementTypes.Entrada));

        Assert.Contains("no puede superar las 85 unidades", ex.Message);

        // Verify stock was not altered
        var dbProduct = await _context.Products.FindAsync(product.Id);
        Assert.NotNull(dbProduct);
        Assert.Equal(80, dbProduct.Stock);

        // Verify no movement was saved
        var movements = await _context.InventoryMovements.Where(m => m.ProductId == product.Id).ToListAsync();
        Assert.Empty(movements);
    }

    [Fact]
    public async Task DisminuirStock_AutoInactivacionEnCero_SetsActivoFalse()
    {
        // Arrange
        var product = new Product
        {
            Nombre = "Lentes Agotamiento",
            Precio = 200000m,
            Stock = 4,
            Activo = true
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Act
        var updated = await _inventoryService.DisminuirStockAsync(product.Id, 4, InventoryConstants.MovementTypes.Salida);

        // Assert
        Assert.Equal(0, updated.Stock);
        Assert.False(updated.Activo);

        var dbProduct = await _context.Products.FindAsync(product.Id);
        Assert.NotNull(dbProduct);
        Assert.Equal(0, dbProduct.Stock);
        Assert.False(dbProduct.Activo);

        // Movement audit check
        var movement = await _context.InventoryMovements.FirstOrDefaultAsync(m => m.ProductId == product.Id);
        Assert.NotNull(movement);
        Assert.Equal(InventoryConstants.MovementTypes.Salida, movement.Tipo);
        Assert.Equal(4, movement.Cantidad);
    }

    [Fact]
    public async Task AumentarStock_ReactivacionCuandoStockMayorACero_SetsActivoTrue()
    {
        // Arrange
        var product = new Product
        {
            Nombre = "Lentes Reactivacion",
            Precio = 220000m,
            Stock = 0,
            Activo = false
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Act
        var updated = await _inventoryService.AumentarStockAsync(product.Id, 12, InventoryConstants.MovementTypes.Entrada);

        // Assert
        Assert.Equal(12, updated.Stock);
        Assert.True(updated.Activo);

        var dbProduct = await _context.Products.FindAsync(product.Id);
        Assert.NotNull(dbProduct);
        Assert.Equal(12, dbProduct.Stock);
        Assert.True(dbProduct.Activo);

        // Movement audit check
        var movement = await _context.InventoryMovements.FirstOrDefaultAsync(m => m.ProductId == product.Id);
        Assert.NotNull(movement);
        Assert.Equal(InventoryConstants.MovementTypes.Entrada, movement.Tipo);
        Assert.Equal(12, movement.Cantidad);
    }

    [Fact]
    public async Task ProcesarRecepcionPedidoProveedor_SumaStockUnaSolaVez_NoDuplica()
    {
        // Arrange
        var supplier = new Supplier
        {
            Id = "PROV-TEST-001",
            Nombre = "Distribuidor Óptico SAS",
            TipoProductos = "Monturas",
            Telefono = "3001234567",
            Correo = "contacto@distribuidor.com",
            Activo = true
        };
        _context.Suppliers.Add(supplier);

        var product = new Product
        {
            Nombre = "Montura Pedido Prov",
            Precio = 300000m,
            Stock = 10,
            Activo = true,
            SupplierId = supplier.Id
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var order = new SupplierOrder
        {
            NumeroPedido = "PED-TEST-1001",
            SupplierId = supplier.Id,
            ProductId = product.Id,
            Cantidad = 15,
            PrecioUnitario = 120000m,
            Total = 1800000m,
            Estado = InventoryConstants.SupplierOrderStates.Pendiente,
            Activo = true,
            Fecha = DateTime.UtcNow
        };
        _context.SupplierOrders.Add(order);
        await _context.SaveChangesAsync();

        // Act 1: Primera recepción
        var receivedOrder = await _inventoryService.ProcesarRecepcionPedidoProveedorAsync(order.Id);

        // Assert 1: Estado recibido, stock incrementado en 15 (10 + 15 = 25)
        Assert.Equal(InventoryConstants.SupplierOrderStates.Recibido, receivedOrder.Estado);
        var productAfterFirst = await _context.Products.FindAsync(product.Id);
        Assert.NotNull(productAfterFirst);
        Assert.Equal(25, productAfterFirst.Stock);

        var movementsCount = await _context.InventoryMovements.CountAsync(m => m.ProductId == product.Id);
        Assert.Equal(1, movementsCount);

        // Act 2: Intento de segunda recepción del mismo pedido
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _inventoryService.ProcesarRecepcionPedidoProveedorAsync(order.Id));

        Assert.Contains("ya fue marcado como recibido previamente", ex.Message);

        // Assert 2: El stock NO debe cambiar ni duplicarse
        var productAfterSecond = await _context.Products.FindAsync(product.Id);
        Assert.NotNull(productAfterSecond);
        Assert.Equal(25, productAfterSecond.Stock);

        var finalMovementsCount = await _context.InventoryMovements.CountAsync(m => m.ProductId == product.Id);
        Assert.Equal(1, finalMovementsCount);
    }

    [Fact]
    public async Task Concurrencia_DbUpdateConcurrencyException_RethrowsFriendlyMessage()
    {
        // Arrange
        var product = new Product
        {
            Nombre = "Producto Concurrencia",
            Precio = 180000m,
            Stock = 20,
            Activo = true
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Creamos un segundo DbContext apuntando a la misma base de datos en memoria
        var options = new DbContextOptionsBuilder<LentSoftDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var secondContext = new LentSoftDbContext(options);
        var secondProduct = await secondContext.Products.FindAsync(product.Id);
        Assert.NotNull(secondProduct);

        // Modificamos y guardamos en el segundo contexto (actualizando RowVersion)
        secondProduct.Stock = 30;
        await secondContext.SaveChangesAsync();

        // Act & Assert: Intentamos actualizar en el primer contexto donde RowVersion está desactualizada
        // Simulando conflicto de concurrencia asignando OriginalValue obsoleto
        _context.Entry(product).Property(p => p.RowVersion).OriginalValue = new byte[] { 1, 2, 3 };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _inventoryService.AumentarStockAsync(product.Id, 5, InventoryConstants.MovementTypes.Entrada));

        Assert.Equal("El producto fue modificado por otro usuario, vuelve a intentarlo.", ex.Message);
    }
}
