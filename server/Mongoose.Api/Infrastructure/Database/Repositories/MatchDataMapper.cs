using MySqlConnector;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.QueryModels;

namespace Mongoose.Api.Infrastructure.Database.Repositories;

/// <summary>
/// Static mapper methods for reading match-related entities from MySqlDataReader rows.
/// </summary>
internal static class MatchDataMapper
{
    internal static Match MapMatch(MySqlDataReader r) => new()
    {
        MatchId = r.GetString(0),
        QueueId = r.GetInt32(1),
        GameDurationSec = r.GetInt32(2),
        GameStartTime = r.GetInt64(3),
        PatchVersion = r.GetString(4),
        SeasonCode = r.IsDBNull(5) ? null : r.GetString(5),
        CreatedAt = r.GetDateTimeUtc(6)
    };

    internal static MatchListRawData MapMatchListRaw(MySqlDataReader r) => new(
        MatchId: r.GetString(0),
        QueueId: r.GetInt32(1),
        ChampionId: r.GetInt32(2),
        ChampionName: r.GetString(3),
        Role: r.GetString(4),
        Lane: r.IsDBNull(5) ? null : r.GetString(5),
        Win: r.GetBoolean(6),
        Kills: r.GetInt32(7),
        Deaths: r.GetInt32(8),
        Assists: r.GetInt32(9),
        CreepScore: r.GetInt32(10),
        GoldEarned: r.GetInt32(11),
        GameDurationSec: r.GetInt32(12),
        GameStartTime: r.GetInt64(13),
        DamageDealt: r.GetInt32(14),
        DamageTaken: r.GetInt32(15),
        VisionScore: r.GetInt32(16),
        KillParticipation: r.GetDecimal(17),
        DamageShare: r.GetDecimal(18),
        DeathsPre10: r.GetInt32(19),
        TeamId: r.GetInt32(20),
        TeamKills: r.GetInt32(21),
        EnemyTeamKills: r.GetInt32(22),
        GoldDiffAt15: r.IsDBNull(23) ? null : r.GetInt32(23),
        TeamTotalDamage: r.GetInt32(24),
        EnemyTeamTotalDamage: r.GetInt32(25),
        TeamGoldLeadAt15: r.IsDBNull(26) ? null : r.GetInt32(26),
        TeamDragons: r.GetInt32(27),
        EnemyTeamDragons: r.GetInt32(28),
        TeamBarons: r.GetInt32(29),
        EnemyTeamBarons: r.GetInt32(30),
        TeamTowers: r.GetInt32(31),
        EnemyTeamTowers: r.GetInt32(32)
    );

    internal static MatchListSummaryRawData MapMatchListSummaryRaw(MySqlDataReader r)
    {
        var matchIdOrdinal = r.GetOrdinal("match_id");
        var accountGameNameOrdinal = r.GetOrdinal("account_game_name");
        var accountTagLineOrdinal = r.GetOrdinal("account_tag_line");
        var accountRegionOrdinal = r.GetOrdinal("account_region");
        var queueIdOrdinal = r.GetOrdinal("queue_id");
        var championIdOrdinal = r.GetOrdinal("champion_id");
        var championNameOrdinal = r.GetOrdinal("champion_name");
        var roleOrdinal = r.GetOrdinal("role");
        var laneOrdinal = r.GetOrdinal("lane");
        var winOrdinal = r.GetOrdinal("win");
        var killsOrdinal = r.GetOrdinal("kills");
        var deathsOrdinal = r.GetOrdinal("deaths");
        var assistsOrdinal = r.GetOrdinal("assists");
        var creepScoreOrdinal = r.GetOrdinal("creep_score");
        var goldEarnedOrdinal = r.GetOrdinal("gold_earned");
        var gameDurationSecOrdinal = r.GetOrdinal("game_duration_sec");
        var gameStartTimeOrdinal = r.GetOrdinal("game_start_time");

        return new MatchListSummaryRawData(
            MatchId: r.GetString(matchIdOrdinal),
            AccountGameName: r.IsDBNull(accountGameNameOrdinal) ? null : r.GetString(accountGameNameOrdinal),
            AccountTagLine: r.IsDBNull(accountTagLineOrdinal) ? null : r.GetString(accountTagLineOrdinal),
            AccountRegion: r.IsDBNull(accountRegionOrdinal) ? null : r.GetString(accountRegionOrdinal),
            QueueId: r.GetInt32(queueIdOrdinal),
            ChampionId: r.GetInt32(championIdOrdinal),
            ChampionName: r.GetString(championNameOrdinal),
            Role: r.GetString(roleOrdinal),
            Lane: r.IsDBNull(laneOrdinal) ? null : r.GetString(laneOrdinal),
            Win: r.GetBoolean(winOrdinal),
            Kills: r.GetInt32(killsOrdinal),
            Deaths: r.GetInt32(deathsOrdinal),
            Assists: r.GetInt32(assistsOrdinal),
            CreepScore: r.GetInt32(creepScoreOrdinal),
            GoldEarned: r.GetInt32(goldEarnedOrdinal),
            GameDurationSec: r.GetInt32(gameDurationSecOrdinal),
            GameStartTime: r.GetInt64(gameStartTimeOrdinal)
        );
    }

    internal static MatchDetailsRawData MapMatchDetailsRaw(MySqlDataReader r) => new(
        MatchId: r.GetString(0),
        QueueId: r.GetInt32(1),
        ChampionId: r.GetInt32(2),
        ChampionName: r.GetString(3),
        Role: r.GetString(4),
        Lane: r.IsDBNull(5) ? null : r.GetString(5),
        Win: r.GetBoolean(6),
        Kills: r.GetInt32(7),
        Deaths: r.GetInt32(8),
        Assists: r.GetInt32(9),
        CreepScore: r.GetInt32(10),
        GoldEarned: r.GetInt32(11),
        GameDurationSec: r.GetInt32(12),
        GameStartTime: r.GetInt64(13),
        DamageDealt: r.GetInt32(14),
        DamageTaken: r.GetInt32(15),
        VisionScore: r.GetInt32(16),
        KillParticipation: r.GetDecimal(17),
        DamageShare: r.GetDecimal(18),
        DeathsPre10: r.GetInt32(19),
        TeamId: r.GetInt32(20),
        GoldDiffAt15: r.IsDBNull(21) ? null : r.GetInt32(21),
        TeamKills: r.GetInt32(22),
        EnemyTeamKills: r.GetInt32(23),
        TeamTotalDamage: r.GetInt32(24),
        EnemyTeamTotalDamage: r.GetInt32(25),
        TeamGoldLeadAt15: r.IsDBNull(26) ? null : r.GetInt32(26),
        TeamDragons: r.GetInt32(27),
        EnemyTeamDragons: r.GetInt32(28),
        TeamBarons: r.GetInt32(29),
        EnemyTeamBarons: r.GetInt32(30),
        TeamTowers: r.GetInt32(31),
        EnemyTeamTowers: r.GetInt32(32)
    );

    internal static MatchupParticipantRaw MapMatchupParticipantRaw(MySqlDataReader r) => new(
        ParticipantId: r.GetInt64(0),
        Puuid: r.GetString(1),
        ChampionId: r.GetInt32(2),
        ChampionName: r.GetString(3),
        TeamId: r.GetInt32(4),
        Role: r.GetString(5),
        Win: r.GetBoolean(6),
        Kills: r.GetInt32(7),
        Deaths: r.GetInt32(8),
        Assists: r.GetInt32(9),
        CreepScore: r.GetInt32(10),
        GoldEarned: r.GetInt32(11),
        KillParticipation: r.GetDecimal(12),
        DamageShare: r.GetDecimal(13),
        VisionScore: r.GetInt32(14),
        DeathsPre10: r.GetInt32(15),
        GoldAt10: r.IsDBNull(16) ? null : r.GetInt32(16),
        CsAt10: r.IsDBNull(17) ? null : r.GetInt32(17),
        GoldDiffAt10: r.IsDBNull(18) ? null : r.GetInt32(18),
        CsDiffAt10: r.IsDBNull(19) ? null : r.GetInt32(19)
    );
}
