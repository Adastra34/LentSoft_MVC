namespace LentSoft.Web.Models.ViewModels;

/// <summary>
/// DTO para el resultado de sp_ReporteCitasPorEstado.
/// Contiene el conteo de citas agrupadas por estado dentro de un rango de fechas.
/// </summary>
public class ReporteCitasEstadoDto
{
    public string Estado { get; set; } = string.Empty;
    public int Total { get; set; }
}
