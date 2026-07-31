using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.ViewModels;
using LentSoft.Web.Services;
using System.Diagnostics;

namespace LentSoft.Web.Controllers;

public class HomeController : Controller
{
    private readonly IProductService _productService;
    private readonly LentSoftDbContext _context;

    public HomeController(IProductService productService, LentSoftDbContext context)
    {
        _productService = productService;
        _context = context;
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

        // Categoría 2 = "Lentes" (antes era "lentes-contacto" string)
        var lentesContacto = await _context.Products
            .Include(p => p.Categoria)
            .Where(p => p.Activo && p.CategoriaId == 2)
            .OrderBy(p => p.Nombre)
            .ToListAsync();

        var viewModel = new HomeViewModel
        {
            BestSellers = bestSellers,
            ProductosDescuento = productosDescuento,
            LentesContacto = lentesContacto
        };

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
