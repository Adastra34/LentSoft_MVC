using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Models.ViewModels;
using LentSoft.Web.Services;

namespace LentSoft.Web.Controllers;

public class ProductController : Controller
{
    private readonly IProductService _productService;
    private readonly IFavoriteService _favoriteService;
    private readonly LentSoftDbContext _context;
    private readonly IWebHostEnvironment _env;

    public ProductController(
        IProductService productService,
        IFavoriteService favoriteService,
        LentSoftDbContext context,
        IWebHostEnvironment env)
    {
        _productService = productService;
        _favoriteService = favoriteService;
        _context = context;
        _env = env;
    }

    /// <summary>
    /// Public store page
    /// </summary>
    public async Task<IActionResult> Tienda(string? categoria, string? marca, string? rangoPrecio)
    {
        var products = await _productService.FilterAsync(categoria, marca, rangoPrecio);
        var categorias = await _productService.GetCategoriasAsync();
        var marcas = await _productService.GetMarcasAsync();
        var featured = await _productService.GetFeaturedAsync();

        // Contadores por categoría para los filtros
        var allActive = await _context.Products
            .Include(p => p.Categoria)
            .Where(p => p.Activo)
            .ToListAsync();

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
    /// Admin: Create product (POST) — con soporte de file upload
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product product, IFormFile? imagenFile)
    {
        // Si se subió una imagen, procesarla
        if (imagenFile != null && imagenFile.Length > 0)
        {
            var imagenUrl = await GuardarImagenProducto(imagenFile);
            if (imagenUrl != null)
                product.ImagenUrl = imagenUrl;
        }

        // Remover errores de validación del campo Categoria (navigation property)
        ModelState.Remove("Categoria");

        if (!ModelState.IsValid)
        {
            var errors = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            TempData["ErrorMessage"] = string.IsNullOrWhiteSpace(errors) ? "Datos del producto no válidos." : errors;
            return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "productos" });
        }

        try
        {
            await _productService.CreateAsync(product);
            TempData["SuccessMessage"] = "Producto creado exitosamente.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al crear el producto: {ex.Message}";
        }

        return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "productos" });
    }

    /// <summary>
    /// Admin: Get product data as JSON (for edit modal)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product == null) return NotFound();

        return Json(new
        {
            product.Id,
            product.Nombre,
            product.Descripcion,
            product.Precio,
            product.PrecioDescuento,
            product.CategoriaId,
            product.Marca,
            product.Stock,
            product.ImagenUrl,
            product.Activo
        });
    }

    /// <summary>
    /// Admin: Edit product (POST) — con soporte de file upload
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Product product, IFormFile? imagenFile)
    {
        // Si se subió una imagen nueva, procesarla
        if (imagenFile != null && imagenFile.Length > 0)
        {
            var imagenUrl = await GuardarImagenProducto(imagenFile);
            if (imagenUrl != null)
                product.ImagenUrl = imagenUrl;
        }

        // Remover errores de validación del campo Categoria (navigation property)
        ModelState.Remove("Categoria");

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Datos del producto no válidos.";
            return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "productos" });
        }

        var updated = await _productService.UpdateAsync(product.Id, product);
        if (updated == null)
        {
            TempData["ErrorMessage"] = "Producto no encontrado.";
        }
        else
        {
            TempData["SuccessMessage"] = "Producto actualizado exitosamente.";
        }

        return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "productos" });
    }

    /// <summary>
    /// Admin: Delete product
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _productService.DeleteAsync(id);
        TempData["SuccessMessage"] = "Producto eliminado exitosamente.";
        return RedirectToAction("Admin", "Dashboard", new { section = "inventario", subtab = "productos" });
    }

    // ── Helpers ──

    private async Task<string?> GuardarImagenProducto(IFormFile file)
    {
        try
        {
            var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!extensionesPermitidas.Contains(ext)) return null;

            var carpeta = Path.Combine(_env.WebRootPath, "img", "productos");
            Directory.CreateDirectory(carpeta);

            var nombreArchivo = $"{Guid.NewGuid()}{ext}";
            var rutaCompleta = Path.Combine(carpeta, nombreArchivo);

            using var stream = new FileStream(rutaCompleta, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/img/productos/{nombreArchivo}";
        }
        catch
        {
            return null;
        }
    }
}
