namespace Mongoose.Api.Core.QueryModels;

/// <summary>
/// Lightweight model for LP estimation - only the fields needed for the algorithm.
/// </summary>
/// <param name="MatchId">The match identifier</param>
/// <param name="Puuid">The player's PUUID</param>
/// <param name="Win">Whether the player won the match</param>
/// <param name="GameDurationSec">Game duration in seconds (used for remake detection)</param>
/// <param name="LpAfter">LP after the match (null if unknown)</param>
/// <param name="TierAfter">Tier after the match (e.g., "GOLD")</param>
/// <param name="RankAfter">Division after the match (e.g., "II")</param>
/// <param name="IsLpEstimated">Whether the LP data was estimated (true) or actual from Riot API (false)</param>
/// <param name="SeasonCode">Season code for the match (e.g., "S2025_S1"). Used to stop estimation at season boundaries.</param>
public record LpEstimationMatch(
    string MatchId,
    string Puuid,
    bool Win,
    int GameDurationSec,
    int? LpAfter,
    string? TierAfter,
    string? RankAfter,
    bool IsLpEstimated,
    string? SeasonCode
);

