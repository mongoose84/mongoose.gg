using System.Text.Json;

namespace RiotProxy.Infrastructure.External.Backfill
{
    /// <summary>
    /// Utility for extracting match data from Riot API JSON responses during backfill operations.
    /// </summary>
    public static class BackfillMatchJsonExtractor
    {
        public static int? ExtractQueueId(JsonDocument matchInfo)
        {
            if (matchInfo.RootElement.TryGetProperty("info", out var infoElement) &&
                infoElement.TryGetProperty("queueId", out var queueIdElement))
            {
                if (queueIdElement.ValueKind == JsonValueKind.Number)
                    return queueIdElement.GetInt32();

                if (queueIdElement.ValueKind == JsonValueKind.String &&
                    int.TryParse(queueIdElement.GetString(), out var parsedId))
                    return parsedId;
            }

            return null;
        }

        public static DateTime ExtractGameEndTimestamp(JsonDocument matchInfo)
        {
            if (matchInfo.RootElement.TryGetProperty("info", out var infoElement))
            {
                var endMs = GetEpochMilliseconds(infoElement, "gameEndTimestamp");

                if (endMs.HasValue && endMs.Value > 0)
                    return DateTimeOffset.FromUnixTimeMilliseconds(endMs.Value).UtcDateTime;
            }

            return DateTime.MinValue;
        }

        public static long ExtractDurationSeconds(JsonDocument matchInfo)
        {
            if (matchInfo.RootElement.TryGetProperty("info", out var infoElement) &&
                infoElement.TryGetProperty("gameDuration", out var durationElement))
            {
                if (durationElement.ValueKind == JsonValueKind.Number)
                    return durationElement.GetInt64();
            }

            return 0;
        }

        public static string ExtractGameMode(JsonDocument matchInfo)
        {
            if (matchInfo.RootElement.TryGetProperty("info", out var infoElement) &&
                infoElement.TryGetProperty("gameMode", out var gameModeElement))
            {
                if (gameModeElement.ValueKind == JsonValueKind.String)
                    return gameModeElement.GetString() ?? string.Empty;

                return gameModeElement.ToString();
            }
            return string.Empty;
        }

        public static long? GetEpochMilliseconds(JsonElement obj, string propertyName)
        {
            if (!obj.TryGetProperty(propertyName, out var el))
                return null;

            return el.ValueKind switch
            {
                JsonValueKind.Number => el.GetInt64(),
                JsonValueKind.String => long.TryParse(el.GetString(), out var v) ? v : null,
                _ => null
            };
        }
    }
}
