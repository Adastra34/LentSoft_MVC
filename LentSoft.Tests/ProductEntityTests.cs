using LentSoft.Web.Models.Entities;
using Xunit;

namespace LentSoft.Tests;

public class ProductEntityTests
{
    [Fact]
    public void GetFinalPrice_ReturnsRegularPrice_WhenNoDiscount()
    {
        // Arrange
        var product = new Product
        {
            Id = 1,
            Nombre = "Montura Elegante",
            Precio = 500000m,
            PrecioDescuento = null
        };

        // Act
        var finalPrice = product.GetFinalPrice();

        // Assert
        Assert.Equal(500000m, finalPrice);
    }

    [Fact]
    public void GetFinalPrice_ReturnsDiscountPrice_WhenDiscountIsLowerThanRegularPrice()
    {
        // Arrange
        var product = new Product
        {
            Id = 2,
            Nombre = "Montura Promocional",
            Precio = 600000m,
            PrecioDescuento = 450000m
        };

        // Act
        var finalPrice = product.GetFinalPrice();

        // Assert
        Assert.Equal(450000m, finalPrice);
    }

    [Fact]
    public void GetDiscountPercentage_CalculatesCorrectPercentage()
    {
        // Arrange (Precio: 1,000,000, Descuento: 750,000 => 25% OFF)
        var product = new Product
        {
            Id = 3,
            Nombre = "Lentes Ray-Ban",
            Precio = 1000000m,
            PrecioDescuento = 750000m
        };

        // Act
        var percent = product.GetDiscountPercentage();

        // Assert
        Assert.Equal(25, percent);
    }

    [Fact]
    public void DefaultOverlayAdjustmentFields_HaveSensibleDefaultValues()
    {
        // Arrange & Act
        var product = new Product
        {
            Nombre = "Montura Nueva"
        };

        // Assert
        Assert.Equal(2.30m, product.EscalaOverlay);
        Assert.Equal(0.00m, product.OffsetXOverlay);
        Assert.Equal(0.12m, product.OffsetYOverlay);
    }

    [Fact]
    public void HasStock_ReturnsTrue_WhenStockIsSufficient()
    {
        // Arrange
        var product = new Product
        {
            Stock = 10
        };

        // Act & Assert
        Assert.True(product.HasStock(5));
        Assert.True(product.HasStock(10));
        Assert.False(product.HasStock(11));
    }
}
