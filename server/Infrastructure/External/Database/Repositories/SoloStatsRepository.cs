using RiotProxy.Infrastructure.External.Database.Records;

namespace RiotProxy.Infrastructure.External.Database.Repositories;

/// <summary>
/// Repository for solo (single player) game statistics.
/// </summary>
public class SoloStatsRepository : RepositoryBase
{
    public SoloStatsRepository(IDbConnectionFactory factory) : base(factory)
    {
    }

    /// <summary>
    /// Get side statistics (blue/red) for a specific puuid.
    /// Excludes ARAM games.
    /// </summary>
    public async Task<SideStatsRecord> GetSideStatsByPuuIdAsync(string puuId)
    {
        const string sql = @"
            SELECT
                SUM(CASE WHEN p.TeamId = 100 THEN 1 ELSE 0 END) as BlueGames,
                SUM(CASE WHEN p.TeamId = 100 AND p.Win = 1 THEN 1 ELSE 0 END) as BlueWins,
                SUM(CASE WHEN p.TeamId = 200 THEN 1 ELSE 0 END) as RedGames,
                SUM(CASE WHEN p.TeamId = 200 AND p.Win = 1 THEN 1 ELSE 0 END) as RedWins
            FROM LolMatchParticipant p
            INNER JOIN LolMatch m ON p.MatchId = m.MatchId
            WHERE p.Puuid = @puuid
              AND m.GameMode != 'ARAM'";

        return await ExecuteSingleAsync(sql, r => new SideStatsRecord(
            r.GetInt32("BlueGames"),
            r.GetInt32("BlueWins"),
            r.GetInt32("RedGames"),
            r.GetInt32("RedWins")
        ), ("@puuid", puuId)) ?? new SideStatsRecord(0, 0, 0, 0);
    }

    /// <summary>
    /// Get champion statistics grouped by champion for a specific puuid.
    /// Excludes ARAM games.
    /// </summary>
    public async Task<IList<ChampionStatsRecord>> GetChampionStatsByPuuIdAsync(string puuId)
    {
        const string sql = @"
            SELECT
                p.ChampionId,
                p.ChampionName,
                COUNT(*) as GamesPlayed,
                SUM(CASE WHEN p.Win = 1 THEN 1 ELSE 0 END) as Wins
            FROM LolMatchParticipant p
            INNER JOIN LolMatch m ON p.MatchId = m.MatchId
            WHERE p.Puuid = @puuid
              AND m.GameMode != 'ARAM'
            GROUP BY p.ChampionId, p.ChampionName
            ORDER BY GamesPlayed DESC";

        return await ExecuteListAsync(sql, r => new ChampionStatsRecord(
            r.GetInt32("ChampionId"),
            r.GetString("ChampionName"),
            r.GetInt32("GamesPlayed"),
            r.GetInt32("Wins")
        ), ("@puuid", puuId));
    }

    /// <summary>
    /// Get role/position distribution for a specific puuid.
    /// Excludes ARAM games.
    /// </summary>
    public async Task<IList<RoleDistributionRecord>> GetRoleDistributionByPuuIdAsync(string puuId)
    {
        const string sql = @"
            SELECT
                COALESCE(NULLIF(p.TeamPosition, ''), 'UNKNOWN') as Position,
                COUNT(*) as GamesPlayed
            FROM LolMatchParticipant p
            INNER JOIN LolMatch m ON p.MatchId = m.MatchId
            WHERE p.Puuid = @puuid
              AND m.GameMode != 'ARAM'
            GROUP BY Position
            ORDER BY GamesPlayed DESC";

        return await ExecuteListAsync(sql, r => new RoleDistributionRecord(
            r.GetString("Position"),
            r.GetInt32("GamesPlayed")
        ), ("@puuid", puuId));
    }

    /// <summary>
    /// Get match duration statistics grouped by duration buckets for a specific puuid.
    /// Excludes ARAM games.
    /// </summary>
    public async Task<IList<DurationBucketRecord>> GetDurationStatsByPuuIdAsync(string puuId)
    {
        const string sql = @"
            SELECT
                FLOOR(m.DurationSeconds / 300) * 5 as MinMinutes,
                COUNT(*) as GamesPlayed,
                SUM(CASE WHEN p.Win = 1 THEN 1 ELSE 0 END) as Wins
            FROM LolMatchParticipant p
            INNER JOIN LolMatch m ON p.MatchId = m.MatchId
            WHERE p.Puuid = @puuid
              AND m.InfoFetched = TRUE
              AND m.DurationSeconds > 0
              AND m.GameMode != 'ARAM'
            GROUP BY MinMinutes
            ORDER BY MinMinutes ASC";

        return await ExecuteListAsync(sql, r =>
        {
            var minMinutes = r.GetInt32("MinMinutes");
            return new DurationBucketRecord(minMinutes, minMinutes + 5, r.GetInt32("GamesPlayed"), r.GetInt32("Wins"));
        }, ("@puuid", puuId));
    }
}

