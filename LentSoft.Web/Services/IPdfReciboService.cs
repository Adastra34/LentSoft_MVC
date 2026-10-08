using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public interface IPdfReciboService
{
    byte[] GenerateReciboPdf(PagoVenta pago);
}
