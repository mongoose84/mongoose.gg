using System.Text.Json.Serialization;

namespace Mongoose.Api.Application.DTOs;

/// <summary>
/// V2 Analytics Event Contract - Versioned event ingestion with explicit common fields
/// Supports strict validation, rejection tracking, and compatibility transform from v1 payloads
/// </summary>
public static class AnalyticsV2Dto
{
    /// <summary>
    /// V2 Event - Ingestion request with versioned schema and explicit common fields
    /// </summary>
    public record TrackEventV2Request(
        [property: JsonPropertyName("eventName")] string EventName,
        [property: JsonPropertyName("eventVersion")] int EventVersion = 1,
        [property: JsonPropertyName("timestamp")] DateTime? Timestamp = null,
        [property: JsonPropertyName("clientTimestamp")] DateTime? ClientTimestamp = null,
        [property: JsonPropertyName("sessionId")] string? SessionId = null,
        [property: JsonPropertyName("payload")] Dictionary<string, object>? Payload = null,
        [property: JsonPropertyName("metadata")] EventMetadata? Metadata = null
    );

    /// <summary>
    /// V2 Batch Request - Multiple events with optional common fields
    /// </summary>
    public record TrackBatchV2Request(
        [property: JsonPropertyName("events")] List<TrackEventV2Request> Events,
        [property: JsonPropertyName("sessionId")] string? SessionId = null
    );

    /// <summary>
    /// Event Metadata - Optional tracking data for analytics
    /// </summary>
    public record EventMetadata(
        [property: JsonPropertyName("clientVersion")] string? ClientVersion = null,
        [property: JsonPropertyName("userAgentHash")] string? UserAgentHash = null,
        [property: JsonPropertyName("ipAnonymized")] string? IpAnonymized = null
    );

    /// <summary>
    /// V2 Response - Enhanced response with acceptance status and rejection reasons
    /// </summary>
    public record TrackEventV2Response(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("eventId")] string? EventId = null,
        [property: JsonPropertyName("rejectionReason")] string? RejectionReason = null,
        [property: JsonPropertyName("message")] string? Message = null
    );

    /// <summary>
    /// V2 Batch Response - Detailed batch result with per-event status
    /// </summary>
    public record TrackBatchV2Response(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("accepted")] int Accepted,
        [property: JsonPropertyName("rejected")] int Rejected,
        [property: JsonPropertyName("rejections")] BatchEventRejection[]? Rejections = null
    );

    /// <summary>
    /// Per-event rejection detail for batch responses
    /// </summary>
    public record BatchEventRejection(
        [property: JsonPropertyName("index")] int Index,
        [property: JsonPropertyName("eventName")] string EventName,
        [property: JsonPropertyName("reason")] string Reason
    );

    /// <summary>
    /// Rejection reason enumeration - Standardized codes for observability
    /// </summary>
    public enum RejectionReason
    {
        /// <summary>Event name missing or empty</summary>
        MissingEventName = 1,

        /// <summary>Event name exceeds 100 character limit</summary>
        EventNameTooLong = 2,

        /// <summary>Event name not in schema registry</summary>
        EventNotInRegistry = 3,

        /// <summary>Payload serialized size exceeds 4KB</summary>
        PayloadTooLarge = 4,

        /// <summary>Payload contains prohibited/sensitive data (PII)</summary>
        ProhibitedDataDetected = 5,

        /// <summary>Required payload field missing</summary>
        RequiredPayloadFieldMissing = 6,

        /// <summary>Payload key not in allowlist</summary>
        UnknownPayloadKey = 7,

        /// <summary>Payload field type mismatch (e.g., string instead of int)</summary>
        PayloadFieldTypeMismatch = 8,

        /// <summary>SessionId exceeds 64 character limit</summary>
        InvalidSessionId = 9,

        /// <summary>Payload JSON parsing error</summary>
        InvalidPayloadJson = 10,

        /// <summary>Event version not supported</summary>
        UnsupportedEventVersion = 11,

        /// <summary>Database or persistence error</summary>
        DatabaseError = 12,

        /// <summary>Generic validation error</summary>
        ValidationError = 99
    }

    /// <summary>
    /// Health Endpoint Response - Pipeline observability metrics
    /// </summary>
    public record AnalyticsHealthResponse(
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("acceptanceRate")] double AcceptanceRate,
        [property: JsonPropertyName("totalEvents")] long TotalEvents,
        [property: JsonPropertyName("acceptedEvents")] long AcceptedEvents,
        [property: JsonPropertyName("rejectedEvents")] long RejectedEvents,
        [property: JsonPropertyName("rejectionBreakdown")] Dictionary<string, long> RejectionBreakdown,
        [property: JsonPropertyName("latencyMs")] LatencyMetrics Latency,
        [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
        [property: JsonPropertyName("timestamp")] DateTime Timestamp
    );

    /// <summary>
    /// Latency Metrics - P50, P95, P99 percentiles
    /// </summary>
    public record LatencyMetrics(
        [property: JsonPropertyName("p50")] double P50,
        [property: JsonPropertyName("p95")] double P95,
        [property: JsonPropertyName("p99")] double P99,
        [property: JsonPropertyName("max")] double Max
    );

    /// <summary>
    /// Schema Information - Metadata about allowed events and properties
    /// </summary>
    public record EventSchemaInfo(
        [property: JsonPropertyName("eventName")] string EventName,
        [property: JsonPropertyName("version")] int Version,
        [property: JsonPropertyName("category")] string Category,
        [property: JsonPropertyName("requiredFields")] string[] RequiredFields,
        [property: JsonPropertyName("allowedPayloadKeys")] string[] AllowedPayloadKeys,
        [property: JsonPropertyName("retentionDays")] int RetentionDays,
        [property: JsonPropertyName("description")] string Description
    );

    /// <summary>
    /// Get Schema Endpoint Response - List all registered event schemas
    /// </summary>
    public record GetSchemasResponse(
        [property: JsonPropertyName("schemas")] EventSchemaInfo[] Schemas,
        [property: JsonPropertyName("count")] int Count,
        [property: JsonPropertyName("schemaVersion")] int SchemaVersion
    );
}
