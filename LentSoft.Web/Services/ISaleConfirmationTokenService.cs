namespace LentSoft.Web.Services;

public interface ISaleConfirmationTokenService
{
    string GenerateToken(int saleId);
    int? ValidateToken(string token);
}
