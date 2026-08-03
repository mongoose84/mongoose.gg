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
/// Real-time analytics endpoint
/// GET /api/v2/analytics/realtime/*
/// </summary>
public class AnalyticsRealtimeEndpoint : IEndpoint
{
    public string Route { get; }
    public string Description => "Real-time event stream and metrics";

    public AnalyticsRealtimeEndpoint(string basePath)
    {
        Route = basePath + "/analytics/realtime";
    }

    public void Configure(WebApplication app)
    {
        // Live event feed
        app.MapGet($"{Route}/events", HandleLiveEventsAsync)
            .WithName("RealtimeEvents")
            .Produces<RealtimeEventsResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        // Real-time metrics
        app.MapGet($"{Route}/stats", HandleRealtimeStatsAsync)
            .WithName("RealtimeStats")
            .Produces<RealtimeStatsResponse>(StatusCodes.Status200OK);

        // WebSocket for live streaming (optional, for future enhancement)
        // app.MapGet($"{Route}/stream", HandleWebSocketStreamAsync)
        //     .WithName("RealtimeStream")
        //     .WithOpenApi();
    }

    private Task<IResult> HandleLiveEventsAsync(
        [FromQuery] string? eventName = null,
        [FromQuery] int limit = 50,
        [FromQuery] int seconds = 60)
    {
        if (limit > 500)
            return Task.FromResult<IResult>(Results.BadRequest(new ErrorResponse { Error = "Limit cannot exceed 500" }));

        if (seconds < 1 || seconds > 3600)
            return Task.FromResult<IResult>(Results.BadRequest(new ErrorResponse { Error = "Seconds must be between 1 and 3600" }));

        var endTime = DateTime.UtcNow;
        var startTime = endTime.AddSeconds(-seconds);

        // Query recent events (would call repository with time filter)
        // For real implementation, would use specialized query on analytics_event_dimensions
        // filtered by event_timestamp_utc >= startTime

        var recentEvents = new List<RealtimeEventDto>();
        // Would populate from repository query

        var response = new RealtimeEventsResponse
        {
            Events = recentEvents,
            GeneratedAt = DateTime.UtcNow,
            EventCount = recentEvents.Count,
            TimeWindowSeconds = seconds
        };

        return Task.FromResult<IResult>(Results.Ok(response));
    }

    private Task<IResult> HandleRealtimeStatsAsync()
    {
        var now = DateTime.UtcNow;
        var lastMinute = now.AddSeconds(-60);
        var lastHour = now.AddHours(-1);

        // Query event counts for different time windows
        // These would use specialized fast queries or cache

        var response = new RealtimeStatsResponse
        {
            GeneratedAt = now,
            LastMinute = new WindowStats
            {
                EventCount = 0, // Would query
                UniqueUsers = 0,
                UniqueSessions = 0,
                EventsPerSecond = 0
            },
            LastHour = new WindowStats
            {
                EventCount = 0, // Would query
                UniqueUsers = 0
            },
            TopEvents = new List<EventCountDto>
            {
                // Would populate from TOP query
            },
            TopPages = new List<PageCountDto>
            {
                // Would populate from TOP query
            }
        };

        return Task.FromResult<IResult>(Results.Ok(response));
    }

    // Future enhancement: WebSocket streaming
    // private async Task HandleWebSocketStreamAsync(HttpContext context)
    // {
    //     if (!context.WebSockets.IsWebSocketRequest)
    //     {
    //         context.Response.StatusCode = 400;
    //         return;
    //     }

    //     var webSocket = await context.WebSockets.AcceptWebSocketAsync();
    //     var buffer = new byte[1024 * 4];

    //     try
    //     {
    //         while (!webSocket.CloseStatus.HasValue)
    //         {
    //             var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

    //             if (result.MessageType == WebSocketMessageType.Text)
    //             {
    //                 // Stream live events
    //                 var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
    //                 // Process subscribe request and stream events
    //             }
    //         }
    //     }
    //     finally
    //     {
    //         webSocket?.Dispose();
    //     }
    // }
}

// ============================================================================
// RESPONSE DTOs
// ============================================================================

public record RealtimeEventsResponse
{
    [JsonPropertyName("events")]
    public List<RealtimeEventDto> Events { get; set; } = new();

    [JsonPropertyName("eventCount")]
    public int EventCount { get; set; }

    [JsonPropertyName("timeWindowSeconds")]
    public int TimeWindowSeconds { get; set; }

    [JsonPropertyName("generatedAt")]
    public DateTime GeneratedAt { get; set; }
}

public record RealtimeEventDto
{
    [JsonPropertyName("eventId")]
    public string EventId { get; set; } = string.Empty;

    [JsonPropertyName("eventName")]
    public string EventName { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public long? UserId { get; set; }

    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("pagePath")]
    public string PagePath { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("deviceType")]
    public string DeviceType { get; set; } = string.Empty;

    [JsonPropertyName("tier")]
    public string Tier { get; set; } = string.Empty;

    [JsonPropertyName("country")]
    public string? Country { get; set; }
}

public record RealtimeStatsResponse
{
    [JsonPropertyName("generatedAt")]
    public DateTime GeneratedAt { get; set; }

    [JsonPropertyName("lastMinute")]
    public WindowStats LastMinute { get; set; } = new();

    [JsonPropertyName("lastHour")]
    public WindowStats LastHour { get; set; } = new();

    [JsonPropertyName("topEvents")]
    public List<EventCountDto> TopEvents { get; set; } = new();

    [JsonPropertyName("topPages")]
    public List<PageCountDto> TopPages { get; set; } = new();
}

public record WindowStats
{
    [JsonPropertyName("eventCount")]
    public long EventCount { get; set; }

    [JsonPropertyName("uniqueUsers")]
    public int UniqueUsers { get; set; }

    [JsonPropertyName("uniqueSessions")]
    public int UniqueSessions { get; set; }

    [JsonPropertyName("eventsPerSecond")]
    public double EventsPerSecond { get; set; }
}

public record EventCountDto
{
    [JsonPropertyName("eventName")]
    public string EventName { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public long Count { get; set; }
}

public record PageCountDto
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public long Count { get; set; }
}
