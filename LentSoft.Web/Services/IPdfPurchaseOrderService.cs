using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public interface IPdfPurchaseOrderService
{
    byte[] GeneratePurchaseOrderPdf(SupplierOrder order);
}
