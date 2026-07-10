namespace Mongoose.Api.Application.Endpoints.Analytics;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Mongoose.Api.Core.Interfaces;

/// <summary>
/// User journey and navigation flow endpoint
/// GET /api/v2/analytics/journey/*
/// </summary>
public class AnalyticsJourneyEndpoint : IEndpoint
{
    public string Route => "/api/v2/analytics/journey";
    public string Description => "User journey and navigation flow analysis";

    private readonly IAnalyticsJourneyRepository _journeyRepository;

    public AnalyticsJourneyEndpoint(IAnalyticsJourneyRepository journeyRepository)
    {
        _journeyRepository = journeyRepository;
    }

    public void Configure(WebApplication app)
    {
        // Get top navigation flows
        app.MapGet($"{Route}/flows", HandleGetFlowsAsync)
            .WithName("JourneyFlows")
            .WithOpenApi()
            .Produces<NavigationFlowsResponse>(StatusCodes.Status200OK);

        // Get user's journey history
        app.MapGet($"{Route}/user/{{userId}}", HandleGetUserJourneyAsync)
            .WithName("UserJourney")
            .WithOpenApi()
            .Produces<UserJourneyResponse>(StatusCodes.Status200OK);

        // Get common path sequences
        app.MapGet($"{Route}/paths", HandleGetPathSequencesAsync)
            .WithName("JourneyPaths")
            .WithOpenApi()
            .Produces<PathSequencesResponse>(StatusCodes.Status200OK);
    }

    private async Task<IResult> HandleGetFlowsAsync(
        [FromQuery] string? timeRange = "last_7d",
        [FromQuery] int minTransitions = 5,
        [FromQuery] string? tier = null,
        [FromQuery] int limit = 50)
    {
        var (startUtc, endUtc) = ParseTimeRange(timeRange);

        // Query top flows (would call repository)
        var flows = await _journeyRepository.GetTopFlowsAsync(startUtc, endUtc, minTransitions, limit);

        var response = new NavigationFlowsResponse
        {
            Flows = flows.Select(f => new NavigationFlowDto
            {
                SourcePage = f.SourcePage,
                DestinationPage = f.DestinationPage,
                TransitionCount = f.TransitionCount,
                UniqueUsers = f.UniqueUsers,
                AvgTimeOnSourcePageSeconds = f.AvgTimeOnSourcePageSeconds,
                ConversionRate = f.ConversionRate
            }).ToList()
        };

        return Results.Ok(response);
    }

    private async Task<IResult> HandleGetUserJourneyAsync(
        long userId,
        [FromQuery] string? sessionId = null,
        [FromQuery] string? timeRange = "last_7d")
    {
        var (startUtc, endUtc) = ParseTimeRange(timeRange);

        // Query user journeys (would call repository)
        var journeys = await _journeyRepository.GetUserJourneysAsync(userId, startUtc, endUtc);

        var groupedBySessions = journeys.GroupBy(j => j.SessionId);

        var response = new UserJourneyResponse
        {
            UserId = userId,
            Sessions = groupedBySessions.Select(sg => new SessionJourneyDto
            {
                SessionId = sg.Key,
                StartTime = sg.Min(s => s.TransitionTimestampUtc),
                EndTime = sg.Max(s => s.TransitionTimestampUtc),
                Steps = sg.OrderBy(s => s.StepNumber).Select(s => new JourneyStepDto
                {
                    StepNumber = s.StepNumber,
                    Page = s.DestinationPage,
                    EventName = s.EventName,
                    Timestamp = s.TransitionTimestampUtc,
                    TimeOnPageSeconds = s.TimeOnPreviousPageSeconds ?? 0,
                    DeviceType = s.DeviceType
                }).ToList()
            }).ToList()
        };

        return Results.Ok(response);
    }

    private async Task<IResult> HandleGetPathSequencesAsync(
        [FromQuery] string startEvent,
        [FromQuery] string? timeRange = "last_7d",
        [FromQuery] int maxSteps = 5,
        [FromQuery] int limit = 100)
    {
        var (startUtc, endUtc) = ParseTimeRange(timeRange);

        // Query path sequences (would call repository)
        var paths = await _journeyRepository.GetPathSequencesAsync(startEvent, startUtc, endUtc, maxSteps, limit);

        var response = new PathSequencesResponse
        {
            StartEvent = startEvent,
            Paths = paths.Select(p => new PathSequenceDto
            {
                Steps = p.Steps,
                Count = p.Count,
                UniqueUsers = p.UniqueUsers,
                ConversionRate = p.ConversionRate
            }).ToList()
        };

        return Results.Ok(response);
    }

    private (DateTime Start, DateTime End) ParseTimeRange(string? timeRange)
    {
        var now = DateTime.UtcNow;
        return timeRange switch
        {
            "last_7d" => (now.AddDays(-7), now),
            "last_30d" => (now.AddDays(-30), now),
            "last_90d" => (now.AddDays(-90), now),
            _ => (now.AddDays(-7), now)
        };
    }
}

/// <summary>
/// Funnel conversion analysis endpoint
/// GET /api/v2/analytics/funnels/*
/// </summary>
public class AnalyticsFunnelEndpoint : IEndpoint
{
    public string Route => "/api/v2/analytics/funnels";
    public string Description => "Funnel conversion analysis";

    private readonly IAnalyticsFunnelRepository _funnelRepository;

    public AnalyticsFunnelEndpoint(IAnalyticsFunnelRepository funnelRepository)
    {
        _funnelRepository = funnelRepository;
    }

    public void Configure(WebApplication app)
    {
        // List all funnels
        app.MapGet(Route, HandleListFunnelsAsync)
            .WithName("ListFunnels")
            .WithOpenApi()
            .Produces<ListFunnelsResponse>(StatusCodes.Status200OK);

        // Analyze single funnel
        app.MapGet($"{Route}/{{funnelId}}", HandleGetFunnelAnalysisAsync)
            .WithName("FunnelAnalysis")
            .WithOpenApi()
            .Produces<FunnelAnalysisResponse>(StatusCodes.Status200OK);
    }

    private async Task<IResult> HandleListFunnelsAsync()
    {
        // Query all funnel definitions (would call repository)
        var definitions = await _funnelRepository.GetAllFunnelDefinitionsAsync();

        var response = new ListFunnelsResponse
        {
            Funnels = definitions.Select(d => new FunnelDefinitionDto
            {
                FunnelId = d.FunnelName,
                FunnelName = d.DisplayName,
                Description = d.Description,
                Enabled = d.Enabled,
                Steps = ParseFunnelSteps(d.Steps)
            }).ToList()
        };

        return Results.Ok(response);
    }

    private async Task<IResult> HandleGetFunnelAnalysisAsync(
        string funnelId,
        [FromQuery] string? timeRange = "last_7d",
        [FromQuery] string? tier = null,
        [FromQuery] string? deviceType = null)
    {
        var (startUtc, endUtc) = ParseTimeRange(timeRange);

        // Analyze funnel (would call repository)
        var analysis = await _funnelRepository.AnalyzeFunnelAsync(funnelId, startUtc, endUtc, tier);

        if (analysis == null)
            return Results.NotFound();

        var response = new FunnelAnalysisResponse
        {
            FunnelId = funnelId,
            FunnelName = funnelId,
            Steps = analysis.Steps.Select(s => new FunnelStepAnalysisDto
            {
                StepNumber = s.StepNumber,
                StepName = s.StepName,
                CompletedCount = s.CompletedCount,
                UniqueUsers = s.UniqueUsers,
                ConversionRate = s.ConversionRate,
                CumulativeConversionRate = s.CumulativeConversionRate,
                AvgTimeToCompleteSeconds = s.AvgTimeToCompleteSeconds
            }).ToList(),
            Summary = new FunnelSummaryDto
            {
                TotalSessions = analysis.TotalSessions,
                CompletedSessions = analysis.CompletedSessions,
                OverallConversionRate = analysis.OverallConversionRate
            },
            SegmentBreakdown = analysis.ByTier.Select(s => new SegmentBreakdownDto
            {
                Segment = s.Segment,
                ConversionRate = s.ConversionRate,
                Sessions = s.Sessions
            }).ToList()
        };

        return Results.Ok(response);
    }

    private List<FunnelStepDefinitionDto> ParseFunnelSteps(string stepsJson)
    {
        // Would parse JSON and return list
        return new List<FunnelStepDefinitionDto>();
    }

    private (DateTime Start, DateTime End) ParseTimeRange(string? timeRange)
    {
        var now = DateTime.UtcNow;
        return timeRange switch
        {
            "last_7d" => (now.AddDays(-7), now),
            "last_30d" => (now.AddDays(-30), now),
            "last_90d" => (now.AddDays(-90), now),
            _ => (now.AddDays(-7), now)
        };
    }
}

// ============================================================================
// RESPONSE DTOs - JOURNEY
// ============================================================================

public record NavigationFlowsResponse
{
    [JsonPropertyName("flows")]
    public List<NavigationFlowDto> Flows { get; set; } = new();
}

public record NavigationFlowDto
{
    [JsonPropertyName("sourcePage")]
    public string SourcePage { get; set; } = string.Empty;

    [JsonPropertyName("destinationPage")]
    public string DestinationPage { get; set; } = string.Empty;

    [JsonPropertyName("transitionCount")]
    public long TransitionCount { get; set; }

    [JsonPropertyName("uniqueUsers")]
    public int UniqueUsers { get; set; }

    [JsonPropertyName("avgTimeOnSourcePageSeconds")]
    public int AvgTimeOnSourcePageSeconds { get; set; }

    [JsonPropertyName("conversionRate")]
    public decimal ConversionRate { get; set; }
}

public record UserJourneyResponse
{
    [JsonPropertyName("userId")]
    public long UserId { get; set; }

    [JsonPropertyName("sessions")]
    public List<SessionJourneyDto> Sessions { get; set; } = new();
}

public record SessionJourneyDto
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("startTime")]
    public DateTime StartTime { get; set; }

    [JsonPropertyName("endTime")]
    public DateTime EndTime { get; set; }

    [JsonPropertyName("steps")]
    public List<JourneyStepDto> Steps { get; set; } = new();
}

public record JourneyStepDto
{
    [JsonPropertyName("stepNumber")]
    public int StepNumber { get; set; }

    [JsonPropertyName("page")]
    public string Page { get; set; } = string.Empty;

    [JsonPropertyName("eventName")]
    public string? EventName { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("timeOnPageSeconds")]
    public int TimeOnPageSeconds { get; set; }

    [JsonPropertyName("deviceType")]
    public string? DeviceType { get; set; }
}

public record PathSequencesResponse
{
    [JsonPropertyName("startEvent")]
    public string StartEvent { get; set; } = string.Empty;

    [JsonPropertyName("paths")]
    public List<PathSequenceDto> Paths { get; set; } = new();
}

public record PathSequenceDto
{
    [JsonPropertyName("steps")]
    public List<string> Steps { get; set; } = new();

    [JsonPropertyName("count")]
    public long Count { get; set; }

    [JsonPropertyName("uniqueUsers")]
    public int UniqueUsers { get; set; }

    [JsonPropertyName("conversionRate")]
    public decimal ConversionRate { get; set; }
}

// ============================================================================
// RESPONSE DTOs - FUNNEL
// ============================================================================

public record ListFunnelsResponse
{
    [JsonPropertyName("funnels")]
    public List<FunnelDefinitionDto> Funnels { get; set; } = new();
}

public record FunnelDefinitionDto
{
    [JsonPropertyName("funnelId")]
    public string FunnelId { get; set; } = string.Empty;

    [JsonPropertyName("funnelName")]
    public string FunnelName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("steps")]
    public List<FunnelStepDefinitionDto> Steps { get; set; } = new();
}

public record FunnelStepDefinitionDto
{
    [JsonPropertyName("stepNumber")]
    public int StepNumber { get; set; }

    [JsonPropertyName("stepName")]
    public string StepName { get; set; } = string.Empty;

    [JsonPropertyName("eventName")]
    public string EventName { get; set; } = string.Empty;
}

public record FunnelAnalysisResponse
{
    [JsonPropertyName("funnelId")]
    public string FunnelId { get; set; } = string.Empty;

    [JsonPropertyName("funnelName")]
    public string FunnelName { get; set; } = string.Empty;

    [JsonPropertyName("steps")]
    public List<FunnelStepAnalysisDto> Steps { get; set; } = new();

    [JsonPropertyName("summary")]
    public FunnelSummaryDto Summary { get; set; } = new();

    [JsonPropertyName("segmentBreakdown")]
    public List<SegmentBreakdownDto> SegmentBreakdown { get; set; } = new();
}

public record FunnelStepAnalysisDto
{
    [JsonPropertyName("stepNumber")]
    public int StepNumber { get; set; }

    [JsonPropertyName("stepName")]
    public string StepName { get; set; } = string.Empty;

    [JsonPropertyName("completedCount")]
    public long CompletedCount { get; set; }

    [JsonPropertyName("uniqueUsers")]
    public int UniqueUsers { get; set; }

    [JsonPropertyName("conversionRate")]
    public decimal ConversionRate { get; set; }

    [JsonPropertyName("cumulativeConversionRate")]
    public decimal CumulativeConversionRate { get; set; }

    [JsonPropertyName("avgTimeToCompleteSeconds")]
    public int AvgTimeToCompleteSeconds { get; set; }
}

public record FunnelSummaryDto
{
    [JsonPropertyName("totalSessions")]
    public long TotalSessions { get; set; }

    [JsonPropertyName("completedSessions")]
    public long CompletedSessions { get; set; }

    [JsonPropertyName("overallConversionRate")]
    public decimal OverallConversionRate { get; set; }
}

public record SegmentBreakdownDto
{
    [JsonPropertyName("segment")]
    public string Segment { get; set; } = string.Empty;

    [JsonPropertyName("conversionRate")]
    public decimal ConversionRate { get; set; }

    [JsonPropertyName("sessions")]
    public long Sessions { get; set; }
}
