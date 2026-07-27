using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;

namespace LentSoft.Web.Services;

/// <summary>
/// Dashboard service — equivalent to sp_DashboardAdmin stored procedure
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly LentSoftDbContext _context;

    public DashboardService(LentSoftDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardStats> GetAdminStatsAsync()
    {
        return new DashboardStats
        {
            TotalUsuarios = await _context.Users.CountAsync(u => u.Role == "usuario"),
            TotalProductos = await _context.Products.CountAsync(p => p.Activo),
            TotalPedidos = await _context.Orders.CountAsync(o => o.Estado != "cancelado"),
            VentasTotales = await _context.Orders
                .Where(o => o.Estado != "cancelado")
                .SumAsync(o => o.Total)
        };
    }
}
