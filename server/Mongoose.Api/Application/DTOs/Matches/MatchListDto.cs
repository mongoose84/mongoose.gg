using System.Text.Json.Serialization;
using Mongoose.Api.Core.QueryModels;

namespace Mongoose.Api.Application.DTOs;

/// <summary>
/// Response for the match list endpoint.
/// Contains lightweight match summaries for fast list rendering.
/// Full match details are fetched on-demand via the match details endpoint.
/// </summary>
public record MatchListResponse(
    [property: JsonPropertyName("matches")] MatchListSummaryItem[] Matches,
    [property: JsonPropertyName("baselinesByRole")] Dictionary<string, RoleBaseline> BaselinesByRole,
    [property: JsonPropertyName("queueType")] string QueueType,
    [property: JsonPropertyName("totalMatches")] int TotalMatches
);

/// <summary>
/// Response for the match details endpoint.
/// Contains full match data including team stats, objectives, and performance metrics.
/// </summary>
public record MatchDetailsResponse(
    [property: JsonPropertyName("match")] MatchDetailsItem Match,
    [property: JsonPropertyName("baseline")] RoleBaseline? Baseline
);

