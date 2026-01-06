namespace RiotProxy.Infrastructure.External.Database.Records;

/// <summary>
/// Record representing duo statistics for games played together.
/// </summary>
public record DuoStatsRecord(
    int GamesPlayed,
    int Wins,
    string? MostCommonQueueType
);

/// <summary>
/// Record representing duo champion combination vs enemy champion statistics.
/// </summary>
public record DuoVsEnemyRecord(
    int DuoChampionId1,
    string DuoChampionName1,
    string DuoLane1,
    int DuoChampionId2,
    string DuoChampionName2,
    string DuoLane2,
    string EnemyLane,
    int EnemyChampionId,
    string EnemyChampionName,
    int GamesPlayed,
    int Wins
);

/// <summary>
/// Record representing lane combination statistics for duo games.
/// </summary>
public record DuoLaneComboRecord(
    string Lane1,
    string Lane2,
    int GamesPlayed,
    int Wins
);

/// <summary>
/// Record representing kill efficiency for a player in duo games.
/// </summary>
public record DuoKillEfficiencyRecord(
    string PuuId,
    int TotalKills,
    int TotalAssists,
    int TeamKills,
    int DeathsInLosses,
    int TeamDeathsInLosses
);

/// <summary>
/// Record for duo multi-kill statistics per player.
/// </summary>
public record DuoMultiKillRecord(
    string PuuId,
    int DoubleKills,
    int TripleKills,
    int QuadraKills,
    int PentaKills
);

/// <summary>
/// Record for duo kills by duration bucket.
/// </summary>
public record DuoKillsByDurationRecord(
    string DurationBucket,
    int GamesPlayed,
    int TotalTeamKills,
    int Wins
);

/// <summary>
/// Record for duo kill participation per player.
/// </summary>
public record DuoKillParticipationRecord(
    string PuuId,
    int TotalKills,
    int TotalAssists,
    int TotalTeamKills,
    int GamesPlayed
);

/// <summary>
/// Record for duo kills trend data point.
/// </summary>
public record DuoKillsTrendRecord(
    string MatchId,
    int TeamKills,
    bool Win,
    DateTime GameDate
);

/// <summary>
/// Record for duo death timer stats per player.
/// </summary>
public record DuoDeathTimerRecord(
    string PuuId,
    double AvgTimeDeadWins,
    double AvgTimeDeadLosses,
    int WinGames,
    int LossGames
);

/// <summary>
/// Record for duo deaths by duration bucket.
/// </summary>
public record DuoDeathsByDurationRecord(
    string DurationBucket,
    int GamesPlayed,
    int TotalTeamDeaths,
    int Wins
);

/// <summary>
/// Record for duo death share per player.
/// </summary>
public record DuoDeathShareRecord(
    string PuuId,
    int TotalDeaths,
    int GamesPlayed
);

/// <summary>
/// Record for duo deaths trend data point.
/// </summary>
public record DuoDeathsTrendRecord(
    string MatchId,
    int TeamDeaths,
    bool Win,
    DateTime GameDate
);

/// <summary>
/// Record for duo win rate trend data point.
/// </summary>
public record DuoWinRateTrendRecord(
    string MatchId,
    bool Win,
    DateTime GameDate
);

/// <summary>
/// Record for duo performance radar metrics.
/// </summary>
public record DuoPerformanceRadarRecord(
    double AvgKills,
    double AvgDeaths,
    double AvgAssists,
    double AvgCs,
    double AvgGoldEarned,
    int GamesPlayed,
    int Wins
);

/// <summary>
/// Record for duo streak data.
/// </summary>
public record DuoStreakRecord(
    int CurrentStreak,
    bool IsWinStreak,
    int LongestWinStreak,
    int LongestLossStreak
);

