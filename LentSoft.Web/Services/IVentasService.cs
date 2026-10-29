using LentSoft.Web.Models.DTOs;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Models.ViewModels;

namespace LentSoft.Web.Services;

public interface IVentasService
{
    Task<DashboardVentasViewModel> ObtenerDashboardAsync(
        string section, 
        string subtab, 
        string? searchTerm, 
        int page, 
        int pageSize, 
        int currentUserId,
        string? pedSearch = null,
        string? pedEstado = null,
        int pedPage = 1,
        int pedPageSize = 15,
        string? movSearch = null,
        string? movTipo = null,
        int? movProductId = null,
        DateTime? movFechaInicio = null,
        DateTime? movFechaFin = null,
        string? movMotivo = null,
        string movSort = "desc",
        int movPage = 1,
        int movPageSize = 20);

    Task<DataTablesResult<VentaRowDTO>> ListarVentasDataTablesAsync(DataTablesRequestDTO request);

    Task<List<VentaRowDTO>> ExportarVentasFiltradasAsync(DataTablesRequestDTO request);

    Task<Order> RegistrarVentaAsync(RegistrarVentaDTO dto, string responsable, string? baseUrl);

    Task<(bool Success, string Message)> CambiarEstadoOrdenAsync(int orderId, string nuevoEstado, string? responsable = null);

    Task<(bool Success, string Message)> VincularFormulaAsync(int orderId, int? formulaOpticaId, string? responsable = null);

    Task<FormulaOpticaVigenteDTO> ObtenerFormulaVigentePorClienteAsync(int userId);

    Task<List<FormulaOpticaResumenDTO>> ObtenerFormulasPorPacienteAsync(int userId);

    Task<List<ProductoVendidoReporteDTO>> ObtenerReporteProductosVendidosAsync(DateTime? fechaInicio, DateTime? fechaFin);

    Task<Order?> ObtenerVentaPorIdAsync(int saleId);

    Task<ResultadoOperacionPedidoDTO> CrearPedidoVentaAsync(CrearPedidoVentaDTO dto, string responsable);

    Task<ResultadoOperacionPedidoDTO> EditarPedidoVentaAsync(EditarPedidoVentaDTO dto, string responsable);

    Task<ResultadoOperacionPedidoDTO> EliminarPedidoVentaAsync(int id, string responsable);
}
