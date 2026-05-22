using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace TracKeee.Services
{
    public class InvoiceEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<InvoiceEmailService> _logger;

        public InvoiceEmailService(IConfiguration configuration, ILogger<InvoiceEmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendInvoiceEmail(string toEmail, string toName, string subject, string htmlBody, byte[] pdfAttachment, string pdfFileName, string? fromName = null)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(
                    fromName ?? _configuration["BrevoSmtp:FromName"],
                    _configuration["BrevoSmtp:FromEmail"]));
                message.To.Add(new MailboxAddress(toName, toEmail));
                message.Subject = subject;

                var builder = new BodyBuilder
                {
                    HtmlBody = htmlBody
                };

                builder.Attachments.Add(pdfFileName, pdfAttachment, new ContentType("application", "pdf"));

                message.Body = builder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(
                    _configuration["BrevoSmtp:Server"],
                    int.Parse(_configuration["BrevoSmtp:Port"] ?? "587"),
                    SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(
                    _configuration["BrevoSmtp:Username"],
                    _configuration["BrevoSmtp:Password"]);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation($"Invoice email sent to {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send invoice email to {toEmail}");
                return false;
            }
        }
    }
}