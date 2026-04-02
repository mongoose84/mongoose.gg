using System.Text.Json.Serialization;

namespace Mongoose.Api.Application.DTOs;

public static class DeleteAccountDto
{
    /// <summary>
    /// Request to delete user account. Requires password confirmation for security.
    /// </summary>
    public record DeleteAccountRequest(
        [property: JsonPropertyName("password")] string Password
    );

    /// <summary>
    /// Response after successful account deletion.
    /// </summary>
    public record DeleteAccountResponse(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("message")] string Message
    );
}

