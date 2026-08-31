namespace LentSoft.Web.Services;

public interface IPasswordResetTokenService
{
    string GenerateToken(int userId, string email);
    int? ValidateToken(string token);
}
