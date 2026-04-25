using MySqlConnector;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Core.QueryModels;

namespace Mongoose.Api.Infrastructure.Database.Repositories;

/// <summary>
/// Repository for champion matchup statistics.
/// Provides data about how a player performs with specific champions against specific opponents.
/// </summary>
public class MatchupRepository : RepositoryBase, IMatchupRepository
{
    private readonly ILogger<MatchupRepository> _logger;
    private readonly IQueryFilterBuilder _filterBuilder;

    public MatchupRepository(
        IDbConnectionFactory factory,
        ILogger<MatchupRepository> logger,
        IQueryFilterBuilder filterBuilder) : base(factory)
    {
        _logger = logger;
        _filterBuilder = filterBuilder;
    }

    /// <inheritdoc />
    public async Task<ChampionMatchupsResponse> GetChampionMatchupsAsync(IReadOnlyList<string> puuids, string? queueType = null, string? timeRange = null)
    {
        queueType = _filterBuilder.ValidateQueueType(queueType);
        var timeRangeFilter = await _filterBuilder.ResolveTimeRangeAsync(timeRange);
        var effectiveTimeRangeForLog = string.IsNullOrWhiteSpace(timeRangeFilter.NormalizedTimeRange) ? "all" : timeRangeFilter.NormalizedTimeRange;
        _logger.LogInformation("GetChampionMatchupsAsync start: accountCount={AccountCount}, queueType={Queue}, timeRange={TimeRange}", puuids.Count, LogSanitizer.Sanitize(queueType), LogSanitizer.Sanitize(effectiveTimeRangeForLog));

        var queueFilter = _filterBuilder.BuildQueueFilter(queueType);
        var timeFilter = _filterBuilder.BuildTimeRangeFilter(timeRangeFilter);

        try
        {
            var topChampions = await GetTopChampionsForMatchupsAsync(puuids, queueFilter, timeFilter, timeRangeFilter);

            var matchups = new List<ChampionMatchup>();
            foreach (var champ in topChampions)
            {
                var opponents = await GetOpponentMatchupsAsync(puuids, champ.ChampionId, champ.Role, queueFilter, timeFilter, timeRangeFilter);
                matchups.Add(new ChampionMatchup(
                    ChampionId: champ.ChampionId,
                    ChampionName: champ.ChampionName,
                    Role: champ.Role,
                    TotalGames: champ.TotalGames,
                    Wins: champ.Wins,
                    WinRate: champ.WinRate,
                    Opponents: opponents.ToArray()
                ));
            }

            _logger.LogInformation("GetChampionMatchupsAsync success: accountCount={AccountCount}, champions={Count}", puuids.Count, matchups.Count);
            return new ChampionMatchupsResponse(matchups.ToArray(), queueType, effectiveTimeRangeForLog);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetChampionMatchupsAsync error: accountCount={AccountCount}, queueType={Queue}", puuids.Count, LogSanitizer.Sanitize(queueType));
            throw;
        }
    }

    private async Task<List<(int ChampionId, string ChampionName, string Role, int TotalGames, int Wins, double WinRate)>> GetTopChampionsForMatchupsAsync(
        IReadOnlyList<string> puuids, string queueFilter, string timeFilter, TimeRangeFilter timeRangeFilter)
    {
        var (puuidPredicate, puuidParams) = BuildStringInClause("p.puuid", puuids, "puuid");
        var sql = $@"
            SELECT
                p.champion_id,
                p.champion_name,
                COALESCE(NULLIF(p.role, ''), 'UNKNOWN') as Role,
                COUNT(DISTINCT p.match_id) as TotalGames,
                SUM(CASE WHEN p.win = 1 THEN 1 ELSE 0 END) as Wins
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            WHERE {puuidPredicate}
            AND m.game_duration_sec >= {MinValidGameDurationSec} {queueFilter} {timeFilter}
            GROUP BY p.champion_id, p.champion_name, Role
            ORDER BY TotalGames DESC
            LIMIT 5";

        var champions = new List<(int, string, string, int, int, double)>();

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
                var champId = reader.GetInt32(0);
                var champName = reader.GetString(1);
                var role = reader.GetString(2);
                var totalGames = reader.GetInt32(3);
                var wins = reader.GetInt32(4);
                var winRate = totalGames > 0 ? Math.Round((double)wins / totalGames * 100, 1) : 0;
                champions.Add((champId, champName, role, totalGames, wins, winRate));
            }
            return 0;
        });

        return champions;
    }

    private async Task<List<OpponentMatchup>> GetOpponentMatchupsAsync(
        IReadOnlyList<string> puuids, int championId, string role, string queueFilter, string timeFilter, TimeRangeFilter timeRangeFilter)
    {
        var (puuidPredicate, puuidParams) = BuildStringInClause("p.puuid", puuids, "puuid");
        var sql = $@"
            SELECT
                t.OpponentChampionId,
                t.OpponentChampionName,
                SUM(CASE WHEN t.IsInLane = 1 AND t.Win = 1 THEN 1 ELSE 0 END) as InLaneWins,
                SUM(CASE WHEN t.IsInLane = 1 AND t.Win = 0 THEN 1 ELSE 0 END) as InLaneLosses,
                SUM(CASE WHEN t.IsInLane = 0 AND t.Win = 1 THEN 1 ELSE 0 END) as OutOfLaneWins,
                SUM(CASE WHEN t.IsInLane = 0 AND t.Win = 0 THEN 1 ELSE 0 END) as OutOfLaneLosses
            FROM (
                SELECT DISTINCT
                    p.match_id,
                    p.win as Win,
                    opp.champion_id as OpponentChampionId,
                    opp.champion_name as OpponentChampionName,
                    CASE
                        WHEN p.role IN ('BOTTOM', 'UTILITY') AND opp.role IN ('BOTTOM', 'UTILITY') THEN 1
                        WHEN p.role NOT IN ('BOTTOM', 'UTILITY') AND opp.role = p.role AND p.role != '' AND p.role IS NOT NULL THEN 1
                        WHEN opp.lane = p.lane AND p.lane != '' AND p.lane IS NOT NULL AND (p.role = '' OR p.role IS NULL) THEN 1
                        ELSE 0
                    END as IsInLane
                FROM participants p
                INNER JOIN matches m ON m.match_id = p.match_id
                INNER JOIN participants opp ON opp.match_id = p.match_id
                    AND opp.team_id != p.team_id
                WHERE {puuidPredicate}
                    AND p.champion_id = @championId
                    AND COALESCE(NULLIF(p.role, ''), 'UNKNOWN') = @role
                    AND m.game_duration_sec >= {MinValidGameDurationSec}
                    {queueFilter} {timeFilter}
            ) t
            GROUP BY t.OpponentChampionId, t.OpponentChampionName
            ORDER BY (SUM(CASE WHEN t.IsInLane = 1 THEN 1 ELSE 0 END) + SUM(CASE WHEN t.IsInLane = 0 THEN 1 ELSE 0 END)) DESC";

        var opponents = new List<OpponentMatchup>();

        await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            foreach (var (name, value) in puuidParams)
            {
                cmd.Parameters.AddWithValue(name, value);
            }
            cmd.Parameters.AddWithValue("@championId", championId);
            cmd.Parameters.AddWithValue("@role", role);
            _filterBuilder.AddTimeRangeParameters(cmd, timeRangeFilter);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                opponents.Add(new OpponentMatchup(
                    OpponentChampionId: reader.GetInt32(0),
                    OpponentChampionName: reader.GetString(1),
                    InLaneWins: reader.GetInt32(2),
                    InLaneLosses: reader.GetInt32(3),
                    OutOfLaneWins: reader.GetInt32(4),
                    OutOfLaneLosses: reader.GetInt32(5)
                ));
            }
            return 0;
        });

        return opponents;
    }
}
