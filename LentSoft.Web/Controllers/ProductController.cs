using System.IO;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Models.ViewModels;
using LentSoft.Web.Services;

namespace LentSoft.Web.Controllers;

public class ProductController : Controller
{
    private readonly IProductService _productService;
    private readonly IFavoriteService _favoriteService;
    private readonly IFileStorageService _fileStorageService;

    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxImageSizeBytes = 5 * 1024 * 1024; // 5 MB

    [ActivatorUtilitiesConstructor]
    public ProductController(
        IProductService productService,
        IFavoriteService favoriteService,
        IFileStorageService fileStorageService)
    {
        _productService = productService;
        _favoriteService = favoriteService;
        _fileStorageService = fileStorageService;
    }

    public ProductController(
        IProductService productService,
        IFavoriteService favoriteService,
        IWebHostEnvironment webHostEnvironment)
        : this(productService, favoriteService, new LocalFileStorageService(webHostEnvironment, Microsoft.Extensions.Options.Options.Create(new FileStorageSettings())))
    {
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
    public async Task<IActionResult> Create(Product product, IFormFile? ImagenArchivo)
    {
        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json");

        if (ImagenArchivo != null && ImagenArchivo.Length > 0)
        {
            var (success, relativePath, error) = await _fileStorageService.SaveImageAsync(ImagenArchivo, "products", AllowedImageExtensions, MaxImageSizeBytes);
            if (!success)
            {
                if (isAjax) return Json(new { success = false, message = error });
                TempData["ErrorMessage"] = error;
                return RedirectToAction("Admin", "Dashboard");
            }
            product.ImagenUrl = relativePath;
        }

        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Datos del producto no válidos.";
            if (isAjax) return Json(new { success = false, message = firstError });
            TempData["ErrorMessage"] = firstError;
            return RedirectToAction("Admin", "Dashboard");
        }

        if (product.Stock == 0)
        {
            product.Activo = false;
        }

        try
        {
            var created = await _productService.CreateAsync(product);
            if (isAjax) return Json(new { success = true, message = "Producto creado exitosamente.", data = created });
            TempData["SuccessMessage"] = "Producto creado exitosamente.";
        }
        catch (DbUpdateConcurrencyException)
        {
            var msg = "El producto fue modificado por otro usuario, vuelve a intentarlo.";
            if (isAjax) return Json(new { success = false, message = msg });
            TempData["ErrorMessage"] = msg;
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
        catch (DbUpdateConcurrencyException)
        {
            var msg = "El producto fue modificado por otro usuario, vuelve a intentarlo.";
            if (isAjax) return Json(new { success = false, message = msg });
            TempData["ErrorMessage"] = msg;
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
    public async Task<IActionResult> Edit(Product product, IFormFile? ImagenArchivo)
    {
        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json");

        var existingProduct = await _productService.GetByIdAsync(product.Id);
        if (existingProduct == null)
        {
            if (isAjax) return Json(new { success = false, message = "Producto no encontrado." });
            TempData["ErrorMessage"] = "Producto no encontrado.";
            return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "productos" });
        }

        string? oldImageUrlToDelete = null;

        if (ImagenArchivo != null && ImagenArchivo.Length > 0)
        {
            var (success, relativePath, error) = await _fileStorageService.SaveImageAsync(ImagenArchivo, "products", AllowedImageExtensions, MaxImageSizeBytes);
            if (!success)
            {
                if (isAjax) return Json(new { success = false, message = error });
                TempData["ErrorMessage"] = error;
                return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "productos" });
            }
            oldImageUrlToDelete = existingProduct.ImagenUrl;
            product.ImagenUrl = relativePath;
        }
        else
        {
            // Conservar la imagen existente si no se sube nada nuevo
            if (string.IsNullOrWhiteSpace(product.ImagenUrl))
            {
                product.ImagenUrl = existingProduct.ImagenUrl;
            }
        }

        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Datos del producto no válidos.";
            if (isAjax) return Json(new { success = false, message = firstError });
            TempData["ErrorMessage"] = firstError;
            return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "productos" });
        }

        if (product.Stock == 0)
        {
            product.Activo = false;
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
                // Si la actualización fue exitosa y se subió una nueva imagen, borrar la previa si era local
                if (!string.IsNullOrEmpty(oldImageUrlToDelete))
                {
                    _fileStorageService.DeleteFile(oldImageUrlToDelete);
                }

                bool stockAgotado = updated.Stock == 0;
                string msg = stockAgotado
                    ? $"Producto actualizado. El stock llegó a 0 y fue inactivado automáticamente."
                    : "Producto actualizado exitosamente.";

                if (isAjax)
                {
                    return Json(new { 
                        success = true, 
                        message = msg, 
                        data = updated, 
                        stockAgotado = stockAgotado 
                    });
                }

                if (stockAgotado)
                {
                    TempData["WarningMessage"] = msg;
                }
                else
                {
                    TempData["SuccessMessage"] = msg;
                }
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            var msg = "El producto fue modificado por otro usuario, vuelve a intentarlo.";
            if (isAjax) return Json(new { success = false, message = msg });
            TempData["ErrorMessage"] = msg;
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
        var isAuthenticated = User?.Identity?.IsAuthenticated == true;
        if (isAuthenticated && User != null)
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
