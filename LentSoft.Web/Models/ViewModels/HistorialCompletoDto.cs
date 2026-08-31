namespace LentSoft.Web.Models.ViewModels;

/// <summary>
/// DTO para el resultado de sp_HistorialCompletoPaciente.
/// Combina HistorialesClinicos, ExamenesVisuales y FormulasOpticas en una línea de tiempo.
/// </summary>
public class HistorialCompletoDto
{
    /// <summary>Tipo de registro: 'Historial', 'Examen' o 'Formula'</summary>
    public string TipoRegistro { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }

    /// <summary>Descripción principal del registro (diagnóstico, tipo examen, tipo lente)</summary>
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>Información adicional (observaciones, resultado, OD/OI)</summary>
    public string? Detalles { get; set; }
}
