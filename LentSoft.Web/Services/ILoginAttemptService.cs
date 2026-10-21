namespace LentSoft.Web.Services;

/// <summary>
/// Servicio para la gestión y mitigación de ataques de fuerza bruta mediante bloqueo temporal de intentos de inicio de sesión.
/// Regla de negocio: 5 intentos fallidos en una ventana de 15 minutos resultan en un bloqueo temporal de 15 minutos.
/// </summary>
public interface ILoginAttemptService
{
    /// <summary>
    /// Comprueba si la clave indicada (combinación de correo e IP) se encuentra en estado de bloqueo temporal.
    /// </summary>
    /// <param name="key">Identificador único (correo en minúsculas + IP del cliente).</param>
    /// <param name="remaining">Tiempo restante de bloqueo en caso de estar bloqueado.</param>
    /// <returns><c>true</c> si la cuenta/IP está bloqueada; de lo contrario <c>false</c>.</returns>
    bool IsLockedOut(string key, out TimeSpan remaining);

    /// <summary>
    /// Registra un intento fallido de autenticación para la clave indicada.
    /// Si se alcanza el umbral de 5 intentos dentro de la ventana de 15 minutos, se activa el bloqueo de 15 minutos.
    /// </summary>
    /// <param name="key">Identificador único (correo en minúsculas + IP del cliente).</param>
    void RegisterFailure(string key);

    /// <summary>
    /// Reinicia y elimina el historial de intentos fallidos para la clave tras un inicio de sesión exitoso.
    /// </summary>
    /// <param name="key">Identificador único (correo en minúsculas + IP del cliente).</param>
    void Reset(string key);
}
