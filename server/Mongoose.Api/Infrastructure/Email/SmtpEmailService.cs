using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using Mongoose.Api.Application.Endpoints.Shared;

namespace Mongoose.Api.Infrastructure.Email;

/// <summary>
/// SMTP-based email service implementation
/// </summary>
public class SmtpEmailService : IEmailService
{
    private const string FromName = "The Mongoose.gg Team";

    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendVerificationEmailAsync(string toEmail, string username, string verificationCode)
    {
        // Dev mode: do not log raw secrets (email/code)
        if (IsDevMode())
        {
            LogDevModeEmail(toEmail, username, "Verification Code Hash", verificationCode);
            return;
        }

        var content = EmailTemplates.Verification(username, verificationCode, hasLogo: GetLogoPath() is not null);
        await SendAsync(toEmail, content, "Verification email");
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string username, string resetCode)
    {
        // Dev mode: do not log raw secrets (email/code)
        if (IsDevMode())
        {
            LogDevModeEmail(toEmail, username, "Password Reset Code Hash", resetCode);
            return;
        }

        var content = EmailTemplates.PasswordReset(username, resetCode, hasLogo: GetLogoPath() is not null);
        await SendAsync(toEmail, content, "Password reset email");
    }

    private bool IsDevMode() => _config.GetValue<bool>("Email:DevMode", false);

    private void LogDevModeEmail(string toEmail, string username, string codeLabel, string code)
    {
        _logger.LogWarning("========================================");
        _logger.LogWarning("DEV MODE: Email sending disabled");
        _logger.LogWarning("ToHash: {EmailHash}", LogSanitizer.HashForLog(toEmail));
        _logger.LogWarning("Username: {Username}", LogSanitizer.HashForLog(username));
        _logger.LogWarning("{CodeLabel}: {CodeHash}", codeLabel, LogSanitizer.HashForLog(code));
        _logger.LogWarning("========================================");
    }

    /// <summary>
    /// Sends one email with a plain-text part and an HTML part (logo embedded as a linked resource for Gmail compatibility).
    /// </summary>
    private async Task SendAsync(string toEmail, EmailContent content, string kind)
    {
        var smtpHost = _config["Email:SmtpHost"] ?? Environment.GetEnvironmentVariable("SMTP_HOST");
        if (string.IsNullOrWhiteSpace(smtpHost))
        {
            _logger.LogError("SMTP host is not configured. Email not sent.");
            throw new InvalidOperationException("SMTP host is not configured");
        }

        var smtpPort = _config.GetValue<int>("Email:SmtpPort", 587);
        if (smtpPort <= 0)
        {
            var errorMessage = "SMTP port is not configured. Email not sent.";
            _logger.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        var smtpUsername = _config["Email:SmtpUsername"] ?? Environment.GetEnvironmentVariable("SMTP_USERNAME");
        var smtpPassword = _config["Email:SmtpPassword"] ?? Environment.GetEnvironmentVariable("SMTP_PASSWORD");
        var fromEmail = _config["Email:FromEmail"] ?? Environment.GetEnvironmentVariable("SMTP_FROM_EMAIL") ?? smtpUsername;

        if (string.IsNullOrWhiteSpace(smtpUsername) || string.IsNullOrWhiteSpace(smtpPassword) || string.IsNullOrWhiteSpace(fromEmail))
        {
            _logger.LogError("SMTP configuration is incomplete. Email not sent to {Email}", RedactEmailForLog(toEmail));
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
                From = new MailAddress(fromEmail, FromName),
                Subject = content.Subject
            };

            mailMessage.To.Add(toEmail);

            // Plain text first, HTML last: clients show the last part they support
            mailMessage.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(content.Text, Encoding.UTF8, "text/plain"));

            var htmlView = AlternateView.CreateAlternateViewFromString(content.Html, Encoding.UTF8, "text/html");
            var logoPath = GetLogoPath();
            if (logoPath is not null)
            {
                var logoResource = new LinkedResource(logoPath, new System.Net.Mime.ContentType("image/png"))
                {
                    ContentId = "logo",
                    TransferEncoding = System.Net.Mime.TransferEncoding.Base64
                };
                htmlView.LinkedResources.Add(logoResource);
            }
            mailMessage.AlternateViews.Add(htmlView);

            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("{Kind} sent successfully", kind);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send {Kind}", kind);
            throw;
        }
    }

    /// <summary>
    /// Gets the path to the mongoose logo file.
    /// Checks both the assembly output directory and the current working directory.
    /// Returns null if the logo file is not found.
    /// </summary>
    private string? GetLogoPath()
    {
        try
        {
            // Try to load from file path relative to the assembly location
            // (may be empty/null in single-file publish scenarios)
            var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (!string.IsNullOrEmpty(assemblyLocation))
            {
                var logoPath = Path.Combine(assemblyLocation, "Infrastructure", "Email", "mongoose.png");
                if (File.Exists(logoPath))
                {
                    return logoPath;
                }
            }

            // Fallback: try relative to current working directory (for development or single-file publish)
            var cwdLogoPath = Path.Combine(Directory.GetCurrentDirectory(), "Infrastructure", "Email", "mongoose.png");
            if (File.Exists(cwdLogoPath))
            {
                return cwdLogoPath;
            }

            _logger.LogWarning("Logo file not found at assembly location or current directory");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to find logo file");
            return null;
        }
    }
}

