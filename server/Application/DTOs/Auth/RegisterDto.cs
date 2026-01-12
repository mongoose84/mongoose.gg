using System.Text.Json.Serialization;

namespace RiotProxy.Application.DTOs;

public static class RegisterDto
{
    public record RegisterRequest(
        [property: JsonPropertyName("username")] string Username,
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("password")] string Password
    );

    public record RegisterResponse(
        [property: JsonPropertyName("userId")] long UserId,
        [property: JsonPropertyName("username")] string Username,
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("emailVerified")] bool EmailVerified,
        [property: JsonPropertyName("message")] string Message
    );
}

