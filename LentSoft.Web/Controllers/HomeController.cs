using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Models.FrameModels;
using LentSoft.Web.Models.ViewModels;
using LentSoft.Web.Services;
using LentSoft.Web.Services.FrameModels;
using System.Diagnostics;

namespace LentSoft.Web.Controllers;

public class HomeController : Controller
{
    private readonly IProductService _productService;
    private readonly LentSoftDbContext _context;
    private readonly IFrameModelStore? _frameModelStore;

    public HomeController(
        IProductService productService,
        LentSoftDbContext context,
        IFrameModelStore? frameModelStore = null)
    {
        _productService = productService;
        _context = context;
        _frameModelStore = frameModelStore;
    }

    /// <summary>
    /// Home page — migrated from Views/home.html
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var bestSellers = await _productService.GetBestSellersAsync(3);

        var productosDescuento = await _context.Products
            .Where(p => p.Activo && p.PrecioDescuento != null && p.PrecioDescuento < p.Precio)
            .OrderBy(p => p.Nombre)
            .ToListAsync();

        var lentesContacto = await _context.Products
            .Where(p => p.Activo && p.Categoria == "lentes-contacto")
            .OrderBy(p => p.Nombre)
            .ToListAsync();

        var viewModel = new HomeViewModel
        {
            BestSellers = bestSellers,
            ProductosDescuento = productosDescuento,
            LentesContacto = lentesContacto
        };

        // Gafa destacada para la mini prueba AR en vivo (desde manifiesto JSON o primera gafa activa)
        var gafas = await _productService.GetGafasAsync();
        FrameModelsManifest? manifest = _frameModelStore != null
            ? await _frameModelStore.GetManifestAsync()
            : null;

        Product? featuredFrame = null;
        if (manifest?.FeaturedProductId.HasValue == true)
        {
            featuredFrame = gafas.FirstOrDefault(g => g.Id == manifest.FeaturedProductId.Value);
        }
        featuredFrame ??= gafas.FirstOrDefault(g => !string.IsNullOrWhiteSpace(g.ImagenOverlayUrl))
                          ?? gafas.FirstOrDefault();

        FrameModelEntry? modelEntry = (featuredFrame != null && manifest != null)
            ? manifest.Models.FirstOrDefault(m => m.ProductId == featuredFrame.Id)
            : null;

        ViewBag.HomeFeaturedFrame = featuredFrame;
        ViewBag.HomeFeaturedModel = modelEntry;

        return View(viewModel);
    }

    /// <summary>
    /// About page — migrated from Views/nosotros.html
    /// </summary>
    public IActionResult About()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
