using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public interface IAuthService
{
    Task<User?> LoginAsync(string email, string password);
    Task<User?> RegisterAsync(string nombre, string apellido, string tipoDocumento, string numeroDocumento, string email, string telefono, string password);
    Task<User?> GetUserByIdAsync(int id);
    Task<User?> GetUserByEmailAsync(string email);
    Task UpdateUserAsync(User user);
}
