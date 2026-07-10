using System.Text.Json;
using System.Text.RegularExpressions;
using Mongoose.Api.Core.Interfaces;

namespace Mongoose.Api.Infrastructure.Telemetry;

/// <summary>
/// Event validator - Strict validation with PII detection and rejection tracking
/// </summary>
public class EventValidator : IEventValidator
{
    private readonly IEventSchemaRegistry _schemaRegistry;
    private readonly ILogger<EventValidator> _logger;
    private const int MaxPayloadSizeBytes = 4096;

    // PII Denylist patterns
    private static readonly Regex EmailPattern = new(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled);
    private static readonly Regex PhonePattern = new(@"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b", RegexOptions.Compiled);
    private static readonly Regex CredentialPattern = new(@"(password|token|secret|api_key|apikey|auth|credential)[\s]*[:=]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex CreditCardPattern = new(@"\b(?:\d{4}[-\s]?){3}\d{4}\b", RegexOptions.Compiled);

    public EventValidator(IEventSchemaRegistry schemaRegistry, ILogger<EventValidator> logger)
    {
        _schemaRegistry = schemaRegistry;
        _logger = logger;
    }

    public EventValidationResult Validate(string eventName, Dictionary<string, object>? payload, int eventVersion = 1)
    {
        // Validate event name
        if (string.IsNullOrWhiteSpace(eventName))
            return new EventValidationResult(false, "MissingEventName", "Event name is required");

        if (eventName.Length > 100)
            return new EventValidationResult(false, "EventNameTooLong", $"Event name exceeds 100 characters: {eventName.Length}");

        // Check schema registry
        var schema = _schemaRegistry.GetSchema(eventName);
        if (schema is null)
            return new EventValidationResult(false, "EventNotInRegistry", $"Event '{eventName}' not in registry");

        // Validate event version
        if (eventVersion != schema.Version)
            return new EventValidationResult(false, "UnsupportedEventVersion", $"Event version {eventVersion} not supported (expected {schema.Version})");

        // Validate payload
        if (payload is not null && payload.Count > 0)
        {
            // Check for prohibited data
            if (ContainsProhibitedData(eventName, payload))
                return new EventValidationResult(false, "ProhibitedDataDetected", "Payload contains prohibited data");

            // Validate required fields
            var missingRequired = schema.RequiredPayloadKeys.Where(k => !payload.ContainsKey(k)).ToList();
            if (missingRequired.Any())
            {
                var details = $"Missing required fields: {string.Join(", ", missingRequired)}";
                return new EventValidationResult(false, "RequiredPayloadFieldMissing", details);
            }

            // Validate field types
            foreach (var (key, value) in payload)
            {
                if (schema.PayloadKeyTypes.TryGetValue(key, out var expectedType))
                {
                    if (!IsValidType(value, expectedType))
                    {
                        var details = $"Field '{key}' has wrong type (expected {expectedType}, got {value?.GetType().Name ?? "null"})";
                        return new EventValidationResult(false, "PayloadFieldTypeMismatch", details);
                    }
                }
            }

            // Check payload size
            var json = JsonSerializer.Serialize(payload);
            if (json.Length > MaxPayloadSizeBytes)
            {
                var details = $"Payload exceeds {MaxPayloadSizeBytes} bytes: {json.Length} bytes";
                return new EventValidationResult(false, "PayloadTooLarge", details);
            }
        }

        return new EventValidationResult(true);
    }

    public (bool IsValid, Dictionary<string, object>? SanitizedPayload, string? RejectionReason) ValidateAndSanitizePayload(
        string eventName,
        Dictionary<string, object>? payload)
    {
        if (payload is null || payload.Count == 0)
            return (true, null, null);

        var schema = _schemaRegistry.GetSchema(eventName);
        if (schema is null)
            return (false, null, "EventNotInRegistry");

        // Check for prohibited data
        if (ContainsProhibitedData(eventName, payload))
            return (false, null, "ProhibitedDataDetected");

        // Filter to allowed keys only
        var sanitized = payload
            .Where(kv => schema.AllowedPayloadKeys.Contains(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        // Validate required fields
        var missingRequired = schema.RequiredPayloadKeys.Where(k => !sanitized.ContainsKey(k)).ToList();
        if (missingRequired.Any())
            return (false, null, "RequiredPayloadFieldMissing");

        // Validate field types
        foreach (var (key, value) in sanitized)
        {
            if (schema.PayloadKeyTypes.TryGetValue(key, out var expectedType))
            {
                if (!IsValidType(value, expectedType))
                    return (false, null, "PayloadFieldTypeMismatch");
            }
        }

        // Check payload size
        var json = JsonSerializer.Serialize(sanitized);
        if (json.Length > MaxPayloadSizeBytes)
            return (false, null, "PayloadTooLarge");

        return (true, sanitized, null);
    }

    public bool ContainsProhibitedData(string eventName, Dictionary<string, object>? payload)
    {
        var schema = _schemaRegistry.GetSchema(eventName);
        if (schema is null || !schema.PiiSensitive)
            return false; // Skip PII check for non-sensitive events

        if (payload is null || payload.Count == 0)
            return false;

        var payloadJson = JsonSerializer.Serialize(payload);

        // Check against PII patterns
        if (EmailPattern.IsMatch(payloadJson))
        {
            _logger.LogWarning("PII detected (email) in event: {EventName}", LogSanitizer.Sanitize(eventName));
            return true;
        }

        if (PhonePattern.IsMatch(payloadJson))
        {
            _logger.LogWarning("PII detected (phone) in event: {EventName}", LogSanitizer.Sanitize(eventName));
            return true;
        }

        if (CredentialPattern.IsMatch(payloadJson))
        {
            _logger.LogWarning("PII detected (credential) in event: {EventName}", LogSanitizer.Sanitize(eventName));
            return true;
        }

        if (CreditCardPattern.IsMatch(payloadJson))
        {
            _logger.LogWarning("PII detected (credit card) in event: {EventName}", LogSanitizer.Sanitize(eventName));
            return true;
        }

        return false;
    }

    public bool IsPayloadSizeValid(string payload)
    {
        return payload.Length <= MaxPayloadSizeBytes;
    }

    public IReadOnlySet<string> GetRequiredFields(string eventName)
    {
        var schema = _schemaRegistry.GetSchema(eventName);
        return schema?.RequiredPayloadKeys ?? System.Collections.Immutable.ImmutableHashSet<string>.Empty;
    }

    /// <summary>
    /// Check if value matches expected type
    /// </summary>
    private static bool IsValidType(object? value, string expectedType)
    {
        if (value is null)
            return true; // Allow null for optional fields

        return expectedType.ToLower() switch
        {
            "string" => value is string,
            "int" => value is int or long,
            "bool" => value is bool,
            "float" => value is float or double,
            "object" => value is IDictionary,
            "array" => value is System.Collections.IList,
            _ => true // Allow unknown types
        };
    }
}
