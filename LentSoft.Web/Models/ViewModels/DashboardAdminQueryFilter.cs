namespace LentSoft.Web.Models.ViewModels;

public class DashboardAdminQueryFilter
{
    public string Section { get; set; } = "general";
    public string Subtab { get; set; } = "productos";
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 5;
    public string? ClientesSearch { get; set; }
    public int ClientesPage { get; set; } = 1;
    public int ClientesPageSize { get; set; } = 5;
    public string? TrabajadoresSearch { get; set; }
    public int TrabajadoresPage { get; set; } = 1;
    public int TrabajadoresPageSize { get; set; } = 5;
    public bool IncludeInactive { get; set; }
    public string MovSort { get; set; } = "desc";
    public string? ProdSearch { get; set; }
    public string? ProdEstado { get; set; }
    public string? ProdCategoria { get; set; }
    public string? ProdProveedor { get; set; }
    public bool ProdStockBajo { get; set; }
    public string? ProdSortBy { get; set; } = "nombre";
    public string ProdSortDir { get; set; } = "asc";
    public int ProdPage { get; set; } = 1;
    public int ProdPageSize { get; set; } = 20;
    public string? MovSearch { get; set; }
    public string? MovTipo { get; set; }
    public int? MovProductId { get; set; }
    public DateTime? MovFechaInicio { get; set; }
    public DateTime? MovFechaFin { get; set; }
    public string? MovMotivo { get; set; }
    public int MovPage { get; set; } = 1;
    public int MovPageSize { get; set; } = 20;
    public string? PedidosProvSearch { get; set; }
    public string? PedidosProvEstado { get; set; }
    public int PedidosProvPage { get; set; } = 1;
    public int PedidosProvPageSize { get; set; } = 20;
}
