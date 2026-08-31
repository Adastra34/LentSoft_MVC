using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public interface IPdfInvoiceService
{
    byte[] GenerateInvoicePdf(Invoice invoice);
}
