using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

/// <summary>
/// Contrato del servicio de autenticación y registro de usuarios en LentSoft.
/// Gestiona la validación de credenciales, unicidad de identidad y persistencia segura de contraseñas.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Autentica a un usuario mediante su correo electrónico y contraseña.
    /// Solo permite el acceso a usuarios activos (<c>Activo == true</c>).
    /// El correo es normalizado con Trim() y ToLowerInvariant().
    /// </summary>
    /// <param name="email">Correo electrónico del usuario.</param>
    /// <param name="password">Contraseña en texto plano a verificar.</param>
    /// <returns>La entidad <see cref="User"/> si las credenciales son válidas y la cuenta está activa; de lo contrario <c>null</c>.</returns>
    Task<User?> LoginAsync(string email, string password);

    /// <summary>
    /// Registra un nuevo usuario en el sistema con el rol predeterminado "usuario".
    /// Verifica la unicidad del correo (normalizado a minúsculas) y del número de documento.
    /// Hashea la contraseña mediante BCrypt.
    /// </summary>
    /// <param name="nombre">Nombre de pila.</param>
    /// <param name="apellido">Apellidos.</param>
    /// <param name="tipoDocumento">Tipo de documento de identidad (ej. CC, TI, CE, Pasaporte).</param>
    /// <param name="numeroDocumento">Número único de identificación.</param>
    /// <param name="email">Correo electrónico único.</param>
    /// <param name="telefono">Número telefónico de contacto.</param>
    /// <param name="password">Contraseña en texto plano a almacenar de forma segura.</param>
    /// <returns>La entidad <see cref="User"/> creada o <c>null</c> si el correo o documento ya existen.</returns>
    Task<User?> RegisterAsync(string nombre, string apellido, string tipoDocumento, string numeroDocumento, string email, string telefono, string password);

    /// <summary>
    /// Obtiene un usuario a partir de su identificador numérico único.
    /// </summary>
    /// <param name="id">Identificador del usuario.</param>
    /// <returns>La entidad <see cref="User"/> o <c>null</c> si no se encuentra.</returns>
    Task<User?> GetUserByIdAsync(int id);

    /// <summary>
    /// Obtiene un usuario a partir de su correo electrónico normalizado (Trim y ToLowerInvariant).
    /// </summary>
    /// <param name="email">Correo electrónico a buscar.</param>
    /// <returns>La entidad <see cref="User"/> o <c>null</c> si no existe.</returns>
    Task<User?> GetUserByEmailAsync(string email);

    /// <summary>
    /// Actualiza y persiste los cambios de una entidad de usuario existente.
    /// </summary>
    /// <param name="user">Entidad de usuario con los datos actualizados.</param>
    Task UpdateUserAsync(User user);
}
