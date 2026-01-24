
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
        double AvgAssists
    );

    private const int MinGamesForChampion = 2;
    private const int MaxChampionsPerRole = 3;
    private const double ApproxLpOnWin = 20.0;
    private const double ApproxLpOnLoss = -15.0;

    public static IReadOnlyList<MainChampionRoleGroup> BuildMainChampionsByRole(
        IEnumerable<ChampionRoleStats> stats)
    {
        if (stats == null) throw new ArgumentNullException(nameof(stats));

        var eligible = stats.Where(s => s.GamesPlayed >= MinGamesForChampion);

        var roleGroups = new List<MainChampionRoleGroup>();

	        foreach (var group in eligible.GroupBy(s => NormalizeRole(s.Role)))
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

        // New stats
        var avgGoldPerMin = s.AvgGoldPerMin;
        var avgCs = s.AvgCs;
        var avgKills = s.AvgKills;
        var avgDeaths = s.AvgDeaths;
        var avgAssists = s.AvgAssists;

        var score = ComputeRecommendedScore(winRate, games, lpPerGame, avgGoldPerMin, avgCs, avgKills, avgDeaths, avgAssists, normalizedRole);

        var entry = new MainChampionEntry(
            ChampionName: s.ChampionName,
            ChampionId: s.ChampionId,
            Role: normalizedRole,
            WinRate: winRate,
            GamesPlayed: games,
            Wins: wins,
            Losses: losses,
            LpPerGame: Math.Round(lpPerGame, 1)
            // Optionally, you can extend MainChampionEntry to include these new stats if needed
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

    private static double ComputeRecommendedScore(double winRatePercent, int games, double lpPerGame,
        double avgGoldPerMin, double avgCs, double avgKills, double avgDeaths, double avgAssists, string role)
    {
        // Normalise win rate between 35% and 65% into [0,1]
        double winRateNorm;
        if (winRatePercent <= 35) winRateNorm = 0;
        else if (winRatePercent >= 65) winRateNorm = 1;
        else winRateNorm = (winRatePercent - 35) / 30.0;

        // Clamp LP per game to [-30, 30] then normalise into [0,1]
        var lpClamped = Math.Max(-30.0, Math.Min(30.0, lpPerGame));
        var lpNorm = (lpClamped + 30.0) / 60.0;

    // note: should only be used when ranked. Add bool isRanked parameter if needed.

        // Sample size bonus, capped at 40 games
        var sampleNorm = Math.Min(1.0, games / 40.0);

        // Normalize new stats (example ranges, adjust as needed)
        // Gold per min: 200-600
        var goldNorm = Math.Min(1.0, Math.Max(0.0, (avgGoldPerMin - 200.0) / 400.0));
        // CS: 0-10 per min, but ignore for supports
        double csNorm = 0.0;
        if (!string.Equals(role, "UTILITY", StringComparison.OrdinalIgnoreCase))
        {
            csNorm = Math.Min(1.0, Math.Max(0.0, (avgCs - 100.0) / 200.0));
        }
        // KDA: kills+assists/deaths, deaths min 1
        var kda = (avgKills + avgAssists) / Math.Max(1.0, avgDeaths);
        var kdaNorm = Math.Min(1.0, kda / 5.0); // 5+ is excellent

        // Heuristic blend – win rate is most important, then LP, then sample size, then new stats
        return 0.35 * winRateNorm + 0.2 * lpNorm + 0.15 * sampleNorm + 0.1 * goldNorm + 0.1 * csNorm + 0.1 * kdaNorm;
    }
    
}

