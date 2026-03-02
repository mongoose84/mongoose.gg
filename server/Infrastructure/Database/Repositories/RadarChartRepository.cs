using MySqlConnector;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Core.Interfaces;
using static Mongoose.Api.Application.DTOs.Solo.RadarChartDto;

namespace Mongoose.Api.Infrastructure.Database.Repositories;

/// <summary>
/// Repository for solo radar chart data.
/// Aggregates and normalizes six performance dimensions for spider chart visualization.
/// </summary>
public class RadarChartRepository : RepositoryBase, IRadarChartRepository
{
    private readonly ILogger<RadarChartRepository> _logger;
    private readonly IQueryFilterBuilder _filterBuilder;

    public RadarChartRepository(
        IDbConnectionFactory factory,
        ILogger<RadarChartRepository> logger,
        IQueryFilterBuilder filterBuilder) : base(factory)
    {
        _logger = logger;
        _filterBuilder = filterBuilder;
    }

    /// <inheritdoc />
    public async Task<RadarChartResponse?> GetRadarChartAsync(string puuid, string? queueType = null, string? timeRange = null)
        => await GetRadarChartAsync([puuid], queueType, timeRange);

    /// <inheritdoc />
    public async Task<RadarChartResponse?> GetRadarChartAsync(IReadOnlyList<string> puuids, string? queueType = null, string? timeRange = null)
    {
        if (puuids.Count == 0)
            return null;

        queueType = _filterBuilder.ValidateQueueType(queueType);
        var timeRangeFilter = await _filterBuilder.ResolveTimeRangeAsync(timeRange);
        var queueFilter = _filterBuilder.BuildQueueFilter(queueType);
        var timeFilter = _filterBuilder.BuildTimeRangeFilter(timeRangeFilter);
        var (puuidPredicate, puuidParams) = BuildStringInClause("p.puuid", puuids, "puuid");

        var sql = $@"
            SELECT
                COUNT(*) AS games_analyzed,
                AVG(pc.gold_diff_vs_lane) AS avg_gold_diff_15,
                AVG(p.creep_score / (m.game_duration_sec / 60.0)) AS avg_cs_per_min,
                AVG(pm.kill_participation_pct) AS avg_kill_participation,
                AVG(pm.vision_per_min) AS avg_vision_per_min,
                AVG(
                    CASE
                        WHEN (COALESCE(tobj.dragons_taken,0) + COALESCE(tobj.heralds_taken,0) + COALESCE(tobj.barons_taken,0) + COALESCE(tobj.towers_taken,0)) > 0
                        THEN (COALESCE(po.dragons_participated,0) + COALESCE(po.heralds_participated,0) + COALESCE(po.barons_participated,0) + COALESCE(po.towers_participated,0)) * 100.0
                            / (COALESCE(tobj.dragons_taken,0) + COALESCE(tobj.heralds_taken,0) + COALESCE(tobj.barons_taken,0) + COALESCE(tobj.towers_taken,0))
                        ELSE NULL
                    END
                ) AS avg_objective_participation,
                AVG(p.deaths) AS avg_deaths
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            LEFT JOIN participant_metrics pm ON pm.participant_id = p.id
            LEFT JOIN participant_checkpoints pc ON pc.participant_id = p.id AND pc.minute_mark = 15
            LEFT JOIN participant_objectives po ON po.participant_id = p.id
            LEFT JOIN team_objectives tobj ON tobj.match_id = p.match_id AND tobj.team_id = p.team_id
            WHERE {puuidPredicate} {queueFilter} {timeFilter}";

        return await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            foreach (var (name, value) in puuidParams)
            {
                cmd.Parameters.AddWithValue(name, value);
            }
            _filterBuilder.AddTimeRangeParameters(cmd, timeRangeFilter);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            var gamesAnalyzed = reader.GetInt32(0);
            if (gamesAnalyzed <= 0)
                return null;

            var avgGoldDiff15 = GetNullableDouble(reader, 1) ?? 0;
            var avgCsPerMin = GetNullableDouble(reader, 2) ?? 0;
            var avgKillParticipation = GetNullableDouble(reader, 3) ?? 0;
            var avgVisionPerMin = GetNullableDouble(reader, 4) ?? 0;
            var avgObjectiveParticipation = GetNullableDouble(reader, 5) ?? 0;
            var avgDeaths = GetNullableDouble(reader, 6) ?? 0;

            _logger.LogInformation(
                "Radar chart query success: accountCount={AccountCount}, games={Games}, queueType={Queue}, timeRange={TimeRange}",
                puuids.Count,
                gamesAnalyzed,
                LogSanitizer.Sanitize(queueType) ?? "all",
                LogSanitizer.Sanitize(timeRangeFilter.NormalizedTimeRange) ?? "all");

            var axes = new[]
            {
                new RadarAxis(
                    Key: "laning",
                    Label: "Laning",
                    Value: NormalizeToPercentage(avgGoldDiff15, -2000, 2000),
                    RawValue: Math.Round(avgGoldDiff15, 1),
                    RawUnit: "gold diff @15"
                ),
                new RadarAxis(
                    Key: "farming",
                    Label: "Farming",
                    Value: NormalizeToPercentage(avgCsPerMin, 0, 10),
                    RawValue: Math.Round(avgCsPerMin, 2),
                    RawUnit: "CS/min"
                ),
                new RadarAxis(
                    Key: "combat",
                    Label: "Combat",
                    Value: Clamp(avgKillParticipation, 0, 100),
                    RawValue: Math.Round(avgKillParticipation, 1),
                    RawUnit: "% KP"
                ),
                new RadarAxis(
                    Key: "vision",
                    Label: "Vision",
                    Value: NormalizeToPercentage(avgVisionPerMin, 0, 2.5),
                    RawValue: Math.Round(avgVisionPerMin, 2),
                    RawUnit: "VS/min"
                ),
                new RadarAxis(
                    Key: "objectives",
                    Label: "Objectives",
                    Value: Clamp(avgObjectiveParticipation, 0, 100),
                    RawValue: Math.Round(avgObjectiveParticipation, 1),
                    RawUnit: "% obj"
                ),
                new RadarAxis(
                    Key: "survivability",
                    Label: "Survivability",
                    Value: NormalizeInvertedToPercentage(avgDeaths, 0, 10),
                    RawValue: Math.Round(avgDeaths, 2),
                    RawUnit: "deaths/game"
                )
            };

            return new RadarChartResponse(axes, gamesAnalyzed);
        });
    }

    private static double? GetNullableDouble(MySqlDataReader reader, int index)
    {
        if (reader.IsDBNull(index))
            return null;

        var raw = reader.GetValue(index);
        return raw switch
        {
            decimal decimalValue => Convert.ToDouble(decimalValue),
            double doubleValue => doubleValue,
            float floatValue => floatValue,
            int intValue => intValue,
            long longValue => longValue,
            _ => Convert.ToDouble(raw)
        };
    }

    private static double NormalizeToPercentage(double value, double min, double max)
    {
        if (max <= min)
            return 0;

        var normalized = (value - min) / (max - min) * 100;
        return Math.Round(Clamp(normalized, 0, 100), 1);
    }

    private static double NormalizeInvertedToPercentage(double value, double min, double max)
    {
        if (max <= min)
            return 0;

        var normalized = (max - value) / (max - min) * 100;
        return Math.Round(Clamp(normalized, 0, 100), 1);
    }

    private static double Clamp(double value, double min, double max)
    {
        double clamped;
        if (value < min)
        {
            clamped = min;
        }
        else if (value > max)
        {
            clamped = max;
        }
        else
        {
            clamped = value;
        }
        // Ensure consistent precision for normalized values that rely on Clamp directly.
        return Math.Round(clamped, 1);
    }
}