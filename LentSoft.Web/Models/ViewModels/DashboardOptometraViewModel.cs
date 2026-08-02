using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Models.ViewModels;

public class DashboardOptometraViewModel
{
    // ── Dashboard / Resumen ──
    public int TotalPacientes { get; set; }
    public int CitasHoy { get; set; }
    public int CitasPendientes { get; set; }
    public int ExamenesEsteMes { get; set; }
    public int TotalExamenes { get; set; }
    public int TotalFormulas { get; set; }
    public string ProximaCitaFecha { get; set; } = "Sin citas";

    // ── Pacientes ──
    public List<User> Pacientes { get; set; } = new();

    // ── Citas ──
    public List<Appointment> Citas { get; set; } = new();

    // ── Historial Clínico ──
    public List<HistorialClinico> HistorialClinico { get; set; } = new();

    // ── Exámenes Visuales ──
    public List<ExamenVisual> ExamenesVisuales { get; set; } = new();

    // ── Fórmulas Ópticas ──
    public List<FormulaOptica> FormulasOpticas { get; set; } = new();

    // ── Perfil del optómetra ──
    public User? UsuarioActual { get; set; }

    // Navigation
    public string ActiveSection { get; set; } = "dashboard";
}
