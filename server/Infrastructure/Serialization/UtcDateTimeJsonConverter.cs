using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RiotProxy.Infrastructure.Serialization;

/// <summary>
/// Custom JSON converter for DateTime that ensures UTC format serialization.
/// When DateTimeKind is Utc, the value is serialized with 'Z' suffix (ISO 8601).
/// When DateTimeKind is Unspecified, the value is treated as UTC and serialized with 'Z' suffix.
/// When DateTimeKind is Local, the value is converted to UTC first.
/// </summary>
public sealed class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    private const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dateTime = reader.GetDateTime();
        // Ensure the parsed DateTime is treated as UTC
        return dateTime.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
            : dateTime.ToUniversalTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // Convert to UTC if not already, then format with Z suffix
        var utcValue = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            // Treat Unspecified as UTC (our database values should all be UTC)
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => value
        };
        
        writer.WriteStringValue(utcValue.ToString(DateTimeFormat, CultureInfo.InvariantCulture));
    }
}

/// <summary>
/// Custom JSON converter for nullable DateTime that ensures UTC format serialization.
/// </summary>
public sealed class UtcNullableDateTimeJsonConverter : JsonConverter<DateTime?>
{
    private const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;
            
        var dateTime = reader.GetDateTime();
        // Ensure the parsed DateTime is treated as UTC
        return dateTime.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
            : dateTime.ToUniversalTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }
        
        // Convert to UTC if not already, then format with Z suffix
        var utcValue = value.Value.Kind switch
        {
            DateTimeKind.Utc => value.Value,
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            // Treat Unspecified as UTC (our database values should all be UTC)
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc),
            _ => value.Value
        };
        
        writer.WriteStringValue(utcValue.ToString(DateTimeFormat, CultureInfo.InvariantCulture));
    }
}

