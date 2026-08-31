using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LentSoft.Web.Models.Entities;

public class User : IValidatableObject
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El apellido debe tener entre 2 y 100 caracteres")]
    public string Apellido { get; set; } = string.Empty;

    [Required(ErrorMessage = "El tipo de documento es obligatorio")]
    [StringLength(20)]
    public string TipoDocumento { get; set; } = "CC";

    [Required(ErrorMessage = "El número de documento es obligatorio")]
    [StringLength(30)]
    public string NumeroDocumento { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es obligatorio")]
    [EmailAddress(ErrorMessage = "El email no es válido")]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [StringLength(20)]
    [Phone(ErrorMessage = "El teléfono no es válido")]
    public string? Telefono { get; set; }

    [Required]
    [StringLength(20)]
    public string Role { get; set; } = "usuario";

    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    public DateTime? UltimaCompra { get; set; }

    // Campos Clínicos del Paciente (Tarea 1)
    public DateTime? FechaNacimiento { get; set; }

    [StringLength(20)]
    public string? Genero { get; set; }

    [StringLength(255)]
    public string? Direccion { get; set; }

    [StringLength(100)]
    public string? EPS { get; set; }

    [StringLength(20)]
    public string? EstadoPaciente { get; set; } = "Activo";

    [StringLength(2000)]
    public string? ObservacionesPaciente { get; set; }

    // Datos Profesionales del Optómetra (Tarea 6)
    [StringLength(50)]
    public string? RegistroMedico { get; set; }

    [StringLength(150)]
    public string? Universidad { get; set; }

    [StringLength(200)]
    public string? EspecialidadDetalle { get; set; }

    public int? AniosExperiencia { get; set; }

    [StringLength(500)]
    public string? FotoUrl { get; set; }

    public bool Activo { get; set; } = true;

    // Computed display name
    [NotMapped]
    public string NombreCompleto => $"{Nombre} {Apellido}".Trim();

    // Navigation properties
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validRoles = new[] { "usuario", "admin", "optometra", "ventas" };
        if (!validRoles.Contains(Role))
        {
            yield return new ValidationResult(
                "El rol debe ser 'usuario', 'admin', 'optometra' o 'ventas'",
                new[] { nameof(Role) });
        }

        var validDocTypes = new[] { "CC", "CE", "TI", "Pasaporte" };
        if (!validDocTypes.Contains(TipoDocumento))
        {
            yield return new ValidationResult(
                "El tipo de documento debe ser 'CC', 'CE', 'TI' o 'Pasaporte'",
                new[] { nameof(TipoDocumento) });
        }
    }
}
