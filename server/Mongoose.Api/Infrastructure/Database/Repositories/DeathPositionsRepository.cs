using MySqlConnector;
using Mongoose.Api.Application.Services;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Core.QueryModels;

namespace Mongoose.Api.Infrastructure.Database.Repositories;

/// <summary>
/// Repository for death positions data (danger zone heatmap).
/// Provides death coordinates, phase summary, and match metadata for visualization.
/// </summary>
public class DeathPositionsRepository : RepositoryBase, IDeathPositionsRepository
{
    private readonly ILogger<DeathPositionsRepository> _logger;
    private readonly IQueryFilterBuilder _filterBuilder;

    public DeathPositionsRepository(
        IDbConnectionFactory factory,
        ILogger<DeathPositionsRepository> logger,
        IQueryFilterBuilder filterBuilder) : base(factory)
    {
        _logger = logger;
        _filterBuilder = filterBuilder;
    }

    /// <inheritdoc />
    public async Task<DeathPositionsResult?> GetDeathPositionsAsync(
        string puuid, 
        string? queueType = null, 
        string? timeRange = null, 
        string? side = null)
        => await GetDeathPositionsAsync([puuid], queueType, timeRange, side);

    /// <inheritdoc />
    public async Task<DeathPositionsResult?> GetDeathPositionsAsync(
        IReadOnlyList<string> puuids,
        string? queueType = null,
        string? timeRange = null,
        string? side = null)
    {
        if (puuids.Count == 0)
            return null;

        queueType = _filterBuilder.ValidateQueueType(queueType);
        var timeRangeFilter = await _filterBuilder.ResolveTimeRangeAsync(timeRange);
        var effectiveTimeRange = string.IsNullOrWhiteSpace(timeRangeFilter.NormalizedTimeRange) 
            ? "all" 
            : timeRangeFilter.NormalizedTimeRange;

        _logger.LogInformation(
            "GetDeathPositionsAsync start: accountCount={AccountCount}, queueType={Queue}, timeRange={TimeRange}, side={Side}",
            puuids.Count, LogSanitizer.Sanitize(queueType) ?? "all", LogSanitizer.Sanitize(effectiveTimeRange) ?? "all", LogSanitizer.Sanitize(side) ?? "all");

        var queueFilter = _filterBuilder.BuildQueueFilter(queueType);
        var timeFilter = _filterBuilder.BuildTimeRangeFilter(timeRangeFilter);
        var sideFilter = BuildSideFilter(side);

        try
        {
            // Fetch death positions (includes MatchId for in-memory summary aggregation)
            var deathPositions = await GetDeathPositionsInternalAsync(
                puuids, queueFilter, timeFilter, sideFilter, timeRangeFilter);

            // Compute summary in-memory from fetched deaths — guarantees phase counts
            // and totalDeaths exactly match what is returned in the Deaths array.
            var response = new DeathPositionsResult(
                Deaths: deathPositions.ToArray(),
                TotalDeaths: deathPositions.Count,
                MatchesAnalyzed: deathPositions.Select(d => d.MatchId).Distinct().Count(),
                PhaseSummary: new DeathPositionPhaseSummary(
                    Early: deathPositions.Count(d => d.Phase == "early"),
                    Mid: deathPositions.Count(d => d.Phase == "mid"),
                    Late: deathPositions.Count(d => d.Phase == "late"),
                    VeryLate: deathPositions.Count(d => d.Phase == "veryLate")
                )
            );

            _logger.LogInformation(
                "GetDeathPositionsAsync success: accountCount={AccountCount}, totalDeaths={Deaths}, matches={Matches}",
                puuids.Count, deathPositions.Count, deathPositions.Select(d => d.MatchId).Distinct().Count());

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "GetDeathPositionsAsync error: accountCount={AccountCount}, queueType={Queue}, timeRange={TimeRange}, side={Side}",
                puuids.Count, LogSanitizer.Sanitize(queueType) ?? "all", LogSanitizer.Sanitize(effectiveTimeRange) ?? "all", LogSanitizer.Sanitize(side) ?? "all");
            throw;
        }
    }

    private async Task<List<DeathPositionData>> GetDeathPositionsInternalAsync(
        IReadOnlyList<string> puuids,
        string queueFilter,
        string timeFilter,
        string sideFilter,
        TimeRangeFilter timeRangeFilter)
    {
        var (puuidPredicate, puuidParams) = BuildStringInClause("p.puuid", puuids, "puuid");
        var sql = $@"
            SELECT
                pde.position_x,
                pde.position_y,
                pde.minute_mark,
                pde.killer_champion_id,
                pde.assist_count,
                p.match_id
            FROM participant_death_events pde
            INNER JOIN participants p ON p.id = pde.participant_id
            INNER JOIN matches m ON m.match_id = p.match_id
            WHERE {puuidPredicate}
                {queueFilter}
                {timeFilter}
                {sideFilter}
            ORDER BY m.game_start_time DESC, pde.minute_mark ASC
            LIMIT 100";

        _logger.LogDebug(
            "GetDeathPositionsInternalAsync SQL: {Sql} | accountCount={AccountCount}, seasonCode={SeasonCode}",
            sql, puuids.Count, timeRangeFilter.SeasonCode);

        return await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            foreach (var (name, value) in puuidParams)
            {
                cmd.Parameters.AddWithValue(name, value);
            }
            _filterBuilder.AddTimeRangeParameters(cmd, timeRangeFilter);

            var results = new List<DeathPositionData>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var x = reader.GetInt32(0);
                var y = reader.GetInt32(1);
                var minuteMark = reader.GetInt32(2);
                var killerChampionId = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3);
                var assistCount = reader.GetInt32(4);
                var matchId = reader.GetString(5);
                var phase = ClassifyPhase(minuteMark);

                results.Add(new DeathPositionData(
                    X: x,
                    Y: y,
                    MinuteMark: minuteMark,
                    Phase: phase,
                    KillerChampionId: killerChampionId,
                    AssistCount: assistCount,
                    MatchId: matchId
                ));
            }

            return results;
        });
    }

    private static string BuildSideFilter(string? side)
    {
        return side?.ToLowerInvariant() switch
        {
            "blue" => "AND p.team_id = 100",
            "red" => "AND p.team_id = 200",
            _ => "" // "all" or null
        };
    }

    private static string ClassifyPhase(int minute)
    {
        return minute switch
        {
            < 10 => "early",
            < 20 => "mid",
            < 30 => "late",
            _ => "veryLate"
        };
    }
}
