namespace Mongoose.Api.Core.Entities;

/// <summary>
/// V2 Analytics Event - Versioned schema with strict validation
/// Replaces legacy AnalyticsEvent; maintains compatibility during migration
/// </summary>
public class AnalyticsEventV2 : EntityBase
{
    /// <summary>
    /// Unique event ID (UUID); optional client-provided for idempotency
    /// </summary>
    public string? EventId { get; set; }

    /// <summary>
    /// User who triggered the event (NULL for anonymous)
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// User tier at time of event (free/pro); server-set
    /// </summary>
    public string Tier { get; set; } = "free";

    /// <summary>
    /// Session ID for grouping related events (max 64 chars)
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Event name from schema registry (max 100 chars)
    /// </summary>
    public string EventName { get; set; } = string.Empty;

    /// <summary>
    /// Event category (system, navigation, auth, feature, engagement, premium)
    /// Denormalized for faster filtering and retention purges
    /// </summary>
    public string EventCategory { get; set; } = "system";

    /// <summary>
    /// Schema version of this event (e.g., 1, 2)
    /// </summary>
    public int EventVersion { get; set; } = 1;

    /// <summary>
    /// JSON payload with event-specific data (max 4KB)
    /// </summary>
    public string? PayloadJson { get; set; }

    /// <summary>
    /// Rejection reason if event validation failed (NULL if accepted)
    /// Enum: MissingEventName, EventNameTooLong, EventNotInRegistry, PayloadTooLarge, etc.
    /// </summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Size of serialized payload in bytes (for monitoring)
    /// </summary>
    public int? PayloadSizeBytes { get; set; }

    /// <summary>
    /// Optional client version from metadata
    /// </summary>
    public string? ClientVersion { get; set; }

    /// <summary>
    /// Optional hash of user agent (for analytics, not personally identifiable)
    /// </summary>
    public string? UserAgentHash { get; set; }

    /// <summary>
    /// Optional anonymized IP (e.g., last octet masked or geohash)
    /// </summary>
    public string? IpAnonymized { get; set; }

    /// <summary>
    /// Client-provided timestamp (UTC); used for clock skew detection
    /// </summary>
    public DateTime? ClientTimestampUtc { get; set; }

    /// <summary>
    /// Server timestamp (UTC); used for retention policy
    /// </summary>
    public DateTime ServerTimestampUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Insertion time (UTC); denormalized for sorting
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Check if event was accepted (no rejection)
    /// </summary>
    public bool IsAccepted => string.IsNullOrEmpty(RejectionReason);

    /// <summary>
    /// Determine retention date based on event category
    /// </summary>
    public DateTime GetRetentionDate(int retentionDays)
    {
        return ServerTimestampUtc.AddDays(retentionDays);
    }
}
