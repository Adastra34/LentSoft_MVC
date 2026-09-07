using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public interface IMobileJwtService
{
    string GenerateToken(User user);
}
