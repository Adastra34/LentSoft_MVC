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
    public List<Product> TodosLosProductos { get; set; } = new();
    public string? ProdSearchTerm { get; set; }
    public string? ProdEstado { get; set; }
    public string? ProdCategoria { get; set; }
    public int ProdPage { get; set; } = 1;
    public int ProdPageSize { get; set; } = 20;
    public int ProdTotalCount { get; set; }
    public int ProdTotalPages => (int)Math.Ceiling((double)ProdTotalCount / (ProdPageSize > 0 ? ProdPageSize : 20));

    public List<Supplier> Proveedores { get; set; } = new();

    public List<InventoryMovement> HistorialMovimientos { get; set; } = new();
    public string? MovSearchTerm { get; set; }
    public string? MovTipo { get; set; }
    public string MovSort { get; set; } = "desc";
    public int MovPage { get; set; } = 1;
    public int MovPageSize { get; set; } = 20;
    public int MovTotalCount { get; set; }
    public int MovTotalPages => (int)Math.Ceiling((double)MovTotalCount / (MovPageSize > 0 ? MovPageSize : 20));

    public List<SalesOrder> PedidosVentas { get; set; } = new();

    public List<SupplierOrder> PedidosProveedores { get; set; } = new();
    public string? PedidosProvSearchTerm { get; set; }
    public string? PedidosProvEstado { get; set; }
    public int PedidosProvPage { get; set; } = 1;
    public int PedidosProvPageSize { get; set; } = 20;
    public int PedidosProvTotalCount { get; set; }
    public int PedidosProvTotalPages => (int)Math.Ceiling((double)PedidosProvTotalCount / (PedidosProvPageSize > 0 ? PedidosProvPageSize : 20));

    // ── Inventario: Resumen ──
    public List<ChartDataPoint> EvolucionVentas { get; set; } = new();
    public List<TopProductoVendidoViewModel> ProductosMasVendidos { get; set; } = new();
    public List<Product> ProductosAgotados { get; set; } = new();
    public int ProductosAgotadosCount { get; set; }
    public List<Product> ProductosPorAgotarse { get; set; } = new();
    public int ProductosPorAgotarseCount { get; set; }

    // ── Ventas ──
    public List<Order> Ventas { get; set; } = new();

    // ── Citas ──
    public List<Appointment> Citas { get; set; } = new();

    // ── Usuarios ──
    public List<User> Clientes { get; set; } = new();
    public List<User> TodosLosClientes { get; set; } = new();
    public string? ClientesSearchTerm { get; set; }
    public int ClientesPage { get; set; } = 1;
    public int ClientesPageSize { get; set; } = 5;
    public int ClientesTotalCount { get; set; }
    public int ClientesTotalPages => (int)Math.Ceiling((double)ClientesTotalCount / (ClientesPageSize > 0 ? ClientesPageSize : 5));

    public List<TrabajadorItemViewModel> Trabajadores { get; set; } = new();
    public string? TrabajadoresSearchTerm { get; set; }
    public int TrabajadoresPage { get; set; } = 1;
    public int TrabajadoresPageSize { get; set; } = 5;
    public int TrabajadoresTotalCount { get; set; }
    public int TrabajadoresTotalPages => (int)Math.Ceiling((double)TrabajadoresTotalCount / (TrabajadoresPageSize > 0 ? TrabajadoresPageSize : 5));

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


public class TrabajadorItemViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string Puesto { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
    public decimal Salario { get; set; }
    public string Rol { get; set; } = "Trabajador";
    public bool Activo { get; set; } = true;
    public int PedidosCount { get; set; }
}

public class ChartDataPoint
{
    public string Periodo { get; set; } = string.Empty;
    public decimal TotalVentas { get; set; }
    public int CantidadPedidos { get; set; }
}

public class TopProductoVendidoViewModel
{
    public int ProductId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string? ImagenUrl { get; set; }
    public int CantidadVendida { get; set; }
    public decimal TotalRecaudado { get; set; }
    public int StockActual { get; set; }
    public bool Activo { get; set; } = true;
}

