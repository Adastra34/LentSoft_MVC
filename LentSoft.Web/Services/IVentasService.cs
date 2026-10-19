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
        int currentUserId);

    Task<DataTablesResult<VentaRowDTO>> ListarVentasDataTablesAsync(DataTablesRequestDTO request);

    Task<Order> RegistrarVentaAsync(RegistrarVentaDTO dto, string responsable, string? baseUrl);

    Task<(bool Success, string Message)> CambiarEstadoOrdenAsync(int orderId, string nuevoEstado);

    Task<(bool Success, string Message)> VincularFormulaAsync(int orderId, int? formulaOpticaId);

    Task<FormulaOpticaVigenteDTO> ObtenerFormulaVigentePorClienteAsync(int userId);

    Task<List<FormulaOpticaResumenDTO>> ObtenerFormulasPorPacienteAsync(int userId);

    Task<List<ProductoVendidoReporteDTO>> ObtenerReporteProductosVendidosAsync(DateTime? fechaInicio, DateTime? fechaFin);

    Task<Order?> ObtenerVentaPorIdAsync(int saleId);
}
