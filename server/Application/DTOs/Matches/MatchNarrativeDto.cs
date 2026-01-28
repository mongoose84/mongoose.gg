using System.Text.Json.Serialization;

namespace RiotProxy.Application.DTOs.Matches;

/// <summary>
/// Response for the match narrative endpoint.
/// Contains lane matchups for all 5 roles with detailed stats.
/// </summary>
public record MatchNarrativeResponse(
    [property: JsonPropertyName("matchId")] string MatchId,
    [property: JsonPropertyName("userRole")] string UserRole,
    [property: JsonPropertyName("laneMatchups")] LaneMatchup[] LaneMatchups
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
/// Participant data for a lane matchup.
/// </summary>
public record MatchupParticipant(
    [property: JsonPropertyName("puuid")] string Puuid,
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
    // Laning phase (0-15m)
    [property: JsonPropertyName("goldAt15")] int? GoldAt15,
    [property: JsonPropertyName("csAt15")] int? CsAt15,
    [property: JsonPropertyName("goldDiffAt15")] int? GoldDiffAt15,
    [property: JsonPropertyName("csDiffAt15")] int? CsDiffAt15,
    [property: JsonPropertyName("soloKills")] int SoloKills,
    // Game impact (post-laning)
    [property: JsonPropertyName("damageShare")] double DamageShare,
    [property: JsonPropertyName("killParticipation")] double KillParticipation,
    [property: JsonPropertyName("visionScore")] int VisionScore,
    [property: JsonPropertyName("creepScore")] int CreepScore,
    [property: JsonPropertyName("goldEarned")] int GoldEarned
);

/// <summary>
/// Internal DTO for raw participant data from database query.
/// </summary>
public record MatchupParticipantRaw(
    long ParticipantId,
    string Puuid,
    int ChampionId,
    string ChampionName,
    int TeamId,
    string? Role,
    bool Win,
    int Kills,
    int Deaths,
    int Assists,
    int CreepScore,
    int GoldEarned,
    // From participant_metrics
    decimal KillParticipation,
    decimal DamageShare,
    int VisionScore,
    // From participant_checkpoints at minute 15
    int? GoldAt15,
    int? CsAt15,
    int? GoldDiffAt15,
    int? CsDiffAt15
);

