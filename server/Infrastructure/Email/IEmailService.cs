namespace Mongoose.Api.Infrastructure.Email;

/// <summary>
/// Service for sending emails
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send a verification email with a 6-digit code
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="username">Username for personalization</param>
    /// <param name="verificationCode">6-digit verification code</param>
    Task SendVerificationEmailAsync(string toEmail, string username, string verificationCode);
}

