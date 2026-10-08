using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Services;
using Xunit;

namespace LentSoft.Tests;

public class ProductServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LentSoftDbContext _context;
    private readonly ProductService _service;

    public ProductServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LentSoftDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new LentSoftDbContext(options);
        _context.Database.EnsureCreated();

        _service = new ProductService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsProductWithCorrectPrice()
    {
        // Arrange
        var product = new Product
        {
            Nombre = "Montura Test Aviador",
            Precio = 800000m,
            PrecioDescuento = 650000m,
            Categoria = "monturas",
            Stock = 20,
            Activo = true,
            EscalaOverlay = 2.45m,
            OffsetXOverlay = 0.05m,
            OffsetYOverlay = 0.15m
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(product.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Montura Test Aviador", result.Nombre);
        Assert.Equal(800000m, result.Precio);
        Assert.Equal(650000m, result.PrecioDescuento);
        Assert.Equal(2.45m, result.EscalaOverlay);
        Assert.Equal(0.05m, result.OffsetXOverlay);
        Assert.Equal(0.15m, result.OffsetYOverlay);
    }

    [Fact]
    public async Task GetGafasAsync_FiltersOnlyGlassesCategoriesAndActiveProducts()
    {
        // Arrange
        _context.Products.AddRange(
            new Product { Nombre = "Gafas Sol 1", Categoria = "lentes-sol", Activo = true, Precio = 300000m, Stock = 5 },
            new Product { Nombre = "Montura 2", Categoria = "monturas", Activo = true, Precio = 400000m, Stock = 5 },
            new Product { Nombre = "Lente Graduado 3", Categoria = "lentes-graduados", Activo = true, Precio = 350000m, Stock = 5 },
            new Product { Nombre = "Accesorio Invalido", Categoria = "accesorios", Activo = true, Precio = 50000m, Stock = 10 },
            new Product { Nombre = "Montura Inactiva", Categoria = "monturas", Activo = false, Precio = 400000m, Stock = 5 }
        );
        await _context.SaveChangesAsync();

        // Act
        var gafas = await _service.GetGafasAsync();

        // Assert
        Assert.Contains(gafas, g => g.Nombre == "Gafas Sol 1");
        Assert.Contains(gafas, g => g.Nombre == "Montura 2");
        Assert.Contains(gafas, g => g.Nombre == "Lente Graduado 3");
        Assert.DoesNotContain(gafas, g => g.Nombre == "Accesorio Invalido");
        Assert.DoesNotContain(gafas, g => g.Nombre == "Montura Inactiva");
    }

    [Fact]
    public async Task UpdateAsync_PersistsOverlayAdjustments()
    {
        // Arrange
        var original = new Product
        {
            Nombre = "Montura Ajustable",
            Categoria = "monturas",
            Precio = 450000m,
            Stock = 15,
            Activo = true,
            EscalaOverlay = 2.30m,
            OffsetXOverlay = 0.00m,
            OffsetYOverlay = 0.12m
        };
        _context.Products.Add(original);
        await _context.SaveChangesAsync();

        var updated = new Product
        {
            Id = original.Id,
            Nombre = "Montura Ajustada Calibrada",
            Categoria = "monturas",
            Precio = 480000m,
            Stock = 15,
            Activo = true,
            EscalaOverlay = 2.60m,
            OffsetXOverlay = -0.05m,
            OffsetYOverlay = 0.20m
        };

        // Act
        var result = await _service.UpdateAsync(original.Id, updated);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2.60m, result.EscalaOverlay);
        Assert.Equal(-0.05m, result.OffsetXOverlay);
        Assert.Equal(0.20m, result.OffsetYOverlay);
        Assert.Equal(480000m, result.Precio);
    }
}
