namespace LentSoft.Web.Models.ViewModels;

/// <summary>
/// Fuente única de verdad para el catálogo de servicios de citas optométricas en LentSoft.
/// Contiene la unión de todos los servicios disponibles para Administrador, Optómetra y Usuario.
/// </summary>
public static class ServiciosCita
{
    public static readonly IReadOnlyList<string> Todos = new[]
    {
        "Examen visual completo",
        "Examen de vista",
        "Primera consulta",
        "Control de lentes",
        "Ajuste de lentes",
        "Ajuste y mantenimiento de lentes",
        "Consulta por lentes de contacto",
        "Adaptación de lentes de contacto",
        "Adaptación lentes contacto",
        "Control optometría",
        "Seguimiento glaucoma",
        "Consulta General"
    };

    public static readonly IReadOnlyDictionary<string, string> ClavesI18n = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Examen visual completo"] = "user.service.exam",
        ["Control de lentes"] = "user.service.control",
        ["Primera consulta"] = "user.service.first",
        ["Adaptación de lentes de contacto"] = "user.service.contact",
        ["Ajuste y mantenimiento de lentes"] = "user.service.adjustment",
        ["Examen de vista"] = "user.service.exam_vista",
        ["Ajuste de lentes"] = "user.service.ajuste",
        ["Consulta por lentes de contacto"] = "user.service.consulta_contacto",
        ["Adaptación lentes contacto"] = "user.service.adaptacion_contacto",
        ["Control optometría"] = "user.service.control_opto",
        ["Seguimiento glaucoma"] = "user.service.glaucoma",
        ["Consulta General"] = "user.service.general"
    };
}

