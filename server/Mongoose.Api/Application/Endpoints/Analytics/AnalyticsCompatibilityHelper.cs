using Mongoose.Api.Application.DTOs;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;
using static Mongoose.Api.Application.DTOs.AnalyticsV2Dto;

namespace Mongoose.Api.Application.Endpoints.Analytics;

/// <summary>
/// Compatibility helper for v1 → v2 migration
/// Transforms legacy events to v2 contract
/// </summary>
public static class AnalyticsCompatibilityHelper
{
    /// <summary>
    /// Transform legacy v1 event to v2
    /// Handles mapping of old payload format to new schema
    /// </summary>
    public static TrackEventV2Request TransformV1ToV2(AnalyticsDto.TrackEventRequest v1Request, string? sessionId = null)
    {
        var eventName = v1Request.EventName;

        // Transform legacy event names to new registry format if needed
        // (This allows old client code to continue working)
        var transformedName = TransformEventName(eventName);

        return new TrackEventV2Request(
            EventName: transformedName,
            EventVersion: 1, // Legacy events are schema v1
            Timestamp: null,  // Let server set
            ClientTimestamp: DateTime.UtcNow,
            SessionId: sessionId ?? v1Request.SessionId,
            Payload: v1Request.Payload,
            Metadata: null
        );
    }

    /// <summary>
    /// Transform v2 request to AnalyticsEventV2 entity for storage
    /// </summary>
    public static AnalyticsEventV2 TransformV2RequestToEntity(
        TrackEventV2Request request,
        long? userId,
        string tier,
        IEventValidator validator,
        IEventSchemaRegistry schemaRegistry)
    {
        var eventName = request.EventName;
        var schema = schemaRegistry.GetSchema(eventName);

        // Validate and sanitize payload
        var (isValid, sanitizedPayload, rejectionReason) = validator.ValidateAndSanitizePayload(eventName, request.Payload);

        string? payloadJson = null;
        int? payloadSizeBytes = null;

        if (isValid && sanitizedPayload is not null)
        {
            payloadJson = System.Text.Json.JsonSerializer.Serialize(sanitizedPayload,
                new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
            payloadSizeBytes = payloadJson.Length;
        }
        else if (!isValid && rejectionReason is not null)
        {
            // Even rejected events are stored for observability
            if (request.Payload is not null)
            {
                payloadJson = System.Text.Json.JsonSerializer.Serialize(request.Payload,
                    new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
                payloadSizeBytes = payloadJson.Length;
            }
        }

        var timestamp = request.Timestamp ?? DateTime.UtcNow;
        var eventCategory = schema?.Category ?? "system";

        return new AnalyticsEventV2
        {
            EventId = request.Metadata?.ClientVersion != null ? GenerateEventId() : null,
            UserId = userId,
            Tier = tier,
            SessionId = request.SessionId?.Length <= 64 ? request.SessionId : request.SessionId?[..64],
            EventName = eventName,
            EventCategory = eventCategory,
            EventVersion = request.EventVersion,
            PayloadJson = payloadJson,
            RejectionReason = rejectionReason,
            PayloadSizeBytes = payloadSizeBytes,
            ClientVersion = request.Metadata?.ClientVersion,
            UserAgentHash = request.Metadata?.UserAgentHash,
            IpAnonymized = request.Metadata?.IpAnonymized,
            ClientTimestampUtc = request.ClientTimestamp,
            ServerTimestampUtc = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Transform legacy v1 event to v2 entity
    /// </summary>
    public static AnalyticsEventV2 TransformV1RequestToEntity(
        AnalyticsDto.TrackEventRequest v1Request,
        string? sessionId,
        long? userId,
        string tier,
        IEventValidator validator,
        IEventSchemaRegistry schemaRegistry)
    {
        var v2Request = TransformV1ToV2(v1Request, sessionId);
        return TransformV2RequestToEntity(v2Request, userId, tier, validator, schemaRegistry);
    }

    /// <summary>
    /// Check if event name needs migration (v1 → v2 format)
    /// e.g., "match:select" stays the same, but "oldEventName" might map to new format
    /// </summary>
    public static string TransformEventName(string legacyName)
    {
        // Legacy events already use colon notation, so no transformation needed
        // This is where we'd handle any breaking API changes in the future
        return legacyName;
    }

    /// <summary>
    /// Generate UUID for event idempotency
    /// </summary>
    private static string GenerateEventId()
    {
        return Guid.NewGuid().ToString("D");
    }

    /// <summary>
    /// Create v2 response from entity
    /// </summary>
    public static TrackEventV2Response CreateResponseFromEntity(AnalyticsEventV2 entity)
    {
        return new TrackEventV2Response(
            Success: entity.IsAccepted,
            EventId: entity.EventId,
            RejectionReason: entity.RejectionReason,
            Message: entity.IsAccepted ? "Event recorded" : $"Event rejected: {entity.RejectionReason}"
        );
    }

    /// <summary>
    /// Create batch response from entities
    /// </summary>
    public static TrackBatchV2Response CreateBatchResponseFromEntities(IReadOnlyList<AnalyticsEventV2> entities)
    {
        var accepted = entities.Count(e => e.IsAccepted);
        var rejected = entities.Count(e => !e.IsAccepted);

        var rejections = entities
            .Where(e => !e.IsAccepted)
            .Select((e, idx) => new BatchEventRejection(idx, e.EventName, e.RejectionReason ?? "Unknown"))
            .ToArray();

        return new TrackBatchV2Response(
            Success: accepted > 0,
            Accepted: accepted,
            Rejected: rejected,
            Rejections: rejections.Length > 0 ? rejections : null
        );
    }
}
