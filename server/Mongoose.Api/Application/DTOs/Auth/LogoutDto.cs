using System.Text.Json.Serialization;

namespace Mongoose.Api.Application.DTOs;

/// <summary>
/// Data Transfer Objects for the logout endpoint.
/// </summary>
public static class LogoutDto
{
    /// <summary>
    /// Response payload for successful logout.
    /// </summary>
    public record LogoutResponse(
        [property: JsonPropertyName("message")] string Message
    );
}

