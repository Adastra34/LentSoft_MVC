using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Models;

public enum LoginResult
{
    Success,
    InvalidCredentials,
    AccountLocked
}

public class LoginAttemptResult
{
    public LoginResult Result { get; set; }
    public User? User { get; set; }
    public TimeSpan? TiempoRestanteBloqueo { get; set; }
}
