using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Models.ViewModels;

public class DashboardAdminViewModel
{
    // ── Stats Cards (con % variación) ──
    public decimal VentasDelMes { get; set; }
    public decimal VentasDelMesAnterior { get; set; }
    public int PedidosActivos { get; set; }
    public int PedidosActivosAnterior { get; set; }
    public int ClientesTotales { get; set; }
    public int ClientesTotalesAnterior { get; set; }
    public int ProductosEnStock { get; set; }
    public int ProductosEnStockAnterior { get; set; }

    // ── General ──
    public List<Order> PedidosRecientes { get; set; } = new();

    // ── Inventario ──
    public List<Product> Productos { get; set; } = new();
    public List<Categoria> Categorias { get; set; } = new();
    public List<Proveedor> Proveedores { get; set; } = new();
    public List<MovimientoInventario> HistorialMovimientos { get; set; } = new();

    // ── Ventas ──
    public List<Order> Ventas { get; set; } = new();

    // ── Citas ──
    public List<Appointment> Citas { get; set; } = new();

    // ── Usuarios ──
    public List<User> Clientes { get; set; } = new();
    public List<Employee> Trabajadores { get; set; } = new();

    // ── Facturas (Paginadas y Filtradas) ──
    public List<Invoice> Facturas { get; set; } = new();
    public string? FacturasSearchTerm { get; set; }
    public int FacturasPage { get; set; } = 1;
    public int FacturasPageSize { get; set; } = 5;
    public int FacturasTotalCount { get; set; }
    public int FacturasTotalPages => (int)Math.Ceiling((double)FacturasTotalCount / (FacturasPageSize > 0 ? FacturasPageSize : 5));
    public List<Order> PedidosDisponibles { get; set; } = new();

    // Active section + sub-tab
    public string ActiveSection { get; set; } = "general";
    public string ActiveSubTab { get; set; } = "productos";

    // Helper: calcular % de variación
    public static string GetVariacion(decimal actual, decimal anterior)
    {
        if (anterior == 0) return actual > 0 ? "+100%" : "0%";
        var pct = ((actual - anterior) / anterior) * 100;
        return (pct >= 0 ? "+" : "") + pct.ToString("F1") + "%";
    }

    public static string GetVariacion(int actual, int anterior)
    {
        return GetVariacion((decimal)actual, (decimal)anterior);
    }

    public static string GetVariacionColor(decimal actual, decimal anterior)
    {
        return actual >= anterior ? "var(--success)" : "var(--error)";
    }

    public static string GetVariacionColor(int actual, int anterior)
    {
        return actual >= anterior ? "var(--success)" : "var(--error)";
    }
}
