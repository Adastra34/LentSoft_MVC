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

        try
        {
            var isFavorite = await _favoriteService.ToggleFavoriteAsync(userId, productId);
            return Json(new { success = true, isFavorite });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Admin: Create product (POST) — migrated from ProductController.js create()
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product product)
    {
        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json");

        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Datos del producto no válidos.";
            if (isAjax) return Json(new { success = false, message = firstError });
            TempData["ErrorMessage"] = firstError;
            return RedirectToAction("Admin", "Dashboard");
        }

        try
        {
            var created = await _productService.CreateAsync(product);
            if (isAjax) return Json(new { success = true, message = "Producto creado exitosamente.", data = created });
            TempData["SuccessMessage"] = "Producto creado exitosamente.";
        }
        catch (Exception ex)
        {
            if (isAjax) return Json(new { success = false, message = $"Error al crear el producto: {ex.Message}" });
            TempData["ErrorMessage"] = $"Error al crear el producto: {ex.Message}";
        }

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
        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json");

        try
        {
            var deleted = await _productService.DeleteAsync(id);
            if (deleted)
            {
                if (isAjax) return Json(new { success = true, message = "Producto eliminado exitosamente.", id });
                TempData["SuccessMessage"] = "Producto eliminado exitosamente.";
            }
            else
            {
                if (isAjax) return Json(new { success = false, message = "Producto no encontrado." });
                TempData["ErrorMessage"] = "Producto no encontrado.";
            }
        }
        catch (Exception ex)
        {
            if (isAjax) return Json(new { success = false, message = $"Error al eliminar el producto: {ex.Message}" });
            TempData["ErrorMessage"] = $"Error al eliminar el producto: {ex.Message}";
        }

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
        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json");

        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Datos del producto no válidos.";
            if (isAjax) return Json(new { success = false, message = firstError });
            TempData["ErrorMessage"] = firstError;
            return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "productos" });
        }

        try
        {
            var updated = await _productService.UpdateAsync(product.Id, product);
            if (updated == null)
            {
                if (isAjax) return Json(new { success = false, message = "Producto no encontrado." });
                TempData["ErrorMessage"] = "Producto no encontrado.";
            }
            else
            {
                if (isAjax) return Json(new { success = true, message = "Producto actualizado exitosamente.", data = updated });
                TempData["SuccessMessage"] = "Producto actualizado exitosamente.";
            }
        }
        catch (Exception ex)
        {
            if (isAjax) return Json(new { success = false, message = $"Error al actualizar el producto: {ex.Message}" });
            TempData["ErrorMessage"] = $"Error al actualizar el producto: {ex.Message}";
        }

        return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "productos" });
    }

    /// <summary>
    /// Admin: Toggle product active status (POST)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        try
        {
            var product = await _productService.GetByIdAsync(id);
            if (product == null)
            {
                return Json(new { success = false, message = "Producto no encontrado." });
            }
            product.Activo = !product.Activo;
            var updated = await _productService.UpdateAsync(id, product);
            return Json(new { success = true, active = product.Activo, message = $"Estado del producto actualizado a {(product.Activo ? "Activo" : "Inactivo")}." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error al actualizar estado: {ex.Message}" });
        }
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

    /// <summary>
    /// GET /Product/CheckStock?productId={id}&quantity={qty}
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CheckStock(int productId, int quantity)
    {
        var product = await _productService.GetByIdAsync(productId);
        if (product == null)
        {
            return Json(new { success = false, message = "Producto no encontrado." });
        }
        var available = product.Stock;
        return Json(new { success = true, available, sufficient = available >= quantity });
    }
}
