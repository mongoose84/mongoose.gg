namespace RiotProxy.Core.Entities;

public class VerificationToken : EntityBase
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string TokenType { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public int Attempts { get; set; }
    public DateTime CreatedAt { get; set; }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsUsed => UsedAt.HasValue;
    public bool IsValid => !IsExpired && !IsUsed;
}

public static class TokenTypes
{
    public const string EmailVerification = "email_verification";
    public const string PasswordReset = "password_reset";
    public const string EmailChange = "email_change";
}

