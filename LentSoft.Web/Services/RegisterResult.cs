using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

/// <summary>
/// Resultado del proceso de registro de usuarios, permitiendo identificar
/// con precisión si la duplicidad ocurrió por correo electrónico o por número de documento.
/// </summary>
public class RegisterResult
{
    public bool Success => User != null;
    public User? User { get; set; }
    public bool EmailExists { get; set; }
    public bool DocumentExists { get; set; }
    public string? ErrorMessage { get; set; }
}

