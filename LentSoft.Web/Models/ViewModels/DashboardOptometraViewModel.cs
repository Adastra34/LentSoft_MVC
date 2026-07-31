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

    // ── Historial Clínico (mock) ──
    public List<HistorialClinicoMock> HistorialClinico { get; set; } = new();

    // ── Exámenes Visuales (mock) ──
    public List<ExamenVisualMock> ExamenesVisuales { get; set; } = new();

    // ── Fórmulas Ópticas (mock) ──
    public List<FormulaOpticaMock> FormulasOpticas { get; set; } = new();

    // ── Perfil del optómetra ──
    public User? UsuarioActual { get; set; }

    // Navigation
    public string ActiveSection { get; set; } = "dashboard";
}

/// <summary>Mock: Historial clínico</summary>
public class HistorialClinicoMock
{
    public int Id { get; set; }
    public string Paciente { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string Diagnostico { get; set; } = string.Empty;
    public string Tratamiento { get; set; } = string.Empty;
    public string Optometra { get; set; } = string.Empty;
}

/// <summary>Mock: Examen visual</summary>
public class ExamenVisualMock
{
    public int Id { get; set; }
    public string Paciente { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string TipoExamen { get; set; } = string.Empty;
    public string OjoDerecho { get; set; } = string.Empty;
    public string OjoIzquierdo { get; set; } = string.Empty;
    public string Resultado { get; set; } = string.Empty;
}

/// <summary>Mock: Fórmula óptica</summary>
public class FormulaOpticaMock
{
    public int Id { get; set; }
    public string Paciente { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string EsferaOD { get; set; } = string.Empty;
    public string CilindroOD { get; set; } = string.Empty;
    public string EjeOD { get; set; } = string.Empty;
    public string EsferaOI { get; set; } = string.Empty;
    public string CilindroOI { get; set; } = string.Empty;
    public string EjeOI { get; set; } = string.Empty;
    public string Observaciones { get; set; } = string.Empty;
}
