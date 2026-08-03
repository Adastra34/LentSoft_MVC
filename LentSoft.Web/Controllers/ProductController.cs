using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Models.ViewModels;
using LentSoft.Web.Services;

namespace LentSoft.Web.Controllers;

public class ProductController : Controller
{
    private readonly IProductService _productService;
    private readonly IFavoriteService _favoriteService;

    public ProductController(IProductService productService, IFavoriteService favoriteService)
    {
        _productService = productService;
        _favoriteService = favoriteService;
    }

    /// <summary>
    /// Public store page — migrated from Views/tienda.html
    /// </summary>
    public async Task<IActionResult> Tienda(string? categoria, string? marca, string? rangoPrecio)
    {
        var products = await _productService.FilterAsync(categoria, marca, rangoPrecio);
        var categorias = await _productService.GetCategoriasAsync();
        var marcas = await _productService.GetMarcasAsync();
        var featured = await _productService.GetFeaturedAsync();

        // Load user favorites if authenticated
        var favoriteIds = new HashSet<int>();
        if (User.Identity?.IsAuthenticated == true)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out var userId))
            {
                favoriteIds = await _favoriteService.GetUserFavoriteProductIdsAsync(userId);
            }
        }

        var viewModel = new ProductListViewModel
        {
            Products = products,
            FeaturedProducts = featured,
            Categoria = categoria,
            Marca = marca,
            RangoPrecio = rangoPrecio,
            Categorias = categorias,
            Marcas = marcas,
            FavoriteProductIds = favoriteIds
        };

        return View(viewModel);
    }

    /// <summary>
    /// Toggle favorite via AJAX — returns JSON { isFavorite: true/false }
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> ToggleFavorite(int productId)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        var isFavorite = await _favoriteService.ToggleFavoriteAsync(userId, productId);
        return Json(new { isFavorite });
    }

    /// <summary>
    /// Admin: Create product (POST) — migrated from ProductController.js create()
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product product)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Datos del producto no válidos.";
            return RedirectToAction("Admin", "Dashboard");
        }

        await _productService.CreateAsync(product);
        TempData["SuccessMessage"] = "Producto creado exitosamente.";
        return RedirectToAction("Admin", "Dashboard");
    }

    /// <summary>
    /// Admin: Delete product — migrated from ProductController.js delete()
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _productService.DeleteAsync(id);
        TempData["SuccessMessage"] = "Producto eliminado exitosamente.";
        return RedirectToAction("Admin", "Dashboard");
    }

    /// <summary>
    /// Admin: Get product data for editing (GET)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product == null)
        {
            return NotFound();
        }
        return Json(product);
    }

    /// <summary>
    /// Admin: Edit product (POST)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Product product)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Datos del producto no válidos.";
            return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "productos" });
        }

        var updated = await _productService.UpdateAsync(product.Id, product);
        if (updated == null)
        {
            TempData["ErrorMessage"] = "Producto no encontrado.";
            return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "productos" });
        }

        TempData["SuccessMessage"] = "Producto actualizado exitosamente.";
        return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "productos" });
    }

    /// <summary>
    /// GET /Product/Details/{id}
    /// </summary>
    public async Task<IActionResult> Details(int id)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product == null)
        {
            return NotFound();
        }

        var isFavorite = false;
        var isAuthenticated = User.Identity?.IsAuthenticated == true;
        if (isAuthenticated)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out var userId))
            {
                var favoriteIds = await _favoriteService.GetUserFavoriteProductIdsAsync(userId);
                isFavorite = favoriteIds.Contains(id);
            }
        }

        var viewModel = new ProductDetailsViewModel
        {
            Product = product,
            IsFavorite = isFavorite,
            IsAuthenticated = isAuthenticated
        };

        return View(viewModel);
    }

    /// <summary>
    /// GET /Product/MuestraMontura/{id?}
    /// </summary>
    public async Task<IActionResult> MuestraMontura(int? id)
    {
        var glasses = await _productService.GetGafasAsync();
        Product? preselected = null;

        if (id.HasValue)
        {
            preselected = glasses.FirstOrDefault(g => g.Id == id.Value);
            if (preselected == null)
            {
                preselected = await _productService.GetByIdAsync(id.Value);
            }
        }

        var viewModel = new MuestraMonturaViewModel
        {
            Gafas = glasses,
            PreselectedProduct = preselected
        };

        return View(viewModel);
    }
}
