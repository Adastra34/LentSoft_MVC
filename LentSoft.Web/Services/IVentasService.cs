using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public interface IVentasService
{
    Task<VentasIndicadoresDto> ObtenerIndicadoresVentasAsync();
    Task<DataTablesResponse<VentaDataTablesDto>> ObtenerVentasDataTablesAsync(VentasFilterRequest request);
    Task<Order> RegistrarVentaAsync(RegistrarVentaDto dto, string responsable);
    Task<bool> CambiarEstadoPedidoAsync(int orderId, string nuevoEstado);
    Task<List<ProductoVendidoDto>> ObtenerReporteProductosVendidosAsync(DateTime? fechaInicio, DateTime? fechaFin);
    Task<FormulaOptica?> ObtenerFormulaVigentePorClienteAsync(int userId);
    Task<List<FormulaOptica>> ObtenerFormulasPorPacienteAsync(int userId);
    Task VincularFormulaAsync(int orderId, int? formulaOpticaId);
}
