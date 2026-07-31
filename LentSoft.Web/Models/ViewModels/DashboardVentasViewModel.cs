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

    // ── Facturas ──
    public List<Invoice> Facturas { get; set; } = new();

    // ── Inventarios (solo lectura) ──
    public List<Product> Productos { get; set; } = new();

    // ── Perfil ──
    public User? UsuarioActual { get; set; }

    // Navigation
    public string ActiveSection { get; set; } = "general";
}
