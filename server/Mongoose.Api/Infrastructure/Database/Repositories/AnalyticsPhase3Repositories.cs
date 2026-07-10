namespace Mongoose.Api.Infrastructure.Database.Repositories;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Mongoose.Api.Core.Interfaces;

/// <summary>
/// Repository for analytics event dimensions queries
/// </summary>
public class AnalyticsEventDimensionsRepository : IAnalyticsEventDimensionsRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<AnalyticsEventDimensionsRepository> _logger;

    public AnalyticsEventDimensionsRepository(
        IDbConnectionFactory connectionFactory,
        ILogger<AnalyticsEventDimensionsRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task InsertDimensionAsync(AnalyticsEventDimension dimension)
    {
        using (var connection = await _connectionFactory.GetConnectionAsync())
        {
            const string sql = @"
                INSERT INTO analytics_event_dimensions (
                    event_id, event_name, event_category, page_path, referrer_domain, referrer_path,
                    device_type, browser_name, browser_version, os_name, os_version,
                    country_code, region_code, city, tier, is_authenticated,
                    user_id, session_id, event_timestamp_utc, custom_properties
                ) VALUES (
                    @EventId, @EventName, @EventCategory, @PagePath, @ReferrerDomain, @ReferrerPath,
                    @DeviceType, @BrowserName, @BrowserVersion, @OsName, @OsVersion,
                    @CountryCode, @RegionCode, @City, @Tier, @IsAuthenticated,
                    @UserId, @SessionId, @EventTimestampUtc, @CustomProperties
                )";

            await connection.ExecuteAsync(sql, new
            {
                dimension.EventId,
                dimension.EventName,
                dimension.EventCategory,
                dimension.PagePath,
                dimension.ReferrerDomain,
                dimension.ReferrerPath,
                dimension.DeviceType,
                dimension.BrowserName,
                dimension.BrowserVersion,
                dimension.OsName,
                dimension.OsVersion,
                dimension.CountryCode,
                dimension.RegionCode,
                dimension.City,
                dimension.Tier,
                dimension.IsAuthenticated,
                dimension.UserId,
                dimension.SessionId,
                dimension.EventTimestampUtc,
                dimension.CustomProperties
            });
        }
    }

    public async Task<IEnumerable<AnalyticsEventDimension>> GetExtractedDimensionsSinceAsync(long lastEventId, int limit = 1000)
    {
        using (var connection = await _connectionFactory.GetConnectionAsync())
        {
            const string sql = @"
                SELECT * FROM analytics_event_dimensions
                WHERE event_id > @LastEventId
                ORDER BY event_id ASC
                LIMIT @Limit";

            return await connection.QueryAsync<AnalyticsEventDimension>(sql, new { LastEventId = lastEventId, Limit = limit });
        }
    }

    public async Task<IEnumerable<AnalyticsEventDimension>> GetEventsByNameAsync(string eventName, DateTime startUtc, DateTime endUtc, int limit = 1000)
    {
        using (var connection = await _connectionFactory.GetConnectionAsync())
        {
            const string sql = @"
                SELECT * FROM analytics_event_dimensions
                WHERE event_name = @EventName
                  AND event_timestamp_utc BETWEEN @StartUtc AND @EndUtc
                ORDER BY event_timestamp_utc DESC
                LIMIT @Limit";

            return await connection.QueryAsync<AnalyticsEventDimension>(sql, new { EventName = eventName, StartUtc = startUtc, EndUtc = endUtc, Limit = limit });
        }
    }

    public async Task<IEnumerable<DimensionValue>> GetDimensionValuesAsync(string dimensionName, DateTime startUtc, DateTime endUtc, int limit = 50)
    {
        using (var connection = await _connectionFactory.GetConnectionAsync())
        {
            var columnName = MapDimensionColumn(dimensionName);
            var sql = $@"
                SELECT
                    {columnName} as Value,
                    COUNT(*) as Count,
                    ROUND(100.0 * COUNT(*) / SUM(COUNT(*)) OVER(), 2) as PercentOfTotal,
                    COUNT(DISTINCT user_id) as UniqueUsers
                FROM analytics_event_dimensions
                WHERE event_timestamp_utc BETWEEN @StartUtc AND @EndUtc
                GROUP BY {columnName}
                ORDER BY Count DESC
                LIMIT @Limit";

            return await connection.QueryAsync<DimensionValue>(sql, new { StartUtc = startUtc, EndUtc = endUtc, Limit = limit });
        }
    }

    public async Task<EventDimensionDetail> GetEventDetailAsync(string eventName, DateTime startUtc, DateTime endUtc)
    {
        using (var connection = await _connectionFactory.GetConnectionAsync())
        {
            const string baseSql = @"
                SELECT
                    event_name as EventName,
                    event_category as EventCategory,
                    COUNT(*) as TotalCount,
                    COUNT(DISTINCT user_id) as UniqueUsers
                FROM analytics_event_dimensions
                WHERE event_name = @EventName
                  AND event_timestamp_utc BETWEEN @StartUtc AND @EndUtc
                GROUP BY event_name, event_category";

            var baseResult = await connection.QueryFirstOrDefaultAsync<dynamic>(baseSql, new { EventName = eventName, StartUtc = startUtc, EndUtc = endUtc });

            if (baseResult == null)
                return null;

            var detail = new EventDimensionDetail
            {
                EventName = baseResult.EventName,
                EventCategory = baseResult.EventCategory,
                TotalCount = baseResult.TotalCount,
                UniqueUsers = baseResult.UniqueUsers
            };

            // Get breakdowns for each dimension
            detail.ByPath = (await GetDimensionValuesAsync("pagePath", startUtc, endUtc, 20)).ToList();
            detail.ByDevice = (await GetDimensionValuesAsync("deviceType", startUtc, endUtc, 10)).ToList();
            detail.ByBrowser = (await GetDimensionValuesAsync("browser", startUtc, endUtc, 15)).ToList();
            detail.ByTier = (await GetDimensionValuesAsync("tier", startUtc, endUtc, 10)).ToList();
            detail.ByCountry = (await GetDimensionValuesAsync("country", startUtc, endUtc, 20)).ToList();

            return detail;
        }
    }

    public async Task<int> CountEventsByTierAsync(string tier, DateTime startUtc, DateTime endUtc)
    {
        using (var connection = await _connectionFactory.GetConnectionAsync())
        {
            const string sql = @"
                SELECT COUNT(*) FROM analytics_event_dimensions
                WHERE tier = @Tier
                  AND event_timestamp_utc BETWEEN @StartUtc AND @EndUtc";

            return await connection.QueryFirstAsync<int>(sql, new { Tier = tier, StartUtc = startUtc, EndUtc = endUtc });
        }
    }

    private string MapDimensionColumn(string dimensionName) => dimensionName switch
    {
        "pagePath" => "page_path",
        "referrer" => "referrer_domain",
        "deviceType" => "device_type",
        "browser" => "browser_name",
        "os" => "os_name",
        "country" => "country_code",
        "tier" => "tier",
        _ => throw new ArgumentException($"Unknown dimension: {dimensionName}")
    };
}

/// <summary>
/// Repository for journey/navigation queries
/// </summary>
public class AnalyticsJourneyRepository : IAnalyticsJourneyRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<AnalyticsJourneyRepository> _logger;

    public AnalyticsJourneyRepository(
        IDbConnectionFactory connectionFactory,
        ILogger<AnalyticsJourneyRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task InsertJourneyStepAsync(AnalyticsJourneyStep step)
    {
        using (var connection = await _connectionFactory.GetConnectionAsync())
        {
            const string sql = @"
                INSERT INTO analytics_journey_steps (
                    session_id, user_id, step_number, source_page, destination_page,
                    event_name, transition_timestamp_utc, time_on_previous_page_seconds,
                    device_type, tier
                ) VALUES (
                    @SessionId, @UserId, @StepNumber, @SourcePage, @DestinationPage,
                    @EventName, @TransitionTimestampUtc, @TimeOnPreviousPageSeconds,
                    @DeviceType, @Tier
                )";

            await connection.ExecuteAsync(sql, step);
        }
    }

    public async Task<IEnumerable<AnalyticsJourneyStep>> GetSessionJourneyAsync(string sessionId)
    {
        using (var connection = await _connectionFactory.GetConnectionAsync())
        {
            const string sql = @"
                SELECT * FROM analytics_journey_steps
                WHERE session_id = @SessionId
                ORDER BY step_number ASC";

            return await connection.QueryAsync<AnalyticsJourneyStep>(sql, new { SessionId = sessionId });
        }
    }

    public async Task<IEnumerable<NavigationFlow>> GetTopFlowsAsync(DateTime startUtc, DateTime endUtc, int minTransitions = 5, int limit = 50)
    {
        using (var connection = await _connectionFactory.GetConnectionAsync())
        {
            const string sql = @"
                SELECT
                    source_page as SourcePage,
                    destination_page as DestinationPage,
                    COUNT(*) as TransitionCount,
                    COUNT(DISTINCT user_id) as UniqueUsers,
                    AVG(IFNULL(time_on_previous_page_seconds, 0)) as AvgTimeOnSourcePageSeconds,
                    ROUND(100.0 * COUNT(*) / SUM(COUNT(*)) OVER(), 2) as ConversionRate
                FROM analytics_journey_steps
                WHERE transition_timestamp_utc BETWEEN @StartUtc AND @EndUtc
                GROUP BY source_page, destination_page
                HAVING TransitionCount >= @MinTransitions
                ORDER BY TransitionCount DESC
                LIMIT @Limit";

            return await connection.QueryAsync<NavigationFlow>(sql, new { StartUtc = startUtc, EndUtc = endUtc, MinTransitions = minTransitions, Limit = limit });
        }
    }

    public async Task<IEnumerable<AnalyticsJourneyStep>> GetUserJourneysAsync(long userId, DateTime startUtc, DateTime endUtc)
    {
        using (var connection = await _connectionFactory.GetConnectionAsync())
        {
            const string sql = @"
                SELECT * FROM analytics_journey_steps
                WHERE user_id = @UserId
                  AND transition_timestamp_utc BETWEEN @StartUtc AND @EndUtc
                ORDER BY transition_timestamp_utc DESC";

            return await connection.QueryAsync<AnalyticsJourneyStep>(sql, new { UserId = userId, StartUtc = startUtc, EndUtc = endUtc });
        }
    }

    public async Task<IEnumerable<PathSequence>> GetPathSequencesAsync(string startEvent, DateTime startUtc, DateTime endUtc, int maxSteps = 5, int limit = 100)
    {
        // Simplified implementation - would require recursive CTE in production
        return new List<PathSequence>();
    }
}

/// <summary>
/// Repository for funnel queries
/// </summary>
public class AnalyticsFunnelRepository : IAnalyticsFunnelRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<AnalyticsFunnelRepository> _logger;

    public AnalyticsFunnelRepository(
        IDbConnectionFactory connectionFactory,
        ILogger<AnalyticsFunnelRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task InsertFunnelStepAsync(AnalyticsFunnelStep step)
    {
        using (var connection = await _connectionFactory.GetConnectionAsync())
        {
            const string sql = @"
                INSERT INTO analytics_funnel_steps (
                    funnel_name, session_id, user_id, step_number, step_name,
                    event_name, completed, completed_at_utc, step_timestamp_utc,
                    time_since_previous_step_seconds, tier, device_type
                ) VALUES (
                    @FunnelName, @SessionId, @UserId, @StepNumber, @StepName,
                    @EventName, @Completed, @CompletedAtUtc, @StepTimestampUtc,
                    @TimeSincePreviousStepSeconds, @Tier, @DeviceType
                )";

            await connection.ExecuteAsync(sql, step);
        }
    }

    public async Task MarkFunnelStepCompletedAsync(long stepId, DateTime completedAtUtc)
    {
        using (var connection = await _connectionFactory.GetConnectionAsync())
        {
            const string sql = @"
                UPDATE analytics_funnel_steps
                SET completed = 1, completed_at_utc = @CompletedAtUtc
                WHERE id = @StepId";

            await connection.ExecuteAsync(sql, new { StepId = stepId, CompletedAtUtc = completedAtUtc });
        }
    }

    public async Task<AnalyticsFunnelDefinition> GetFunnelDefinitionAsync(string funnelName)
    {
        using (var connection = await _connectionFactory.GetConnectionAsync())
        {
            const string sql = @"
                SELECT * FROM analytics_funnel_definitions
                WHERE funnel_name = @FunnelName";

            return await connection.QueryFirstOrDefaultAsync<AnalyticsFunnelDefinition>(sql, new { FunnelName = funnelName });
        }
    }

    public async Task<IEnumerable<AnalyticsFunnelDefinition>> GetAllFunnelDefinitionsAsync()
    {
        using (var connection = await _connectionFactory.GetConnectionAsync())
        {
            const string sql = "SELECT * FROM analytics_funnel_definitions ORDER BY funnel_name";
            return await connection.QueryAsync<AnalyticsFunnelDefinition>(sql);
        }
    }

    public async Task<FunnelAnalysis> AnalyzeFunnelAsync(string funnelName, DateTime startUtc, DateTime endUtc, string? tierFilter = null)
    {
        using (var connection = await _connectionFactory.GetConnectionAsync())
        {
            // Get all step completions for this funnel
            var sql = @"
                SELECT
                    step_number as StepNumber,
                    step_name as StepName,
                    COUNT(CASE WHEN completed = 1 THEN 1 END) as CompletedCount,
                    COUNT(DISTINCT CASE WHEN completed = 1 THEN user_id END) as UniqueUsers,
                    AVG(CASE WHEN completed = 1 THEN time_since_previous_step_seconds END) as AvgTimeToCompleteSeconds
                FROM analytics_funnel_steps
                WHERE funnel_name = @FunnelName
                  AND step_timestamp_utc BETWEEN @StartUtc AND @EndUtc
                GROUP BY step_number, step_name
                ORDER BY step_number ASC";

            var steps = (await connection.QueryAsync<FunnelStepAnalysis>(sql, new { FunnelName = funnelName, StartUtc = startUtc, EndUtc = endUtc })).ToList();

            // Calculate conversion rates
            if (steps.Count > 0)
            {
                var firstStepTotal = steps[0].CompletedCount;
                for (int i = 0; i < steps.Count; i++)
                {
                    if (i > 0)
                    {
                        var prevStepTotal = steps[i - 1].CompletedCount;
                        steps[i].ConversionRate = prevStepTotal > 0 ? (decimal)steps[i].CompletedCount / prevStepTotal * 100 : 0;
                    }

                    steps[i].CumulativeConversionRate = firstStepTotal > 0 ? (decimal)steps[i].CompletedCount / firstStepTotal * 100 : 0;
                }
            }

            var analysis = new FunnelAnalysis
            {
                FunnelName = funnelName,
                Steps = steps,
                TotalSessions = steps.FirstOrDefault()?.CompletedCount ?? 0,
                CompletedSessions = steps.LastOrDefault()?.CompletedCount ?? 0,
                OverallConversionRate = steps.FirstOrDefault()?.CompletedCount > 0
                    ? (decimal)(steps.LastOrDefault()?.CompletedCount ?? 0) / (steps.FirstOrDefault()?.CompletedCount ?? 1) * 100
                    : 0
            };

            return analysis;
        }
    }

    public async Task<IEnumerable<string>> GetFunnelQualifyingSessionsAsync(string funnelName, DateTime startUtc, DateTime endUtc, string? eventNameFilter = null)
    {
        using (var connection = await _connectionFactory.GetConnectionAsync())
        {
            const string sql = @"
                SELECT DISTINCT session_id FROM analytics_funnel_steps
                WHERE funnel_name = @FunnelName
                  AND step_number = 1
                  AND step_timestamp_utc BETWEEN @StartUtc AND @EndUtc";

            return await connection.QueryAsync<string>(sql, new { FunnelName = funnelName, StartUtc = startUtc, EndUtc = endUtc });
        }
    }

    public async Task<IEnumerable<AnalyticsFunnelStep>> GetSessionFunnelStepsAsync(string sessionId, string funnelName)
    {
        using (var connection = await _connectionFactory.GetConnectionAsync())
        {
            const string sql = @"
                SELECT * FROM analytics_funnel_steps
                WHERE session_id = @SessionId AND funnel_name = @FunnelName
                ORDER BY step_number ASC";

            return await connection.QueryAsync<AnalyticsFunnelStep>(sql, new { SessionId = sessionId, FunnelName = funnelName });
        }
    }
}

/// <summary>
/// Repository for rollup aggregates
/// </summary>
public class AnalyticsRollupRepository : IAnalyticsRollupRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<AnalyticsRollupRepository> _logger;

    public AnalyticsRollupRepository(
        IDbConnectionFactory connectionFactory,
        ILogger<AnalyticsRollupRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task UpsertHourlyRollupAsync(AnalyticsRollupHourly rollup)
    {
        using (var connection = await _connectionFactory.GetConnectionAsync())
        {
            const string sql = @"
                INSERT INTO analytics_rollup_hourly (
                    date_hour, event_name, event_category, event_count, unique_users, unique_sessions,
                    count_authenticated, count_free_tier, count_pro_tier, count_desktop, count_mobile, count_tablet
                ) VALUES (
                    @DateHour, @EventName, @EventCategory, @EventCount, @UniqueUsers, @UniqueSessions,
                    @CountAuthenticated, @CountFreetier, @CountProTier, @CountDesktop, @CountMobile, @CountTablet
                )
                ON DUPLICATE KEY UPDATE
                    event_count = VALUES(event_count),
                    unique_users = VALUES(unique_users),
                    unique_sessions = VALUES(unique_sessions),
                    count_authenticated = VALUES(count_authenticated),
                    count_free_tier = VALUES(count_free_tier),
                    count_pro_tier = VALUES(count_pro_tier),
                    count_desktop = VALUES(count_desktop),
                    count_mobile = VALUES(count_mobile),
                    count_tablet = VALUES(count_tablet)";

            await connection.ExecuteAsync(sql, rollup);
        }
    }

    public async Task<IEnumerable<AnalyticsRollupHourly>> GetEventRollupsAsync(string eventName, DateTime startUtc, DateTime endUtc)
    {
        using (var connection = await _connectionFactory.GetConnectionAsync())
        {
            const string sql = @"
                SELECT * FROM analytics_rollup_hourly
                WHERE (event_name = @EventName OR @EventName = '*')
                  AND date_hour BETWEEN @StartUtc AND @EndUtc
                ORDER BY date_hour DESC";

            return await connection.QueryAsync<AnalyticsRollupHourly>(sql, new { EventName = eventName, StartUtc = startUtc, EndUtc = endUtc });
        }
    }

    public async Task<IEnumerable<AnalyticsRollupHourly>> GetLatestRollupsAsync(int hoursBack = 24, int limit = 100)
    {
        using (var connection = await _connectionFactory.GetConnectionAsync())
        {
            const string sql = @"
                SELECT * FROM analytics_rollup_hourly
                WHERE date_hour >= DATE_SUB(UTC_TIMESTAMP, INTERVAL @HoursBack HOUR)
                ORDER BY date_hour DESC
                LIMIT @Limit";

            return await connection.QueryAsync<AnalyticsRollupHourly>(sql, new { HoursBack = hoursBack, Limit = limit });
        }
    }

    public async Task<IEnumerable<RollupTrendData>> GetEventTrendAsync(string eventName, int hoursBack = 168)
    {
        using (var connection = await _connectionFactory.GetConnectionAsync())
        {
            const string sql = @"
                SELECT
                    date_hour as DateHour,
                    event_count as EventCount,
                    unique_users as UniqueUsers,
                    unique_sessions as UniqueSessions,
                    count_pro_tier as ProTierCount
                FROM analytics_rollup_hourly
                WHERE event_name = @EventName
                  AND date_hour >= DATE_SUB(UTC_TIMESTAMP, INTERVAL @HoursBack HOUR)
                ORDER BY date_hour ASC";

            return await connection.QueryAsync<RollupTrendData>(sql, new { EventName = eventName, HoursBack = hoursBack });
        }
    }
}
