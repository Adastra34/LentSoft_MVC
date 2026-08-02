using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Models.ViewModels;

public class DashboardUsuarioViewModel
{
    public User Usuario { get; set; } = null!;
    public List<Order> Pedidos { get; set; } = new();
    public List<Appointment> Citas { get; set; } = new();
    public List<Product> Favoritos { get; set; } = new();
    public List<HistorialClinico> Historiales { get; set; } = new();
    public List<FormulaOptica> Formulas { get; set; } = new();

    // ── Citas Datatable (Paginadas y Filtradas) ──
    public string? CitasSearchTerm { get; set; }
    public int CitasPage { get; set; } = 1;
    public int CitasPageSize { get; set; } = 5;
    public int CitasTotalCount { get; set; }
    public int CitasTotalPages => (int)Math.Ceiling((double)CitasTotalCount / (CitasPageSize > 0 ? CitasPageSize : 5));

    // Active section
    public string ActiveSection { get; set; } = "perfil";
}
