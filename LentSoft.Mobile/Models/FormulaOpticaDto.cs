using System.Text.Json.Serialization;

namespace LentSoft.Mobile.Models;

public class FormulaOpticaDto
{
    [JsonPropertyName("hasFormula")]
    public bool HasFormula { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("formulaId")]
    public int FormulaId { get; set; }

    [JsonPropertyName("fecha")]
    public string? Fecha { get; set; }

    [JsonPropertyName("fechaVencimiento")]
    public string? FechaVencimiento { get; set; }

    [JsonPropertyName("diasParaVencer")]
    public int DiasParaVencer { get; set; }

    [JsonPropertyName("estado")]
    public string Estado { get; set; } = "Vigente";

    [JsonPropertyName("esferaOD")]
    public object? EsferaODRaw { get; set; }

    [JsonPropertyName("cilindroOD")]
    public object? CilindroODRaw { get; set; }

    [JsonPropertyName("ejeOD")]
    public object? EjeODRaw { get; set; }

    [JsonPropertyName("adicionOD")]
    public object? AdicionODRaw { get; set; }

    [JsonPropertyName("esferaOI")]
    public object? EsferaOIRaw { get; set; }

    [JsonPropertyName("cilindroOI")]
    public object? CilindroOIRaw { get; set; }

    [JsonPropertyName("ejeOI")]
    public object? EjeOIRaw { get; set; }

    [JsonPropertyName("adicionOI")]
    public object? AdicionOIRaw { get; set; }

    [JsonPropertyName("tipoLente")]
    public string? TipoLente { get; set; }

    [JsonPropertyName("distanciaPupilar")]
    public string? DistanciaPupilar { get; set; }

    [JsonPropertyName("observaciones")]
    public string? Observaciones { get; set; }

    [JsonPropertyName("optometraNombre")]
    public string? OptometraNombre { get; set; }

    // Helpers para lectura de valores
    public string EsferaOD => EsferaODRaw?.ToString() ?? "0.00";
    public string CilindroOD => CilindroODRaw?.ToString() ?? "0.00";
    public string EjeOD => EjeODRaw?.ToString() ?? "0";
    public string? AdicionOD => AdicionODRaw?.ToString();

    public string EsferaOI => EsferaOIRaw?.ToString() ?? "0.00";
    public string CilindroOI => CilindroOIRaw?.ToString() ?? "0.00";
    public string EjeOI => EjeOIRaw?.ToString() ?? "0";
    public string? AdicionOI => AdicionOIRaw?.ToString();

    // Propiedades de ayuda para la interfaz
    public bool IsVigente => (Estado.Equals("Vigente", StringComparison.OrdinalIgnoreCase) || Estado.Equals("completada", StringComparison.OrdinalIgnoreCase)) && DiasParaVencer >= 0;
    public bool IsProximaAVencer => IsVigente && DiasParaVencer <= 30;
    public bool IsVencida => !IsVigente;

    public string VigenciaBadgeText
    {
        get
        {
            if (IsVencida) return "Vencida";
            if (IsProximaAVencer) return $"Vence en {DiasParaVencer}d";
            return "Vigente";
        }
    }

    public string BadgeText => VigenciaBadgeText;
    public string BadgeBackgroundColor => IsVencida ? "#FEE2E2" : (IsProximaAVencer ? "#FEF3C7" : "#DCFCE7");
    public string BadgeTextColor => IsVencida ? "#DC2626" : (IsProximaAVencer ? "#D97706" : "#2FBF71");

    public string DisplayEsferaOD => string.IsNullOrWhiteSpace(EsferaOD) ? "0.00" : EsferaOD;
    public string DisplayCilindroOD => string.IsNullOrWhiteSpace(CilindroOD) ? "0.00" : CilindroOD;
    public string DisplayEjeOD => string.IsNullOrWhiteSpace(EjeOD) ? "0°" : (EjeOD.EndsWith("°") ? EjeOD : $"{EjeOD}°");
    public string DisplayAdicionOD => string.IsNullOrWhiteSpace(AdicionOD) ? "—" : AdicionOD;

    public string DisplayEsferaOI => string.IsNullOrWhiteSpace(EsferaOI) ? "0.00" : EsferaOI;
    public string DisplayCilindroOI => string.IsNullOrWhiteSpace(CilindroOI) ? "0.00" : CilindroOI;
    public string DisplayEjeOI => string.IsNullOrWhiteSpace(EjeOI) ? "0°" : (EjeOI.EndsWith("°") ? EjeOI : $"{EjeOI}°");
    public string DisplayAdicionOI => string.IsNullOrWhiteSpace(AdicionOI) ? "—" : AdicionOI;

    public string DisplayDistanciaPupilar => string.IsNullOrWhiteSpace(DistanciaPupilar) ? "—" : DistanciaPupilar;
    public string DisplayFechaEmision => string.IsNullOrWhiteSpace(Fecha) ? "—" : Fecha;
    public string DisplayFechaVencimiento => string.IsNullOrWhiteSpace(FechaVencimiento) ? "—" : FechaVencimiento;
    public string DisplayObservaciones => string.IsNullOrWhiteSpace(Observaciones) ? "Sin observaciones adicionales." : Observaciones;
    public string DoctorDisplay => string.IsNullOrWhiteSpace(OptometraNombre) ? "Especialista en Optometría" : OptometraNombre;
    public string TipoLenteDisplay => string.IsNullOrWhiteSpace(TipoLente) ? "Monofocal" : TipoLente;

    public bool TieneAdicion => !string.IsNullOrWhiteSpace(AdicionOD) || !string.IsNullOrWhiteSpace(AdicionOI);
    public bool TieneObservaciones => !string.IsNullOrWhiteSpace(Observaciones);
}
