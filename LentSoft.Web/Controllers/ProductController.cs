using System.IO;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Models.ViewModels;
using LentSoft.Web.Services;

namespace LentSoft.Web.Controllers;

public class ProductController : Controller
{
    private readonly IProductService _productService;
    private readonly IFavoriteService _favoriteService;
    private readonly IWebHostEnvironment _webHostEnvironment;

    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxImageSizeBytes = 5 * 1024 * 1024; // 5 MB

    public ProductController(
        IProductService productService,
        IFavoriteService favoriteService,
        IWebHostEnvironment webHostEnvironment)
    {
        _productService = productService;
        _favoriteService = favoriteService;
        _webHostEnvironment = webHostEnvironment;
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
            var (success, relativePath, error) = await ProcessProductImageAsync(ImagenArchivo);
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
            var (success, relativePath, error) = await ProcessProductImageAsync(ImagenArchivo);
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
                    DeleteLocalProductImage(oldImageUrlToDelete);
                }

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

    private async Task<(bool success, string? relativePath, string? errorMessage)> ProcessProductImageAsync(IFormFile file)
    {
        if (file.Length == 0)
        {
            return (false, null, "El archivo de imagen está vacío.");
        }

        if (file.Length > MaxImageSizeBytes)
        {
            return (false, null, "El tamaño de la imagen no debe superar los 5 MB.");
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(ext))
        {
            return (false, null, "Formato de imagen no permitido. Solo se aceptan archivos .jpg, .jpeg, .png y .webp.");
        }

        try
        {
            var webRoot = _webHostEnvironment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadsDir = Path.Combine(webRoot, "uploads", "products");
            if (!Directory.Exists(uploadsDir))
            {
                Directory.CreateDirectory(uploadsDir);
            }

            var uniqueFileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(uploadsDir, uniqueFileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativePath = $"/uploads/products/{uniqueFileName}";
            return (true, relativePath, null);
        }
        catch (Exception ex)
        {
            return (false, null, $"Error al guardar la imagen en el servidor: {ex.Message}");
        }
    }

    private void DeleteLocalProductImage(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl)) return;

        var normalized = imageUrl.Replace('\\', '/').TrimStart('/');
        if (normalized.StartsWith("uploads/products/", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var webRoot = _webHostEnvironment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var fullPath = Path.Combine(webRoot, normalized.Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
            catch
            {
                // No bloquear si ocurre un problema al remover el archivo viejo
            }
        }
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
