
using static RiotProxy.Application.DTOs.SoloSummaryDto;

namespace RiotProxy.Application.Services;

/// <summary>
/// Builds per-role "main champion" recommendations from aggregated match stats.
/// LP per game is approximated from wins/losses only, since historical LP snapshots
/// are not available in the data model.
/// </summary>
public static class MainChampionRecommender
{
    public record ChampionRoleStats(
        string Role,
        int ChampionId,
        string ChampionName,
        int GamesPlayed,
        int Wins,
        double AvgGoldPerMin,
        double AvgCs,
        double AvgKills,
        double AvgDeaths,
        double AvgAssists,
        // Early-game laning stats
        double AvgGoldDiff15,      // Average gold difference vs lane opponent at 15 min
        double AvgDeathsPre10,     // Average deaths before 10 minutes
        double AvgVisionPerMin     // Average vision score per minute (important for support)
    );

    private const int MaxChampionsPerRole = 3;
    private const double ApproxLpOnWin = 20.0;
    private const double ApproxLpOnLoss = -15.0;

    public static IReadOnlyList<MainChampionRoleGroup> BuildMainChampionsByRole(
        IEnumerable<ChampionRoleStats> stats)
    {
        if (stats == null) throw new ArgumentNullException(nameof(stats));

        var roleGroups = new List<MainChampionRoleGroup>();

	        foreach (var group in stats.GroupBy(s => NormalizeRole(s.Role)))
	        {
	            // Ignore unknown/unassigned roles – only show meaningful lanes
	            if (group.Key == "UNKNOWN")
	                continue;

            var champions = group
                .Select(s => BuildEntryForChampion(group.Key, s))
                .OrderByDescending(x => x.score)
                .Take(MaxChampionsPerRole)
                .Select(x => x.entry)
                .ToArray();

            if (champions.Length > 0)
            {
                roleGroups.Add(new MainChampionRoleGroup(group.Key, champions));
            }
        }

        // Order roles by total games played across their recommended champions
        return roleGroups
            .OrderByDescending(g => g.Champions.Sum(c => c.GamesPlayed))
            .ToArray();
    }

    private static (MainChampionEntry entry, double score) BuildEntryForChampion(
        string normalizedRole,
        ChampionRoleStats s)
    {
        var games = s.GamesPlayed;
        var wins = s.Wins;
        var losses = Math.Max(0, games - wins);

        var winRate = games > 0
            ? Math.Round((double)wins / games * 100, 1)
            : 0.0;

        var lpPerGame = ComputeLpPerGameApprox(wins, losses);

        var score = ComputeRecommendedScore(
            winRate, games,
            s.AvgKills, s.AvgDeaths, s.AvgAssists,
            s.AvgGoldDiff15, s.AvgDeathsPre10, s.AvgVisionPerMin,
            normalizedRole);

        var entry = new MainChampionEntry(
            ChampionName: s.ChampionName,
            ChampionId: s.ChampionId,
            Role: normalizedRole,
            WinRate: winRate,
            GamesPlayed: games,
            Wins: wins,
            Losses: losses,
            LpPerGame: Math.Round(lpPerGame, 1)
        );

        return (entry, score);
    }

    private static string NormalizeRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role)) return "UNKNOWN";
        return role.Trim().ToUpperInvariant();
    }

    private static double ComputeLpPerGameApprox(int wins, int losses)
    {
        var games = wins + losses;
        if (games == 0) return 0;

        var totalLp = wins * ApproxLpOnWin + losses * ApproxLpOnLoss;
        return totalLp / games;
    }

    /// <summary>
    /// Computes a recommendation score for a champion based on:
    /// - Win rate (40%): The ultimate measure of success
    /// - Laning score (25%): Early game competence (gold diff @15, deaths pre-10, vision)
    /// - Sample size (20%): Confidence in the data
    /// - Overall KDA (15%): General performance indicator
    /// </summary>
    private static double ComputeRecommendedScore(
        double winRatePercent, int games,
        double avgKills, double avgDeaths, double avgAssists,
        double avgGoldDiff15, double avgDeathsPre10, double avgVisionPerMin,
        string role)
    {
        // === Win Rate (40%) ===
        // Normalise win rate between 35% and 65% into [0,1]
        double winRateNorm;
        if (winRatePercent <= 35) winRateNorm = 0;
        else if (winRatePercent >= 65) winRateNorm = 1;
        else winRateNorm = (winRatePercent - 35) / 30.0;

        // === Sample Size (20%) ===
        // Capped at 40 games for full confidence
        var sampleNorm = Math.Min(1.0, games / 40.0);

        // === KDA (15%) ===
        // KDA ratio: (kills + assists) / deaths, with deaths min 1
        var kda = (avgKills + avgAssists) / Math.Max(1.0, avgDeaths);
        var kdaNorm = Math.Min(1.0, kda / 5.0); // 5+ KDA is excellent

        // === Laning Score (25%) ===
        // Composed of role-specific early-game metrics
        var laningScore = ComputeLaningScore(role, avgGoldDiff15, avgDeathsPre10, avgVisionPerMin);

        // Final weighted score
        return 0.40 * winRateNorm
             + 0.25 * laningScore
             + 0.20 * sampleNorm
             + 0.15 * kdaNorm;
    }

    /// <summary>
    /// Computes a normalized laning score [0,1] based on role-specific metrics.
    /// </summary>
    private static double ComputeLaningScore(
        string role,
        double avgGoldDiff15,
        double avgDeathsPre10,
        double avgVisionPerMin)
    {
        // Gold diff @15: Normalize from [-1500, +1500] to [0, 1]
        // Being 1500+ gold ahead is excellent, 1500+ behind is terrible
        var goldDiff15Norm = Math.Clamp((avgGoldDiff15 + 1500.0) / 3000.0, 0.0, 1.0);

        // Deaths pre-10: 0 is best, 3+ is bad → invert to [0, 1]
        // 0 deaths = 1.0, 3+ deaths = 0.0
        var earlyDeathsNorm = Math.Clamp(1.0 - (avgDeathsPre10 / 3.0), 0.0, 1.0);

        // Vision per min: 1.5+ is excellent for supports, normalize to [0, 1]
        var visionNorm = Math.Clamp(avgVisionPerMin / 1.5, 0.0, 1.0);

        // Role-specific weighting
        return role.ToUpperInvariant() switch
        {
            // Support: Vision is critical, gold diff less important (often behind in gold)
            "UTILITY" => 0.25 * goldDiff15Norm + 0.35 * earlyDeathsNorm + 0.40 * visionNorm,

            // Jungle: No lane opponent, so gold diff is less meaningful
            // Focus on not dying early and vision control
            "JUNGLE" => 0.20 * goldDiff15Norm + 0.50 * earlyDeathsNorm + 0.30 * visionNorm,

            // Laners (Top, Mid, ADC): Gold diff and early deaths are key
            _ => 0.50 * goldDiff15Norm + 0.40 * earlyDeathsNorm + 0.10 * visionNorm
        };
    }
    
}

