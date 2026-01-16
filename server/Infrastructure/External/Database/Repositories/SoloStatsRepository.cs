using System;
using MySqlConnector;
using RiotProxy.Application.Services;
using static RiotProxy.Application.DTOs.SoloSummaryDto;

namespace RiotProxy.Infrastructure.External.Database.Repositories;

/// <summary>
/// Repository for solo dashboard statistics, optimized for dashboard rendering.
/// All queries use match and participant tables.
/// </summary>
public class SoloStatsRepository : RepositoryBase
{
    private readonly ILogger<SoloStatsRepository> _logger;
    public SoloStatsRepository(IDbConnectionFactory factory, ILogger<SoloStatsRepository> logger) : base(factory)
    {
        _logger = logger;
    }

		/// <summary>
		/// Get comprehensive solo dashboard data for a player.
		/// Includes: overall stats, champion pool, performance by phase, role breakdown, death efficiency.
		/// Supports optional queue filtering and time range filtering.
		/// </summary>
		public async Task<SoloDashboardResponse?> GetSoloDashboardAsync(string puuid, string? queueType = null, string? timeRange = null)
		{
		    // Validate queueType and time range
		    queueType = ValidateQueueType(queueType);
		    var (timeRangeStart, seasonCode, normalizedTimeRange) = await ResolveTimeRangeAsync(timeRange);
		    var effectiveTimeRangeForLog = string.IsNullOrWhiteSpace(normalizedTimeRange) ? "all" : normalizedTimeRange;
		    _logger.LogInformation("GetSoloDashboardAsync start: puuid={Puuid}, queueType={Queue}, timeRange={TimeRange}", puuid, queueType, effectiveTimeRangeForLog);

		    // Build base query with optional queue and time/season filters
		    var queueFilter = BuildQueueFilter(queueType);
		    var timeFilter = BuildTimeRangeFilter(normalizedTimeRange, timeRangeStart, seasonCode);

		        // Fetch all necessary data
		        try
		        {
		            var overallStats = await GetOverallStatsAsync(puuid, queueFilter, timeFilter, timeRangeStart, seasonCode);
	            if (overallStats == null)
	                return null;

		            var sideStats = await GetSideStatsAsync(puuid, queueFilter, timeFilter, timeRangeStart, seasonCode);
		            var champions = await GetChampionStatsAsync(puuid, queueFilter, timeFilter, timeRangeStart, seasonCode);
		            var mainChampionsByRole = await GetMainChampionsByRoleAsync(puuid, queueFilter, timeFilter, timeRangeStart, seasonCode);
		            var roleBreakdown = await GetRoleBreakdownAsync(puuid, queueFilter, timeFilter, timeRangeStart, seasonCode);
		            var deathStats = await GetDeathEfficiencyAsync(puuid, queueFilter, timeFilter, timeRangeStart, seasonCode);
		            var matchDurations = await GetMatchDurationsAsync(puuid, queueFilter, timeFilter, timeRangeStart, seasonCode);

	            var totalGames = overallStats.Value.Games;
	            
	            // Calculate main champion (overall, not per role)
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
			            var last10 = await GetRecentTrendAsync(puuid, queueFilter, timeFilter, timeRangeStart, seasonCode, Math.Min(10, totalGames));
			            var last20 = await GetRecentTrendAsync(puuid, queueFilter, timeFilter, timeRangeStart, seasonCode, Math.Min(20, totalGames));

                // Fetch LP trend for ranked queues only
                LpTrendPoint[] lpTrend = Array.Empty<LpTrendPoint>();
                if (queueType is "ranked_solo" or "ranked_flex" or "all")
                {
                    // For "all", fetch both ranked modes; for specific ranked mode, fetch only that
                    var lpTrendList = await GetLpTrendAsync(puuid, queueType == "all" ? null : queueType, 100);
                    lpTrend = lpTrendList.ToArray();
                }

	            var response = new SoloDashboardResponse(
	                GamesPlayed: totalGames,
	                Wins: overallStats.Value.Wins,
	                WinRate: overallStats.Value.WinRate,
	                AvgKda: overallStats.Value.AvgKda,
	                AvgGameDurationMinutes: overallStats.Value.AvgGameDurationMinutes,
	                SideStats: sideStats,
	                UniqueChampsPlayedCount: champions.Count,
	                MainChampion: mainChamp,
	                MainChampions: mainChampionsByRole.ToArray(),
	                Last10Games: last10,
	                Last20Games: last20,
	                PerformanceByPhase: performancePhases.ToArray(),
	                RoleBreakdown: roleBreakdown.ToArray(),
		                DeathEfficiency: deathStats,
		                QueueType: queueType,
                    LpTrend: lpTrend
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
		    string puuid, string queueFilter, string timeFilter, DateTime? timeRangeStart, string? seasonCode)
    {
        var sql = $@"
            SELECT
                COUNT(DISTINCT p.match_id) as Games,
                SUM(CASE WHEN p.win = 1 THEN 1 ELSE 0 END) as Wins,
                AVG(CASE WHEN p.deaths > 0 THEN (p.kills + p.assists) / p.deaths ELSE (p.kills + p.assists) END) as AvgKda,
                AVG(m.game_duration_sec / 60.0) as AvgGameDurationMinutes
	            FROM participants p
	            INNER JOIN matches m ON m.match_id = p.match_id
	            WHERE p.puuid = @puuid {queueFilter} {timeFilter}";

        _logger.LogDebug("GetOverallStatsAsync SQL: {Sql} | puuid={Puuid}, queueFilter={QueueFilter}, timeFilter={TimeFilter}, seasonCode={SeasonCode}",
            sql, puuid, queueFilter, timeFilter, seasonCode);

		        var result = await ExecuteWithConnectionAsync(async conn =>
		        {
		            await using var cmd = new MySqlCommand(sql, conn);
		            cmd.Parameters.AddWithValue("@puuid", puuid);
		            if (timeRangeStart.HasValue)
		            {
		                cmd.Parameters.AddWithValue("@startTime", new DateTimeOffset(timeRangeStart.Value).ToUnixTimeMilliseconds());
		            }
		            if (!string.IsNullOrEmpty(seasonCode))
		            {
		                cmd.Parameters.AddWithValue("@season", seasonCode);
		            }
		        await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync() && !reader.IsDBNull(0))
            {
                var games = reader.GetInt32(0);
                var wins = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
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

		    private async Task<SideWinDistribution> GetSideStatsAsync(string puuid, string queueFilter, string timeFilter, DateTime? timeRangeStart, string? seasonCode)
    {
        var sql = $@"
            SELECT
                SUM(CASE WHEN p.team_id = 100 THEN 1 ELSE 0 END) as BlueGames,
                SUM(CASE WHEN p.team_id = 100 AND p.win = 1 THEN 1 ELSE 0 END) as BlueWins,
                SUM(CASE WHEN p.team_id = 200 THEN 1 ELSE 0 END) as RedGames,
                SUM(CASE WHEN p.team_id = 200 AND p.win = 1 THEN 1 ELSE 0 END) as RedWins
	            FROM participants p
	            INNER JOIN matches m ON m.match_id = p.match_id
	            WHERE p.puuid = @puuid {queueFilter} {timeFilter}";

		        return await ExecuteWithConnectionAsync(async conn =>
		        {
		            await using var cmd = new MySqlCommand(sql, conn);
		            cmd.Parameters.AddWithValue("@puuid", puuid);
		            if (timeRangeStart.HasValue)
		            {
		                cmd.Parameters.AddWithValue("@startTime", new DateTimeOffset(timeRangeStart.Value).ToUnixTimeMilliseconds());
		            }
		            if (!string.IsNullOrEmpty(seasonCode))
		            {
		                cmd.Parameters.AddWithValue("@season", seasonCode);
		            }
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
		        string puuid, string queueFilter, string timeFilter, DateTime? timeRangeStart, string? seasonCode)
    {
        var sql = $@"
            SELECT
                p.champion_id,
                p.champion_name,
                COUNT(DISTINCT p.match_id) as Picks,
                SUM(CASE WHEN p.win = 1 THEN 1 ELSE 0 END) as Wins
	            FROM participants p
	            INNER JOIN matches m ON m.match_id = p.match_id
	            WHERE p.puuid = @puuid {queueFilter} {timeFilter}
            GROUP BY p.champion_id, p.champion_name
            ORDER BY Picks DESC
            LIMIT 20";

		        var champs = new List<(int, string, int, double)>();
		        await ExecuteWithConnectionAsync(async conn =>
		        {
		            await using var cmd = new MySqlCommand(sql, conn);
		            cmd.Parameters.AddWithValue("@puuid", puuid);
		            if (timeRangeStart.HasValue)
		            {
		                cmd.Parameters.AddWithValue("@startTime", new DateTimeOffset(timeRangeStart.Value).ToUnixTimeMilliseconds());
		            }
		            if (!string.IsNullOrEmpty(seasonCode))
		            {
		                cmd.Parameters.AddWithValue("@season", seasonCode);
		            }
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

			    private async Task<IReadOnlyList<MainChampionRoleGroup>> GetMainChampionsByRoleAsync(string puuid, string queueFilter, string timeFilter, DateTime? timeRangeStart, string? seasonCode)
			    {
	        var sql = $@"
	            SELECT
	                COALESCE(NULLIF(p.role, ''), 'UNKNOWN') as Role,
	                p.champion_id,
	                p.champion_name,
	                COUNT(DISTINCT p.match_id) as Games,
	                SUM(CASE WHEN p.win = 1 THEN 1 ELSE 0 END) as Wins
	            FROM participants p
	            INNER JOIN matches m ON m.match_id = p.match_id
	            WHERE p.puuid = @puuid {queueFilter} {timeFilter}
	            GROUP BY Role, p.champion_id, p.champion_name";

		        var rows = new List<MainChampionRecommender.ChampionRoleStats>();
		        await ExecuteWithConnectionAsync(async conn =>
		        {
		            await using var cmd = new MySqlCommand(sql, conn);
		            cmd.Parameters.AddWithValue("@puuid", puuid);
		            if (timeRangeStart.HasValue)
		            {
		                cmd.Parameters.AddWithValue("@startTime", new DateTimeOffset(timeRangeStart.Value).ToUnixTimeMilliseconds());
		            }
		            if (!string.IsNullOrEmpty(seasonCode))
		            {
		                cmd.Parameters.AddWithValue("@season", seasonCode);
		            }
		            await using var reader = await cmd.ExecuteReaderAsync();
	            while (await reader.ReadAsync())
	            {
	                var role = reader.IsDBNull(0) ? "UNKNOWN" : reader.GetString(0);
	                var champId = reader.GetInt32(1);
	                var champName = reader.GetString(2);
	                var games = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
	                var wins = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);

	                rows.Add(new MainChampionRecommender.ChampionRoleStats(role, champId, champName, games, wins));
	            }
	            return 0;
	        });

	        if (rows.Count == 0)
	            return Array.Empty<MainChampionRoleGroup>();

	        return MainChampionRecommender.BuildMainChampionsByRole(rows);
	    }

		    private async Task<List<RolePerformance>> GetRoleBreakdownAsync(string puuid, string queueFilter, string timeFilter, DateTime? timeRangeStart, string? seasonCode)
    {
        var sql = $@"
            SELECT
                COALESCE(NULLIF(p.role, ''), 'UNKNOWN') as Role,
                COUNT(DISTINCT p.match_id) as Games,
                SUM(CASE WHEN p.win = 1 THEN 1 ELSE 0 END) as Wins,
                AVG(CASE WHEN p.deaths > 0 THEN (p.kills + p.assists) / p.deaths ELSE (p.kills + p.assists) END) as AvgKda
		            FROM participants p
		            INNER JOIN matches m ON m.match_id = p.match_id
		            WHERE p.puuid = @puuid {queueFilter} {timeFilter}
            GROUP BY Role
            ORDER BY Games DESC";

		    var roles = new List<RolePerformance>();
		            await ExecuteWithConnectionAsync(async conn =>
		            {
		                await using var cmd = new MySqlCommand(sql, conn);
		                cmd.Parameters.AddWithValue("@puuid", puuid);
		                if (timeRangeStart.HasValue)
		                {
		                    cmd.Parameters.AddWithValue("@startTime", new DateTimeOffset(timeRangeStart.Value).ToUnixTimeMilliseconds());
		                }
		                if (!string.IsNullOrEmpty(seasonCode))
		                {
		                    cmd.Parameters.AddWithValue("@season", seasonCode);
		                }
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

		    private async Task<DeathEfficiency> GetDeathEfficiencyAsync(string puuid, string queueFilter, string timeFilter, DateTime? timeRangeStart, string? seasonCode)
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
	            WHERE p.puuid = @puuid {queueFilter} {timeFilter}";

		        return await ExecuteWithConnectionAsync(async conn =>
		        {
		            await using var cmd = new MySqlCommand(sql, conn);
		            cmd.Parameters.AddWithValue("@puuid", puuid);
		            if (timeRangeStart.HasValue)
		            {
		                cmd.Parameters.AddWithValue("@startTime", new DateTimeOffset(timeRangeStart.Value).ToUnixTimeMilliseconds());
		            }
		            if (!string.IsNullOrEmpty(seasonCode))
		            {
		                cmd.Parameters.AddWithValue("@season", seasonCode);
		            }
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
		        string puuid, string queueFilter, string timeFilter, DateTime? timeRangeStart, string? seasonCode)
    {
        var sql = $@"
            SELECT
                FLOOR(m.game_duration_sec / 60) as Minutes,
                COUNT(DISTINCT p.match_id) as Games,
                SUM(CASE WHEN p.win = 1 THEN 1 ELSE 0 END) as Wins,
                AVG(CASE WHEN p.deaths > 0 THEN (p.kills + p.assists) / p.deaths ELSE (p.kills + p.assists) END) as AvgKda
	            FROM participants p
	            INNER JOIN matches m ON m.match_id = p.match_id
	            WHERE p.puuid = @puuid {queueFilter} {timeFilter}
            GROUP BY FLOOR(m.game_duration_sec / 60)
            ORDER BY Minutes";

		    var durations = new List<(int, int, int, double)>();
		            await ExecuteWithConnectionAsync(async conn =>
		            {
		                await using var cmd = new MySqlCommand(sql, conn);
		                cmd.Parameters.AddWithValue("@puuid", puuid);
		                if (timeRangeStart.HasValue)
		                {
		                    cmd.Parameters.AddWithValue("@startTime", new DateTimeOffset(timeRangeStart.Value).ToUnixTimeMilliseconds());
		                }
		                if (!string.IsNullOrEmpty(seasonCode))
		                {
		                    cmd.Parameters.AddWithValue("@season", seasonCode);
		                }
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

		    private async Task<(DateTime? TimeRangeStart, string? SeasonCode, string NormalizedTimeRange)> ResolveTimeRangeAsync(string? timeRange)
		    {
		        if (string.IsNullOrWhiteSpace(timeRange))
		            return (null, null, "all");

		        var normalized = timeRange.Trim().ToLowerInvariant();

		        if (normalized is "current_season" or "current-season")
		        {
		            var seasonCode = await GetCurrentSeasonCodeAsync();
		            return (null, seasonCode, "current_season");
		        }

		        if (normalized is "last_season" or "last-season" or "previous_season" or "previous-season")
		        {
		            var seasonCode = await GetPreviousSeasonCodeAsync();
		            return (null, seasonCode, "last_season");
		        }

		        var timeRangeStart = GetTimeRangeStartUtc(normalized);
		        if (timeRangeStart.HasValue)
		            return (timeRangeStart, null, normalized);

		        // Unknown or unsupported range => treat as "all"
		        return (null, null, "all");
		    }

		    private async Task<string?> GetCurrentSeasonCodeAsync()
		    {
		        const string sql = @"SELECT season_code FROM seasons WHERE end_date IS NULL ORDER BY start_date DESC LIMIT 1";

		        return await ExecuteWithConnectionAsync(async conn =>
		        {
		            await using var cmd = new MySqlCommand(sql, conn);
		            var result = await cmd.ExecuteScalarAsync();
		            return result == null || result == DBNull.Value ? null : Convert.ToString(result);
		        });
		    }

		    private async Task<string?> GetPreviousSeasonCodeAsync()
		    {
		        const string sql = @"SELECT season_code FROM seasons WHERE end_date IS NOT NULL ORDER BY end_date DESC LIMIT 1";

		        return await ExecuteWithConnectionAsync(async conn =>
		        {
		            await using var cmd = new MySqlCommand(sql, conn);
		            var result = await cmd.ExecuteScalarAsync();
		            return result == null || result == DBNull.Value ? null : Convert.ToString(result);
		        });
		    }

		    private static DateTime? GetTimeRangeStartUtc(string? timeRange)
		    {
		        if (string.IsNullOrWhiteSpace(timeRange))
		            return null;

		        var normalized = timeRange.Trim().ToLowerInvariant();
		        var now = DateTime.UtcNow;

		        return normalized switch
		        {
		            "1w" => now.AddDays(-7),
		            "1m" => now.AddMonths(-1),
		            "3m" => now.AddMonths(-3),
		            "6m" => now.AddMonths(-6),
		            _ => null
		        };
		    }
		
		    private string BuildTimeRangeFilter(string? normalizedTimeRange, DateTime? timeRangeStart, string? seasonCode)
		    {
		        if (!string.IsNullOrWhiteSpace(normalizedTimeRange))
		        {
		            switch (normalizedTimeRange)
		            {
		                case "current_season":
		                case "current-season":
		                case "last_season":
		                case "last-season":
		                case "previous_season":
		                case "previous-season":
		                    if (!string.IsNullOrEmpty(seasonCode))
		                    {
		                        return "AND m.season_code = @season";
		                    }
		                    // Season requested but seasons table not populated - return impossible filter
		                    // to avoid silently returning "all time" data when seasonal data was expected
		                    _logger.LogWarning(
		                        "Seasonal time range '{TimeRange}' requested but no season data found. Returning empty result set.",
		                        normalizedTimeRange);
		                    return "AND 1=0"; // No matches - explicit empty result
		            }
		        }

		        return timeRangeStart.HasValue
		            ? "AND m.game_start_time >= @startTime"
		            : string.Empty;
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

		    private async Task<TrendMetric?> GetRecentTrendAsync(string puuid, string queueFilter, string timeFilter, DateTime? timeRangeStart, string? seasonCode, int limit)
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
	                WHERE p.puuid = @puuid {queueFilter} {timeFilter}
                ORDER BY m.game_start_time DESC
                LIMIT @limit
            ) r";

		        return await ExecuteWithConnectionAsync(async conn =>
		        {
		            await using var cmd = new MySqlCommand(sql, conn);
		            cmd.Parameters.AddWithValue("@puuid", puuid);
		            if (timeRangeStart.HasValue)
		            {
		                cmd.Parameters.AddWithValue("@startTime", new DateTimeOffset(timeRangeStart.Value).ToUnixTimeMilliseconds());
		            }
		            if (!string.IsNullOrEmpty(seasonCode))
		            {
		                cmd.Parameters.AddWithValue("@season", seasonCode);
		            }
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

    /// <summary>
    /// Get winrate trend data as a rolling 20-game average for chart display.
    /// Public method that accepts raw query parameters.
    /// Returns an array of data points with gameIndex, winRate, and timestamp.
    /// Maximum 100 data points with downsampling for larger datasets.
    /// </summary>
    public async Task<WinrateTrendPoint[]> GetWinrateTrendAsync(string puuid, string? queueType = null, string? timeRange = null)
    {
        // Validate and process filters (same as GetSoloDashboardAsync)
        queueType = ValidateQueueType(queueType);
        var (timeRangeStart, seasonCode, normalizedTimeRange) = await ResolveTimeRangeAsync(timeRange);
        var queueFilter = BuildQueueFilter(queueType);
        var timeFilter = BuildTimeRangeFilter(normalizedTimeRange, timeRangeStart, seasonCode);

        return await GetWinrateTrendInternalAsync(puuid, queueFilter, timeFilter, timeRangeStart, seasonCode);
    }

    /// <summary>
    /// Internal method for winrate trend calculation with pre-processed filters.
    /// </summary>
    private async Task<WinrateTrendPoint[]> GetWinrateTrendInternalAsync(
        string puuid, string queueFilter, string timeFilter, DateTime? timeRangeStart, string? seasonCode)
    {
        // Fetch all games in chronological order (oldest first)
        var sql = $@"
            SELECT
                p.win,
                m.game_start_time
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            WHERE p.puuid = @puuid {queueFilter} {timeFilter}
            ORDER BY m.game_start_time ASC";

        var games = new List<(bool Win, long Timestamp)>();

        await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuid);
            if (timeRangeStart.HasValue)
            {
                cmd.Parameters.AddWithValue("@startTime", new DateTimeOffset(timeRangeStart.Value).ToUnixTimeMilliseconds());
            }
            if (!string.IsNullOrEmpty(seasonCode))
            {
                cmd.Parameters.AddWithValue("@season", seasonCode);
            }

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var win = reader.GetInt32(0) == 1;
                var timestamp = reader.GetInt64(1);
                games.Add((win, timestamp));
            }
            return 0;
        });

        if (games.Count == 0)
            return Array.Empty<WinrateTrendPoint>();

        // Calculate rolling 20-game average for each game
        const int windowSize = 20;
        var trendPoints = new List<WinrateTrendPoint>();

        for (int i = 0; i < games.Count; i++)
        {
            // Calculate rolling average using last 'windowSize' games (or fewer for early games)
            var windowStart = Math.Max(0, i - windowSize + 1);
            var windowGames = games.Skip(windowStart).Take(i - windowStart + 1).ToList();

            var wins = windowGames.Count(g => g.Win);
            var total = windowGames.Count;
            var winRate = total > 0 ? Math.Round((double)wins / total * 100, 1) : 0;

            var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(games[i].Timestamp).UtcDateTime;

            trendPoints.Add(new WinrateTrendPoint(
                GameIndex: i + 1,
                WinRate: winRate,
                Timestamp: timestamp
            ));
        }

        // Downsample if more than 100 data points
        const int maxDataPoints = 100;
        if (trendPoints.Count > maxDataPoints)
        {
            var step = (double)trendPoints.Count / maxDataPoints;
            var downsampled = new List<WinrateTrendPoint>();

            for (int i = 0; i < maxDataPoints; i++)
            {
                var index = (int)(i * step);
                if (index < trendPoints.Count)
                {
                    downsampled.Add(trendPoints[index]);
                }
            }

            // Always include the last data point
            if (downsampled.Count > 0 && downsampled[^1].GameIndex != trendPoints[^1].GameIndex)
            {
                downsampled[^1] = trendPoints[^1];
            }

            return downsampled.ToArray();
        }

        return trendPoints.ToArray();
    }

    /// <summary>
    /// Get daily match counts for the past 6 months for heatmap display.
    /// Returns a dictionary keyed by date (YYYY-MM-DD) with match count values.
    /// </summary>
    public async Task<Dictionary<string, int>> GetDailyMatchCountsAsync(string puuid, int daysBack = 91)
    {
        var startDate = DateTime.UtcNow.Date.AddDays(-daysBack);
        var startTimestamp = new DateTimeOffset(startDate).ToUnixTimeMilliseconds();

        var sql = @"
            SELECT
                DATE(FROM_UNIXTIME(m.game_start_time / 1000)) as game_date,
                COUNT(DISTINCT m.match_id) as match_count
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            WHERE p.puuid = @puuid
              AND m.game_start_time >= @start_timestamp
            GROUP BY DATE(FROM_UNIXTIME(m.game_start_time / 1000))
            ORDER BY game_date ASC";

        var result = new Dictionary<string, int>();

        await ExecuteWithConnectionAsync<int>(async (conn, cmd) =>
        {
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@puuid", puuid);
            cmd.Parameters.AddWithValue("@start_timestamp", startTimestamp);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var gameDate = reader.GetDateTime(0);
                var matchCount = reader.GetInt32(1);
                result[gameDate.ToString("yyyy-MM-dd")] = matchCount;
            }
            return 0;
        });

        return result;
    }

    /// <summary>
    /// Get LP trend data for ranked matches with LP data available.
    /// Returns LP points ordered from oldest to newest for chart visualization.
    /// </summary>
    /// <param name="puuid">Player PUUID</param>
    /// <param name="queueType">Queue type filter (ranked_solo, ranked_flex, or null for both)</param>
    /// <param name="limit">Maximum number of data points to return</param>
    /// <returns>List of LP trend points ordered oldest to newest</returns>
    public async Task<IList<LpTrendPoint>> GetLpTrendAsync(string puuid, string? queueType = null, int limit = 100)
    {
        // Build queue filter for ranked modes only
        // 420 = Ranked Solo/Duo, 440 = Ranked Flex
        var queueFilter = queueType?.ToLowerInvariant() switch
        {
            "ranked_solo" => "AND m.queue_id = 420",
            "ranked_flex" => "AND m.queue_id = 440",
            _ => "AND m.queue_id IN (420, 440)" // Both ranked modes
        };

        // Query participants with LP data, ordered by game time (oldest first for indexing)
        var sql = $@"
            SELECT
                p.lp_after,
                p.tier_after,
                p.rank_after,
                p.win,
                m.game_start_time
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            WHERE p.puuid = @puuid
              AND p.lp_after IS NOT NULL
              AND p.tier_after IS NOT NULL
              {queueFilter}
            ORDER BY m.game_start_time ASC
            LIMIT @limit";

        var points = new List<LpTrendPoint>();

        await ExecuteWithConnectionAsync<int>(async (conn, cmd) =>
        {
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@puuid", puuid);
            cmd.Parameters.AddWithValue("@limit", limit);

            await using var reader = await cmd.ExecuteReaderAsync();

            int gameIndex = 1;
            int? previousLp = null;
            string? previousTier = null;
            string? previousRank = null;

            while (await reader.ReadAsync())
            {
                var lpAfter = reader.GetInt32(0);
                var tierAfter = reader.GetString(1);
                var rankAfter = reader.IsDBNull(2) ? "" : reader.GetString(2);
                var win = reader.GetBoolean(3);
                var gameStartTime = reader.GetInt64(4);

                // Format rank string (e.g., "Silver IV")
                var rankString = FormatRankString(tierAfter, rankAfter);

                // Detect promotion/demotion first (needed for LP gain calculation)
                var isPromotion = DetectPromotion(previousTier, previousRank, tierAfter, rankAfter);
                var isDemotion = DetectDemotion(previousTier, previousRank, tierAfter, rankAfter);

                // Calculate LP gain/loss
                // - null for first game (no previous data)
                // - null for promotions/demotions (LP resets make the raw diff misleading)
                int? lpGain = null;
                if (previousLp.HasValue && !isPromotion && !isDemotion)
                {
                    lpGain = lpAfter - previousLp.Value;
                }

                // Convert game_start_time (milliseconds) to DateTime
                var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(gameStartTime).UtcDateTime;

                points.Add(new LpTrendPoint(
                    GameIndex: gameIndex,
                    LpGain: lpGain,
                    CurrentLp: lpAfter,
                    Rank: rankString,
                    Timestamp: timestamp,
                    IsPromotion: isPromotion,
                    IsDemotion: isDemotion,
                    Win: win
                ));

                previousLp = lpAfter;
                previousTier = tierAfter;
                previousRank = rankAfter;
                gameIndex++;
            }

            return 0;
        });

        return points;
    }

    /// <summary>
    /// Formats tier and rank into a readable string (e.g., "Silver IV")
    /// </summary>
    private static string FormatRankString(string tier, string rank)
    {
        // Capitalize first letter of tier
        var formattedTier = tier.Length > 0
            ? char.ToUpper(tier[0]) + tier.Substring(1).ToLower()
            : tier;

        return string.IsNullOrEmpty(rank) ? formattedTier : $"{formattedTier} {rank}";
    }

    /// <summary>
    /// Detects if a rank change represents a promotion.
    /// </summary>
    private static bool DetectPromotion(string? prevTier, string? prevRank, string currTier, string currRank)
    {
        if (string.IsNullOrEmpty(prevTier)) return false;

        var prevTierLevel = GetTierLevel(prevTier);
        var currTierLevel = GetTierLevel(currTier);

        // Tier promotion
        if (currTierLevel > prevTierLevel) return true;

        // Division promotion (same tier, lower division number = higher rank)
        if (currTierLevel == prevTierLevel)
        {
            var prevDivision = GetDivisionLevel(prevRank);
            var currDivision = GetDivisionLevel(currRank);
            return currDivision > prevDivision;
        }

        return false;
    }

    /// <summary>
    /// Detects if a rank change represents a demotion.
    /// </summary>
    private static bool DetectDemotion(string? prevTier, string? prevRank, string currTier, string currRank)
    {
        if (string.IsNullOrEmpty(prevTier)) return false;

        var prevTierLevel = GetTierLevel(prevTier);
        var currTierLevel = GetTierLevel(currTier);

        // Tier demotion
        if (currTierLevel < prevTierLevel) return true;

        // Division demotion (same tier, higher division number = lower rank)
        if (currTierLevel == prevTierLevel)
        {
            var prevDivision = GetDivisionLevel(prevRank);
            var currDivision = GetDivisionLevel(currRank);
            return currDivision < prevDivision;
        }

        return false;
    }

    /// <summary>
    /// Gets the numeric level of a tier (higher = better).
    /// </summary>
    private static int GetTierLevel(string? tier)
    {
        return tier?.ToUpperInvariant() switch
        {
            "IRON" => 1,
            "BRONZE" => 2,
            "SILVER" => 3,
            "GOLD" => 4,
            "PLATINUM" => 5,
            "EMERALD" => 6,
            "DIAMOND" => 7,
            "MASTER" => 8,
            "GRANDMASTER" => 9,
            "CHALLENGER" => 10,
            _ => 0
        };
    }

    /// <summary>
    /// Gets the numeric level of a division (higher = better within tier).
    /// IV = 1, III = 2, II = 3, I = 4
    /// </summary>
    private static int GetDivisionLevel(string? rank)
    {
        return rank?.ToUpperInvariant() switch
        {
            "IV" => 1,
            "III" => 2,
            "II" => 3,
            "I" => 4,
            _ => 0 // Master+ don't have divisions
        };
    }
}
