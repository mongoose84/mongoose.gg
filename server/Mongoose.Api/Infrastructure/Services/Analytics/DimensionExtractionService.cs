namespace Mongoose.Api.Infrastructure.Services.Analytics;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mongoose.Api.Core.Interfaces;

/// <summary>
/// Service for extracting and enriching event dimensions from raw event data.
/// Parses user agents, extracts URLs, and prepares dimension data for queries.
/// </summary>
public class DimensionExtractionService
{
    private readonly IAnalyticsEventDimensionsRepository _dimensionRepository;
    private readonly IAnalyticsEventsV2Repository _eventsRepository;
    private readonly ILogger<DimensionExtractionService> _logger;

    public DimensionExtractionService(
        IAnalyticsEventDimensionsRepository dimensionRepository,
        IAnalyticsEventsV2Repository eventsRepository,
        ILogger<DimensionExtractionService> logger)
    {
        _dimensionRepository = dimensionRepository;
        _eventsRepository = eventsRepository;
        _logger = logger;
    }

    /// <summary>
    /// Extract dimensions for a batch of raw events
    /// </summary>
    public async Task ExtractDimensionsAsync(IEnumerable<AnalyticsEventV2> events)
    {
        var tasks = events.Select(async e =>
        {
            try
            {
                var dimension = ExtractDimensionsFromEvent(e);
                await _dimensionRepository.InsertDimensionAsync(dimension);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to extract dimensions for event {e.Id}: {ex.Message}");
            }
        });

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Extract all dimensions from a single event
    /// </summary>
    private AnalyticsEventDimension ExtractDimensionsFromEvent(AnalyticsEventV2 @event)
    {
        var payload = TryParsePayload(@event.PayloadJson);
        var (deviceType, browser, os) = ParseUserAgent(@event.UserAgentHash);

        return new AnalyticsEventDimension
        {
            EventId = @event.Id,
            EventName = @event.EventName,
            EventCategory = @event.EventCategory,
            PagePath = ExtractPagePath(payload),
            ReferrerDomain = ExtractReferrerDomain(payload),
            ReferrerPath = ExtractReferrerPath(payload),
            DeviceType = deviceType,
            BrowserName = browser.Name,
            BrowserVersion = browser.Version,
            OsName = os.Name,
            OsVersion = os.Version,
            CountryCode = ExtractCountryCode(payload, @event.IpAnonymized),
            Tier = @event.Tier,
            IsAuthenticated = @event.UserId.HasValue,
            UserId = @event.UserId,
            SessionId = @event.SessionId,
            EventTimestampUtc = @event.ClientTimestampUtc ?? @event.ServerTimestampUtc,
            CustomProperties = ExtractCustomProperties(payload, @event.EventName)
        };
    }

    /// <summary>
    /// Parse user agent hash to extract device, browser, and OS
    /// </summary>
    private (string DeviceType, (string Name, string Version) Browser, (string Name, string Version) Os) ParseUserAgent(string? uaHash)
    {
        if (string.IsNullOrEmpty(uaHash))
            return ("unknown", ("unknown", "unknown"), ("unknown", "unknown"));

        // In real implementation, use a UA parser library
        // For now, simple pattern matching
        var ua = uaHash.ToLowerInvariant();

        var deviceType = DetectDeviceType(ua);
        var browser = DetectBrowser(ua);
        var os = DetectOs(ua);

        return (deviceType, browser, os);
    }

    private string DetectDeviceType(string ua)
    {
        if (ua.Contains("mobile") || ua.Contains("android"))
            return "mobile";
        if (ua.Contains("tablet") || ua.Contains("ipad"))
            return "tablet";
        if (ua.Contains("windows") || ua.Contains("mac") || ua.Contains("linux"))
            return "desktop";
        return "unknown";
    }

    private (string Name, string Version) DetectBrowser(string ua)
    {
        if (ua.Contains("chrome"))
            return ("chrome", ExtractVersion(ua, "chrome"));
        if (ua.Contains("safari"))
            return ("safari", ExtractVersion(ua, "safari"));
        if (ua.Contains("firefox"))
            return ("firefox", ExtractVersion(ua, "firefox"));
        if (ua.Contains("edg"))
            return ("edge", ExtractVersion(ua, "edge"));
        return ("other", "unknown");
    }

    private (string Name, string Version) DetectOs(string ua)
    {
        if (ua.Contains("windows"))
            return ("windows", ExtractVersion(ua, "windows"));
        if (ua.Contains("mac"))
            return ("mac", ExtractVersion(ua, "mac"));
        if (ua.Contains("iphone") || ua.Contains("ios"))
            return ("ios", ExtractVersion(ua, "ios"));
        if (ua.Contains("android"))
            return ("android", ExtractVersion(ua, "android"));
        if (ua.Contains("linux"))
            return ("linux", ExtractVersion(ua, "linux"));
        return ("other", "unknown");
    }

    private string ExtractVersion(string ua, string component)
    {
        var match = Regex.Match(ua, $@"{component}[/\s]+([\d.]+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : "unknown";
    }

    private string? ExtractPagePath(JsonElement? payload)
    {
        if (payload == null)
            return null;

        try
        {
            return payload.Value.TryGetProperty("page", out var page)
                ? page.GetString()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private string? ExtractReferrerDomain(JsonElement? payload)
    {
        if (payload == null)
            return null;

        try
        {
            if (payload.Value.TryGetProperty("referrer", out var referrer))
            {
                var referrerStr = referrer.GetString();
                if (string.IsNullOrEmpty(referrerStr))
                    return null;

                // Extract domain from URL
                if (Uri.TryCreate(referrerStr, UriKind.Absolute, out var uri))
                    return uri.Host;
            }
        }
        catch
        {
        }

        return null;
    }

    private string? ExtractReferrerPath(JsonElement? payload)
    {
        if (payload == null)
            return null;

        try
        {
            if (payload.Value.TryGetProperty("referrer", out var referrer))
            {
                var referrerStr = referrer.GetString();
                if (string.IsNullOrEmpty(referrerStr))
                    return null;

                if (Uri.TryCreate(referrerStr, UriKind.Absolute, out var uri))
                    return uri.PathAndQuery;
            }
        }
        catch
        {
        }

        return null;
    }

    private string? ExtractCountryCode(JsonElement? payload, string? ipAnonymized)
    {
        // First try to get from payload (if explicitly passed)
        if (payload != null)
        {
            try
            {
                if (payload.Value.TryGetProperty("countryCode", out var cc))
                    return cc.GetString();
            }
            catch
            {
            }
        }

        // In real implementation, would use IP geolocation service
        // For now, return null (can be enriched later)
        return null;
    }

    private string? ExtractCustomProperties(JsonElement? payload, string eventName)
    {
        if (payload == null)
            return null;

        try
        {
            // Extract properties that are specific to this event type
            var customProps = new Dictionary<string, object>();

            // Enumerate all properties in payload except known ones
            var knownProps = new[] { "page", "referrer", "countryCode", "timestamp" };

            foreach (var property in payload.Value.EnumerateObject())
            {
                if (!knownProps.Contains(property.Name))
                {
                    customProps[property.Name] = property.Value.GetRawText();
                }
            }

            return customProps.Count > 0 ? JsonSerializer.Serialize(customProps) : null;
        }
        catch
        {
            return null;
        }
    }

    private JsonElement? TryParsePayload(string? payloadJson)
    {
        if (string.IsNullOrEmpty(payloadJson))
            return null;

        try
        {
            return JsonDocument.Parse(payloadJson).RootElement;
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// DTO for analytics events v2 (placeholder for reference)
/// </summary>
public record AnalyticsEventV2
{
    public long Id { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string EventCategory { get; set; } = string.Empty;
    public long? UserId { get; set; }
    public string? SessionId { get; set; }
    public string Tier { get; set; } = "free";
    public string? PayloadJson { get; set; }
    public string? UserAgentHash { get; set; }
    public string? IpAnonymized { get; set; }
    public DateTime? ClientTimestampUtc { get; set; }
    public DateTime ServerTimestampUtc { get; set; }
}
