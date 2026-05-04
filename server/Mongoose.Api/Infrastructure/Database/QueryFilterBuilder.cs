using Microsoft.Extensions.Logging;
using MySqlConnector;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Application.Endpoints.Shared;

namespace Mongoose.Api.Infrastructure.Database;

/// <summary>
/// Service for building SQL query filters for queue types and time ranges.
/// Centralizes filter logic to eliminate duplication across repositories.
/// </summary>
public class QueryFilterBuilder : IQueryFilterBuilder
{
    private readonly IDbConnectionFactory _factory;
    private readonly ILogger<QueryFilterBuilder> _logger;

    public QueryFilterBuilder(IDbConnectionFactory factory, ILogger<QueryFilterBuilder> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    /// <inheritdoc />
    public string ValidateQueueType(string? queueType)
    {
        var normalized = queueType?.ToLowerInvariant() ?? "all";
        return normalized switch
        {
            "ranked_solo" or "ranked_flex" or "normal" or "aram" or "all" => normalized,
            _ => "all"
        };
    }

    /// <inheritdoc />
    public string BuildQueueFilter(string queueType)
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

    /// <inheritdoc />
    public async Task<TimeRangeFilter> ResolveTimeRangeAsync(string? timeRange)
    {
        if (string.IsNullOrWhiteSpace(timeRange))
            return new TimeRangeFilter(null, null, "all");

        var normalized = timeRange.Trim().ToLowerInvariant();

        if (normalized is "current_season" or "current-season")
        {
            var seasonCode = await GetCurrentSeasonCodeAsync();
            return new TimeRangeFilter(null, seasonCode, "current_season");
        }

        if (normalized is "last_season" or "last-season" or "previous_season" or "previous-season")
        {
            var seasonCode = await GetPreviousSeasonCodeAsync();
            return new TimeRangeFilter(null, seasonCode, "last_season");
        }

        var timeRangeStart = GetTimeRangeStartUtc(normalized);
        if (timeRangeStart.HasValue)
            return new TimeRangeFilter(timeRangeStart, null, normalized);

        // Unknown or unsupported range => treat as "all"
        return new TimeRangeFilter(null, null, "all");
    }

    /// <inheritdoc />
    public string BuildTimeRangeFilter(TimeRangeFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.NormalizedTimeRange))
        {
            switch (filter.NormalizedTimeRange)
            {
                case "current_season":
                case "current-season":
                case "last_season":
                case "last-season":
                case "previous_season":
                case "previous-season":
                    if (!string.IsNullOrEmpty(filter.SeasonCode))
                    {
                        return "AND m.season_code = @season";
                    }
                    // Season requested but seasons table not populated - return impossible filter
                    // to avoid silently returning "all time" data when seasonal data was expected
                    _logger.LogWarning(
                        "Seasonal time range '{TimeRange}' requested but no season data found. Returning empty result set.",
                        LogSanitizer.Sanitize(filter.NormalizedTimeRange));
                    return "AND 1=0"; // No matches - explicit empty result
            }
        }

        return filter.TimeRangeStart.HasValue
            ? "AND m.game_start_time >= @startTime"
            : string.Empty;
    }

    /// <inheritdoc />
    public void AddTimeRangeParameters(MySqlCommand cmd, TimeRangeFilter filter)
    {
        if (filter.TimeRangeStart.HasValue)
        {
            cmd.Parameters.AddWithValue("@startTime", 
                new DateTimeOffset(filter.TimeRangeStart.Value).ToUnixTimeMilliseconds());
        }
        if (!string.IsNullOrEmpty(filter.SeasonCode))
        {
            cmd.Parameters.AddWithValue("@season", filter.SeasonCode);
        }
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

    private async Task<string?> GetCurrentSeasonCodeAsync()
    {
        const string sqlFromMatches = @"
            SELECT ranked.season_code
            FROM (
                SELECT m.season_code, MAX(m.game_start_time) AS last_game_start
                FROM matches m
                WHERE m.season_code IS NOT NULL
                GROUP BY m.season_code
                ORDER BY last_game_start DESC
                LIMIT 1
            ) ranked";

        const string sqlFromSeasons = @"SELECT season_code FROM seasons WHERE end_date IS NULL ORDER BY start_date DESC LIMIT 1";

        await using var conn = await _factory.CreateOpenConnectionAsync();
        await using (var cmd = new MySqlCommand(sqlFromMatches, conn))
        {
            var result = await cmd.ExecuteScalarAsync();
            if (result is string seasonCode && !string.IsNullOrWhiteSpace(seasonCode))
            {
                return seasonCode;
            }
        }

        await using (var cmd = new MySqlCommand(sqlFromSeasons, conn))
        {
            var result = await cmd.ExecuteScalarAsync();
            return result as string;
        }
    }

    private async Task<string?> GetPreviousSeasonCodeAsync()
    {
        const string sqlFromMatches = @"
            SELECT ranked.season_code
            FROM (
                SELECT m.season_code, MAX(m.game_start_time) AS last_game_start
                FROM matches m
                WHERE m.season_code IS NOT NULL
                GROUP BY m.season_code
                ORDER BY last_game_start DESC
                LIMIT 2
            ) ranked
            ORDER BY ranked.last_game_start DESC
            LIMIT 1 OFFSET 1";

        const string sqlFromSeasons = @"SELECT season_code FROM seasons WHERE end_date IS NOT NULL ORDER BY end_date DESC LIMIT 1";

        await using var conn = await _factory.CreateOpenConnectionAsync();
        await using (var cmd = new MySqlCommand(sqlFromMatches, conn))
        {
            var result = await cmd.ExecuteScalarAsync();
            if (result is string seasonCode && !string.IsNullOrWhiteSpace(seasonCode))
            {
                return seasonCode;
            }
        }

        await using (var cmd = new MySqlCommand(sqlFromSeasons, conn))
        {
            var result = await cmd.ExecuteScalarAsync();
            return result as string;
        }
    }
}

