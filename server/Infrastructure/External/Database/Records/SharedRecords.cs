namespace RiotProxy.Infrastructure.External.Database.Records;

/// <summary>
/// Record representing the latest game details for a player.
/// </summary>
public record LatestGameRecord(
    DateTime GameEndTimestamp,
    bool Win,
    string Role,
    int ChampionId,
    string ChampionName,
    int Kills,
    int Deaths,
    int Assists
);

/// <summary>
/// Record representing a player's performance in a shared game (played together).
/// Includes Puuid to identify which player this record belongs to.
/// </summary>
public record LatestGameTogetherPlayerRecord(
    string Puuid,
    bool Win,
    string Role,
    int ChampionId,
    string ChampionName,
    int Kills,
    int Deaths,
    int Assists
);

/// <summary>
/// Record representing the latest game played together by multiple players.
/// </summary>
public record LatestGameTogetherRecord(
    DateTime GameEndTimestamp,
    bool Win,
    IList<LatestGameTogetherPlayerRecord> Players
);

/// <summary>
/// Record representing per-match performance data for timeline charts.
/// </summary>
public record MatchPerformanceRecord(
    bool Win,
    int GoldEarned,
    int CreepScore,
    double DurationMinutes,
    DateTime GameEndTimestamp
);

/// <summary>
/// Record representing champion statistics for a specific puuid.
/// </summary>
public record ChampionStatsRecord(
    int ChampionId,
    string ChampionName,
    int GamesPlayed,
    int Wins
);

/// <summary>
/// Record representing role/position distribution for a specific puuid.
/// </summary>
public record RoleDistributionRecord(
    string Position,
    int GamesPlayed
);

/// <summary>
/// Record representing match duration bucket statistics.
/// </summary>
public record DurationBucketRecord(
    int MinMinutes,
    int MaxMinutes,
    int GamesPlayed,
    int Wins
);

/// <summary>
/// Record representing champion matchup statistics.
/// </summary>
public record ChampionMatchupRecord(
    int ChampionId,
    string ChampionName,
    string Role,
    int OpponentChampionId,
    string OpponentChampionName,
    int GamesPlayed,
    int Wins
);

/// <summary>
/// Record representing performance statistics for a player (duo or solo).
/// </summary>
public record PlayerPerformanceRecord(
    int GamesPlayed,
    int Wins,
    int TotalKills,
    int TotalDeaths,
    int TotalAssists,
    long TotalGoldEarned,
    long TotalDurationSeconds
);

/// <summary>
/// Record representing champion synergy statistics for duo games.
/// </summary>
public record ChampionSynergyRecord(
    int ChampionId1,
    string ChampionName1,
    int ChampionId2,
    string ChampionName2,
    int GamesPlayed,
    int Wins
);

/// <summary>
/// Record representing side (blue/red) statistics.
/// </summary>
public record SideStatsRecord(
    int BlueGames,
    int BlueWins,
    int RedGames,
    int RedWins
);

/// <summary>
/// Record for multi-kill statistics per player.
/// </summary>
public record PlayerMultiKillRecord(
    string PuuId,
    int DoubleKills,
    int TripleKills,
    int QuadraKills,
    int PentaKills
);

