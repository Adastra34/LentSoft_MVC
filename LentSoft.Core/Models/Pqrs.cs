namespace LentSoft.Core.Models;

public class Pqrs
{
    public int Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Asunto { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public string Estado { get; set; } = "Pendiente";
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public string? RespuestaAdmin { get; set; }
    public DateTime? FechaRespuesta { get; set; }
    public int UserId { get; set; }
    public string? Radicado { get; set; }
}
