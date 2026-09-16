using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string toEmail, string resetLink);
    Task SendSaleConfirmationEmailAsync(string toEmail, Order order, string confirmLink);
}
