using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LentSoft.Web.Models.Reviews;
using LentSoft.Web.Models.ViewModels;
using LentSoft.Web.Services;
using LentSoft.Web.Services.Reviews;

namespace LentSoft.Web.Controllers;

/// <summary>
/// Gestiona la creación, edición, eliminación y carga parcial de reseñas de productos.
/// </summary>
public class ReviewsController : Controller
{
    private readonly IReviewStore _reviewStore;
    private readonly IProductService _productService;
    private readonly ILogger<ReviewsController> _logger;

    public ReviewsController(
        IReviewStore reviewStore,
        IProductService productService,
        ILogger<ReviewsController> logger)
    {
        _reviewStore = reviewStore;
        _productService = productService;
        _logger = logger;
    }

    private bool IsAjaxRequest()
    {
        return Request.Headers["X-Requested-With"] == "XMLHttpRequest"
               || Request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<ProductReviewsViewModel> BuildViewModelAsync(int productId)
    {
        var summary = await _reviewStore.GetSummaryAsync(productId);
        var reviews = await _reviewStore.GetByProductAsync(productId);
        var isAuth = User.Identity?.IsAuthenticated == true;
        var userId = isAuth ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
        var isAdmin = isAuth && User.IsInRole("admin");

        ProductReview? userReview = null;
        if (isAuth && !string.IsNullOrEmpty(userId) && !isAdmin)
        {
            userReview = await _reviewStore.GetByUserAndProductAsync(userId, productId);
        }

        return new ProductReviewsViewModel
        {
            ProductId = productId,
            Summary = summary,
            Reviews = reviews,
            IsAuthenticated = isAuth,
            IsAdmin = isAdmin,
            CurrentUserId = userId,
            CurrentUserReview = userReview
        };
    }

    [HttpGet]
    public async Task<IActionResult> Partial(int productId)
    {
        var vm = await BuildViewModelAsync(productId);
        return PartialView("~/Views/Product/_ProductReviews.cshtml", vm);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int productId, int rating, string comment)
    {
        var product = await _productService.GetByIdAsync(productId);
        if (product == null)
        {
            if (IsAjaxRequest()) return NotFound(new { success = false, message = "Producto no encontrado." });
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var userName = User.FindFirstValue(ClaimTypes.Name) ?? "Usuario LentSoft";
        var isAdmin = User.IsInRole("admin");

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var review = new ProductReview
            {
                ProductoId = productId,
                UserId = userId,
                UserName = userName,
                Rating = rating,
                Comment = comment ?? string.Empty
            };

            var result = await _reviewStore.AddAsync(review, isAdmin);
            if (!result.Success)
            {
                if (IsAjaxRequest())
                {
                    return BadRequest(new { success = false, message = result.ErrorMessage });
                }
                TempData["ErrorMessage"] = result.ErrorMessage;
                return RedirectToAction("Details", "Product", new { id = productId });
            }

            if (IsAjaxRequest())
            {
                var summary = await _reviewStore.GetSummaryAsync(productId);
                return Json(new
                {
                    success = true,
                    message = "¡Gracias por compartir tu reseña!",
                    averageRating = summary.AverageRating,
                    totalReviews = summary.TotalReviews
                });
            }

            TempData["SuccessMessage"] = "¡Gracias por compartir tu reseña!";
            return RedirectToAction("Details", "Product", new { id = productId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear reseña para producto {ProductId}", productId);
            const string msg = "No se pudo guardar tu reseña. Intenta de nuevo.";
            if (IsAjaxRequest()) return StatusCode(500, new { success = false, message = msg });
            TempData["ErrorMessage"] = msg;
            return RedirectToAction("Details", "Product", new { id = productId });
        }
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, int productId, int rating, string comment)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var isAdmin = User.IsInRole("admin");

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _reviewStore.UpdateAsync(id, userId, isAdmin, rating, comment);
            if (!result.Success)
            {
                if (IsAjaxRequest()) return BadRequest(new { success = false, message = result.ErrorMessage });
                TempData["ErrorMessage"] = result.ErrorMessage;
                return RedirectToAction("Details", "Product", new { id = productId });
            }

            if (IsAjaxRequest())
            {
                var summary = await _reviewStore.GetSummaryAsync(productId);
                return Json(new
                {
                    success = true,
                    message = "Tu reseña ha sido actualizada.",
                    averageRating = summary.AverageRating,
                    totalReviews = summary.TotalReviews
                });
            }

            TempData["SuccessMessage"] = "Tu reseña ha sido actualizada.";
            return RedirectToAction("Details", "Product", new { id = productId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar reseña {ReviewId}", id);
            const string msg = "No se pudo actualizar la reseña.";
            if (IsAjaxRequest()) return StatusCode(500, new { success = false, message = msg });
            TempData["ErrorMessage"] = msg;
            return RedirectToAction("Details", "Product", new { id = productId });
        }
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, int productId, string? returnUrl = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var isAdmin = User.IsInRole("admin");

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _reviewStore.DeleteAsync(id, userId, isAdmin);
            if (!result.Success)
            {
                if (IsAjaxRequest()) return BadRequest(new { success = false, message = result.ErrorMessage });
                TempData["ErrorMessage"] = result.ErrorMessage;
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl);
                return RedirectToAction("Details", "Product", new { id = productId });
            }

            if (IsAjaxRequest())
            {
                var summary = await _reviewStore.GetSummaryAsync(productId);
                return Json(new
                {
                    success = true,
                    message = "Reseña eliminada correctamente.",
                    averageRating = summary.AverageRating,
                    totalReviews = summary.TotalReviews
                });
            }

            TempData["SuccessMessage"] = "Reseña eliminada correctamente.";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl);
            return RedirectToAction("Details", "Product", new { id = productId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar reseña {ReviewId}", id);
            const string msg = "No se pudo eliminar la reseña.";
            if (IsAjaxRequest()) return StatusCode(500, new { success = false, message = msg });
            TempData["ErrorMessage"] = msg;
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl);
            return RedirectToAction("Details", "Product", new { id = productId });
        }
    }
}
