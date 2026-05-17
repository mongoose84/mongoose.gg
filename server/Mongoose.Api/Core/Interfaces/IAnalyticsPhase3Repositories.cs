namespace Mongoose.Api.Core.Interfaces;

using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Repository for event dimension queries and persistence.
/// Handles enriched dimension data (device, browser, geography, etc.)
/// </summary>
public interface IAnalyticsEventDimensionsRepository
{
    /// <summary>
    /// Insert a dimension record for an event
    /// </summary>
    Task InsertDimensionAsync(AnalyticsEventDimension dimension);

    /// <summary>
    /// Get all unprocessed events since last extraction
    /// </summary>
    Task<IEnumerable<AnalyticsEventDimension>> GetExtractedDimensionsSinceAsync(long lastEventId, int limit = 1000);

    /// <summary>
    /// Query events by name within time range
    /// </summary>
    Task<IEnumerable<AnalyticsEventDimension>> GetEventsByNameAsync(string eventName, DateTime startUtc, DateTime endUtc, int limit = 1000);

    /// <summary>
    /// Get unique values for a dimension with counts
    /// </summary>
    Task<IEnumerable<DimensionValue>> GetDimensionValuesAsync(string dimensionName, DateTime startUtc, DateTime endUtc, int limit = 50);

    /// <summary>
    /// Get event detail with all dimension breakdowns
    /// </summary>
    Task<EventDimensionDetail> GetEventDetailAsync(string eventName, DateTime startUtc, DateTime endUtc);

    /// <summary>
    /// Count events by tier within time range
    /// </summary>
    Task<int> CountEventsByTierAsync(string tier, DateTime startUtc, DateTime endUtc);
}

/// <summary>
/// Repository for user journey tracking and analysis
/// </summary>
public interface IAnalyticsJourneyRepository
{
    /// <summary>
    /// Record a navigation step in user journey
    /// </summary>
    Task InsertJourneyStepAsync(AnalyticsJourneyStep step);

    /// <summary>
    /// Get complete journey for a session
    /// </summary>
    Task<IEnumerable<AnalyticsJourneyStep>> GetSessionJourneyAsync(string sessionId);

    /// <summary>
    /// Get top navigation flows (source -> destination)
    /// </summary>
    Task<IEnumerable<NavigationFlow>> GetTopFlowsAsync(DateTime startUtc, DateTime endUtc, int minTransitions = 5, int limit = 50);

    /// <summary>
    /// Get all journeys for a user (across all sessions)
    /// </summary>
    Task<IEnumerable<AnalyticsJourneyStep>> GetUserJourneysAsync(long userId, DateTime startUtc, DateTime endUtc);

    /// <summary>
    /// Get multi-step paths starting from an event
    /// </summary>
    Task<IEnumerable<PathSequence>> GetPathSequencesAsync(string startEvent, DateTime startUtc, DateTime endUtc, int maxSteps = 5, int limit = 100);
}

/// <summary>
/// Repository for funnel tracking and analysis
/// </summary>
public interface IAnalyticsFunnelRepository
{
    /// <summary>
    /// Record a step in a funnel for a session
    /// </summary>
    Task InsertFunnelStepAsync(AnalyticsFunnelStep step);

    /// <summary>
    /// Mark a funnel step as completed
    /// </summary>
    Task MarkFunnelStepCompletedAsync(long stepId, DateTime completedAtUtc);

    /// <summary>
    /// Get funnel definition by name
    /// </summary>
    Task<AnalyticsFunnelDefinition> GetFunnelDefinitionAsync(string funnelName);

    /// <summary>
    /// Get all enabled funnel definitions
    /// </summary>
    Task<IEnumerable<AnalyticsFunnelDefinition>> GetAllFunnelDefinitionsAsync();

    /// <summary>
    /// Analyze funnel conversion for time range
    /// </summary>
    Task<FunnelAnalysis> AnalyzeFunnelAsync(string funnelName, DateTime startUtc, DateTime endUtc, string? tierFilter = null);

    /// <summary>
    /// Get sessions matching first step of funnel
    /// </summary>
    Task<IEnumerable<string>> GetFunnelQualifyingSessionsAsync(string funnelName, DateTime startUtc, DateTime endUtc, string? eventNameFilter = null);

    /// <summary>
    /// Get funnel steps for a session
    /// </summary>
    Task<IEnumerable<AnalyticsFunnelStep>> GetSessionFunnelStepsAsync(string sessionId, string funnelName);
}

/// <summary>
/// Repository for hourly rollup aggregates
/// </summary>
public interface IAnalyticsRollupRepository
{
    /// <summary>
    /// Insert or update hourly rollup record
    /// </summary>
    Task UpsertHourlyRollupAsync(AnalyticsRollupHourly rollup);

    /// <summary>
    /// Get hourly rollups for an event across time range
    /// </summary>
    Task<IEnumerable<AnalyticsRollupHourly>> GetEventRollupsAsync(string eventName, DateTime startUtc, DateTime endUtc);

    /// <summary>
    /// Get latest hourly rollup data (for dashboards)
    /// </summary>
    Task<IEnumerable<AnalyticsRollupHourly>> GetLatestRollupsAsync(int hoursBack = 24, int limit = 100);

    /// <summary>
    /// Get trend data for an event across multiple hours
    /// </summary>
    Task<IEnumerable<RollupTrendData>> GetEventTrendAsync(string eventName, int hoursBack = 168);
}

// ============================================================================
// QUERY RESULT TYPES
// ============================================================================

public record AnalyticsEventDimension
{
    public long Id { get; set; }
    public long EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string EventCategory { get; set; } = string.Empty;
    public string? PagePath { get; set; }
    public string? ReferrerDomain { get; set; }
    public string? ReferrerPath { get; set; }
    public string? DeviceType { get; set; }
    public string? BrowserName { get; set; }
    public string? BrowserVersion { get; set; }
    public string? OsName { get; set; }
    public string? OsVersion { get; set; }
    public string? CountryCode { get; set; }
    public string? RegionCode { get; set; }
    public string? City { get; set; }
    public string Tier { get; set; } = "free";
    public bool IsAuthenticated { get; set; }
    public long? UserId { get; set; }
    public string? SessionId { get; set; }
    public DateTime EventTimestampUtc { get; set; }
    public string? CustomProperties { get; set; }
}

public record AnalyticsJourneyStep
{
    public long Id { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public long? UserId { get; set; }
    public int StepNumber { get; set; }
    public string? SourcePage { get; set; }
    public string DestinationPage { get; set; } = string.Empty;
    public string? EventName { get; set; }
    public DateTime TransitionTimestampUtc { get; set; }
    public int? TimeOnPreviousPageSeconds { get; set; }
    public string? DeviceType { get; set; }
    public string Tier { get; set; } = "free";
}

public record AnalyticsFunnelStep
{
    public long Id { get; set; }
    public string FunnelName { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public long? UserId { get; set; }
    public int StepNumber { get; set; }
    public string StepName { get; set; } = string.Empty;
    public string? EventName { get; set; }
    public bool Completed { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime StepTimestampUtc { get; set; }
    public int? TimeSincePreviousStepSeconds { get; set; }
    public string Tier { get; set; } = "free";
    public string? DeviceType { get; set; }
}

public record AnalyticsRollupHourly
{
    public long Id { get; set; }
    public DateTime DateHour { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string EventCategory { get; set; } = string.Empty;
    public long EventCount { get; set; }
    public int UniqueUsers { get; set; }
    public int UniqueSessions { get; set; }
    public long CountAuthenticated { get; set; }
    public long CountFreetier { get; set; }
    public long CountProTier { get; set; }
    public long CountDesktop { get; set; }
    public long CountMobile { get; set; }
    public long CountTablet { get; set; }
    public string? TopCountries { get; set; }
}

public record AnalyticsFunnelDefinition
{
    public int Id { get; set; }
    public string FunnelName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Enabled { get; set; }
    public string Steps { get; set; } = "[]";  // JSON array
    public int MaxTimeBetweenStepsHours { get; set; } = 24;
}

// ============================================================================
// QUERY RESULT DTOs
// ============================================================================

public record DimensionValue
{
    public string Value { get; set; } = string.Empty;
    public long Count { get; set; }
    public decimal PercentOfTotal { get; set; }
    public int UniqueUsers { get; set; }
}

public record EventDimensionDetail
{
    public string EventName { get; set; } = string.Empty;
    public string EventCategory { get; set; } = string.Empty;
    public long TotalCount { get; set; }
    public int UniqueUsers { get; set; }
    
    public List<DimensionValue> ByPath { get; set; } = new();
    public List<DimensionValue> ByDevice { get; set; } = new();
    public List<DimensionValue> ByBrowser { get; set; } = new();
    public List<DimensionValue> ByTier { get; set; } = new();
    public List<DimensionValue> ByCountry { get; set; } = new();
}

public record NavigationFlow
{
    public string SourcePage { get; set; } = string.Empty;
    public string DestinationPage { get; set; } = string.Empty;
    public long TransitionCount { get; set; }
    public int UniqueUsers { get; set; }
    public int AvgTimeOnSourcePageSeconds { get; set; }
    public decimal ConversionRate { get; set; }
}

public record PathSequence
{
    public List<string> Steps { get; set; } = new();
    public long Count { get; set; }
    public int UniqueUsers { get; set; }
    public decimal ConversionRate { get; set; }
}

public record FunnelAnalysis
{
    public string FunnelName { get; set; } = string.Empty;
    public List<FunnelStepAnalysis> Steps { get; set; } = new();
    public long TotalSessions { get; set; }
    public long CompletedSessions { get; set; }
    public decimal OverallConversionRate { get; set; }
    public List<SegmentBreakdown> ByTier { get; set; } = new();
    public List<SegmentBreakdown> ByDeviceType { get; set; } = new();
}

public record FunnelStepAnalysis
{
    public int StepNumber { get; set; }
    public string StepName { get; set; } = string.Empty;
    public long CompletedCount { get; set; }
    public int UniqueUsers { get; set; }
    public decimal ConversionRate { get; set; }
    public decimal CumulativeConversionRate { get; set; }
    public int AvgTimeToCompleteSeconds { get; set; }
}

public record SegmentBreakdown
{
    public string Segment { get; set; } = string.Empty;
    public decimal ConversionRate { get; set; }
    public long Sessions { get; set; }
}

public record RollupTrendData
{
    public DateTime DateHour { get; set; }
    public long EventCount { get; set; }
    public int UniqueUsers { get; set; }
    public int UniqueSessions { get; set; }
    public long ProTierCount { get; set; }
}
