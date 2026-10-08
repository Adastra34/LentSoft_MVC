using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using LentSoft.Web.Controllers;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Models.ViewModels;
using LentSoft.Web.Services;
using Xunit;

namespace LentSoft.Tests;

public class ProductControllerTests
{
    private class FakeProductService : IProductService
    {
        public List<Product> Products { get; } = new();

        public Task<List<Product>> GetAllAsync(bool includeInactive = false) => Task.FromResult(Products.FindAll(p => includeInactive || p.Activo));
        public Task<List<Product>> GetActiveAsync() => Task.FromResult(Products.FindAll(p => p.Activo));
        public Task<Product?> GetByIdAsync(int id) => Task.FromResult(Products.Find(p => p.Id == id));
        public Task<Product> CreateAsync(Product product) { Products.Add(product); return Task.FromResult(product); }
        public Task<Product?> UpdateAsync(int id, Product product) => Task.FromResult<Product?>(product);
        public Task<bool> DeleteAsync(int id) => Task.FromResult(true);
        public Task<bool> ReactivateAsync(int id) => Task.FromResult(true);
        public Task<List<Product>> GetGafasAsync() => Task.FromResult(Products.FindAll(p => p.Activo));
        public Task<List<Product>> GetFeaturedAsync() => Task.FromResult(new List<Product>());
        public Task<List<Product>> GetBestSellersAsync(int count = 3) => Task.FromResult(new List<Product>());
        public Task<List<Product>> FilterAsync(string? categoria, string? marca, string? rangoPrecio) => Task.FromResult(Products);
        public Task<List<string>> GetCategoriasAsync() => Task.FromResult(new List<string>());
        public Task<List<string>> GetMarcasAsync() => Task.FromResult(new List<string>());
    }

    private class FakeFavoriteService : IFavoriteService
    {
        public Task<bool> ToggleFavoriteAsync(int userId, int productId) => Task.FromResult(true);
        public Task<bool> IsFavoriteAsync(int userId, int productId) => Task.FromResult(false);
        public Task<List<Product>> GetFavoritesByUserIdAsync(int userId) => Task.FromResult(new List<Product>());
        public Task<HashSet<int>> GetUserFavoriteProductIdsAsync(int userId) => Task.FromResult(new HashSet<int>());
    }

    private class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "LentSoft.Web";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    [Fact]
    public async Task MuestraMontura_ReturnsViewWithPreselectedProductAndGlassesList()
    {
        // Arrange
        var fakeProductService = new FakeProductService();
        var fakeFavService = new FakeFavoriteService();
        var fakeEnv = new FakeWebHostEnvironment();

        var g1 = new Product { Id = 10, Nombre = "Montura Aviador", Precio = 500000m, Activo = true };
        var g2 = new Product { Id = 20, Nombre = "Montura Wayfarer", Precio = 600000m, Activo = true };
        fakeProductService.Products.Add(g1);
        fakeProductService.Products.Add(g2);

        var controller = new ProductController(fakeProductService, fakeFavService, fakeEnv);

        // Act
        var result = await controller.MuestraMontura(10);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<MuestraMonturaViewModel>(viewResult.Model);

        Assert.Equal(2, model.Gafas.Count);
        Assert.NotNull(model.PreselectedProduct);
        Assert.Equal(10, model.PreselectedProduct.Id);
        Assert.Equal(500000m, model.PreselectedProduct.Precio);
    }

    [Fact]
    public async Task CheckStock_ReturnsJsonWithStockAvailability()
    {
        // Arrange
        var fakeProductService = new FakeProductService();
        var fakeFavService = new FakeFavoriteService();
        var fakeEnv = new FakeWebHostEnvironment();

        fakeProductService.Products.Add(new Product { Id = 5, Nombre = "Gafas de Sol", Precio = 200000m, Stock = 8 });

        var controller = new ProductController(fakeProductService, fakeFavService, fakeEnv);

        // Act (Solicitar 5 cuando hay 8 => suficiente)
        var resultSufficient = await controller.CheckStock(5, 5);
        // Act (Solicitar 10 cuando hay 8 => insuficiente)
        var resultInsufficient = await controller.CheckStock(5, 10);

        // Assert
        var jsonSufficient = Assert.IsType<JsonResult>(resultSufficient);
        var jsonInsufficient = Assert.IsType<JsonResult>(resultInsufficient);

        Assert.NotNull(jsonSufficient.Value);
        Assert.NotNull(jsonInsufficient.Value);
    }

    [Fact]
    public async Task Details_ReturnsViewWithProductAndCorrectPrice()
    {
        // Arrange
        var fakeProductService = new FakeProductService();
        var fakeFavService = new FakeFavoriteService();
        var fakeEnv = new FakeWebHostEnvironment();

        var product = new Product
        {
            Id = 15,
            Nombre = "Montura Ray-Ban Especial",
            Precio = 750000m,
            PrecioDescuento = 600000m,
            Stock = 12,
            Activo = true,
            EscalaOverlay = 2.35m,
            OffsetXOverlay = 0.02m,
            OffsetYOverlay = 0.14m
        };
        fakeProductService.Products.Add(product);

        var controller = new ProductController(fakeProductService, fakeFavService, fakeEnv);

        // Act
        var result = await controller.Details(15);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ProductDetailsViewModel>(viewResult.Model);

        Assert.NotNull(model.Product);
        Assert.Equal(15, model.Product.Id);
        Assert.Equal("Montura Ray-Ban Especial", model.Product.Nombre);
        Assert.Equal(750000m, model.Product.Precio);
        Assert.Equal(600000m, model.Product.PrecioDescuento);
        Assert.Equal(2.35m, model.Product.EscalaOverlay);
        Assert.Equal(0.02m, model.Product.OffsetXOverlay);
        Assert.Equal(0.14m, model.Product.OffsetYOverlay);
    }

    [Fact]
    public async Task Details_ReturnsNotFound_WhenProductDoesNotExist()
    {
        // Arrange
        var fakeProductService = new FakeProductService();
        var fakeFavService = new FakeFavoriteService();
        var fakeEnv = new FakeWebHostEnvironment();

        var controller = new ProductController(fakeProductService, fakeFavService, fakeEnv);

        // Act
        var result = await controller.Details(999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}

