using System.Text.Json.Serialization;

namespace RiotProxy.Application.DTOs;

public static class LoginDto
{
    public record LoginRequest(
        [property: JsonPropertyName("username")] string Username,
        [property: JsonPropertyName("password")] string Password,
        [property: JsonPropertyName("rememberMe")] bool RememberMe = false
    );

    public record LoginResponse(
        [property: JsonPropertyName("userId")] long UserId,
        [property: JsonPropertyName("username")] string Username,
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("emailVerified")] bool EmailVerified,
        [property: JsonPropertyName("tier")] string Tier,
        [property: JsonPropertyName("message")] string Message
    );
}
