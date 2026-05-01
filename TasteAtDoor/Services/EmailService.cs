using System.Net;
using System.Net.Mail;
using System.Text;

namespace TasteAtDoor.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly IAppLogService _appLogService;

        public EmailService(
            IConfiguration configuration,
            IWebHostEnvironment environment,
            IAppLogService appLogService)
        {
            _configuration = configuration;
            _environment = environment;
            _appLogService = appLogService;
        }

        public async Task<bool> SendAsync(string to, string subject, string htmlBody)
        {
            try
            {
                var useFilePreview = bool.TryParse(
                    _configuration["EmailSettings:UseFilePreview"],
                    out var previewMode)
                    ? previewMode
                    : true;

                if (useFilePreview)
                {
                    var previewFolder = Path.Combine(_environment.ContentRootPath, "EmailPreview");
                    Directory.CreateDirectory(previewFolder);

                    var safeFileName = MakeSafeFileName($"{DateTime.Now:yyyyMMdd_HHmmssfff}_{to}.html");
                    var filePath = Path.Combine(previewFolder, safeFileName);

                    var fullHtml = $"""
                    <html>
                    <head>
                        <meta charset="utf-8" />
                        <title>{WebUtility.HtmlEncode(subject)}</title>
                    </head>
                    <body style="font-family:Arial,Helvetica,sans-serif;">
                        <h2>{WebUtility.HtmlEncode(subject)}</h2>
                        <p><strong>To:</strong> {WebUtility.HtmlEncode(to)}</p>
                        <hr />
                        {htmlBody}
                    </body>
                    </html>
                    """;

                    await File.WriteAllTextAsync(filePath, fullHtml, Encoding.UTF8);

                    await _appLogService.LogAsync(
                        eventType: "EmailPreviewGenerated",
                        message: "Email preview file generated.",
                        userEmail: to,
                        details: filePath);

                    return true;
                }

                var host = _configuration["EmailSettings:Host"];
                var portText = _configuration["EmailSettings:Port"];
                var enableSslText = _configuration["EmailSettings:EnableSsl"];
                var userName = _configuration["EmailSettings:UserName"];
                var password = _configuration["EmailSettings:Password"];
                var fromEmail = _configuration["EmailSettings:FromEmail"];
                var fromName = _configuration["EmailSettings:FromName"] ?? "Taste At Door";

                if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromEmail))
                {
                    await _appLogService.LogAsync(
                        eventType: "EmailFailure",
                        message: "Email settings are missing.",
                        level: "Warning",
                        userEmail: to);

                    return false;
                }

                var port = int.TryParse(portText, out var parsedPort) ? parsedPort : 587;
                var enableSsl = bool.TryParse(enableSslText, out var parsedSsl) ? parsedSsl : true;

                using var message = new MailMessage();
                message.From = new MailAddress(fromEmail, fromName);
                message.To.Add(to);
                message.Subject = subject;
                message.Body = htmlBody;
                message.IsBodyHtml = true;

                using var client = new SmtpClient(host, port)
                {
                    EnableSsl = enableSsl
                };

                if (!string.IsNullOrWhiteSpace(userName))
                {
                    client.Credentials = new NetworkCredential(userName, password);
                }

                await client.SendMailAsync(message);

                await _appLogService.LogAsync(
                    eventType: "EmailSent",
                    message: "Email sent successfully.",
                    userEmail: to,
                    details: subject);

                return true;
            }
            catch (Exception ex)
            {
                await _appLogService.LogAsync(
                    eventType: "EmailFailure",
                    message: "Email sending failed.",
                    level: "Warning",
                    userEmail: to,
                    details: ex.Message);

                return false;
            }
        }

        private static string MakeSafeFileName(string value)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var safeChars = value.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray();
            return new string(safeChars);
        }
    }
}