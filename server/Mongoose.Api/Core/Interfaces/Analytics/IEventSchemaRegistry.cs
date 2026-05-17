using System.Collections.Immutable;

namespace Mongoose.Api.Core.Interfaces;

/// <summary>
/// Event schema definition from registry
/// </summary>
public record EventSchema(
    string Name,
    string Category,
    int Version,
    int RetentionDays,
    bool PiiSensitive,
    IReadOnlySet<string> AllowedPayloadKeys,
    IReadOnlySet<string> RequiredPayloadKeys,
    IReadOnlyDictionary<string, string> PayloadKeyTypes,
    string Description,
    bool Deprecated = false,
    string? Replacement = null
);

/// <summary>
/// Event validation result
/// </summary>
public record EventValidationResult(
    bool IsValid,
    string? RejectionReason = null,
    string? RejectionDetails = null
);

/// <summary>
/// Service for loading and validating events against schema registry
/// </summary>
public interface IEventSchemaRegistry
{
    /// <summary>
    /// Get schema for an event name
    /// </summary>
    /// <returns>EventSchema or null if not found</returns>
    EventSchema? GetSchema(string eventName);

    /// <summary>
    /// Get all registered event schemas
    /// </summary>
    IReadOnlyDictionary<string, EventSchema> GetAllSchemas();

    /// <summary>
    /// Check if event is registered
    /// </summary>
    bool IsRegistered(string eventName);

    /// <summary>
    /// Reload schema registry (for hot updates)
    /// </summary>
    Task ReloadAsync();

    /// <summary>
    /// Get schema version
    /// </summary>
    int GetSchemaVersion();
}

/// <summary>
/// Service for validating analytics events
/// </summary>
public interface IEventValidator
{
    /// <summary>
    /// Validate event against schema
    /// Checks: event name, required fields, payload keys, types, PII denylist
    /// </summary>
    EventValidationResult Validate(
        string eventName,
        Dictionary<string, object>? payload,
        int eventVersion = 1
    );

    /// <summary>
    /// Validate and sanitize payload
    /// Returns filtered payload with only allowed keys
    /// </summary>
    (bool IsValid, Dictionary<string, object>? SanitizedPayload, string? RejectionReason) ValidateAndSanitizePayload(
        string eventName,
        Dictionary<string, object>? payload
    );

    /// <summary>
    /// Check payload for prohibited data (PII denylist)
    /// </summary>
    bool ContainsProhibitedData(string eventName, Dictionary<string, object>? payload);

    /// <summary>
    /// Check payload size
    /// </summary>
    bool IsPayloadSizeValid(string payload);

    /// <summary>
    /// Get required fields for an event
    /// </summary>
    IReadOnlySet<string> GetRequiredFields(string eventName);
}
