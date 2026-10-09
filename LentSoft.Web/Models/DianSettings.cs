namespace LentSoft.Web.Models;

public class DianSettings
{
    public const string SectionName = "DianSettings";

    public string ResolucionNumero { get; set; } = "18764028920000";
    public string RangoDesde { get; set; } = "FAC-2026-0001";
    public string RangoHasta { get; set; } = "FAC-2026-9999";
    public string EmpresaNit { get; set; } = "900123456-7";
    public string EmpresaNombre { get; set; } = "LentSoft Óptica S.A.S.";
    public string EmpresaDireccion { get; set; } = "Calle 100 # 15-20, Bogotá, Colombia";
    public string EmpresaTelefono { get; set; } = "(+57) 300 123 4567";
}
