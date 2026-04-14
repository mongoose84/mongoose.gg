using MySqlConnector;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Application.Services;
using Mongoose.Api.Core.Interfaces;
using static Mongoose.Api.Application.DTOs.ChampionSelectDto;
using static Mongoose.Api.Application.DTOs.MainChampionDto;

namespace Mongoose.Api.Infrastructure.Database.Repositories;

/// <summary>
/// Repository for champion select data.
/// Provides focused queries for champion recommendations without over-fetching.
/// </summary>
public class ChampionSelectRepository : RepositoryBase, IChampionSelectRepository
{
    private readonly ILogger<ChampionSelectRepository> _logger;
    private readonly IQueryFilterBuilder _filterBuilder;

    public ChampionSelectRepository(
        IDbConnectionFactory factory,
        ILogger<ChampionSelectRepository> logger,
        IQueryFilterBuilder filterBuilder) : base(factory)
    {
        _logger = logger;
        _filterBuilder = filterBuilder;
    }

    /// <inheritdoc />
    public async Task<ChampionSelectResponse?> GetChampionSelectDataAsync(IReadOnlyList<string> puuids, string? queueType = null, string? timeRange = null)
    {
        if (puuids.Count == 0)
            return null;

        queueType = _filterBuilder.ValidateQueueType(queueType);
        var timeRangeFilter = await _filterBuilder.ResolveTimeRangeAsync(timeRange);
        var effectiveTimeRangeForLog = string.IsNullOrWhiteSpace(timeRangeFilter.NormalizedTimeRange) ? "all" : timeRangeFilter.NormalizedTimeRange;
        _logger.LogInformation("GetChampionSelectDataAsync start: accountCount={AccountCount}, queueType={Queue}, timeRange={TimeRange}", puuids.Count, LogSanitizer.Sanitize(queueType), LogSanitizer.Sanitize(effectiveTimeRangeForLog));

        var queueFilter = _filterBuilder.BuildQueueFilter(queueType);
        var timeFilter = _filterBuilder.BuildTimeRangeFilter(timeRangeFilter);

        try
        {
            // Get basic stats (games played, win rate)
            var basicStats = await GetBasicStatsAsync(puuids, queueFilter, timeFilter, timeRangeFilter);
            if (basicStats == null)
                return null;

            // Get main champions by role
            var mainChampionsByRole = await GetMainChampionsByRoleAsync(puuids, queueType, queueFilter, timeFilter, timeRangeFilter);

            var response = new ChampionSelectResponse(
                MainChampions: mainChampionsByRole.ToArray(),
                GamesPlayed: basicStats.Value.Games,
                WinRate: basicStats.Value.WinRate
            );

            _logger.LogInformation("GetChampionSelectDataAsync success: accountCount={AccountCount}, games={Games}", puuids.Count, basicStats.Value.Games);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetChampionSelectDataAsync error: accountCount={AccountCount}, queueType={Queue}", puuids.Count, LogSanitizer.Sanitize(queueType));
            throw;
        }
    }

    private async Task<(int Games, double WinRate)?> GetBasicStatsAsync(
        IReadOnlyList<string> puuids, string queueFilter, string timeFilter, TimeRangeFilter timeRangeFilter)
    {
        var (puuidPredicate, puuidParams) = BuildStringInClause("p.puuid", puuids, "puuid");
        var sql = $@"
            SELECT
                COUNT(DISTINCT p.match_id) as Games,
                SUM(CASE WHEN p.win = 1 THEN 1 ELSE 0 END) as Wins
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            WHERE {puuidPredicate} {queueFilter} {timeFilter}";

        var result = await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            foreach (var (name, value) in puuidParams)
            {
                cmd.Parameters.AddWithValue(name, value);
            }
            _filterBuilder.AddTimeRangeParameters(cmd, timeRangeFilter);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync() && !reader.IsDBNull(0))
            {
                var games = reader.GetInt32(0);
                if (games == 0)
                    return null;
                var wins = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                var winRate = games > 0 ? Math.Round((double)wins / games * 100, 1) : 0;
                return ((int Games, double WinRate)?)(Games: games, WinRate: winRate);
            }
            return null;
        });
        return result;
    }

    private async Task<IReadOnlyList<MainChampionRoleGroup>> GetMainChampionsByRoleAsync(
        IReadOnlyList<string> puuids, string queueType, string queueFilter, string timeFilter, TimeRangeFilter timeRangeFilter)
    {
        var (puuidPredicate, puuidParams) = BuildStringInClause("p.puuid", puuids, "puuid");
        var sql = $@"
            SELECT
                COALESCE(NULLIF(p.role, ''), 'UNKNOWN') as Role,
                p.champion_id,
                p.champion_name,
                COUNT(DISTINCT p.match_id) as Games,
                SUM(CASE WHEN p.win = 1 THEN 1 ELSE 0 END) as Wins,
                AVG(p.creep_score) as AvgCs,
                AVG(p.gold_earned / (m.game_duration_sec / 60.0)) as AvgGoldPerMin,
                AVG(p.kills) as AvgKills,
                AVG(p.deaths) as AvgDeaths,
                AVG(p.assists) as AvgAssists,
                AVG(cp15.gold_diff_vs_lane) as AvgGoldDiff15,
                AVG(pm.deaths_pre_10) as AvgDeathsPre10,
                AVG(pm.vision_per_min) as AvgVisionPerMin
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            LEFT JOIN participant_checkpoints cp15 ON cp15.participant_id = p.id AND cp15.minute_mark = 15
            LEFT JOIN participant_metrics pm ON pm.participant_id = p.id
            WHERE {puuidPredicate} {queueFilter} {timeFilter}
            GROUP BY Role, p.champion_id, p.champion_name";

        var rows = new List<MainChampionRecommender.ChampionRoleStats>();
        await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            foreach (var (name, value) in puuidParams)
            {
                cmd.Parameters.AddWithValue(name, value);
            }
            _filterBuilder.AddTimeRangeParameters(cmd, timeRangeFilter);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var role = reader.IsDBNull(0) ? "UNKNOWN" : reader.GetString(0);
                var champId = reader.GetInt32(1);
                var champName = reader.GetString(2);
                var games = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                var wins = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);
                var avgCs = reader.IsDBNull(5) ? 0 : reader.GetDouble(5);
                var avgGoldPerMin = reader.IsDBNull(6) ? 0 : reader.GetDouble(6);
                var avgKills = reader.IsDBNull(7) ? 0 : reader.GetDouble(7);
                var avgDeaths = reader.IsDBNull(8) ? 0 : reader.GetDouble(8);
                var avgAssists = reader.IsDBNull(9) ? 0 : reader.GetDouble(9);
                double? avgGoldDiff15 = reader.IsDBNull(10) ? null : reader.GetDouble(10);
                double? avgDeathsPre10 = reader.IsDBNull(11) ? null : reader.GetDouble(11);
                double? avgVisionPerMin = reader.IsDBNull(12) ? null : reader.GetDouble(12);

                rows.Add(new MainChampionRecommender.ChampionRoleStats(
                    role, champId, champName, games, wins,
                    avgGoldPerMin, avgCs, avgKills, avgDeaths, avgAssists,
                    avgGoldDiff15, avgDeathsPre10, avgVisionPerMin));
            }
            return 0;
        });

        if (rows.Count == 0)
            return Array.Empty<MainChampionRoleGroup>();

        return MainChampionRecommender.BuildMainChampionsByRole(rows, queueType);
    }
}

