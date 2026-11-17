using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LentSoft.Web.Models.Entities;

public class Pqrs
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "El tipo de PQRS es obligatorio.")]
    [StringLength(50)]
    public string Tipo { get; set; } = string.Empty; // Petición, Queja, Reclamo, Solicitud

    [Required(ErrorMessage = "El asunto es obligatorio.")]
    [StringLength(200, ErrorMessage = "El asunto no puede superar 200 caracteres.")]
    public string Asunto { get; set; } = string.Empty;

    [Required(ErrorMessage = "El mensaje es obligatorio.")]
    [StringLength(2000, ErrorMessage = "El mensaje no puede superar 2000 caracteres.")]
    public string Mensaje { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Estado { get; set; } = "Pendiente"; // Pendiente, En Proceso, Resuelto

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    [StringLength(2000)]
    public string? RespuestaAdmin { get; set; }

    public DateTime? FechaRespuesta { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }

    [StringLength(50)]
    public string? Radicado { get; set; }

    [NotMapped]
    public string CodigoRadicado => !string.IsNullOrWhiteSpace(Radicado)
        ? Radicado
        : $"PQRS-{FechaCreacion:yyyyMMdd}-{Id:D4}";
}
