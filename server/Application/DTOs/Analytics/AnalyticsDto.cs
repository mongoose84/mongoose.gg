using System.Text.Json.Serialization;

namespace RiotProxy.Application.DTOs;

public static class AnalyticsDto
{
    /// <summary>
    /// Request to record an analytics event
    /// </summary>
    public record TrackEventRequest(
        [property: JsonPropertyName("eventName")] string EventName,
        [property: JsonPropertyName("payload")] Dictionary<string, object>? Payload = null,
        [property: JsonPropertyName("sessionId")] string? SessionId = null
    );

    /// <summary>
    /// Response after successfully recording an event
    /// </summary>
    public record TrackEventResponse(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("eventId")] long? EventId = null
    );

    /// <summary>
    /// Batch request to record multiple analytics events
    /// </summary>
    public record TrackBatchRequest(
        [property: JsonPropertyName("events")] TrackEventRequest[] Events
    );

    /// <summary>
    /// Response after recording batch events
    /// </summary>
    public record TrackBatchResponse(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("count")] int Count
    );
}

