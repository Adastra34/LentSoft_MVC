namespace LentSoft.Web.Services;

/// <summary>
/// Servicio para la generación y validación de tokens JWT de recuperación de contraseña de un solo uso.
/// Utiliza una huella digital criptográfica (claim "pwdfp") basada en el PasswordHash actual para invalidar
/// automáticamente el token una vez la contraseña ha sido actualizada, sin requerir persistencia en base de datos.
/// </summary>
public interface IPasswordResetTokenService
{
    /// <summary>
    /// Genera un token JWT firmado para la recuperación de contraseña con vigencia configurada.
    /// Incluye un fingerprint ("pwdfp") de los primeros 16 caracteres hexadecimales del SHA256 del PasswordHash actual.
    /// </summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <param name="email">Correo electrónico del usuario.</param>
    /// <param name="passwordHash">Hash actual de la contraseña del usuario.</param>
    /// <returns>Token JWT en formato string.</returns>
    string GenerateToken(int userId, string email, string passwordHash);

    /// <summary>
    /// Valida la firma criptográfica, emisor, audiencia, expiración y propósito del token.
    /// </summary>
    /// <param name="token">Token JWT a validar.</param>
    /// <returns>El identificador del usuario si el token es válido; de lo contrario <c>null</c>.</returns>
    int? ValidateToken(string token);

    /// <summary>
    /// Comprueba si el fingerprint ("pwdfp") incrustado en el token coincide con el PasswordHash actual del usuario.
    /// Si el usuario ya cambió su contraseña previamente, el fingerprint diferirá y el método devolverá <c>false</c>.
    /// </summary>
    /// <param name="token">Token JWT recibido.</param>
    /// <param name="currentPasswordHash">Hash de contraseña actual en base de datos.</param>
    /// <returns><c>true</c> si el token corresponde a la contraseña vigente; de lo contrario <c>false</c>.</returns>
    bool MatchesCurrentPassword(string token, string currentPasswordHash);
}
