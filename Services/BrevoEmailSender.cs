using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity.UI.Services;
using MimeKit;

namespace TracKeee.Services
{
    public class BrevoEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<BrevoEmailSender> _logger;

        public BrevoEmailSender(IConfiguration configuration, ILogger<BrevoEmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(
                    _configuration["BrevoSmtp:FromName"],
                    _configuration["BrevoSmtp:FromEmail"]));
                message.To.Add(MailboxAddress.Parse(email));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder { HtmlBody = htmlMessage };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(
                    _configuration["BrevoSmtp:Server"],
                    int.Parse(_configuration["BrevoSmtp:Port"] ?? "587"),
                    MailKit.Security.SecureSocketOptions.StartTls);

                await client.AuthenticateAsync(
                    _configuration["BrevoSmtp:Username"],
                    _configuration["BrevoSmtp:Password"]);

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation($"Email sent to {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {email}");
                throw;
            }
        }
    }
}