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
using Mongoose.Api.Infrastructure.Services.Analytics;

/// <summary>
/// Product analytics exploration endpoint
/// GET /api/v2/analytics/explore/*
/// </summary>
public class AnalyticsExploreEndpoint : IEndpoint
{
    public string Route => "/api/v2/analytics/explore";
    public string Description => "Product analytics exploration queries";

    private readonly IAnalyticsEventDimensionsRepository _dimensionRepository;
    private readonly IAnalyticsJourneyRepository _journeyRepository;

    public AnalyticsExploreEndpoint(
        IAnalyticsEventDimensionsRepository dimensionRepository,
        IAnalyticsJourneyRepository journeyRepository)
    {
        _dimensionRepository = dimensionRepository;
        _journeyRepository = journeyRepository;
    }

    public void Configure(WebApplication app)
    {
        // List unique events with filters
        app.MapGet($"{Route}/events", HandleListEventsAsync)
            .WithName("ExploreEvents")
            .WithOpenApi()
            .Produces<EventListResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        // Get dimension values for exploration
        app.MapGet($"{Route}/dimensions", HandleGetDimensionValuesAsync)
            .WithName("ExploreDimensions")
            .WithOpenApi()
            .Produces<DimensionValuesResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        // Deep dive into single event
        app.MapGet($"{Route}/events/{{eventName}}", HandleGetEventDetailAsync)
            .WithName("ExploreEventDetail")
            .WithOpenApi()
            .Produces<EventDetailResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);
    }

    private async Task<IResult> HandleListEventsAsync(
        [FromQuery] string? timeRange = "last_7d",
        [FromQuery] string? eventName = null,
        [FromQuery] string? eventCategory = null,
        [FromQuery] string? tier = null,
        [FromQuery] string? deviceType = null,
        [FromQuery] string? countryCode = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        // Parse time range
        var (startUtc, endUtc) = ParseTimeRange(timeRange);

        // Query events with filters (would call repository)
        var events = new List<EventSummary>();

        var response = new EventListResponse
        {
            Events = events,
            PageInfo = new PageInfo { Page = page, PageSize = pageSize, Total = events.Count }
        };

        return Results.Ok(response);
    }

    private async Task<IResult> HandleGetDimensionValuesAsync(
        [FromQuery] string dimension,
        [FromQuery] string? timeRange = "last_7d",
        [FromQuery] string? eventName = null,
        [FromQuery] int limit = 50)
    {
        // Validate dimension
        var validDimensions = new[] { "pagePath", "referrer", "deviceType", "browser", "os", "country", "tier" };
        if (!validDimensions.Contains(dimension))
            return Results.BadRequest(new ErrorResponse { Error = $"Invalid dimension: {dimension}" });

        var (startUtc, endUtc) = ParseTimeRange(timeRange);

        // Query dimension values (would call repository)
        var values = await _dimensionRepository.GetDimensionValuesAsync(dimension, startUtc, endUtc, limit);

        var response = new DimensionValuesResponse
        {
            Dimension = dimension,
            Values = values.Select(v => new DimensionValueDto
            {
                Value = v.Value,
                Count = v.Count,
                PercentOfTotal = v.PercentOfTotal,
                UniqueUsers = v.UniqueUsers
            }).ToList()
        };

        return Results.Ok(response);
    }

    private async Task<IResult> HandleGetEventDetailAsync(
        string eventName,
        [FromQuery] string? timeRange = "last_7d")
    {
        var (startUtc, endUtc) = ParseTimeRange(timeRange);

        // Get event detail (would call repository)
        var detail = await _dimensionRepository.GetEventDetailAsync(eventName, startUtc, endUtc);

        if (detail == null)
            return Results.NotFound(new ErrorResponse { Error = $"Event not found: {eventName}" });

        var response = new EventDetailResponse
        {
            EventName = detail.EventName,
            EventCategory = detail.EventCategory,
            TotalCount = detail.TotalCount,
            UniqueUsers = detail.UniqueUsers,
            DimensionBreakdown = new DimensionBreakdownDto
            {
                ByPath = detail.ByPath.Select(d => new DimensionValueDto { Value = d.Value, Count = d.Count, PercentOfTotal = d.PercentOfTotal }).ToList(),
                ByDevice = detail.ByDevice.Select(d => new DimensionValueDto { Value = d.Value, Count = d.Count, PercentOfTotal = d.PercentOfTotal }).ToList(),
                ByBrowser = detail.ByBrowser.Select(d => new DimensionValueDto { Value = d.Value, Count = d.Count, PercentOfTotal = d.PercentOfTotal }).ToList(),
                ByTier = detail.ByTier.Select(d => new DimensionValueDto { Value = d.Value, Count = d.Count, PercentOfTotal = d.PercentOfTotal }).ToList(),
                ByCountry = detail.ByCountry.Select(d => new DimensionValueDto { Value = d.Value, Count = d.Count, PercentOfTotal = d.PercentOfTotal }).ToList()
            }
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

// ============================================================================
// RESPONSE DTOs
// ============================================================================

public record EventListResponse
{
    [JsonPropertyName("events")]
    public List<EventSummary> Events { get; set; } = new();

    [JsonPropertyName("pageInfo")]
    public PageInfo PageInfo { get; set; } = new();
}

public record EventSummary
{
    [JsonPropertyName("eventName")]
    public string EventName { get; set; } = string.Empty;

    [JsonPropertyName("eventCategory")]
    public string EventCategory { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public long Count { get; set; }

    [JsonPropertyName("uniqueUsers")]
    public int UniqueUsers { get; set; }

    [JsonPropertyName("uniqueSessions")]
    public int UniqueSessions { get; set; }

    [JsonPropertyName("lastOccurred")]
    public DateTime LastOccurred { get; set; }

    [JsonPropertyName("topPaths")]
    public List<string> TopPaths { get; set; } = new();

    [JsonPropertyName("topReferrers")]
    public List<string> TopReferrers { get; set; } = new();
}

public record DimensionValuesResponse
{
    [JsonPropertyName("dimension")]
    public string Dimension { get; set; } = string.Empty;

    [JsonPropertyName("values")]
    public List<DimensionValueDto> Values { get; set; } = new();
}

public record DimensionValueDto
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public long Count { get; set; }

    [JsonPropertyName("percentOfTotal")]
    public decimal PercentOfTotal { get; set; }

    [JsonPropertyName("uniqueUsers")]
    public int UniqueUsers { get; set; }
}

public record EventDetailResponse
{
    [JsonPropertyName("eventName")]
    public string EventName { get; set; } = string.Empty;

    [JsonPropertyName("eventCategory")]
    public string EventCategory { get; set; } = string.Empty;

    [JsonPropertyName("totalCount")]
    public long TotalCount { get; set; }

    [JsonPropertyName("uniqueUsers")]
    public int UniqueUsers { get; set; }

    [JsonPropertyName("dimensionBreakdown")]
    public DimensionBreakdownDto DimensionBreakdown { get; set; } = new();

    [JsonPropertyName("customProperties")]
    public Dictionary<string, CustomPropertyInfo> CustomProperties { get; set; } = new();

    [JsonPropertyName("timeSeriesHourly")]
    public List<TimeSeriesPoint> TimeSeriesHourly { get; set; } = new();
}

public record DimensionBreakdownDto
{
    [JsonPropertyName("byPath")]
    public List<DimensionValueDto> ByPath { get; set; } = new();

    [JsonPropertyName("byDevice")]
    public List<DimensionValueDto> ByDevice { get; set; } = new();

    [JsonPropertyName("byBrowser")]
    public List<DimensionValueDto> ByBrowser { get; set; } = new();

    [JsonPropertyName("byTier")]
    public List<DimensionValueDto> ByTier { get; set; } = new();

    [JsonPropertyName("byCountry")]
    public List<DimensionValueDto> ByCountry { get; set; } = new();
}

public record CustomPropertyInfo
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("sampleValues")]
    public List<string> SampleValues { get; set; } = new();

    [JsonPropertyName("cardinality")]
    public int Cardinality { get; set; }
}

public record TimeSeriesPoint
{
    [JsonPropertyName("hour")]
    public DateTime Hour { get; set; }

    [JsonPropertyName("count")]
    public long Count { get; set; }

    [JsonPropertyName("uniqueUsers")]
    public int UniqueUsers { get; set; }
}

public record PageInfo
{
    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("total")]
    public long Total { get; set; }
}

public record ErrorResponse
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
