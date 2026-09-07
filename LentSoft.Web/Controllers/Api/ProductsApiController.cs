using Microsoft.AspNetCore.Mvc;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Services;

namespace LentSoft.Web.Controllers.Api;

[ApiController]
[Route("api/products")]
public class ProductsApiController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsApiController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] string? categoria, [FromQuery] string? search)
    {
        List<Product> products;

        // Normalizar categoría según filtros de la UI (Monturas, Contacto, Sol, Accesorios)
        string? catFilter = null;
        if (!string.IsNullOrWhiteSpace(categoria) && !categoria.Equals("todos", StringComparison.OrdinalIgnoreCase))
        {
            var lower = categoria.Trim().ToLower();
            if (lower == "monturas") catFilter = "monturas";
            else if (lower == "contacto" || lower == "lentes-contacto") catFilter = "lentes-contacto";
            else if (lower == "sol" || lower == "lentes-sol") catFilter = "lentes-sol";
            else if (lower == "accesorios") catFilter = "accesorios";
            else catFilter = lower;
        }

        if (string.IsNullOrWhiteSpace(catFilter))
        {
            products = await _productService.GetActiveAsync();
        }
        else
        {
            products = await _productService.FilterAsync(catFilter, null, null);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            products = products.Where(p =>
                p.Nombre.ToLower().Contains(term) ||
                (p.Marca != null && p.Marca.ToLower().Contains(term)) ||
                (p.Descripcion != null && p.Descripcion.ToLower().Contains(term))
            ).ToList();
        }

        var result = products.Select(p => new
        {
            p.Id,
            p.Nombre,
            p.Descripcion,
            p.Precio,
            p.PrecioDescuento,
            PrecioFinal = p.GetFinalPrice(),
            DescuentoPorcentaje = p.GetDiscountPercentage(),
            p.Categoria,
            p.Marca,
            p.Stock,
            p.ImagenUrl,
            p.Rating,
            p.ReviewCount,
            p.EsDestacado
        });

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetProduct(int id)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product == null || !product.Activo)
            return NotFound(new { message = "Producto no encontrado." });

        return Ok(new
        {
            product.Id,
            product.Nombre,
            product.Descripcion,
            product.Precio,
            product.PrecioDescuento,
            PrecioFinal = product.GetFinalPrice(),
            DescuentoPorcentaje = product.GetDiscountPercentage(),
            product.Categoria,
            product.Marca,
            product.Stock,
            product.ImagenUrl,
            product.Rating,
            product.ReviewCount,
            product.EsDestacado,
            product.Material,
            product.Color,
            product.Proteccion
        });
    }
}
