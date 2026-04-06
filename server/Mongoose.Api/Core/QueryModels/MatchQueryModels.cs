using System.Text.Json.Serialization;

namespace Mongoose.Api.Core.QueryModels;

/// <summary>
/// Lightweight match summary for the list view.
/// Contains only data needed to render the match row (champion, KDA, result, timestamp).
/// </summary>
public record MatchListSummaryItem(
    [property: JsonPropertyName("matchId")] string MatchId,
    [property: JsonPropertyName("accountGameName")] string? AccountGameName,
    [property: JsonPropertyName("accountTagLine")] string? AccountTagLine,
    [property: JsonPropertyName("accountRegion")] string? AccountRegion,
    [property: JsonPropertyName("queueId")] int QueueId,
    [property: JsonPropertyName("queueType")] string QueueType,
    [property: JsonPropertyName("championId")] int ChampionId,
    [property: JsonPropertyName("championName")] string ChampionName,
    [property: JsonPropertyName("championIconUrl")] string ChampionIconUrl,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("lane")] string? Lane,
    [property: JsonPropertyName("win")] bool Win,
    [property: JsonPropertyName("kills")] int Kills,
    [property: JsonPropertyName("deaths")] int Deaths,
    [property: JsonPropertyName("assists")] int Assists,
    [property: JsonPropertyName("creepScore")] int CreepScore,
    [property: JsonPropertyName("goldEarned")] int GoldEarned,
    [property: JsonPropertyName("gameDurationSec")] int GameDurationSec,
    [property: JsonPropertyName("gameStartTime")] long GameStartTime,
    [property: JsonPropertyName("csPerMin")] double CsPerMin,
    [property: JsonPropertyName("goldPerMin")] double GoldPerMin,
    [property: JsonPropertyName("trendBadge")] TrendBadge? TrendBadge
);

/// <summary>
/// Internal DTO for raw match summary data from database query.
/// </summary>
public record MatchListSummaryRawData(
    string MatchId,
    string? AccountGameName,
    string? AccountTagLine,
    string? AccountRegion,
    int QueueId,
    int ChampionId,
    string ChampionName,
    string Role,
    string? Lane,
    bool Win,
    int Kills,
    int Deaths,
    int Assists,
    int CreepScore,
    int GoldEarned,
    int GameDurationSec,
    long GameStartTime
);

/// <summary>
/// Full match details for the details panel.
/// Fetched on-demand when user selects a match.
/// </summary>
public record MatchDetailsItem(
    [property: JsonPropertyName("matchId")] string MatchId,
    [property: JsonPropertyName("queueId")] int QueueId,
    [property: JsonPropertyName("queueType")] string QueueType,
    [property: JsonPropertyName("championId")] int ChampionId,
    [property: JsonPropertyName("championName")] string ChampionName,
    [property: JsonPropertyName("championIconUrl")] string ChampionIconUrl,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("lane")] string? Lane,
    [property: JsonPropertyName("win")] bool Win,
    [property: JsonPropertyName("kills")] int Kills,
    [property: JsonPropertyName("deaths")] int Deaths,
    [property: JsonPropertyName("assists")] int Assists,
    [property: JsonPropertyName("creepScore")] int CreepScore,
    [property: JsonPropertyName("goldEarned")] int GoldEarned,
    [property: JsonPropertyName("gameDurationSec")] int GameDurationSec,
    [property: JsonPropertyName("gameStartTime")] long GameStartTime,
    [property: JsonPropertyName("damageDealt")] int DamageDealt,
    [property: JsonPropertyName("damageTaken")] int DamageTaken,
    [property: JsonPropertyName("visionScore")] int VisionScore,
    [property: JsonPropertyName("killParticipation")] double KillParticipation,
    [property: JsonPropertyName("damageShare")] double DamageShare,
    [property: JsonPropertyName("deathsPre10")] int DeathsPre10,
    [property: JsonPropertyName("csPerMin")] double CsPerMin,
    [property: JsonPropertyName("goldPerMin")] double GoldPerMin,
    [property: JsonPropertyName("teamKills")] int TeamKills,
    [property: JsonPropertyName("enemyTeamKills")] int EnemyTeamKills,
    [property: JsonPropertyName("goldDiffAt15")] int? GoldDiffAt15,
    [property: JsonPropertyName("teamTotalDamage")] int TeamTotalDamage,
    [property: JsonPropertyName("enemyTeamTotalDamage")] int EnemyTeamTotalDamage,
    [property: JsonPropertyName("teamGoldLeadAt15")] int? TeamGoldLeadAt15,
    [property: JsonPropertyName("teamDragons")] int TeamDragons,
    [property: JsonPropertyName("enemyTeamDragons")] int EnemyTeamDragons,
    [property: JsonPropertyName("teamBarons")] int TeamBarons,
    [property: JsonPropertyName("enemyTeamBarons")] int EnemyTeamBarons,
    [property: JsonPropertyName("teamTowers")] int TeamTowers,
    [property: JsonPropertyName("enemyTeamTowers")] int EnemyTeamTowers,
    [property: JsonPropertyName("dragonsParticipated")] int DragonsParticipated
);

/// <summary>
/// Internal DTO for raw match details data from database query.
/// </summary>
public record MatchDetailsRawData(
    string MatchId,
    int QueueId,
    int ChampionId,
    string ChampionName,
    string Role,
    string? Lane,
    bool Win,
    int Kills,
    int Deaths,
    int Assists,
    int CreepScore,
    int GoldEarned,
    int GameDurationSec,
    long GameStartTime,
    int DamageDealt,
    int DamageTaken,
    int VisionScore,
    decimal KillParticipation,
    decimal DamageShare,
    int DeathsPre10,
    int TeamId,
    int? GoldDiffAt15,
    int TeamKills,
    int EnemyTeamKills,
    int TeamTotalDamage,
    int EnemyTeamTotalDamage,
    int? TeamGoldLeadAt15,
    int TeamDragons,
    int EnemyTeamDragons,
    int TeamBarons,
    int EnemyTeamBarons,
    int TeamTowers,
    int EnemyTeamTowers,
    int DragonsParticipated
);

/// <summary>
/// Individual match item in the match list.
/// Contains all stats needed for both the list row and details panel.
/// @deprecated Use MatchListSummaryItem for list view and MatchDetailsItem for details.
/// </summary>
public record MatchListItem(
    [property: JsonPropertyName("matchId")] string MatchId,
    [property: JsonPropertyName("queueId")] int QueueId,
    [property: JsonPropertyName("queueType")] string QueueType,
    [property: JsonPropertyName("championId")] int ChampionId,
    [property: JsonPropertyName("championName")] string ChampionName,
    [property: JsonPropertyName("championIconUrl")] string ChampionIconUrl,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("lane")] string? Lane,
    [property: JsonPropertyName("win")] bool Win,
    [property: JsonPropertyName("kills")] int Kills,
    [property: JsonPropertyName("deaths")] int Deaths,
    [property: JsonPropertyName("assists")] int Assists,
    [property: JsonPropertyName("creepScore")] int CreepScore,
    [property: JsonPropertyName("goldEarned")] int GoldEarned,
    [property: JsonPropertyName("gameDurationSec")] int GameDurationSec,
    [property: JsonPropertyName("gameStartTime")] long GameStartTime,
    [property: JsonPropertyName("damageDealt")] int DamageDealt,
    [property: JsonPropertyName("damageTaken")] int DamageTaken,
    [property: JsonPropertyName("visionScore")] int VisionScore,
    [property: JsonPropertyName("killParticipation")] double KillParticipation,
    [property: JsonPropertyName("damageShare")] double DamageShare,
    [property: JsonPropertyName("deathsPre10")] int DeathsPre10,
    [property: JsonPropertyName("csPerMin")] double CsPerMin,
    [property: JsonPropertyName("goldPerMin")] double GoldPerMin,
    [property: JsonPropertyName("teamKills")] int TeamKills,
    [property: JsonPropertyName("enemyTeamKills")] int EnemyTeamKills,
    [property: JsonPropertyName("goldDiffAt15")] int? GoldDiffAt15,
    // Team comparison data
    [property: JsonPropertyName("teamTotalDamage")] int TeamTotalDamage,
    [property: JsonPropertyName("enemyTeamTotalDamage")] int EnemyTeamTotalDamage,
    [property: JsonPropertyName("teamGoldLeadAt15")] int? TeamGoldLeadAt15,
    [property: JsonPropertyName("teamDragons")] int TeamDragons,
    [property: JsonPropertyName("enemyTeamDragons")] int EnemyTeamDragons,
    [property: JsonPropertyName("teamBarons")] int TeamBarons,
    [property: JsonPropertyName("enemyTeamBarons")] int EnemyTeamBarons,
    [property: JsonPropertyName("teamTowers")] int TeamTowers,
    [property: JsonPropertyName("enemyTeamTowers")] int EnemyTeamTowers,
    [property: JsonPropertyName("trendBadge")] TrendBadge? TrendBadge
);

/// <summary>
/// Trend badge showing the most notable insight for a match.
/// Pre-computed on the backend by comparing match stats to role baseline.
/// </summary>
public record TrendBadge(
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("type")] string Type,  // "positive", "neutral", "negative"
    [property: JsonPropertyName("stat")] string Stat   // Which stat this badge refers to
);

/// <summary>
/// Baseline averages for a specific role.
/// Computed from the last 10 games in that role within the filtered queue.
/// </summary>
public record RoleBaseline(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("gamesCount")] int GamesCount,
    [property: JsonPropertyName("avgKills")] double AvgKills,
    [property: JsonPropertyName("avgDeaths")] double AvgDeaths,
    [property: JsonPropertyName("avgAssists")] double AvgAssists,
    [property: JsonPropertyName("avgKda")] double AvgKda,
    [property: JsonPropertyName("avgCreepScore")] double AvgCreepScore,
    [property: JsonPropertyName("avgCsPerMin")] double AvgCsPerMin,
    [property: JsonPropertyName("avgGoldEarned")] double AvgGoldEarned,
    [property: JsonPropertyName("avgGoldPerMin")] double AvgGoldPerMin,
    [property: JsonPropertyName("avgDamageDealt")] double AvgDamageDealt,
    [property: JsonPropertyName("avgDamageTaken")] double AvgDamageTaken,
    [property: JsonPropertyName("avgVisionScore")] double AvgVisionScore,
    [property: JsonPropertyName("avgKillParticipation")] double AvgKillParticipation,
    [property: JsonPropertyName("avgGameDurationSec")] double AvgGameDurationSec,
    [property: JsonPropertyName("winRate")] double WinRate
);

/// <summary>
/// Internal DTO for raw match data from database query.
/// Used before transformation to MatchListItem.
/// </summary>
public record MatchListRawData(
    string MatchId,
    int QueueId,
    int ChampionId,
    string ChampionName,
    string Role,
    string? Lane,
    bool Win,
    int Kills,
    int Deaths,
    int Assists,
    int CreepScore,
    int GoldEarned,
    int GameDurationSec,
    long GameStartTime,
    int DamageDealt,
    int DamageTaken,
    int VisionScore,
    decimal KillParticipation,
    decimal DamageShare,
    int DeathsPre10,
    int TeamId,
    int TeamKills,
    int EnemyTeamKills,
    int? GoldDiffAt15,
    // Team comparison data
    int TeamTotalDamage,
    int EnemyTeamTotalDamage,
    int? TeamGoldLeadAt15,
    int TeamDragons,
    int EnemyTeamDragons,
    int TeamBarons,
    int EnemyTeamBarons,
    int TeamTowers,
    int EnemyTeamTowers
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
    int DeathsPre10,
    // From participant_checkpoints at minute 10
    int? GoldAt10,
    int? CsAt10,
    int? GoldDiffAt10,
    int? CsDiffAt10
);

