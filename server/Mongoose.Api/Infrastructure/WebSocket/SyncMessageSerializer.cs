using System.Text;
using System.Text.Json;

namespace Mongoose.Api.Infrastructure.WebSocket;

/// <summary>
/// Serializes sync WebSocket messages by their runtime type, so a message passed as its base type
/// (<see cref="SyncAggregateMessage"/>, <see cref="SyncServerMessage"/>) keeps its own fields.
/// </summary>
public static class SyncMessageSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static byte[] Serialize(object message) =>
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, message.GetType(), JsonOptions));
}
