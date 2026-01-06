namespace RiotProxy.Infrastructure.External.Database.Records;

/// <summary>
/// Record representing team statistics for games played together.
/// </summary>
public record TeamStatsRecord(
    int GamesPlayed,
    int Wins,
    int TotalKills,
    int TotalDeaths,
    int TotalAssists,
    double AvgDurationSeconds,
    string MostCommonGameMode
);

/// <summary>
/// Record representing player pair synergy within a team.
/// </summary>
public record PlayerPairSynergyRecord(
    string PuuId1,
    string PuuId2,
    int GamesPlayed,
    int Wins
);

/// <summary>
/// Record representing a player's role distribution in team games.
/// </summary>
public record TeamPlayerRoleRecord(
    string PuuId,
    string Position,
    int GamesPlayed,
    int Wins
);

/// <summary>
/// Record representing a player's performance in team games.
/// </summary>
public record TeamPlayerPerformanceRecord(
    string PuuId,
    int GamesPlayed,
    int Wins,
    int TotalKills,
    int TotalDeaths,
    int TotalAssists,
    long TotalGoldEarned,
    long TotalCreepScore,
    long TotalDurationSeconds
);

/// <summary>
/// Record for team aggregate kills and deaths.
/// </summary>
public record TeamKillsDeathsRecord(
    int TeamKills,
    int TeamDeaths
);

/// <summary>
/// Record for team match result.
/// </summary>
public record TeamMatchResultRecord(
    string MatchId,
    bool Win,
    DateTime GameEndTimestamp
);

/// <summary>
/// Record for team duration bucket statistics.
/// </summary>
public record TeamDurationRecord(
    string DurationBucket,
    int GamesPlayed,
    int Wins
);

/// <summary>
/// Record for team champion combination.
/// </summary>
public record TeamChampionComboRecord(
    IList<(string Puuid, int ChampionId, string ChampionName, string GameName)> Champions,
    int GamesPlayed,
    int Wins
);

/// <summary>
/// Record for team role pair statistics.
/// </summary>
public record TeamRolePairRecord(
    string Role1,
    string Role2,
    int GamesPlayed,
    int Wins
);

/// <summary>
/// Record for player death timer statistics.
/// </summary>
public record PlayerDeathTimerRecord(
    string PuuId,
    int GamesWon,
    int GamesLost,
    int TotalDeathsInWins,
    int TotalDeathsInLosses,
    int TotalTimeDeadInWins,
    int TotalTimeDeadInLosses
);

/// <summary>
/// Record for team deaths by duration bucket.
/// </summary>
public record TeamDeathsByDurationRecord(
    string DurationBucket,
    int GamesPlayed,
    int Wins,
    int TotalTeamDeaths
);

/// <summary>
/// Record for team match death data.
/// </summary>
public record TeamMatchDeathRecord(
    string MatchId,
    bool Win,
    int TeamDeaths,
    DateTime GameEndTimestamp
);

/// <summary>
/// Record for team match kill data.
/// </summary>
public record TeamMatchKillRecord(
    string MatchId,
    bool Win,
    int TeamKills,
    DateTime GameEndTimestamp
);

/// <summary>
/// Record for team kills by duration bucket.
/// </summary>
public record TeamKillsByDurationRecord(
    string DurationBucket,
    int GamesPlayed,
    int Wins,
    int TotalTeamKills
);

