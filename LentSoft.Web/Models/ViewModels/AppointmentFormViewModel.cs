using System.ComponentModel.DataAnnotations;

namespace LentSoft.Web.Models.ViewModels;

public class AppointmentFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Debe seleccionar un paciente / usuario")]
    [Display(Name = "Paciente / Usuario")]
    public int UserId { get; set; }

    [Required(ErrorMessage = "El servicio es obligatorio")]
    [Display(Name = "Servicio")]
    public string Servicio { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha y hora son obligatorias")]
    [Display(Name = "Fecha y Hora")]
    public DateTime FechaHora { get; set; } = DateTime.UtcNow;

    [Display(Name = "Notas / Motivo")]
    public string? Notas { get; set; }

    [Display(Name = "Estado")]
    public string Estado { get; set; } = "pendiente";
}
