using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Graduation_Project.Services
{
    public class EmailSettings
    {
        public string Host { get; set; } = "";
        public int Port { get; set; } = 587;
        public bool UseSsl { get; set; } = false;
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string FromAddress { get; set; } = "";
        public string FromName { get; set; } = "NABD نبض";
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _settings = config.GetSection("Email").Get<EmailSettings>() ?? new EmailSettings();
            _logger = logger;
        }

        public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(_settings.Host) ||
                string.IsNullOrWhiteSpace(_settings.Username))
            {
                _logger.LogWarning("Email not configured — skipping send to {Email}. " +
                    "Set Email:Host, Email:Username, Email:Password in appsettings.", toEmail);
                return;
            }

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
                message.To.Add(new MailboxAddress(toName, toEmail));
                message.Subject = subject;
                message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

                using var client = new SmtpClient();
                var socketOptions = _settings.UseSsl
                    ? SecureSocketOptions.SslOnConnect
                    : SecureSocketOptions.StartTlsWhenAvailable;

                await client.ConnectAsync(_settings.Host, _settings.Port, socketOptions);
                await client.AuthenticateAsync(_settings.Username, _settings.Password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent to {Email}: {Subject}", toEmail, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}: {Subject}", toEmail, subject);
            }
        }
    }
}
