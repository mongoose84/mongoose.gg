namespace RiotProxy.External.Domain.Entities;

/// <summary>
/// Represents a user behavior analytics event for tracking app usage.
/// Events are stored for Grafana dashboards and usage analysis.
/// </summary>
public class AnalyticsEvent : EntityBase
{
    public long Id { get; set; }
    
    /// <summary>
    /// User who triggered the event. Null for anonymous/pre-login events.
    /// </summary>
    public long? UserId { get; set; }
    
    /// <summary>
    /// User tier at time of event (free/pro) for segmentation.
    /// </summary>
    public string Tier { get; set; } = "free";
    
    /// <summary>
    /// Event name using colon-separated hierarchy.
    /// Examples: "page:view", "auth:login", "click:nav", "feature:champion_matchup"
    /// </summary>
    public string EventName { get; set; } = string.Empty;
    
    /// <summary>
    /// JSON payload with event-specific data.
    /// </summary>
    public string? PayloadJson { get; set; }
    
    /// <summary>
    /// Client-generated session ID to group events from the same session.
    /// </summary>
    public string? SessionId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

