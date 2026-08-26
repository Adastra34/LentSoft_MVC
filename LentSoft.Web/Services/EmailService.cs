using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using LentSoft.Web.Models.Entities;

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

    public async Task SendSaleConfirmationEmailAsync(string toEmail, Order order, string confirmLink)
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
        message.Subject = $"Confirmación de tu compra en LentSoft - Orden #ORD-{order.Id:D4}";

        // Format products table rows
        var rowsHtml = "";
        foreach (var item in order.OrderItems)
        {
            var productName = item.Product?.Nombre ?? $"Producto #{item.ProductId}";
            rowsHtml += $@"
                <tr style=""border-bottom: 1px solid #E9D5FF;"">
                    <td style=""padding: 10px; color: #4B5563;"">{productName}</td>
                    <td style=""padding: 10px; text-align: center; color: #4B5563;"">{item.Cantidad}</td>
                    <td style=""padding: 10px; text-align: right; color: #4B5563;"">{item.PrecioUnitario.ToString("C")}</td>
                    <td style=""padding: 10px; text-align: right; font-weight: 700; color: #4C1D95;"">{item.Subtotal.ToString("C")}</td>
                </tr>";
        }

        // Calculate discount
        decimal subtotalSum = order.OrderItems.Sum(oi => oi.Subtotal);
        decimal discountAmount = subtotalSum - order.Total;
        string discountRowHtml = "";
        if (discountAmount > 0)
        {
            var discountPercent = (int)Math.Round((discountAmount / subtotalSum) * 100);
            discountRowHtml = $@"
                <tr style=""color: #B45309; font-size: 13px;"">
                    <td colspan=""3"" style=""padding: 8px 10px; text-align: right;"">Descuento ({discountPercent}%):</td>
                    <td style=""padding: 8px 10px; text-align: right; font-weight: bold;"">-{discountAmount.ToString("C")}</td>
                </tr>";
        }

        var clientName = order.User != null ? order.User.NombreCompleto : "Cliente";

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
                <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #FFFFFF; border-radius: 16px; box-shadow: 0 4px 24px rgba(0,0,0,0.08); overflow: hidden;"">
                    <!-- Header -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #7C3AED, #4C1D95); padding: 32px 40px; text-align: center;"">
                            <h1 style=""color: #FFFFFF; margin: 0; font-size: 24px; font-weight: 700; letter-spacing: 0.5px;"">LentSoft</h1>
                            <p style=""color: #E9D5FF; margin: 6px 0 0 0; font-size: 13px;"">Confirmación de Compra</p>
                        </td>
                    </tr>
                    <!-- Body -->
                    <tr>
                        <td style=""padding: 36px 40px;"">
                            <h2 style=""color: #4C1D95; margin: 0 0 16px 0; font-size: 20px; font-weight: 700;"">¡Gracias por tu compra, {clientName}!</h2>
                            <p style=""color: #4B5563; font-size: 14px; line-height: 1.7; margin: 0 0 24px 0;"">
                                Hemos registrado tu pedido con éxito. A continuación encontrarás el resumen de tu compra:
                            </p>
                            
                            <!-- Detalle de Venta -->
                            <table width=""100%"" style=""margin-bottom: 24px; font-size: 14px; color: #4B5563;"">
                                <tr>
                                    <td style=""padding: 6px 0; font-weight: bold; width: 140px;"">Orden:</td>
                                    <td style=""padding: 6px 0;"">#ORD-{order.Id:D4}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 6px 0; font-weight: bold;"">Fecha:</td>
                                    <td style=""padding: 6px 0;"">{order.FechaPedido.ToLocalTime().ToString("dd/MM/yyyy HH:mm")}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 6px 0; font-weight: bold;"">Método de Pago:</td>
                                    <td style=""padding: 6px 0;"">{order.MetodoPagoSimulado ?? "Efectivo"}</td>
                                </tr>
                            </table>

                            <!-- Tabla Productos -->
                            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border-collapse: collapse; margin-bottom: 24px; font-size: 13px;"">
                                <thead>
                                    <tr style=""background-color: #F3E8FF; color: #4C1D95; text-align: left;"">
                                        <th style=""padding: 8px 10px; border-bottom: 2px solid #E9D5FF;"">Producto</th>
                                        <th style=""padding: 8px 10px; border-bottom: 2px solid #E9D5FF; text-align: center;"">Cant</th>
                                        <th style=""padding: 8px 10px; border-bottom: 2px solid #E9D5FF; text-align: right;"">P. Unit</th>
                                        <th style=""padding: 8px 10px; border-bottom: 2px solid #E9D5FF; text-align: right;"">Subtotal</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {rowsHtml}
                                </tbody>
                                <tfoot>
                                    {discountRowHtml}
                                    <tr style=""font-weight: 700; color: #4C1D95; font-size: 15px;"">
                                        <td colspan=""3"" style=""padding: 10px 10px; text-align: right; border-top: 2px solid #E9D5FF;"">Total Pagado:</td>
                                        <td style=""padding: 10px 10px; text-align: right; border-top: 2px solid #E9D5FF;"">{order.Total.ToString("C")}</td>
                                    </tr>
                                </tfoot>
                            </table>

                            <!-- Botón Link Confirmación -->
                            <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
                                <tr>
                                    <td align=""center"" style=""padding: 8px 0 24px 0;"">
                                        <a href=""{confirmLink}"" 
                                           style=""display: inline-block; background: linear-gradient(135deg, #7C3AED, #9333EA); color: #FFFFFF; text-decoration: none; padding: 14px 36px; border-radius: 9999px; font-size: 15px; font-weight: 700; letter-spacing: 0.3px;"">
                                            Ver detalle de mi compra
                                        </a>
                                    </td>
                                </tr>
                            </table>
                            
                            <hr style=""border: none; border-top: 1px solid #E9D5FF; margin: 20px 0;"" />
                            <p style=""color: #9CA3AF; font-size: 12px; margin: 0; text-align: center;"">
                                Si tienes alguna duda, comunícate con soporte. Este correo es una confirmación automática.
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
            _logger.LogInformation("Correo de confirmación de venta enviado a {Email} para Orden #ORD-{OrderId:D4}", toEmail, order.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar correo de confirmación de venta a {Email}", toEmail);
            throw;
        }
    }
}
