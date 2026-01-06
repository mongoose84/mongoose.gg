using MySqlConnector;
using RiotProxy.Infrastructure.External.Database.Records;

namespace RiotProxy.Infrastructure.External.Database.Repositories;

/// <summary>
/// Repository for team (3-5 player) game statistics.
/// </summary>
public class TeamStatsRepository
{
    private readonly IDbConnectionFactory _factory;

    public TeamStatsRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Get team statistics for games where all specified players played together.
    /// </summary>
    public async Task<TeamStatsRecord?> GetTeamStatsByPuuIdsAsync(string[] puuIds)
    {
        if (puuIds == null || puuIds.Length < 3)
            return null;

        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        // Build join clauses for all team members
        var joinClauses = new List<string>();
        for (int i = 1; i < puuIds.Length; i++)
        {
            joinClauses.Add($@"
                INNER JOIN LolMatchParticipant p{i}
                    ON p0.MatchId = p{i}.MatchId
                    AND p0.TeamId = p{i}.TeamId
                    AND p{i}.Puuid = @puuid{i}");
        }

        // Sum kills/deaths/assists for all team members in each match
        var sql = $@"
            SELECT
                COUNT(DISTINCT p0.MatchId) as GamesPlayed,
                SUM(CASE WHEN p0.Win = 1 THEN 1 ELSE 0 END) as Wins,
                SUM(p0.Kills + {string.Join(" + ", Enumerable.Range(1, puuIds.Length - 1).Select(i => $"p{i}.Kills"))}) as TotalKills,
                SUM(p0.Deaths + {string.Join(" + ", Enumerable.Range(1, puuIds.Length - 1).Select(i => $"p{i}.Deaths"))}) as TotalDeaths,
                SUM(p0.Assists + {string.Join(" + ", Enumerable.Range(1, puuIds.Length - 1).Select(i => $"p{i}.Assists"))}) as TotalAssists,
                AVG(m.DurationSeconds) as AvgDurationSeconds,
                m.GameMode
            FROM LolMatchParticipant p0
            {string.Join(" ", joinClauses)}
            INNER JOIN LolMatch m ON p0.MatchId = m.MatchId
            WHERE p0.Puuid = @puuid0
              AND m.InfoFetched = TRUE
              AND m.DurationSeconds > 0
            GROUP BY m.GameMode
            ORDER BY GamesPlayed DESC
            LIMIT 1";

        await using var cmd = new MySqlCommand(sql, conn);
        for (int i = 0; i < puuIds.Length; i++)
            cmd.Parameters.AddWithValue($"@puuid{i}", puuIds[i]);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var gamesPlayed = reader.GetInt32("GamesPlayed");
            if (gamesPlayed == 0) return null;

            return new TeamStatsRecord(
                gamesPlayed,
                reader.GetInt32("Wins"),
                reader.GetInt32("TotalKills"),
                reader.GetInt32("TotalDeaths"),
                reader.GetInt32("TotalAssists"),
                reader.GetDouble("AvgDurationSeconds"),
                reader.IsDBNull(reader.GetOrdinal("GameMode")) ? "Unknown" : reader.GetString("GameMode")
            );
        }

        return null;
    }

    /// <summary>
    /// Get side statistics (blue/red) for team games.
    /// </summary>
    public async Task<SideStatsRecord> GetTeamSideStatsByPuuIdsAsync(string[] puuIds)
    {
        if (puuIds == null || puuIds.Length < 3)
            return new SideStatsRecord(0, 0, 0, 0);

        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        var joinClauses = new List<string>();
        for (int i = 1; i < puuIds.Length; i++)
        {
            joinClauses.Add($@"
                INNER JOIN LolMatchParticipant p{i}
                    ON p0.MatchId = p{i}.MatchId
                    AND p0.TeamId = p{i}.TeamId
                    AND p{i}.Puuid = @puuid{i}");
        }

        var sql = $@"
            SELECT
                SUM(CASE WHEN p0.TeamId = 100 THEN 1 ELSE 0 END) as BlueGames,
                SUM(CASE WHEN p0.TeamId = 100 AND p0.Win = 1 THEN 1 ELSE 0 END) as BlueWins,
                SUM(CASE WHEN p0.TeamId = 200 THEN 1 ELSE 0 END) as RedGames,
                SUM(CASE WHEN p0.TeamId = 200 AND p0.Win = 1 THEN 1 ELSE 0 END) as RedWins
            FROM LolMatchParticipant p0
            {string.Join(" ", joinClauses)}
            WHERE p0.Puuid = @puuid0";

        await using var cmd = new MySqlCommand(sql, conn);
        for (int i = 0; i < puuIds.Length; i++)
            cmd.Parameters.AddWithValue($"@puuid{i}", puuIds[i]);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new SideStatsRecord(
                reader.IsDBNull(reader.GetOrdinal("BlueGames")) ? 0 : reader.GetInt32("BlueGames"),
                reader.IsDBNull(reader.GetOrdinal("BlueWins")) ? 0 : reader.GetInt32("BlueWins"),
                reader.IsDBNull(reader.GetOrdinal("RedGames")) ? 0 : reader.GetInt32("RedGames"),
                reader.IsDBNull(reader.GetOrdinal("RedWins")) ? 0 : reader.GetInt32("RedWins")
            );
        }

        return new SideStatsRecord(0, 0, 0, 0);
    }
}

