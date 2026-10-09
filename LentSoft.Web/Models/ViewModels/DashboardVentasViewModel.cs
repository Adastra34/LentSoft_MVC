using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Models.ViewModels;

public class DashboardVentasViewModel
{
    // ── General / Resumen ──
    public decimal VentasDelMes { get; set; }
    public int PedidosActivos { get; set; }
    public int ClientesAtendidos { get; set; }
    public decimal TicketPromedio { get; set; }

    // ── Ventas (Paginadas y Filtradas en Servidor) ──
    public List<Order> Ventas { get; set; } = new();
    public string? VentasSearchTerm { get; set; }
    public int VentasPage { get; set; } = 1;
    public int VentasPageSize { get; set; } = 10;
    public int VentasTotalCount { get; set; }
    public int VentasTotalPages => (int)Math.Ceiling((double)VentasTotalCount / (VentasPageSize > 0 ? VentasPageSize : 10));
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
    public List<SalesOrder> PedidosVentas { get; set; } = new();
    public int PedidosVentasPage { get; set; } = 1;
    public int PedidosVentasPageSize { get; set; } = 10;
    public int PedidosVentasTotalCount { get; set; }
    public int PedidosVentasTotalPages => (int)Math.Ceiling((double)PedidosVentasTotalCount / (PedidosVentasPageSize > 0 ? PedidosVentasPageSize : 10));

    public List<InventoryMovement> HistorialMovimientos { get; set; } = new();
    public int MovimientosPage { get; set; } = 1;
    public int MovimientosPageSize { get; set; } = 15;
    public int MovimientosTotalCount { get; set; }
    public int MovimientosTotalPages => (int)Math.Ceiling((double)MovimientosTotalCount / (MovimientosPageSize > 0 ? MovimientosPageSize : 15));

    // ── Clientes ──
    public List<User> Clientes { get; set; } = new();

    // ── Perfil ──
    public User? UsuarioActual { get; set; }

    // Navigation
    public string ActiveSection { get; set; } = "general";
    public string ActiveSubTab { get; set; } = "productos";
}
