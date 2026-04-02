namespace Mongoose.Api.Core.Entities;

/// <summary>
/// Represents the M:M relationship between users and Riot accounts.
/// Multiple users can link the same Riot account.
/// </summary>
public class UserRiotAccountLink : EntityBase
{
    public long UserId { get; set; }
    public string Puuid { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public DateTime LinkedAt { get; set; }
}

