using System.Net;
using System.Net.Mail;

namespace RiotProxy.Infrastructure.Email;

/// <summary>
/// SMTP-based email service implementation
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendVerificationEmailAsync(string toEmail, string username, string verificationCode)
    {
        // Dev mode: log verification code instead of sending email
        var devMode = _config.GetValue<bool>("Email:DevMode", false);
        if (devMode)
        {
            _logger.LogWarning("========================================");
            _logger.LogWarning("DEV MODE: Email sending disabled");
            _logger.LogWarning("To: {Email}", toEmail);
            _logger.LogWarning("Username: {Username}", username);
            _logger.LogWarning("Verification Code: {Code}", verificationCode);
            _logger.LogWarning("========================================");
            await Task.CompletedTask;
            return;
        }

        var smtpHost = _config["Email:SmtpHost"] ?? Environment.GetEnvironmentVariable("SMTP_HOST");
        if (string.IsNullOrWhiteSpace(smtpHost))
        {
            var errorMessage = $"SMTP host is not configured. Email not sent to {toEmail}";
            _logger.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        var smtpPort = _config.GetValue<int>("Email:SmtpPort", 587);
        if (smtpPort <= 0)
        {
            var errorMessage = $"SMTP port is not configured. Email not sent to {toEmail}";
            _logger.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        var smtpUsername = _config["Email:SmtpUsername"] ?? Environment.GetEnvironmentVariable("SMTP_USERNAME");
        if (string.IsNullOrWhiteSpace(smtpUsername))
        {
            var errorMessage = $"SMTP username is not configured. Email not sent to {toEmail}";
            _logger.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        var smtpPassword = _config["Email:SmtpPassword"] ?? Environment.GetEnvironmentVariable("SMTP_PASSWORD");
        if (string.IsNullOrWhiteSpace(smtpPassword))
        {
            var errorMessage = $"SMTP password is not configured. Email not sent to {toEmail}";
            _logger.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        var fromEmail = _config["Email:FromEmail"] ?? Environment.GetEnvironmentVariable("SMTP_FROM_EMAIL") ?? smtpUsername;
        if (string.IsNullOrWhiteSpace(fromEmail))
        {
            var errorMessage = $"SMTP from email is not configured. Email not sent to {toEmail}";
            _logger.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        var fromName = "The Mongoose.gg Team";
        if (string.IsNullOrWhiteSpace(fromName))
        {
            var errorMessage = $"SMTP from name is not configured. Email not sent to {toEmail}";
            _logger.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(smtpUsername) || string.IsNullOrWhiteSpace(smtpPassword))
        {
            _logger.LogError("SMTP configuration is incomplete. Email not sent to {Email}", toEmail);
            throw new InvalidOperationException("SMTP configuration is incomplete. Please configure Email:SmtpHost, Email:SmtpUsername, and Email:SmtpPassword.");
        }

        try
        {
            using var smtpClient = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                EnableSsl = true
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = "Verify your Mongoose.gg email",
                Body = BuildEmailBody(username, verificationCode),
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("Verification email sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification email to {Email}", toEmail);
            throw;
        }
    }

    private string BuildEmailBody(string username, string verificationCode)
    {
        // HTML-encode user-controlled data to prevent HTML injection
        var encodedUsername = WebUtility.HtmlEncode(username);
        var encodedCode = WebUtility.HtmlEncode(verificationCode);

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Verify Your Email</title>
</head>
<body style=""margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; background-color: #0a0a0a; color: #e5e5e5;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td align=""center"" style=""padding: 40px 0;"">
                <table role=""presentation"" style=""width: 600px; max-width: 100%; border-collapse: collapse; background-color: #1a1a1a; border: 1px solid #2a2a2a; border-radius: 8px;"">
                    <!-- Header -->
                    <tr>
                        <td align=""center"" style=""padding: 40px 40px 20px 40px;"">
                            <h1 style=""margin: 0; font-size: 24px; font-weight: 700; color: #ffffff;"">Verify Your Email</h1>
                        </td>
                    </tr>
                    
                    <!-- Body -->
                    <tr>
                        <td style=""padding: 0 40px 20px 40px;"">
                            <p style=""margin: 0 0 20px 0; font-size: 16px; line-height: 24px; color: #b3b3b3;"">
                                Hi <strong style=""color: #ffffff;"">{encodedUsername}</strong>,
                            </p>
                            <p style=""margin: 0 0 20px 0; font-size: 16px; line-height: 24px; color: #b3b3b3;"">
                                Thanks for signing up for Mongoose.gg! To complete your registration, please verify your email address by entering the code below:
                            </p>
                        </td>
                    </tr>

                    <!-- Verification Code -->
                    <tr>
                        <td align=""center"" style=""padding: 0 40px 30px 40px;"">
                            <div style=""background-color: #2a2a2a; border: 1px solid #3a3a3a; border-radius: 8px; padding: 24px; display: inline-block;"">
                                <div style=""font-size: 36px; font-weight: 700; letter-spacing: 8px; color: #ffffff; font-family: 'Courier New', monospace;"">
                                    {encodedCode}
                                </div>
                            </div>
                        </td>
                    </tr>
                    
                    <!-- Footer -->
                    <tr>
                        <td style=""padding: 0 40px 40px 40px;"">
                            <p style=""margin: 0 0 10px 0; font-size: 14px; line-height: 20px; color: #808080;"">
                                This code will expire in 15 minutes.
                            </p>
                            <p style=""margin: 0; font-size: 14px; line-height: 20px; color: #808080;"">
                                If you didn't create an account with Mongoose.gg, you can safely ignore this email.
                            </p>
                        </td>
                    </tr>
                    
                    <!-- Branding -->
                    <tr>
                        <td align=""center"" style=""padding: 20px 40px 40px 40px; border-top: 1px solid #2a2a2a;"">
                            <p style=""margin: 0; font-size: 12px; color: #666666;"">
                                © 2026 Mongoose.gg - League of Legends Performance Tracker
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }
}

