using MySqlConnector;
using RiotProxy.Application.DTOs;
using static RiotProxy.Application.DTOs.SoloSummaryDto;

namespace RiotProxy.Infrastructure.External.Database.Repositories.V2;

/// <summary>
/// Repository for v2 solo dashboard statistics, optimized for dashboard rendering.
/// All queries use v2 match and participant tables.
/// </summary>
public class V2SoloStatsRepository : RepositoryBase
{
    private readonly ILogger<V2SoloStatsRepository> _logger;
    public V2SoloStatsRepository(IV2DbConnectionFactory factory, ILogger<V2SoloStatsRepository> logger) : base(factory)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get comprehensive solo dashboard data for a player.
    /// Includes: overall stats, champion pool, performance by phase, role breakdown, death efficiency.
    /// Supports optional queue filtering.
    /// </summary>
    public async Task<SoloDashboardResponse?> GetSoloDashboardAsync(string puuid, string? queueType = null)
    {
        // Validate queueType
        queueType = ValidateQueueType(queueType);
        _logger.LogInformation("GetSoloDashboardAsync start: puuid={Puuid}, queueType={Queue}", puuid, queueType);
        
        // Build base query with optional queue filter
        var queueFilter = BuildQueueFilter(queueType);
        
        // Fetch all necessary data
        try
        {
            var overallStats = await GetOverallStatsAsync(puuid, queueFilter);
        if (overallStats == null)
            return null;

        var sideStats = await GetSideStatsAsync(puuid, queueFilter);
        var champions = await GetChampionStatsAsync(puuid, queueFilter);
        var roleBreakdown = await GetRoleBreakdownAsync(puuid, queueFilter);
        var deathStats = await GetDeathEfficiencyAsync(puuid, queueFilter);
        var matchDurations = await GetMatchDurationsAsync(puuid, queueFilter);

        var totalGames = overallStats.Value.Games;
        
        // Calculate main champion
        ChampionSummary? mainChamp = null;
        if (champions.Count > 0)
        {
            var topChamp = champions[0];
            mainChamp = new ChampionSummary(
                topChamp.ChampionId,
                topChamp.ChampionName,
                topChamp.Picks,
                topChamp.WinRate,
                totalGames > 0 ? Math.Round((double)topChamp.Picks / totalGames * 100, 1) : 0
            );
        }

        // Calculate performance by phase (early/mid/late based on match duration)
        var performancePhases = CalculatePerformancePhases(overallStats.Value, matchDurations);

        // Prepare response
            var last10 = await GetRecentTrendAsync(puuid, queueFilter, Math.Min(10, totalGames));
            var last20 = await GetRecentTrendAsync(puuid, queueFilter, Math.Min(20, totalGames));

            var response = new SoloDashboardResponse(
            GamesPlayed: totalGames,
            Wins: overallStats.Value.Wins,
            WinRate: overallStats.Value.WinRate,
            AvgKda: overallStats.Value.AvgKda,
            AvgGameDurationMinutes: overallStats.Value.AvgGameDurationMinutes,
            SideStats: sideStats,
            UniqueChampsPlayedCount: champions.Count,
            MainChampion: mainChamp,
            Last10Games: last10,
            Last20Games: last20,
            PerformanceByPhase: performancePhases.ToArray(),
            RoleBreakdown: roleBreakdown.ToArray(),
            DeathEfficiency: deathStats,
            QueueType: queueType
        );
            _logger.LogInformation("GetSoloDashboardAsync success: puuid={Puuid}, games={Games}", puuid, totalGames);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetSoloDashboardAsync error: puuid={Puuid}, queueType={Queue}", puuid, queueType);
            throw;
        }
    }

    private async Task<(int Games, int Wins, double WinRate, double AvgKda, double AvgGameDurationMinutes)?> GetOverallStatsAsync(
        string puuid, string queueFilter)
    {
        var sql = $@"
            SELECT
                COUNT(DISTINCT p.match_id) as Games,
                SUM(CASE WHEN p.win = 1 THEN 1 ELSE 0 END) as Wins,
                AVG(CASE WHEN p.deaths > 0 THEN (p.kills + p.assists) / p.deaths ELSE (p.kills + p.assists) END) as AvgKda,
                AVG(m.game_duration_sec / 60.0) as AvgGameDurationMinutes
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            WHERE p.puuid = @puuid {queueFilter}";

        var result = await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuid);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync() && !reader.IsDBNull(0))
            {
                var games = reader.GetInt32(0);
                var wins = reader.GetInt32(1);
                var avgKda = reader.IsDBNull(2) ? 0 : reader.GetDouble(2);
                var avgDuration = reader.IsDBNull(3) ? 0 : reader.GetDouble(3);
                var winRate = games > 0 ? Math.Round((double)wins / games * 100, 1) : 0;

                return ((int Games, int Wins, double WinRate, double AvgKda, double AvgGameDurationMinutes)?)(Games: games, Wins: wins, WinRate: winRate, AvgKda: Math.Round(avgKda, 2), 
                    AvgGameDurationMinutes: Math.Round(avgDuration, 1));
            }
            return null;
        });
        return result;
    }

    private async Task<SideWinDistribution> GetSideStatsAsync(string puuid, string queueFilter)
    {
        var sql = $@"
            SELECT
                SUM(CASE WHEN p.team_id = 100 THEN 1 ELSE 0 END) as BlueGames,
                SUM(CASE WHEN p.team_id = 100 AND p.win = 1 THEN 1 ELSE 0 END) as BlueWins,
                SUM(CASE WHEN p.team_id = 200 THEN 1 ELSE 0 END) as RedGames,
                SUM(CASE WHEN p.team_id = 200 AND p.win = 1 THEN 1 ELSE 0 END) as RedWins
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            WHERE p.puuid = @puuid {queueFilter}";

        return await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuid);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var blueGames = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                var blueWins = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                var redGames = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                var redWins = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                var totalGames = blueGames + redGames;
                var totalWins = blueWins + redWins;
                
                return new SideWinDistribution(
                    BlueWins: blueWins,
                    RedWins: redWins,
                    BlueGames: blueGames,
                    RedGames: redGames,
                    TotalGames: totalGames,
                    BlueWinDistribution: totalWins > 0 ? Math.Round((double)blueWins / totalWins * 100, 1) : 0,
                    RedWinDistribution: totalWins > 0 ? Math.Round((double)redWins / totalWins * 100, 1) : 0
                );
            }
            return new SideWinDistribution(0, 0, 0, 0, 0, 0, 0);
        });
    }

    private async Task<List<(int ChampionId, string ChampionName, int Picks, double WinRate)>> GetChampionStatsAsync(
        string puuid, string queueFilter)
    {
        var sql = $@"
            SELECT
                p.champion_id,
                p.champion_name,
                COUNT(DISTINCT p.match_id) as Picks,
                SUM(CASE WHEN p.win = 1 THEN 1 ELSE 0 END) as Wins
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            WHERE p.puuid = @puuid {queueFilter}
            GROUP BY p.champion_id, p.champion_name
            ORDER BY Picks DESC
            LIMIT 20";

        var champs = new List<(int, string, int, double)>();
        await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuid);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var champId = reader.GetInt32(0);
                var champName = reader.GetString(1);
                var picks = reader.GetInt32(2);
                var wins = reader.GetInt32(3);
                var wr = picks > 0 ? Math.Round((double)wins / picks * 100, 1) : 0;
                champs.Add((champId, champName, picks, wr));
            }
            return 0;
        });
        return champs;
    }

    private async Task<List<RolePerformance>> GetRoleBreakdownAsync(string puuid, string queueFilter)
    {
        var sql = $@"
            SELECT
                COALESCE(NULLIF(p.role, ''), 'UNKNOWN') as Role,
                COUNT(DISTINCT p.match_id) as Games,
                SUM(CASE WHEN p.win = 1 THEN 1 ELSE 0 END) as Wins,
                AVG(CASE WHEN p.deaths > 0 THEN (p.kills + p.assists) / p.deaths ELSE (p.kills + p.assists) END) as AvgKda
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            WHERE p.puuid = @puuid {queueFilter}
            GROUP BY Role
            ORDER BY Games DESC";

        var roles = new List<RolePerformance>();
        await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuid);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var role = reader.GetString(0);
                var games = reader.GetInt32(1);
                var wins = reader.GetInt32(2);
                var avgKda = reader.IsDBNull(3) ? 0 : reader.GetDouble(3);
                var wr = games > 0 ? Math.Round((double)wins / games * 100, 1) : 0;
                roles.Add(new RolePerformance(role, games, wins, wr, Math.Round(avgKda, 2)));
            }
            return 0;
        });
        return roles;
    }

    private async Task<DeathEfficiency> GetDeathEfficiencyAsync(string puuid, string queueFilter)
    {
        var sql = $@"
            SELECT
                SUM(pm.deaths_pre_10) as DeathsPre10,
                SUM(pm.deaths_10_20) as Deaths10To20,
                SUM(pm.deaths_20_30) as Deaths20To30,
                SUM(pm.deaths_30_plus) as Deaths30Plus,
                AVG(pm.first_death_minute) as AvgFirstDeathMinute,
                AVG(pm.first_kill_participation_minute) as AvgFirstKillParticipationMinute
            FROM participants p
            INNER JOIN participant_metrics pm ON pm.participant_id = p.id
            INNER JOIN matches m ON m.match_id = p.match_id
            WHERE p.puuid = @puuid {queueFilter}";

        return await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuid);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var pre10 = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                var m10_20 = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                var m20_30 = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                var m30plus = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                var firstDeath = reader.IsDBNull(4) ? null : (double?)reader.GetDouble(4);
                var firstKp = reader.IsDBNull(5) ? null : (double?)reader.GetDouble(5);

                return new DeathEfficiency(
                    DeathsPre10: pre10,
                    Deaths10To20: m10_20,
                    Deaths20To30: m20_30,
                    Deaths30Plus: m30plus,
                    AvgFirstDeathMinute: firstDeath.HasValue ? Math.Round(firstDeath.Value, 1) : null,
                    AvgFirstKillParticipationMinute: firstKp.HasValue ? Math.Round(firstKp.Value, 1) : null
                );
            }
            return new DeathEfficiency(0, 0, 0, 0, null, null);
        });
    }

    private async Task<List<(int Minutes, int Games, int Wins, double AvgKda)>> GetMatchDurationsAsync(
        string puuid, string queueFilter)
    {
        var sql = $@"
            SELECT
                FLOOR(m.game_duration_sec / 60) as Minutes,
                COUNT(DISTINCT p.match_id) as Games,
                SUM(CASE WHEN p.win = 1 THEN 1 ELSE 0 END) as Wins,
                AVG(CASE WHEN p.deaths > 0 THEN (p.kills + p.assists) / p.deaths ELSE (p.kills + p.assists) END) as AvgKda
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            WHERE p.puuid = @puuid {queueFilter}
            GROUP BY FLOOR(m.game_duration_sec / 60)
            ORDER BY Minutes";

        var durations = new List<(int, int, int, double)>();
        await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuid);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var minutes = reader.GetInt32(0);
                var games = reader.GetInt32(1);
                var wins = reader.GetInt32(2);
                var avgKda = reader.IsDBNull(3) ? 0 : reader.GetDouble(3);
                durations.Add((minutes, games, wins, avgKda));
            }
            return 0;
        });
        return durations;
    }

    private string BuildQueueFilter(string queueType)
    {
        return queueType switch
        {
            "ranked_solo" => "AND m.queue_id = 420",
            "ranked_flex" => "AND m.queue_id = 440",
            "normal" => "AND m.queue_id IN (430, 400)",
            "aram" => "AND m.queue_id = 450",
            _ => ""  // all
        };
    }

    private string ValidateQueueType(string? queueType)
    {
        var normalized = queueType?.ToLowerInvariant() ?? "all";
        return normalized switch
        {
            "ranked_solo" or "ranked_flex" or "normal" or "aram" or "all" => normalized,
            _ => "all"
        };
    }

    private List<PerformancePhase> CalculatePerformancePhases(
        (int Games, int Wins, double WinRate, double AvgKda, double AvgGameDurationMinutes) overallStats,
        List<(int Minutes, int Games, int Wins, double AvgKda)> matchDurations)
    {
        var phases = new List<PerformancePhase>();

        // Early game (0-15 min)
        var earlyGames = matchDurations.Where(d => d.Minutes <= 15).ToList();
        if (earlyGames.Count > 0)
        {
            var earlyTotal = earlyGames.Sum(d => d.Games);
            var earlyWins = earlyGames.Sum(d => d.Wins);
            var earlyKda = earlyGames.Average(d => d.AvgKda);
            phases.Add(new PerformancePhase(
                Phase: "Early (0-15 min)",
                Games: earlyTotal,
                Wins: earlyWins,
                WinRate: earlyTotal > 0 ? Math.Round((double)earlyWins / earlyTotal * 100, 1) : 0,
                AvgKda: Math.Round(earlyKda, 2),
                AvgGoldPerMin: 0,  // Would require checkpoint data
                AvgDamagePerMin: 0  // Would require metrics data
            ));
        }

        // Mid game (15-25 min)
        var midGames = matchDurations.Where(d => d.Minutes > 15 && d.Minutes <= 25).ToList();
        if (midGames.Count > 0)
        {
            var midTotal = midGames.Sum(d => d.Games);
            var midWins = midGames.Sum(d => d.Wins);
            var midKda = midGames.Average(d => d.AvgKda);
            phases.Add(new PerformancePhase(
                Phase: "Mid (15-25 min)",
                Games: midTotal,
                Wins: midWins,
                WinRate: midTotal > 0 ? Math.Round((double)midWins / midTotal * 100, 1) : 0,
                AvgKda: Math.Round(midKda, 2),
                AvgGoldPerMin: 0,
                AvgDamagePerMin: 0
            ));
        }

        // Late game (25+ min)
        var lateGames = matchDurations.Where(d => d.Minutes > 25).ToList();
        if (lateGames.Count > 0)
        {
            var lateTotal = lateGames.Sum(d => d.Games);
            var lateWins = lateGames.Sum(d => d.Wins);
            var lateKda = lateGames.Average(d => d.AvgKda);
            phases.Add(new PerformancePhase(
                Phase: "Late (25+ min)",
                Games: lateTotal,
                Wins: lateWins,
                WinRate: lateTotal > 0 ? Math.Round((double)lateWins / lateTotal * 100, 1) : 0,
                AvgKda: Math.Round(lateKda, 2),
                AvgGoldPerMin: 0,
                AvgDamagePerMin: 0
            ));
        }

        return phases;
    }

    private async Task<TrendMetric?> GetRecentTrendAsync(string puuid, string queueFilter, int limit)
    {
        if (limit <= 0)
            return null;

        var sql = $@"
            SELECT
                COUNT(*) as Games,
                SUM(CASE WHEN r.win = 1 THEN 1 ELSE 0 END) as Wins,
                AVG(CASE WHEN r.deaths > 0 THEN (r.kills + r.assists) / r.deaths ELSE (r.kills + r.assists) END) as AvgKda
            FROM (
                SELECT p.win, p.kills, p.deaths, p.assists
                FROM participants p
                INNER JOIN matches m ON m.match_id = p.match_id
                WHERE p.puuid = @puuid {queueFilter}
                ORDER BY m.game_start_time DESC
                LIMIT @limit
            ) r";

        return await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuid);
            cmd.Parameters.AddWithValue("@limit", limit);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var games = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                if (games == 0)
                    return null;

                var wins = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                var avgKda = reader.IsDBNull(2) ? 0 : reader.GetDouble(2);
                var winRate = Math.Round(games > 0 ? (double)wins / games * 100 : 0, 1);
                return new TrendMetric(
                    Games: games,
                    Wins: wins,
                    WinRate: winRate,
                    AvgKda: Math.Round(avgKda, 2)
                );
            }
            return null;
        });
    }
}
