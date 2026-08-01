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

    // ── Facturas (Paginadas y Filtradas) ──
    public List<Invoice> Facturas { get; set; } = new();
    public string? FacturasSearchTerm { get; set; }
    public int FacturasPage { get; set; } = 1;
    public int FacturasPageSize { get; set; } = 5;
    public int FacturasTotalCount { get; set; }
    public int FacturasTotalPages => (int)Math.Ceiling((double)FacturasTotalCount / (FacturasPageSize > 0 ? FacturasPageSize : 5));
    public List<Order> PedidosDisponibles { get; set; } = new();

    // ── Inventarios (solo lectura) ──
    public List<Product> Productos { get; set; } = new();

    // ── Clientes ──
    public List<User> Clientes { get; set; } = new();

    // ── Perfil ──
    public User? UsuarioActual { get; set; }

    // Navigation
    public string ActiveSection { get; set; } = "general";
}
