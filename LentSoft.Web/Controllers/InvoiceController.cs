using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Services;

namespace LentSoft.Web.Controllers;

[Authorize(Roles = "admin")]
public class InvoiceController : Controller
{
    private readonly IInvoiceService _invoiceService;
    private readonly IPdfInvoiceService _pdfInvoiceService;

    public InvoiceController(IInvoiceService invoiceService, IPdfInvoiceService pdfInvoiceService)
    {
        _invoiceService = invoiceService;
        _pdfInvoiceService = pdfInvoiceService;
    }

    [HttpGet]
    public IActionResult Index(string? searchTerm, int page = 1, int pageSize = 5)
    {
        return RedirectToAction("Admin", "Dashboard", new { section = "facturas", searchTerm, page, pageSize });
    }

    [HttpGet]
    public async Task<IActionResult> GetJson(int id)
    {
        var invoice = await _invoiceService.GetByIdAsync(id);
        if (invoice == null) return NotFound();

        return Json(new
        {
            invoice.Id,
            invoice.NumeroFactura,
            invoice.OrderId,
            invoice.Subtotal,
            invoice.Impuestos,
            invoice.Total,
            invoice.Estado,
            invoice.FechaEmision,
            invoice.FechaPago,
            invoice.MetodoPago,
            ClienteNombre = invoice.Order?.User?.NombreCompleto ?? "Cliente Genérico"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Invoice invoice)
    {
        if (invoice.OrderId <= 0)
        {
            TempData["ErrorMessage"] = "Debe seleccionar un pedido válido para la factura.";
            return RedirectToAction("Admin", "Dashboard", new { section = "facturas" });
        }

        try
        {
            await _invoiceService.CreateAsync(invoice);
            TempData["SuccessMessage"] = "Factura creada exitosamente.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al crear la factura: {ex.Message}";
        }

        return RedirectToAction("Admin", "Dashboard", new { section = "facturas" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Invoice invoice)
    {
        if (invoice.Id <= 0)
        {
            TempData["ErrorMessage"] = "Factura no válida.";
            return RedirectToAction("Admin", "Dashboard", new { section = "facturas" });
        }

        try
        {
            var updated = await _invoiceService.UpdateAsync(invoice);
            if (updated != null)
            {
                TempData["SuccessMessage"] = "Factura actualizada exitosamente.";
            }
            else
            {
                TempData["ErrorMessage"] = "No se encontró la factura a editar.";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al actualizar la factura: {ex.Message}";
        }

        return RedirectToAction("Admin", "Dashboard", new { section = "facturas" });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result = await _invoiceService.DeleteAsync(id);
            if (result)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Factura eliminada exitosamente." });
                }
                TempData["SuccessMessage"] = "Factura eliminada exitosamente.";
            }
            else
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "No se encontró la factura." });
                }
                TempData["ErrorMessage"] = "No se encontró la factura.";
            }
        }
        catch (Exception ex)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = ex.Message });
            }
            TempData["ErrorMessage"] = $"Error al eliminar la factura: {ex.Message}";
        }

        return RedirectToAction("Admin", "Dashboard", new { section = "facturas" });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadPdf(int id)
    {
        var invoice = await _invoiceService.GetByIdAsync(id);
        if (invoice == null)
        {
            TempData["ErrorMessage"] = "Factura no encontrada.";
            return RedirectToAction("Admin", "Dashboard", new { section = "facturas" });
        }

        try
        {
            var pdfBytes = _pdfInvoiceService.GenerateInvoicePdf(invoice);
            var filename = $"Factura-{invoice.NumeroFactura}.pdf";
            return File(pdfBytes, "application/pdf", filename);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al generar el PDF: {ex.Message}";
            return RedirectToAction("Admin", "Dashboard", new { section = "facturas" });
        }
    }
}
