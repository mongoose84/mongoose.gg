using System.Text.Json.Serialization;

namespace RiotProxy.Application.DTOs
{
    // DTOs for request binding
    public record CreateUserRequest(
        [property: JsonPropertyName("userName")] string UserName,
        [property: JsonPropertyName("userType")] string UserType,
        [property: JsonPropertyName("gamers")] List<GamerDto> Gamers
    );

    public record GamerDto(
        [property: JsonPropertyName("gameName")] string GameName,
        [property: JsonPropertyName("tagLine")] string TagLine
    );
}