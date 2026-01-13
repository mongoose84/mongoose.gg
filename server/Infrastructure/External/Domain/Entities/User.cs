
namespace RiotProxy.External.Domain.Entities;

public class User : EntityBase
{
    public long UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public bool IsActive { get; set; } = true;
    public string Tier { get; set; } = "free";
    public string? MollieCustomerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
