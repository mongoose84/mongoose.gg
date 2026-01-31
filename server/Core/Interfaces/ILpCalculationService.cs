namespace RiotProxy.Core.Interfaces;

/// <summary>
/// Service for League of Legends LP (League Points) calculations.
/// Provides methods for converting ranks to numeric values for comparison and delta calculations.
/// </summary>
public interface ILpCalculationService
{
    /// <summary>
    /// Converts tier + division + LP to an absolute LP value for comparison across ranks.
    /// Each division is worth 100 LP, each tier is worth 400 LP (4 divisions).
    /// Master+ tiers don't have divisions, so LP can exceed 100.
    /// </summary>
    /// <param name="tier">The tier (e.g., "GOLD", "PLATINUM", "MASTER")</param>
    /// <param name="division">The division (e.g., "IV", "III", "II", "I")</param>
    /// <param name="lp">The current LP within the division</param>
    /// <returns>Absolute LP value (0 for Iron IV 0 LP, up to 2800+ for Master+)</returns>
    int CalculateAbsoluteLp(string? tier, string? division, int lp);

    /// <summary>
    /// Gets the base LP value for a tier.
    /// Each tier below Master is worth 400 LP (4 divisions × 100 LP each).
    /// </summary>
    /// <param name="tier">The tier name (e.g., "GOLD", "PLATINUM")</param>
    /// <returns>Base LP value: IRON=0, BRONZE=400, SILVER=800, GOLD=1200, PLATINUM=1600, EMERALD=2000, DIAMOND=2400, MASTER+=2800</returns>
    int GetTierValue(string? tier);

    /// <summary>
    /// Gets the LP offset for a division within a tier.
    /// IV = 0, III = 100, II = 200, I = 300.
    /// Master+ don't have divisions (returns 0).
    /// </summary>
    /// <param name="division">The division (e.g., "IV", "III", "II", "I")</param>
    /// <returns>LP offset: IV=0, III=100, II=200, I=300</returns>
    int GetDivisionValue(string? division);

    /// <summary>
    /// Gets the tier level for comparison purposes.
    /// Used for detecting promotions and demotions between tiers.
    /// </summary>
    /// <param name="tier">The tier name (e.g., "GOLD", "PLATINUM")</param>
    /// <returns>Tier level: IRON=1, BRONZE=2, SILVER=3, GOLD=4, PLATINUM=5, EMERALD=6, DIAMOND=7, MASTER=8, GRANDMASTER=9, CHALLENGER=10</returns>
    int GetTierLevel(string? tier);

    /// <summary>
    /// Gets the division level for comparison purposes.
    /// Used for detecting promotions and demotions within a tier.
    /// </summary>
    /// <param name="division">The division (e.g., "IV", "III", "II", "I")</param>
    /// <returns>Division level: IV=1, III=2, II=3, I=4</returns>
    int GetDivisionLevel(string? division);

    /// <summary>
    /// Detects if a rank change represents a promotion.
    /// </summary>
    /// <param name="previousTier">The previous tier</param>
    /// <param name="previousDivision">The previous division</param>
    /// <param name="currentTier">The current tier</param>
    /// <param name="currentDivision">The current division</param>
    /// <returns>True if the player promoted, false otherwise</returns>
    bool IsPromotion(string? previousTier, string? previousDivision, string currentTier, string currentDivision);

    /// <summary>
    /// Detects if a rank change represents a demotion.
    /// </summary>
    /// <param name="previousTier">The previous tier</param>
    /// <param name="previousDivision">The previous division</param>
    /// <param name="currentTier">The current tier</param>
    /// <param name="currentDivision">The current division</param>
    /// <returns>True if the player demoted, false otherwise</returns>
    bool IsDemotion(string? previousTier, string? previousDivision, string currentTier, string currentDivision);

    /// <summary>
    /// Formats a tier and division into a display string.
    /// </summary>
    /// <param name="tier">The tier (e.g., "GOLD")</param>
    /// <param name="division">The division (e.g., "IV")</param>
    /// <returns>Formatted string (e.g., "Gold IV")</returns>
    string FormatRank(string tier, string? division);
}

