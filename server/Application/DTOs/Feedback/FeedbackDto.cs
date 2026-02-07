using System.Text.Json.Serialization;

namespace Mongoose.Api.Application.DTOs.Feedback;

/// <summary>
/// Data Transfer Objects for the feedback endpoint.
/// </summary>
public static class FeedbackDto
{
    /// <summary>
    /// Request payload for submitting feedback.
    /// </summary>
    public record FeedbackRequest(
        /// <summary>
        /// Type of feedback: "bug" or "feature"
        /// </summary>
        [property: JsonPropertyName("type")] string Type,
        
        /// <summary>
        /// Short summary/title of the feedback
        /// </summary>
        [property: JsonPropertyName("summary")] string Summary,
        
        /// <summary>
        /// Detailed description of the issue or feature request
        /// </summary>
        [property: JsonPropertyName("details")] string? Details,
        
        /// <summary>
        /// The route/page where the feedback originated (e.g., "/app/solo")
        /// </summary>
        [property: JsonPropertyName("route")] string? Route,
        
        /// <summary>
        /// Client-provided environment identifier (e.g., "production", "staging")
        /// </summary>
        [property: JsonPropertyName("environment")] string? Environment,
        
        /// <summary>
        /// Browser information (e.g., "Chrome 120")
        /// </summary>
        [property: JsonPropertyName("browser")] string? Browser,
        
        /// <summary>
        /// Operating system information (e.g., "Windows 11")
        /// </summary>
        [property: JsonPropertyName("os")] string? Os
    );

    /// <summary>
    /// Response payload for successful feedback submission.
    /// Note: Does not include GitHub-specific details (issue number, URL, etc.)
    /// </summary>
    public record FeedbackResponse(
        /// <summary>
        /// Whether the feedback was successfully submitted
        /// </summary>
        [property: JsonPropertyName("success")] bool Success,
        
        /// <summary>
        /// User-friendly message
        /// </summary>
        [property: JsonPropertyName("message")] string Message
    );
}

