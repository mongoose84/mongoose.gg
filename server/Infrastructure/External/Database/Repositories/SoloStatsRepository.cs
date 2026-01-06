using MySqlConnector;
using RiotProxy.Infrastructure.External.Database.Records;

namespace RiotProxy.Infrastructure.External.Database.Repositories;

/// <summary>
/// Repository for solo (single player) game statistics.
/// </summary>
public class SoloStatsRepository
{
    private readonly IDbConnectionFactory _factory;

    public SoloStatsRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Get side statistics (blue/red) for a specific puuid.
    /// </summary>
    public async Task<SideStatsRecord> GetSideStatsByPuuIdAsync(string puuId)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT
                SUM(CASE WHEN TeamId = 100 THEN 1 ELSE 0 END) as BlueGames,
                SUM(CASE WHEN TeamId = 100 AND Win = 1 THEN 1 ELSE 0 END) as BlueWins,
                SUM(CASE WHEN TeamId = 200 THEN 1 ELSE 0 END) as RedGames,
                SUM(CASE WHEN TeamId = 200 AND Win = 1 THEN 1 ELSE 0 END) as RedWins
            FROM LolMatchParticipant
            WHERE Puuid = @puuid";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@puuid", puuId);
        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new SideStatsRecord(
                reader.GetInt32("BlueGames"),
                reader.GetInt32("BlueWins"),
                reader.GetInt32("RedGames"),
                reader.GetInt32("RedWins")
            );
        }

        return new SideStatsRecord(0, 0, 0, 0);
    }

    /// <summary>
    /// Get champion statistics grouped by champion for a specific puuid.
    /// </summary>
    public async Task<IList<ChampionStatsRecord>> GetChampionStatsByPuuIdAsync(string puuId)
    {
        var records = new List<ChampionStatsRecord>();
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT
                ChampionId,
                ChampionName,
                COUNT(*) as GamesPlayed,
                SUM(CASE WHEN Win = 1 THEN 1 ELSE 0 END) as Wins
            FROM LolMatchParticipant
            WHERE Puuid = @puuid
            GROUP BY ChampionId, ChampionName
            ORDER BY GamesPlayed DESC";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@puuid", puuId);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            records.Add(new ChampionStatsRecord(
                reader.GetInt32("ChampionId"),
                reader.GetString("ChampionName"),
                reader.GetInt32("GamesPlayed"),
                reader.GetInt32("Wins")
            ));
        }

        return records;
    }

    /// <summary>
    /// Get role/position distribution for a specific puuid.
    /// </summary>
    public async Task<IList<RoleDistributionRecord>> GetRoleDistributionByPuuIdAsync(string puuId)
    {
        var records = new List<RoleDistributionRecord>();
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT
                COALESCE(NULLIF(TeamPosition, ''), 'UNKNOWN') as Position,
                COUNT(*) as GamesPlayed
            FROM LolMatchParticipant
            WHERE Puuid = @puuid
            GROUP BY Position
            ORDER BY GamesPlayed DESC";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@puuid", puuId);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            records.Add(new RoleDistributionRecord(
                reader.GetString("Position"),
                reader.GetInt32("GamesPlayed")
            ));
        }

        return records;
    }

    /// <summary>
    /// Get match duration statistics grouped by duration buckets for a specific puuid.
    /// </summary>
    public async Task<IList<DurationBucketRecord>> GetDurationStatsByPuuIdAsync(string puuId)
    {
        var records = new List<DurationBucketRecord>();
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

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
            GROUP BY MinMinutes
            ORDER BY MinMinutes ASC";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@puuid", puuId);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var minMinutes = reader.GetInt32("MinMinutes");
            records.Add(new DurationBucketRecord(minMinutes, minMinutes + 5, reader.GetInt32("GamesPlayed"), reader.GetInt32("Wins")));
        }

        return records;
    }
}

