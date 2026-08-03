using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace LentSoft.Web.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
    {
        var settings = _configuration.GetSection("EmailSettings");
        var smtpServer = settings["SmtpServer"]!;
        var smtpPort = int.Parse(settings["SmtpPort"] ?? "587");
        var smtpUser = settings["SmtpUser"]!;
        var smtpPassword = settings["SmtpPassword"]!;
        var fromEmail = settings["FromEmail"]!;
        var fromName = settings["FromName"] ?? "LentSoft";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Recuperar Contraseña - LentSoft";

        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
</head>
<body style=""margin: 0; padding: 0; background-color: #FAF5FF; font-family: 'Segoe UI', Arial, sans-serif;"">
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #FAF5FF; padding: 40px 20px;"">
        <tr>
            <td align=""center"">
                <table width=""520"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #FFFFFF; border-radius: 16px; box-shadow: 0 4px 24px rgba(0,0,0,0.08); overflow: hidden;"">
                    <!-- Header -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #7C3AED, #4C1D95); padding: 32px 40px; text-align: center;"">
                            <h1 style=""color: #FFFFFF; margin: 0; font-size: 24px; font-weight: 700; letter-spacing: 0.5px;"">LentSoft</h1>
                            <p style=""color: #E9D5FF; margin: 6px 0 0 0; font-size: 13px;"">Soluciones Visuales de Alta Calidad</p>
                        </td>
                    </tr>
                    <!-- Body -->
                    <tr>
                        <td style=""padding: 36px 40px;"">
                            <h2 style=""color: #4C1D95; margin: 0 0 16px 0; font-size: 20px; font-weight: 700;"">Recuperar Contraseña</h2>
                            <p style=""color: #4B5563; font-size: 14px; line-height: 1.7; margin: 0 0 24px 0;"">
                                Recibimos una solicitud para restablecer la contraseña de tu cuenta. 
                                Haz clic en el botón de abajo para crear una nueva contraseña.
                            </p>
                            <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
                                <tr>
                                    <td align=""center"" style=""padding: 8px 0 24px 0;"">
                                        <a href=""{resetLink}"" 
                                           style=""display: inline-block; background: linear-gradient(135deg, #7C3AED, #9333EA); color: #FFFFFF; text-decoration: none; padding: 14px 36px; border-radius: 9999px; font-size: 15px; font-weight: 700; letter-spacing: 0.3px;"">
                                            Restablecer Contraseña
                                        </a>
                                    </td>
                                </tr>
                            </table>
                            <p style=""color: #6B7280; font-size: 13px; line-height: 1.6; margin: 0 0 12px 0;"">
                                Este enlace expirará en <strong>30 minutos</strong>. Si no solicitaste este cambio, puedes ignorar este correo de forma segura.
                            </p>
                            <hr style=""border: none; border-top: 1px solid #E9D5FF; margin: 20px 0;"" />
                            <p style=""color: #9CA3AF; font-size: 12px; margin: 0;"">
                                Si el botón no funciona, copia y pega este enlace en tu navegador:
                            </p>
                            <p style=""color: #7C3AED; font-size: 11px; word-break: break-all; margin: 6px 0 0 0;"">
                                {resetLink}
                            </p>
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td style=""background-color: #F3E8FF; padding: 20px 40px; text-align: center;"">
                            <p style=""color: #6B7280; font-size: 12px; margin: 0;"">
                                &copy; 2026 LentSoft Optometría. Todos los derechos reservados.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

        message.Body = new TextPart("html") { Text = htmlBody };

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpUser, smtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            _logger.LogInformation("Correo de recuperación enviado a {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar correo de recuperación a {Email}", toEmail);
            throw;
        }
    }
}
