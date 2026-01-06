using MySqlConnector;
using RiotProxy.Infrastructure.External.Database.Records;

namespace RiotProxy.Infrastructure.External.Database.Repositories;

/// <summary>
/// Repository for duo (2-player) game statistics.
/// </summary>
public class DuoStatsRepository
{
    private readonly IDbConnectionFactory _factory;

    public DuoStatsRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Get statistics for games where two players played together (on the same team).
    /// </summary>
    public async Task<DuoStatsRecord?> GetDuoStatsByPuuIdsAsync(string puuId1, string puuId2)
    {
        if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2))
            return null;

        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT
                COUNT(DISTINCT p1.MatchId) as GamesPlayed,
                SUM(CASE WHEN p1.Win = 1 THEN 1 ELSE 0 END) as Wins,
                m.GameMode
            FROM LolMatchParticipant p1
            INNER JOIN LolMatchParticipant p2
                ON p1.MatchId = p2.MatchId
                AND p1.TeamId = p2.TeamId
                AND p1.Puuid != p2.Puuid
            INNER JOIN LolMatch m ON p1.MatchId = m.MatchId
            WHERE p1.Puuid = @puuid1
              AND p2.Puuid = @puuid2
              AND m.InfoFetched = TRUE
            GROUP BY m.GameMode
            ORDER BY GamesPlayed DESC
            LIMIT 1";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@puuid1", puuId1);
        cmd.Parameters.AddWithValue("@puuid2", puuId2);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new DuoStatsRecord(
                reader.GetInt32("GamesPlayed"),
                reader.GetInt32("Wins"),
                reader.IsDBNull(reader.GetOrdinal("GameMode")) ? "Unknown" : reader.GetString("GameMode")
            );
        }

        return null;
    }

    /// <summary>
    /// Get side statistics (blue/red) for duo games.
    /// </summary>
    public async Task<SideStatsRecord> GetDuoSideStatsByPuuIdsAsync(string puuId1, string puuId2)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT
                SUM(CASE WHEN p1.TeamId = 100 THEN 1 ELSE 0 END) as BlueGames,
                SUM(CASE WHEN p1.TeamId = 100 AND p1.Win = 1 THEN 1 ELSE 0 END) as BlueWins,
                SUM(CASE WHEN p1.TeamId = 200 THEN 1 ELSE 0 END) as RedGames,
                SUM(CASE WHEN p1.TeamId = 200 AND p1.Win = 1 THEN 1 ELSE 0 END) as RedWins
            FROM LolMatchParticipant p1
            INNER JOIN LolMatchParticipant p2
                ON p1.MatchId = p2.MatchId
                AND p1.TeamId = p2.TeamId
                AND p1.Puuid != p2.Puuid
            WHERE p1.Puuid = @puuid1
              AND p2.Puuid = @puuid2";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@puuid1", puuId1);
        cmd.Parameters.AddWithValue("@puuid2", puuId2);
        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new SideStatsRecord(
                BlueGames: reader.IsDBNull(reader.GetOrdinal("BlueGames")) ? 0 : reader.GetInt32("BlueGames"),
                BlueWins: reader.IsDBNull(reader.GetOrdinal("BlueWins")) ? 0 : reader.GetInt32("BlueWins"),
                RedGames: reader.IsDBNull(reader.GetOrdinal("RedGames")) ? 0 : reader.GetInt32("RedGames"),
                RedWins: reader.IsDBNull(reader.GetOrdinal("RedWins")) ? 0 : reader.GetInt32("RedWins")
            );
        }

        return new SideStatsRecord(0, 0, 0, 0);
    }

    /// <summary>
    /// Get champion synergy statistics for duo games.
    /// </summary>
    public async Task<IList<ChampionSynergyRecord>> GetChampionSynergyByPuuIdsAsync(string puuId1, string puuId2, string? gameMode = null)
    {
        if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2))
            return new List<ChampionSynergyRecord>();

        var records = new List<ChampionSynergyRecord>();
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        var sql = @"
            SELECT
                p1.ChampionId as ChampionId1,
                p1.ChampionName as ChampionName1,
                p2.ChampionId as ChampionId2,
                p2.ChampionName as ChampionName2,
                COUNT(DISTINCT p1.MatchId) as GamesPlayed,
                SUM(CASE WHEN p1.Win = 1 THEN 1 ELSE 0 END) as Wins
            FROM LolMatchParticipant p1
            INNER JOIN LolMatchParticipant p2
                ON p1.MatchId = p2.MatchId
                AND p1.TeamId = p2.TeamId
                AND p1.Puuid != p2.Puuid
            INNER JOIN LolMatch m ON p1.MatchId = m.MatchId
            WHERE p1.Puuid = @puuid1
              AND p2.Puuid = @puuid2
              AND m.InfoFetched = TRUE";

        if (!string.IsNullOrWhiteSpace(gameMode))
            sql += " AND m.GameMode = @gameMode";

        sql += @"
            GROUP BY p1.ChampionId, p1.ChampionName, p2.ChampionId, p2.ChampionName
            ORDER BY GamesPlayed DESC";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@puuid1", puuId1);
        cmd.Parameters.AddWithValue("@puuid2", puuId2);
        if (!string.IsNullOrWhiteSpace(gameMode))
            cmd.Parameters.AddWithValue("@gameMode", gameMode);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            records.Add(new ChampionSynergyRecord(
                reader.GetInt32("ChampionId1"),
                reader.GetString("ChampionName1"),
                reader.GetInt32("ChampionId2"),
                reader.GetString("ChampionName2"),
                reader.GetInt32("GamesPlayed"),
                reader.GetInt32("Wins")
            ));
        }

        return records;
    }

    /// <summary>
    /// Get multi-kill statistics for duo games.
    /// </summary>
    public async Task<IList<DuoMultiKillRecord>> GetDuoMultiKillsAsync(string puuId1, string puuId2, string? gameMode = null)
    {
        if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2))
            return new List<DuoMultiKillRecord>();

        var records = new List<DuoMultiKillRecord>();
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        foreach (var puuId in new[] { puuId1, puuId2 })
        {
            var sql = @"
                SELECT
                    SUM(p.DoubleKills) as DoubleKills,
                    SUM(p.TripleKills) as TripleKills,
                    SUM(p.QuadraKills) as QuadraKills,
                    SUM(p.PentaKills) as PentaKills
                FROM LolMatchParticipant p
                INNER JOIN LolMatchParticipant p2 ON p.MatchId = p2.MatchId AND p.TeamId = p2.TeamId AND p.Puuid != p2.Puuid
                INNER JOIN LolMatch m ON p.MatchId = m.MatchId
                WHERE p.Puuid = @puuid
                  AND p2.Puuid = @otherPuuid
                  AND m.InfoFetched = TRUE
                  AND m.DurationSeconds > 0";

            if (!string.IsNullOrWhiteSpace(gameMode))
                sql += " AND m.GameMode = @gameMode";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuId);
            cmd.Parameters.AddWithValue("@otherPuuid", puuId == puuId1 ? puuId2 : puuId1);
            if (!string.IsNullOrWhiteSpace(gameMode))
                cmd.Parameters.AddWithValue("@gameMode", gameMode);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                records.Add(new DuoMultiKillRecord(
                    PuuId: puuId,
                    DoubleKills: reader.IsDBNull(reader.GetOrdinal("DoubleKills")) ? 0 : reader.GetInt32("DoubleKills"),
                    TripleKills: reader.IsDBNull(reader.GetOrdinal("TripleKills")) ? 0 : reader.GetInt32("TripleKills"),
                    QuadraKills: reader.IsDBNull(reader.GetOrdinal("QuadraKills")) ? 0 : reader.GetInt32("QuadraKills"),
                    PentaKills: reader.IsDBNull(reader.GetOrdinal("PentaKills")) ? 0 : reader.GetInt32("PentaKills")
                ));
            }
        }

        return records;
    }

    /// <summary>
    /// Get win rate trend for duo games.
    /// </summary>
    public async Task<IList<DuoWinRateTrendRecord>> GetDuoWinRateTrendAsync(string puuId1, string puuId2, string? gameMode = null, int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2))
            return new List<DuoWinRateTrendRecord>();

        var records = new List<DuoWinRateTrendRecord>();
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        var sql = @"
            SELECT
                p1.MatchId,
                p1.Win,
                m.GameEndTimestamp as GameDate
            FROM LolMatchParticipant p1
            INNER JOIN LolMatchParticipant p2
                ON p1.MatchId = p2.MatchId
                AND p1.TeamId = p2.TeamId
                AND p1.Puuid != p2.Puuid
            INNER JOIN LolMatch m ON p1.MatchId = m.MatchId
            WHERE p1.Puuid = @puuid1
              AND p2.Puuid = @puuid2
              AND m.InfoFetched = TRUE";

        if (!string.IsNullOrWhiteSpace(gameMode))
            sql += " AND m.GameMode = @gameMode";

        sql += " ORDER BY m.GameEndTimestamp DESC LIMIT @limit";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@puuid1", puuId1);
        cmd.Parameters.AddWithValue("@puuid2", puuId2);
        cmd.Parameters.AddWithValue("@limit", limit);
        if (!string.IsNullOrWhiteSpace(gameMode))
            cmd.Parameters.AddWithValue("@gameMode", gameMode);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            records.Add(new DuoWinRateTrendRecord(
                reader.GetString("MatchId"),
                reader.GetBoolean("Win"),
                reader.IsDBNull(reader.GetOrdinal("GameDate")) ? DateTime.MinValue : reader.GetDateTime("GameDate")
            ));
        }

        records.Reverse(); // Return oldest first
        return records;
    }

    /// <summary>
    /// Get streak data for duo games.
    /// </summary>
    public async Task<DuoStreakRecord?> GetDuoStreakAsync(string puuId1, string puuId2, string? gameMode = null)
    {
        if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2))
            return null;

        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        var sql = @"
            SELECT
                p1.Win
            FROM LolMatchParticipant p1
            INNER JOIN LolMatchParticipant p2
                ON p1.MatchId = p2.MatchId
                AND p1.TeamId = p2.TeamId
                AND p1.Puuid != p2.Puuid
            INNER JOIN LolMatch m ON p1.MatchId = m.MatchId
            WHERE p1.Puuid = @puuid1
              AND p2.Puuid = @puuid2
              AND m.InfoFetched = TRUE";

        if (!string.IsNullOrWhiteSpace(gameMode))
            sql += " AND m.GameMode = @gameMode";

        sql += " ORDER BY m.GameEndTimestamp DESC";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@puuid1", puuId1);
        cmd.Parameters.AddWithValue("@puuid2", puuId2);
        if (!string.IsNullOrWhiteSpace(gameMode))
            cmd.Parameters.AddWithValue("@gameMode", gameMode);

        await using var reader = await cmd.ExecuteReaderAsync();

        var wins = new List<bool>();
        while (await reader.ReadAsync())
            wins.Add(reader.GetBoolean("Win"));

        if (wins.Count == 0) return null;

        // Calculate current streak
        var currentStreak = 1;
        var isWinStreak = wins[0];
        for (int i = 1; i < wins.Count && wins[i] == isWinStreak; i++)
            currentStreak++;

        // Calculate longest streaks
        var longestWin = 0;
        var longestLoss = 0;
        var streak = 1;
        for (int i = 1; i < wins.Count; i++)
        {
            if (wins[i] == wins[i - 1])
            {
                streak++;
            }
            else
            {
                if (wins[i - 1]) longestWin = Math.Max(longestWin, streak);
                else longestLoss = Math.Max(longestLoss, streak);
                streak = 1;
            }
        }
        if (wins.Count > 0)
        {
            if (wins[^1]) longestWin = Math.Max(longestWin, streak);
            else longestLoss = Math.Max(longestLoss, streak);
        }

        return new DuoStreakRecord(currentStreak, isWinStreak, longestWin, longestLoss);
    }
}

