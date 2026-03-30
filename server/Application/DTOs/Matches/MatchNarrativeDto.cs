using System.Text.Json.Serialization;

namespace Mongoose.Api.Application.DTOs;

/// <summary>
/// Response for the match narrative endpoint.
/// Contains lane matchups for all 5 roles with detailed stats.
/// For ARAM games, matchups are paired by damage share instead of role.
/// </summary>
public record MatchNarrativeResponse(
    [property: JsonPropertyName("matchId")] string MatchId,
    [property: JsonPropertyName("userRole")] string UserRole,
    [property: JsonPropertyName("laneMatchups")] LaneMatchup[] LaneMatchups,
    [property: JsonPropertyName("isAram")] bool IsAram = false
);

/// <summary>
/// A single lane matchup between two players of the same role.
/// </summary>
public record LaneMatchup(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("allyParticipant")] MatchupParticipant AllyParticipant,
    [property: JsonPropertyName("enemyParticipant")] MatchupParticipant EnemyParticipant,
    [property: JsonPropertyName("laneWinner")] string LaneWinner // "ally", "enemy", or "even"
);

/// <summary>
/// Participant data for a lane matchup (API response DTO).
/// </summary>
public record MatchupParticipant(
    [property: JsonPropertyName("isUserParticipant")] bool IsUserParticipant,
    [property: JsonPropertyName("summonerName")] string SummonerName,
    [property: JsonPropertyName("championId")] int ChampionId,
    [property: JsonPropertyName("championName")] string ChampionName,
    [property: JsonPropertyName("championIconUrl")] string ChampionIconUrl,
    [property: JsonPropertyName("teamId")] int TeamId,
    [property: JsonPropertyName("win")] bool Win,
    // KDA
    [property: JsonPropertyName("kills")] int Kills,
    [property: JsonPropertyName("deaths")] int Deaths,
    [property: JsonPropertyName("assists")] int Assists,
    // Early laning phase (0-10m)
    [property: JsonPropertyName("goldAt10")] int? GoldAt10,
    [property: JsonPropertyName("csAt10")] int? CsAt10,
    [property: JsonPropertyName("goldDiffAt10")] int? GoldDiffAt10,
    [property: JsonPropertyName("csDiffAt10")] int? CsDiffAt10,
    [property: JsonPropertyName("deathsPre10")] int DeathsPre10,
    [property: JsonPropertyName("soloKills")] int SoloKills,
    // Game impact (post-laning)
    [property: JsonPropertyName("damageShare")] double DamageShare,
    [property: JsonPropertyName("killParticipation")] double KillParticipation,
    [property: JsonPropertyName("visionScore")] int VisionScore,
    [property: JsonPropertyName("creepScore")] int CreepScore,
    [property: JsonPropertyName("goldEarned")] int GoldEarned
);

