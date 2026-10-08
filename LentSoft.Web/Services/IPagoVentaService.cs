using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public interface IPagoVentaService
{
    Task<PagoVenta> RegistrarAbonoAsync(int ventaId, decimal monto, string metodoPago, string responsable);
    Task<List<PagoVenta>> ObtenerAbonosPorVentaAsync(int ventaId);
    Task<PagoVenta?> ObtenerPagoPorIdAsync(int pagoId);
}
