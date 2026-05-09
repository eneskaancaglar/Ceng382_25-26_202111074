using System.Net;
using System.Net.Mail;
using System.Text;

namespace TasteAtDoor.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public EmailService(
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
        }

        public async Task SendAsync(string to, string subject, string htmlBody)
        {
            var smtpEnabled = _configuration.GetValue<bool>("Smtp:Enabled");
            var savePreview = _configuration.GetValue<bool>("Smtp:SavePreview");

            if (smtpEnabled)
            {
                await SendRealEmailAsync(to, subject, htmlBody);
            }

            if (savePreview || !smtpEnabled)
            {
                await SaveEmailPreviewAsync(to, subject, htmlBody);
            }
        }

        private async Task SendRealEmailAsync(string to, string subject, string htmlBody)
        {
            var host = _configuration["Smtp:Host"];
            var username = _configuration["Smtp:Username"];
            var password = _configuration["Smtp:Password"];
            var senderEmail = _configuration["Smtp:SenderEmail"];
            var senderName = _configuration["Smtp:SenderName"] ?? "TasteAtDoor";

            var port = _configuration.GetValue<int>("Smtp:Port");
            var enableSsl = _configuration.GetValue<bool>("Smtp:EnableSsl");

            if (string.IsNullOrWhiteSpace(host))
            {
                throw new InvalidOperationException("SMTP host is missing.");
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                throw new InvalidOperationException("SMTP username is missing.");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("SMTP password is missing.");
            }

            if (string.IsNullOrWhiteSpace(senderEmail))
            {
                throw new InvalidOperationException("SMTP sender email is missing.");
            }

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true,
                SubjectEncoding = Encoding.UTF8,
                BodyEncoding = Encoding.UTF8
            };

            mailMessage.To.Add(to);

            using var smtpClient = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                Credentials = new NetworkCredential(username, password),
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            await smtpClient.SendMailAsync(mailMessage);
        }

        private async Task SaveEmailPreviewAsync(string to, string subject, string htmlBody)
        {
            var previewFolder = Path.Combine(_environment.ContentRootPath, "EmailPreview");

            if (!Directory.Exists(previewFolder))
            {
                Directory.CreateDirectory(previewFolder);
            }

            var safeFileName = string.Join("_", subject.Split(Path.GetInvalidFileNameChars()));
            var fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{safeFileName}.html";
            var filePath = Path.Combine(previewFolder, fileName);

            var previewHtml = $"""
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset="utf-8" />
                    <title>{WebUtility.HtmlEncode(subject)}</title>
                </head>
                <body>
                    <h2>Email Preview</h2>
                    <p><strong>To:</strong> {WebUtility.HtmlEncode(to)}</p>
                    <p><strong>Subject:</strong> {WebUtility.HtmlEncode(subject)}</p>
                    <hr />
                    {htmlBody}
                </body>
                </html>
                """;

            await File.WriteAllTextAsync(filePath, previewHtml, Encoding.UTF8);
        }
    }
}