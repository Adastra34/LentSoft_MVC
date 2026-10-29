using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Models.ViewModels;

public class DashboardVentasViewModel
{
    // ── General / Resumen ──
    public decimal VentasDelMes { get; set; }
    public int PedidosActivos { get; set; }
    public int ClientesAtendidos { get; set; }
    public decimal TicketPromedio { get; set; }

    // ── Ventas ──
    public List<Order> Ventas { get; set; } = new();
    public DateTime? FiltroFechaDesde { get; set; }
    public DateTime? FiltroFechaHasta { get; set; }
    public string? FiltroRapido { get; set; }

    // ── Facturas (Paginadas y Filtradas) ──
    public List<Invoice> Facturas { get; set; } = new();
    public string? FacturasSearchTerm { get; set; }
    public int FacturasPage { get; set; } = 1;
    public int FacturasPageSize { get; set; } = 5;
    public int FacturasTotalCount { get; set; }
    public int FacturasTotalPages => (int)Math.Ceiling((double)FacturasTotalCount / (FacturasPageSize > 0 ? FacturasPageSize : 5));
    public List<Order> PedidosDisponibles { get; set; } = new();

    // ── Inventarios ──
    public List<Product> Productos { get; set; } = new();
    public List<Product> TodosLosProductos { get; set; } = new();
    public string? ProdSearchTerm { get; set; }
    public string? ProdEstado { get; set; }
    public string? ProdCategoria { get; set; }
    public string? ProdProveedor { get; set; }
    public bool ProdStockBajo { get; set; }
    public string? ProdSortBy { get; set; }
    public string ProdSortDir { get; set; } = "asc";
    public int ProdPage { get; set; } = 1;
    public int ProdPageSize { get; set; } = 20;
    public int ProdTotalCount { get; set; }
    public int ProdTotalPages => (int)Math.Ceiling((double)ProdTotalCount / (ProdPageSize > 0 ? ProdPageSize : 20));

    public List<Supplier> Proveedores { get; set; } = new();

    public List<InventoryMovement> HistorialMovimientos { get; set; } = new();
    public string? MovSearchTerm { get; set; }
    public string? MovTipo { get; set; }
    public int? MovProductId { get; set; }
    public DateTime? MovFechaInicio { get; set; }
    public DateTime? MovFechaFin { get; set; }
    public string? MovMotivo { get; set; }
    public string MovSort { get; set; } = "desc";
    public int MovPage { get; set; } = 1;
    public int MovPageSize { get; set; } = 20;
    public int MovTotalCount { get; set; }
    public int MovTotalPages => (int)Math.Ceiling((double)MovTotalCount / (MovPageSize > 0 ? MovPageSize : 20));

    public List<SalesOrder> PedidosVentas { get; set; } = new();
    public string? PedidosSearchTerm { get; set; }
    public string? PedidosEstado { get; set; }
    public int PedidosPage { get; set; } = 1;
    public int PedidosPageSize { get; set; } = 15;
    public int PedidosTotalCount { get; set; }
    public int PedidosTotalPages => (int)Math.Ceiling((double)PedidosTotalCount / (PedidosPageSize > 0 ? PedidosPageSize : 15));

    // ── Clientes ──
    public List<User> Clientes { get; set; } = new();

    // ── Perfil ──
    public User? UsuarioActual { get; set; }

    // Navigation
    public string ActiveSection { get; set; } = "general";
    public string ActiveSubTab { get; set; } = "productos";
}
