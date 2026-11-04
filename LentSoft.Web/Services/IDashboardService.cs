using LentSoft.Web.Models.ViewModels;

namespace LentSoft.Web.Services;

public interface IDashboardService
{
    Task<DashboardStats> GetAdminStatsAsync();
    Task<DashboardAdminViewModel> GetDashboardAdminViewModelAsync(DashboardAdminQueryFilter filter);
}

public class DashboardStats
{
    public int TotalUsuarios { get; set; }
    public int TotalProductos { get; set; }
    public int TotalPedidos { get; set; }
    public decimal VentasTotales { get; set; }
}
