using System.Text.Json.Serialization;

namespace LentSoft.Mobile.Models;

public class UserDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("apellido")]
    public string Apellido { get; set; } = string.Empty;

    [JsonPropertyName("nombreCompleto")]
    public string NombreCompleto { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("telefono")]
    public string? Telefono { get; set; }

    [JsonPropertyName("tipoDocumento")]
    public string TipoDocumento { get; set; } = "CC";

    [JsonPropertyName("numeroDocumento")]
    public string NumeroDocumento { get; set; } = string.Empty;

    [JsonPropertyName("direccion")]
    public string? Direccion { get; set; }

    [JsonPropertyName("role")]
    public string Role { get; set; } = "usuario";

    public string Initials
    {
        get
        {
            var n = !string.IsNullOrWhiteSpace(Nombre) ? Nombre[0].ToString().ToUpper() : "L";
            var a = !string.IsNullOrWhiteSpace(Apellido) ? Apellido[0].ToString().ToUpper() : "S";
            return $"{n}{a}";
        }
    }
}
