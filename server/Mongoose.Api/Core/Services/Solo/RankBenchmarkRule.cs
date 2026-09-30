using Mongoose.Api.Core.QueryModels;

namespace Mongoose.Api.Core.Services.Solo;

/// <summary>Whose average the player is compared with: one ranked queue, one tier, one role.</summary>
public sealed record RankBenchmarkTarget(int QueueId, string Tier, string Role);

/// <summary>
/// The rank-average benchmark (features/solo-trends.spec.md FR 16, 5g): when the player's stats are
/// compared with Mongoose.gg players of their tier and role instead of their own season average.
/// Pure and static.
/// </summary>
public static class RankBenchmarkRule
{
    /// <summary>The main role must hold this share of the range's matches; CS and vision differ by role.</summary>
    public const double MinRoleShare = 0.7;

    /// <summary>A pool needs this many other players, so no one player sets the average.</summary>
    public const int MinPlayers = 20;

    /// <summary>A pool needs this many matches in the season.</summary>
    public const int MinMatches = 200;

    private const string UnknownRole = "UNKNOWN";

    /// <summary>
    /// The tier, role and queue to compare with, or null (then the season average stays): one ranked
    /// queue of one account, a known current tier (the latest match in the season that carries one) and
    /// a main role in the range.
    /// </summary>
    public static RankBenchmarkTarget? Target(
        string queueType, bool singleAccount, IReadOnlyList<SoloMatchRow> rows, IReadOnlyList<SoloMatchRow> seasonRows)
    {
        var queueId = queueType switch
        {
            SoloScope.RankedSolo => 420,
            SoloScope.RankedFlex => 440,
            _ => (int?)null
        };
        if (queueId == null || !singleAccount || rows.Count == 0) return null;

        var tier = rows.Concat(seasonRows)
            .Where(r => !string.IsNullOrEmpty(r.TierAfter))
            .OrderByDescending(r => r.GameStartTime)
            .Select(r => r.TierAfter)
            .FirstOrDefault();
        if (tier == null) return null;

        var main = rows
            .Where(r => r.Role != UnknownRole)
            .GroupBy(r => r.Role)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();
        if (main == null || main.Count() < rows.Count * MinRoleShare) return null;

        return new RankBenchmarkTarget(queueId.Value, tier, main.Key);
    }

    /// <summary>True when the pool, without the player's own matches, is large enough to be fair.</summary>
    public static bool Qualifies(IReadOnlyList<SoloRankPoolRow> pool) =>
        pool.Count >= MinMatches && pool.Select(p => p.Puuid).Distinct(StringComparer.Ordinal).Count() >= MinPlayers;
}
