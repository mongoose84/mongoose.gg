using System.Text.Json.Serialization;
using RiotProxy.Core.QueryModels;

namespace RiotProxy.Application.DTOs.Matches;

/// <summary>
/// Response for the match list endpoint.
/// Contains recent matches and baseline averages per role for trend comparisons.
/// </summary>
public record MatchListResponse(
    [property: JsonPropertyName("matches")] MatchListItem[] Matches,
    [property: JsonPropertyName("baselinesByRole")] Dictionary<string, RoleBaseline> BaselinesByRole,
    [property: JsonPropertyName("queueType")] string QueueType,
    [property: JsonPropertyName("totalMatches")] int TotalMatches
);

