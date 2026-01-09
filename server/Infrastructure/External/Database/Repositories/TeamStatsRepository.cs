using MySqlConnector;
using RiotProxy.Infrastructure.External.Database.Records;

namespace RiotProxy.Infrastructure.External.Database.Repositories;

/// <summary>
/// Repository for team (3-5 player) game statistics.
/// </summary>
public class TeamStatsRepository : RepositoryBase
{
    public TeamStatsRepository(IDbConnectionFactory factory) : base(factory)
    {
    }

    /// <summary>
    /// Get team statistics for games where all specified players played together.
    /// Excludes ARAM games.
    /// </summary>
    public async Task<TeamStatsRecord?> GetTeamStatsByPuuIdsAsync(string[] puuIds)
    {
        if (puuIds == null || puuIds.Length < 3)
            return null;

        return await ExecuteWithConnectionAsync(async (conn, cmd) =>
        {
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
            cmd.CommandText = $@"
                SELECT
                    COUNT(DISTINCT p0.MatchId) as GamesPlayed,
                    COUNT(DISTINCT CASE WHEN p0.Win = 1 THEN p0.MatchId ELSE NULL END) as Wins,
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
                  AND m.GameMode != 'ARAM'
                GROUP BY m.GameMode
                ORDER BY GamesPlayed DESC
                LIMIT 1";

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
        });
    }

    /// <summary>
    /// Get side statistics (blue/red) for team games.
    /// Excludes ARAM games.
    /// </summary>
    public async Task<SideStatsRecord> GetTeamSideStatsByPuuIdsAsync(string[] puuIds)
    {
        if (puuIds == null || puuIds.Length < 3)
            return new SideStatsRecord(0, 0, 0, 0);

        return await ExecuteWithConnectionAsync(async (conn, cmd) =>
        {
            var joinClauses = new List<string>();
            for (int i = 1; i < puuIds.Length; i++)
            {
                joinClauses.Add($@"
                    INNER JOIN LolMatchParticipant p{i}
                        ON p0.MatchId = p{i}.MatchId
                        AND p0.TeamId = p{i}.TeamId
                        AND p{i}.Puuid = @puuid{i}");
            }

            cmd.CommandText = $@"
                SELECT
                    SUM(CASE WHEN t1.TeamId = 100 THEN 1 ELSE 0 END) as BlueGames,
                    SUM(CASE WHEN t1.TeamId = 100 AND t1.Win = 1 THEN 1 ELSE 0 END) as BlueWins,
                    SUM(CASE WHEN t1.TeamId = 200 THEN 1 ELSE 0 END) as RedGames,
                    SUM(CASE WHEN t1.TeamId = 200 AND t1.Win = 1 THEN 1 ELSE 0 END) as RedWins
                FROM (
                    SELECT DISTINCT
                        p0.MatchId,
                        p0.TeamId,
                        p0.Win
                    FROM LolMatchParticipant p0
                    {string.Join(" ", joinClauses)}
                    INNER JOIN LolMatch m ON p0.MatchId = m.MatchId
                    WHERE p0.Puuid = @puuid0
                      AND m.InfoFetched = TRUE
                      AND m.DurationSeconds > 0
                      AND m.GameMode != 'ARAM'
                ) as t1";

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
        });
    }

    /// <summary>
    /// Get pairwise synergy statistics for all player pairs in team games.
    /// </summary>
    public async Task<IList<PlayerPairSynergyRecord>> GetTeamPairSynergyByPuuIdsAsync(string[] puuIds, string? gameMode = null)
    {
        if (puuIds == null || puuIds.Length < 3)
            return new List<PlayerPairSynergyRecord>();

        return await ExecuteWithConnectionAsync(async (conn, cmd) =>
        {
            var records = new List<PlayerPairSynergyRecord>();

            for (int i = 0; i < puuIds.Length; i++)
            {
                for (int j = i + 1; j < puuIds.Length; j++)
                {
                    var sql = @"
                        SELECT
                            COUNT(DISTINCT p1.MatchId) as GamesPlayed,
                            COUNT(DISTINCT CASE WHEN p1.Win = 1 THEN p1.MatchId ELSE NULL END) as Wins
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
                    else
                        sql += " AND m.GameMode != 'ARAM'";

                    cmd.CommandText = sql;
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@puuid1", puuIds[i]);
                    cmd.Parameters.AddWithValue("@puuid2", puuIds[j]);
                    if (!string.IsNullOrWhiteSpace(gameMode))
                        cmd.Parameters.AddWithValue("@gameMode", gameMode);

                    await using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        var gamesPlayed = reader.GetInt32("GamesPlayed");
                        if (gamesPlayed > 0)
                        {
                            records.Add(new PlayerPairSynergyRecord(
                                PuuId1: puuIds[i],
                                PuuId2: puuIds[j],
                                GamesPlayed: gamesPlayed,
                                Wins: reader.GetInt32("Wins")
                            ));
                        }
                    }
                }
            }

            return records;
        });
    }

    /// <summary>
    /// Get role distribution for each player in team games.
    /// </summary>
    public async Task<IList<TeamPlayerRoleRecord>> GetTeamRoleDistributionByPuuIdsAsync(string[] puuIds, string? gameMode = null)
    {
        if (puuIds == null || puuIds.Length < 3)
            return new List<TeamPlayerRoleRecord>();

        return await ExecuteWithConnectionAsync(async (conn, cmd) =>
        {
            var records = new List<TeamPlayerRoleRecord>();

            var joinClauses = new List<string>();
            for (int i = 1; i < puuIds.Length; i++)
            {
                joinClauses.Add($@"
                    INNER JOIN LolMatchParticipant tp{i}
                        ON tp0.MatchId = tp{i}.MatchId
                        AND tp0.TeamId = tp{i}.TeamId
                        AND tp{i}.Puuid = @teamPuuid{i}");
            }

            var teamMatchSubquery = $@"
                SELECT DISTINCT tp0.MatchId
                FROM LolMatchParticipant tp0
                {string.Join("", joinClauses)}
                INNER JOIN LolMatch m ON tp0.MatchId = m.MatchId
                WHERE tp0.Puuid = @teamPuuid0
                  AND m.InfoFetched = TRUE";

            if (!string.IsNullOrWhiteSpace(gameMode))
                teamMatchSubquery += " AND m.GameMode = @gameMode";
            else
                teamMatchSubquery += " AND m.GameMode != 'ARAM'";

            foreach (var puuId in puuIds)
            {
                cmd.CommandText = $@"
                    SELECT
                        p.TeamPosition as Position,
                        COUNT(*) as GamesPlayed,
                        SUM(CASE WHEN p.Win = 1 THEN 1 ELSE 0 END) as Wins
                    FROM LolMatchParticipant p
                    WHERE p.Puuid = @puuid
                      AND p.MatchId IN ({teamMatchSubquery})
                      AND p.TeamPosition != ''
                    GROUP BY p.TeamPosition
                    ORDER BY GamesPlayed DESC";

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@puuid", puuId);
                for (int i = 0; i < puuIds.Length; i++)
                    cmd.Parameters.AddWithValue($"@teamPuuid{i}", puuIds[i]);
                if (!string.IsNullOrWhiteSpace(gameMode))
                    cmd.Parameters.AddWithValue("@gameMode", gameMode);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    records.Add(new TeamPlayerRoleRecord(
                        PuuId: puuId,
                        Position: reader.GetString("Position"),
                        GamesPlayed: reader.GetInt32("GamesPlayed"),
                        Wins: reader.GetInt32("Wins")
                    ));
                }
            }

            return records;
        });
    }

    /// <summary>
    /// Get individual performance for each player in team games.
    /// </summary>
    public async Task<IList<TeamPlayerPerformanceRecord>> GetTeamPlayerPerformanceByPuuIdsAsync(string[] puuIds, string? gameMode = null)
    {
        if (puuIds == null || puuIds.Length < 3)
            return new List<TeamPlayerPerformanceRecord>();

        return await ExecuteWithConnectionAsync(async (conn, cmd) =>
        {
            var records = new List<TeamPlayerPerformanceRecord>();

            var joinClauses = new List<string>();
            for (int i = 1; i < puuIds.Length; i++)
            {
                joinClauses.Add($@"
                    INNER JOIN LolMatchParticipant tp{i}
                        ON tp0.MatchId = tp{i}.MatchId
                        AND tp0.TeamId = tp{i}.TeamId
                        AND tp{i}.Puuid = @teamPuuid{i}");
            }

            var teamMatchSubquery = $@"
                SELECT DISTINCT tp0.MatchId
                FROM LolMatchParticipant tp0
                {string.Join("", joinClauses)}
                INNER JOIN LolMatch m ON tp0.MatchId = m.MatchId
                WHERE tp0.Puuid = @teamPuuid0
                  AND m.InfoFetched = TRUE
                  AND m.DurationSeconds > 0";

            if (!string.IsNullOrWhiteSpace(gameMode))
                teamMatchSubquery += " AND m.GameMode = @gameMode";
            else
                teamMatchSubquery += " AND m.GameMode != 'ARAM'";

            foreach (var puuId in puuIds)
            {
                cmd.CommandText = $@"
                    SELECT
                        COUNT(*) as GamesPlayed,
                        SUM(CASE WHEN p.Win = 1 THEN 1 ELSE 0 END) as Wins,
                        SUM(p.Kills) as TotalKills,
                        SUM(p.Deaths) as TotalDeaths,
                        SUM(p.Assists) as TotalAssists,
                        SUM(p.GoldEarned) as TotalGoldEarned,
                        SUM(p.CreepScore) as TotalCreepScore,
                        SUM(m.DurationSeconds) as TotalDurationSeconds
                    FROM LolMatchParticipant p
                    INNER JOIN LolMatch m ON p.MatchId = m.MatchId
                    WHERE p.Puuid = @puuid
                      AND p.MatchId IN ({teamMatchSubquery})";

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@puuid", puuId);
                for (int i = 0; i < puuIds.Length; i++)
                    cmd.Parameters.AddWithValue($"@teamPuuid{i}", puuIds[i]);
                if (!string.IsNullOrWhiteSpace(gameMode))
                    cmd.Parameters.AddWithValue("@gameMode", gameMode);

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var gamesPlayed = reader.GetInt32("GamesPlayed");
                    if (gamesPlayed > 0)
                    {
                        records.Add(new TeamPlayerPerformanceRecord(
                            PuuId: puuId,
                            GamesPlayed: gamesPlayed,
                            Wins: reader.GetInt32("Wins"),
                            TotalKills: reader.GetInt32("TotalKills"),
                            TotalDeaths: reader.GetInt32("TotalDeaths"),
                            TotalAssists: reader.GetInt32("TotalAssists"),
                            TotalGoldEarned: reader.GetInt64("TotalGoldEarned"),
                            TotalCreepScore: reader.GetInt64("TotalCreepScore"),
                            TotalDurationSeconds: reader.GetInt64("TotalDurationSeconds")
                        ));
                    }
                }
            }

            return records;
        });
    }

    /// <summary>
    /// Get team kills and deaths for kill participation calculation.
    /// </summary>
    public async Task<TeamKillsDeathsRecord?> GetTeamKillsDeathsByPuuIdsAsync(string[] puuIds, string? gameMode = null)
    {
        if (puuIds == null || puuIds.Length < 3)
            return null;

        return await ExecuteWithConnectionAsync(async (conn, cmd) =>
        {
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
                    SUM(p0.Kills + {string.Join(" + ", Enumerable.Range(1, puuIds.Length - 1).Select(i => $"p{i}.Kills"))}) as TeamKills,
                    SUM(p0.Deaths + {string.Join(" + ", Enumerable.Range(1, puuIds.Length - 1).Select(i => $"p{i}.Deaths"))}) as TeamDeaths
                FROM LolMatchParticipant p0
                {string.Join("", joinClauses)}
                INNER JOIN LolMatch m ON p0.MatchId = m.MatchId
                WHERE p0.Puuid = @puuid0
                  AND m.InfoFetched = TRUE
                  AND m.DurationSeconds > 0";

            if (!string.IsNullOrWhiteSpace(gameMode))
                sql += " AND m.GameMode = @gameMode";
            else
                sql += " AND m.GameMode != 'ARAM'";

            cmd.CommandText = sql;
            for (int i = 0; i < puuIds.Length; i++)
                cmd.Parameters.AddWithValue($"@puuid{i}", puuIds[i]);
            if (!string.IsNullOrWhiteSpace(gameMode))
                cmd.Parameters.AddWithValue("@gameMode", gameMode);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new TeamKillsDeathsRecord(
                    TeamKills: reader.IsDBNull(reader.GetOrdinal("TeamKills")) ? 0 : reader.GetInt32("TeamKills"),
                    TeamDeaths: reader.IsDBNull(reader.GetOrdinal("TeamDeaths")) ? 0 : reader.GetInt32("TeamDeaths")
                );
            }

            return null;
        });
    }

    /// <summary>
    /// Get team match results for win rate trend analysis.
    /// </summary>
    public async Task<IList<TeamMatchResultRecord>> GetTeamMatchResultsByPuuIdsAsync(string[] puuIds, int limit = 50, string? gameMode = null)
    {
        if (puuIds == null || puuIds.Length < 3)
            return new List<TeamMatchResultRecord>();

        return await ExecuteWithConnectionAsync(async (conn, cmd) =>
        {
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
                SELECT DISTINCT
                    m.MatchId,
                    p0.Win,
                    m.GameEndTimestamp
                FROM LolMatchParticipant p0
                {string.Join("", joinClauses)}
                INNER JOIN LolMatch m ON p0.MatchId = m.MatchId
                WHERE p0.Puuid = @puuid0
                  AND m.InfoFetched = TRUE
                  AND m.DurationSeconds > 0";

            if (!string.IsNullOrWhiteSpace(gameMode))
                sql += " AND m.GameMode = @gameMode";
            else
                sql += " AND m.GameMode != 'ARAM'";

            sql += " ORDER BY m.GameEndTimestamp DESC LIMIT @limit";

            cmd.CommandText = sql;
            for (int i = 0; i < puuIds.Length; i++)
                cmd.Parameters.AddWithValue($"@puuid{i}", puuIds[i]);
            cmd.Parameters.AddWithValue("@limit", limit);
            if (!string.IsNullOrWhiteSpace(gameMode))
                cmd.Parameters.AddWithValue("@gameMode", gameMode);

            var results = new List<TeamMatchResultRecord>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new TeamMatchResultRecord(
                    MatchId: reader.GetString("MatchId"),
                    Win: reader.GetBoolean("Win"),
                    GameEndTimestamp: reader.GetDateTime("GameEndTimestamp")
                ));
            }

            return results;
        });
    }

    /// <summary>
    /// Get team game duration statistics for duration analysis.
    /// </summary>
    public async Task<IList<TeamDurationRecord>> GetTeamDurationStatsByPuuIdsAsync(string[] puuIds, string? gameMode = null)
    {
        if (puuIds == null || puuIds.Length < 3)
            return new List<TeamDurationRecord>();

        return await ExecuteWithConnectionAsync(async (conn, cmd) =>
        {
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
                    CASE
                        WHEN m.DurationSeconds < 1200 THEN 'under20'
                        WHEN m.DurationSeconds < 1500 THEN '20-25'
                        WHEN m.DurationSeconds < 1800 THEN '25-30'
                        WHEN m.DurationSeconds < 2100 THEN '30-35'
                        WHEN m.DurationSeconds < 2400 THEN '35-40'
                        ELSE '40+'
                    END as DurationBucket,
                    COUNT(DISTINCT p0.MatchId) as GamesPlayed,
                    COUNT(DISTINCT CASE WHEN p0.Win = 1 THEN p0.MatchId ELSE NULL END) as Wins
                FROM LolMatchParticipant p0
                {string.Join("", joinClauses)}
                INNER JOIN LolMatch m ON p0.MatchId = m.MatchId
                WHERE p0.Puuid = @puuid0
                  AND m.InfoFetched = TRUE
                  AND m.DurationSeconds > 0";

            if (!string.IsNullOrWhiteSpace(gameMode))
                sql += " AND m.GameMode = @gameMode";
            else
                sql += " AND m.GameMode != 'ARAM'";

            sql += " GROUP BY DurationBucket ORDER BY DurationBucket";

            cmd.CommandText = sql;
            for (int i = 0; i < puuIds.Length; i++)
                cmd.Parameters.AddWithValue($"@puuid{i}", puuIds[i]);
            if (!string.IsNullOrWhiteSpace(gameMode))
                cmd.Parameters.AddWithValue("@gameMode", gameMode);

            var results = new List<TeamDurationRecord>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new TeamDurationRecord(
                    DurationBucket: reader.GetString("DurationBucket"),
                    GamesPlayed: reader.GetInt32("GamesPlayed"),
                    Wins: reader.GetInt32("Wins")
                ));
            }

            return results;
        });
    }

    /// <summary>
    /// Get team champion combinations for combo analysis.
    /// </summary>
    public async Task<IList<TeamChampionComboRecord>> GetTeamChampionCombosByPuuIdsAsync(string[] puuIds, int limit = 10, string? gameMode = null)
    {
        if (puuIds == null || puuIds.Length < 3)
            return new List<TeamChampionComboRecord>();

        return await ExecuteWithConnectionAsync(async (conn, cmd) =>
        {
            var joinClauses = new List<string>();
            var selectColumns = new List<string> { "p0.Puuid as Puuid0", "p0.ChampionId as ChampionId0", "p0.ChampionName as ChampionName0" };
            for (int i = 1; i < puuIds.Length; i++)
            {
                joinClauses.Add($@"
                    INNER JOIN LolMatchParticipant p{i}
                        ON p0.MatchId = p{i}.MatchId
                        AND p0.TeamId = p{i}.TeamId
                        AND p{i}.Puuid = @puuid{i}");
                selectColumns.Add($"p{i}.Puuid as Puuid{i}");
                selectColumns.Add($"p{i}.ChampionId as ChampionId{i}");
                selectColumns.Add($"p{i}.ChampionName as ChampionName{i}");
            }

            var sql = $@"
                SELECT
                    {string.Join(", ", selectColumns)},
                    COUNT(*) as GamesPlayed,
                    SUM(CASE WHEN p0.Win = 1 THEN 1 ELSE 0 END) as Wins
                FROM LolMatchParticipant p0
                {string.Join("", joinClauses)}
                INNER JOIN LolMatch m ON p0.MatchId = m.MatchId
                WHERE p0.Puuid = @puuid0
                  AND m.InfoFetched = TRUE
                  AND m.DurationSeconds > 0";

            if (!string.IsNullOrWhiteSpace(gameMode))
                sql += " AND m.GameMode = @gameMode";
            else
                sql += " AND m.GameMode != 'ARAM'";

            var groupByColumns = Enumerable.Range(0, puuIds.Length).Select(i => $"p{i}.ChampionId, p{i}.ChampionName, p{i}.Puuid");
            sql += $" GROUP BY {string.Join(", ", groupByColumns)}";
            sql += " ORDER BY (SUM(CASE WHEN p0.Win = 1 THEN 1 ELSE 0 END) / COUNT(*)) DESC, GamesPlayed DESC LIMIT @limit";

            cmd.CommandText = sql;
            for (int i = 0; i < puuIds.Length; i++)
                cmd.Parameters.AddWithValue($"@puuid{i}", puuIds[i]);
            cmd.Parameters.AddWithValue("@limit", limit);
            if (!string.IsNullOrWhiteSpace(gameMode))
                cmd.Parameters.AddWithValue("@gameMode", gameMode);

            var results = new List<TeamChampionComboRecord>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var champions = new List<(string Puuid, int ChampionId, string ChampionName, string GameName)>();
                for (int i = 0; i < puuIds.Length; i++)
                {
                    champions.Add((
                        reader.GetString($"Puuid{i}"),
                        reader.GetInt32($"ChampionId{i}"),
                        reader.GetString($"ChampionName{i}"),
                        ""
                    ));
                }

                results.Add(new TeamChampionComboRecord(
                    Champions: champions,
                    GamesPlayed: reader.GetInt32("GamesPlayed"),
                    Wins: reader.GetInt32("Wins")
                ));
            }

            return results;
        });
    }

    /// <summary>
    /// Get role pair effectiveness statistics.
    /// </summary>
    public async Task<IList<TeamRolePairRecord>> GetTeamRolePairStatsByPuuIdsAsync(string[] puuIds, string? gameMode = null)
    {
        if (puuIds == null || puuIds.Length < 3)
            return new List<TeamRolePairRecord>();

        return await ExecuteWithConnectionAsync(async (conn, cmd) =>
        {
            var joinClauses = new List<string>();
            for (int i = 1; i < puuIds.Length; i++)
            {
                joinClauses.Add($@"
                    INNER JOIN LolMatchParticipant p{i}
                        ON p0.MatchId = p{i}.MatchId
                        AND p0.TeamId = p{i}.TeamId
                        AND p{i}.Puuid = @puuid{i}");
            }

            var selectColumns = new List<string> { "p0.TeamPosition as Role0" };
            for (int i = 1; i < puuIds.Length; i++)
                selectColumns.Add($"p{i}.TeamPosition as Role{i}");

            var sql = $@"
                SELECT
                    {string.Join(", ", selectColumns)},
                    p0.Win
                FROM LolMatchParticipant p0
                {string.Join("", joinClauses)}
                INNER JOIN LolMatch m ON p0.MatchId = m.MatchId
                WHERE p0.Puuid = @puuid0
                  AND m.InfoFetched = TRUE
                  AND m.DurationSeconds > 0";

            if (!string.IsNullOrWhiteSpace(gameMode))
                sql += " AND m.GameMode = @gameMode";
            else
                sql += " AND m.GameMode != 'ARAM'";

            cmd.CommandText = sql;
            for (int i = 0; i < puuIds.Length; i++)
                cmd.Parameters.AddWithValue($"@puuid{i}", puuIds[i]);
            if (!string.IsNullOrWhiteSpace(gameMode))
                cmd.Parameters.AddWithValue("@gameMode", gameMode);

            var rolePairStats = new Dictionary<string, (int Games, int Wins)>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var roles = new List<string>();
                for (int i = 0; i < puuIds.Length; i++)
                {
                    var role = reader.IsDBNull(reader.GetOrdinal($"Role{i}")) ? "" : reader.GetString($"Role{i}");
                    if (!string.IsNullOrEmpty(role))
                        roles.Add(role);
                }

                var win = reader.GetBoolean("Win");

                for (int i = 0; i < roles.Count; i++)
                {
                    for (int j = i + 1; j < roles.Count; j++)
                    {
                        var pair = string.Compare(roles[i], roles[j]) <= 0
                            ? $"{roles[i]}|{roles[j]}"
                            : $"{roles[j]}|{roles[i]}";

                        if (!rolePairStats.ContainsKey(pair))
                            rolePairStats[pair] = (0, 0);

                        var current = rolePairStats[pair];
                        rolePairStats[pair] = (current.Games + 1, current.Wins + (win ? 1 : 0));
                    }
                }
            }

            return rolePairStats.Select(kvp =>
            {
                var parts = kvp.Key.Split('|');
                return new TeamRolePairRecord(
                    Role1: parts[0],
                    Role2: parts[1],
                    GamesPlayed: kvp.Value.Games,
                    Wins: kvp.Value.Wins
                );
            }).ToList();
        });
    }

    /// <summary>
    /// Get death timer stats per player in wins vs losses for team games.
    /// </summary>
    public async Task<IList<PlayerDeathTimerRecord>> GetTeamDeathTimerStatsByPuuIdsAsync(string[] puuIds, string? gameMode = null)
    {
        if (puuIds == null || puuIds.Length < 3)
            return new List<PlayerDeathTimerRecord>();

        return await ExecuteWithConnectionAsync(async (conn, cmd) =>
        {
            var records = new List<PlayerDeathTimerRecord>();

            var joinClauses = new List<string>();
            for (int i = 1; i < puuIds.Length; i++)
            {
                joinClauses.Add($@"
                    INNER JOIN LolMatchParticipant p{i}
                        ON p0.MatchId = p{i}.MatchId
                        AND p0.TeamId = p{i}.TeamId
                        AND p{i}.Puuid = @puuid{i}");
            }

            foreach (var targetPuuId in puuIds)
            {
                cmd.Parameters.Clear();

                var sql = $@"
                    SELECT
                        SUM(CASE WHEN p.Win = 1 THEN 1 ELSE 0 END) as GamesWon,
                        SUM(CASE WHEN p.Win = 0 THEN 1 ELSE 0 END) as GamesLost,
                        SUM(CASE WHEN p.Win = 1 THEN p.Deaths ELSE 0 END) as TotalDeathsInWins,
                        SUM(CASE WHEN p.Win = 0 THEN p.Deaths ELSE 0 END) as TotalDeathsInLosses,
                        SUM(CASE WHEN p.Win = 1 THEN p.TimeBeingDeadSeconds ELSE 0 END) as TotalTimeDeadInWins,
                        SUM(CASE WHEN p.Win = 0 THEN p.TimeBeingDeadSeconds ELSE 0 END) as TotalTimeDeadInLosses
                    FROM LolMatchParticipant p
                    INNER JOIN LolMatchParticipant p0 ON p.MatchId = p0.MatchId AND p.TeamId = p0.TeamId
                    {string.Join("", joinClauses)}
                    INNER JOIN LolMatch m ON p0.MatchId = m.MatchId
                    WHERE p0.Puuid = @puuid0
                      AND p.Puuid = @targetPuuid
                      AND m.InfoFetched = TRUE";

                if (!string.IsNullOrWhiteSpace(gameMode))
                    sql += " AND m.GameMode = @gameMode";
                else
                    sql += " AND m.GameMode != 'ARAM'";

                cmd.CommandText = sql;
                for (int i = 0; i < puuIds.Length; i++)
                    cmd.Parameters.AddWithValue($"@puuid{i}", puuIds[i]);
                cmd.Parameters.AddWithValue("@targetPuuid", targetPuuId);
                if (!string.IsNullOrWhiteSpace(gameMode))
                    cmd.Parameters.AddWithValue("@gameMode", gameMode);

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    records.Add(new PlayerDeathTimerRecord(
                        PuuId: targetPuuId,
                        GamesWon: reader.IsDBNull(reader.GetOrdinal("GamesWon")) ? 0 : reader.GetInt32("GamesWon"),
                        GamesLost: reader.IsDBNull(reader.GetOrdinal("GamesLost")) ? 0 : reader.GetInt32("GamesLost"),
                        TotalDeathsInWins: reader.IsDBNull(reader.GetOrdinal("TotalDeathsInWins")) ? 0 : reader.GetInt32("TotalDeathsInWins"),
                        TotalDeathsInLosses: reader.IsDBNull(reader.GetOrdinal("TotalDeathsInLosses")) ? 0 : reader.GetInt32("TotalDeathsInLosses"),
                        TotalTimeDeadInWins: reader.IsDBNull(reader.GetOrdinal("TotalTimeDeadInWins")) ? 0 : reader.GetInt32("TotalTimeDeadInWins"),
                        TotalTimeDeadInLosses: reader.IsDBNull(reader.GetOrdinal("TotalTimeDeadInLosses")) ? 0 : reader.GetInt32("TotalTimeDeadInLosses")
                    ));
                }
            }

            return records;
        });
    }

    /// <summary>
    /// Get team deaths grouped by game duration buckets.
    /// </summary>
    public async Task<IList<TeamDeathsByDurationRecord>> GetTeamDeathsByDurationAsync(string[] puuIds, string? gameMode = null)
    {
        if (puuIds == null || puuIds.Length < 3)
            return new List<TeamDeathsByDurationRecord>();

        return await ExecuteWithConnectionAsync(async (conn, cmd) =>
        {
            var joinClauses = new List<string>();
            for (int i = 1; i < puuIds.Length; i++)
            {
                joinClauses.Add($@"
                    INNER JOIN LolMatchParticipant p{i}
                        ON p0.MatchId = p{i}.MatchId
                        AND p0.TeamId = p{i}.TeamId
                        AND p{i}.Puuid = @puuid{i}");
            }

            var deathsSum = string.Join(" + ", Enumerable.Range(0, puuIds.Length).Select(i => $"p{i}.Deaths"));

            var sql = $@"
                SELECT
                    CASE
                        WHEN m.DurationSeconds < 1200 THEN 'under20'
                        WHEN m.DurationSeconds < 1500 THEN '20-25'
                        WHEN m.DurationSeconds < 1800 THEN '25-30'
                        WHEN m.DurationSeconds < 2100 THEN '30-35'
                        WHEN m.DurationSeconds < 2400 THEN '35-40'
                        ELSE '40+'
                    END as DurationBucket,
                    COUNT(DISTINCT p0.MatchId) as GamesPlayed,
                    COUNT(DISTINCT CASE WHEN p0.Win = 1 THEN p0.MatchId ELSE NULL END) as Wins,
                    SUM({deathsSum}) as TotalTeamDeaths
                FROM LolMatchParticipant p0
                {string.Join("", joinClauses)}
                INNER JOIN LolMatch m ON p0.MatchId = m.MatchId
                WHERE p0.Puuid = @puuid0
                  AND m.InfoFetched = TRUE";

            if (!string.IsNullOrWhiteSpace(gameMode))
                sql += " AND m.GameMode = @gameMode";
            else
                sql += " AND m.GameMode != 'ARAM'";

            sql += " GROUP BY DurationBucket ORDER BY DurationBucket";

            cmd.CommandText = sql;
            for (int i = 0; i < puuIds.Length; i++)
                cmd.Parameters.AddWithValue($"@puuid{i}", puuIds[i]);
            if (!string.IsNullOrWhiteSpace(gameMode))
                cmd.Parameters.AddWithValue("@gameMode", gameMode);

            var records = new List<TeamDeathsByDurationRecord>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                records.Add(new TeamDeathsByDurationRecord(
                    DurationBucket: reader.GetString("DurationBucket"),
                    GamesPlayed: reader.GetInt32("GamesPlayed"),
                    Wins: reader.GetInt32("Wins"),
                    TotalTeamDeaths: reader.IsDBNull(reader.GetOrdinal("TotalTeamDeaths")) ? 0 : reader.GetInt32("TotalTeamDeaths")
                ));
            }

            return records;
        });
    }

    /// <summary>
    /// Get team match results with death counts for trend analysis.
    /// </summary>
    public async Task<IList<TeamMatchDeathRecord>> GetTeamMatchDeathsAsync(string[] puuIds, int limit = 50, string? gameMode = null)
    {
        if (puuIds == null || puuIds.Length < 3)
            return new List<TeamMatchDeathRecord>();

        return await ExecuteWithConnectionAsync(async (conn, cmd) =>
        {
            var joinClauses = new List<string>();
            for (int i = 1; i < puuIds.Length; i++)
            {
                joinClauses.Add($@"
                    INNER JOIN LolMatchParticipant p{i}
                        ON p0.MatchId = p{i}.MatchId
                        AND p0.TeamId = p{i}.TeamId
                        AND p{i}.Puuid = @puuid{i}");
            }

            var deathsSum = string.Join(" + ", Enumerable.Range(0, puuIds.Length).Select(i => $"p{i}.Deaths"));

            var sql = $@"
                SELECT DISTINCT
                    p0.MatchId,
                    p0.Win,
                    ({deathsSum}) as TeamDeaths,
                    m.GameEndTimestamp
                FROM LolMatchParticipant p0
                {string.Join("", joinClauses)}
                INNER JOIN LolMatch m ON p0.MatchId = m.MatchId
                WHERE p0.Puuid = @puuid0
                  AND m.InfoFetched = TRUE";

            if (!string.IsNullOrWhiteSpace(gameMode))
                sql += " AND m.GameMode = @gameMode";
            else
                sql += " AND m.GameMode != 'ARAM'";

            sql += " ORDER BY m.GameEndTimestamp ASC LIMIT @limit";

            cmd.CommandText = sql;
            for (int i = 0; i < puuIds.Length; i++)
                cmd.Parameters.AddWithValue($"@puuid{i}", puuIds[i]);
            cmd.Parameters.AddWithValue("@limit", limit);
            if (!string.IsNullOrWhiteSpace(gameMode))
                cmd.Parameters.AddWithValue("@gameMode", gameMode);

            var records = new List<TeamMatchDeathRecord>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                records.Add(new TeamMatchDeathRecord(
                    MatchId: reader.GetString("MatchId"),
                    Win: reader.GetBoolean("Win"),
                    TeamDeaths: reader.GetInt32("TeamDeaths"),
                    GameEndTimestamp: reader.GetDateTime("GameEndTimestamp")
                ));
            }

            return records;
        });
    }

    /// <summary>
    /// Get team match results with kill counts for trend analysis.
    /// </summary>
    public async Task<IList<TeamMatchKillRecord>> GetTeamMatchKillsAsync(string[] puuIds, int limit = 50, string? gameMode = null)
    {
        if (puuIds == null || puuIds.Length < 3)
            return new List<TeamMatchKillRecord>();

        return await ExecuteWithConnectionAsync(async (conn, cmd) =>
        {
            var joinClauses = new List<string>();
            for (int i = 1; i < puuIds.Length; i++)
            {
                joinClauses.Add($@"
                    INNER JOIN LolMatchParticipant p{i}
                        ON p0.MatchId = p{i}.MatchId
                        AND p0.TeamId = p{i}.TeamId
                        AND p{i}.Puuid = @puuid{i}");
            }

            var killsSum = string.Join(" + ", Enumerable.Range(0, puuIds.Length).Select(i => $"p{i}.Kills"));

            var sql = $@"
                SELECT DISTINCT
                    p0.MatchId,
                    p0.Win,
                    ({killsSum}) as TeamKills,
                    m.GameEndTimestamp
                FROM LolMatchParticipant p0
                {string.Join("", joinClauses)}
                INNER JOIN LolMatch m ON p0.MatchId = m.MatchId
                WHERE p0.Puuid = @puuid0
                  AND m.InfoFetched = TRUE";

            if (!string.IsNullOrWhiteSpace(gameMode))
                sql += " AND m.GameMode = @gameMode";
            else
                sql += " AND m.GameMode != 'ARAM'";

            sql += " ORDER BY m.GameEndTimestamp ASC LIMIT @limit";

            cmd.CommandText = sql;
            for (int i = 0; i < puuIds.Length; i++)
                cmd.Parameters.AddWithValue($"@puuid{i}", puuIds[i]);
            cmd.Parameters.AddWithValue("@limit", limit);
            if (!string.IsNullOrWhiteSpace(gameMode))
                cmd.Parameters.AddWithValue("@gameMode", gameMode);

            var records = new List<TeamMatchKillRecord>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                records.Add(new TeamMatchKillRecord(
                    MatchId: reader.GetString("MatchId"),
                    Win: reader.GetBoolean("Win"),
                    TeamKills: reader.GetInt32("TeamKills"),
                    GameEndTimestamp: reader.GetDateTime("GameEndTimestamp")
                ));
            }

            return records;
        });
    }

    /// <summary>
    /// Get team kills grouped by game duration buckets.
    /// </summary>
    public async Task<IList<TeamKillsByDurationRecord>> GetTeamKillsByDurationAsync(string[] puuIds, string? gameMode = null)
    {
        if (puuIds == null || puuIds.Length < 3)
            return new List<TeamKillsByDurationRecord>();

        return await ExecuteWithConnectionAsync(async (conn, cmd) =>
        {
            var joinClauses = new List<string>();
            for (int i = 1; i < puuIds.Length; i++)
            {
                joinClauses.Add($@"
                    INNER JOIN LolMatchParticipant p{i}
                        ON p0.MatchId = p{i}.MatchId
                        AND p0.TeamId = p{i}.TeamId
                        AND p{i}.Puuid = @puuid{i}");
            }

            var killsSum = string.Join(" + ", Enumerable.Range(0, puuIds.Length).Select(i => $"p{i}.Kills"));

            var sql = $@"
                SELECT
                    CASE
                        WHEN m.DurationSeconds < 1200 THEN 'under20'
                        WHEN m.DurationSeconds < 1500 THEN '20-25'
                        WHEN m.DurationSeconds < 1800 THEN '25-30'
                        WHEN m.DurationSeconds < 2100 THEN '30-35'
                        WHEN m.DurationSeconds < 2400 THEN '35-40'
                        ELSE '40+'
                    END as DurationBucket,
                    COUNT(DISTINCT p0.MatchId) as GamesPlayed,
                    COUNT(DISTINCT CASE WHEN p0.Win = 1 THEN p0.MatchId ELSE NULL END) as Wins,
                    SUM({killsSum}) as TotalTeamKills
                FROM LolMatchParticipant p0
                {string.Join("", joinClauses)}
                INNER JOIN LolMatch m ON p0.MatchId = m.MatchId
                WHERE p0.Puuid = @puuid0
                  AND m.InfoFetched = TRUE
                  AND m.DurationSeconds > 0";

            if (!string.IsNullOrWhiteSpace(gameMode))
                sql += " AND m.GameMode = @gameMode";
            else
                sql += " AND m.GameMode != 'ARAM'";

            sql += " GROUP BY DurationBucket ORDER BY DurationBucket";

            cmd.CommandText = sql;
            for (int i = 0; i < puuIds.Length; i++)
                cmd.Parameters.AddWithValue($"@puuid{i}", puuIds[i]);
            if (!string.IsNullOrWhiteSpace(gameMode))
                cmd.Parameters.AddWithValue("@gameMode", gameMode);

            var records = new List<TeamKillsByDurationRecord>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                records.Add(new TeamKillsByDurationRecord(
                    DurationBucket: reader.GetString("DurationBucket"),
                    GamesPlayed: reader.GetInt32("GamesPlayed"),
                    Wins: reader.GetInt32("Wins"),
                    TotalTeamKills: reader.GetInt32("TotalTeamKills")
                ));
            }

            return records;
        });
    }

    /// <summary>
    /// Get multi-kill statistics for each player in team games.
    /// </summary>
    public async Task<IList<PlayerMultiKillRecord>> GetTeamMultiKillsAsync(string[] puuIds, string? gameMode = null)
    {
        if (puuIds == null || puuIds.Length < 3)
            return new List<PlayerMultiKillRecord>();

        return await ExecuteWithConnectionAsync(async (conn, cmd) =>
        {
            var records = new List<PlayerMultiKillRecord>();

            var joinClauses = new List<string>();
            for (int i = 1; i < puuIds.Length; i++)
            {
                joinClauses.Add($@"
                    INNER JOIN LolMatchParticipant tp{i}
                        ON tp0.MatchId = tp{i}.MatchId
                        AND tp0.TeamId = tp{i}.TeamId
                        AND tp{i}.Puuid = @teamPuuid{i}");
            }

            var teamMatchSubquery = $@"
                SELECT DISTINCT tp0.MatchId
                FROM LolMatchParticipant tp0
                {string.Join("", joinClauses)}
                INNER JOIN LolMatch m ON tp0.MatchId = m.MatchId
                WHERE tp0.Puuid = @teamPuuid0
                  AND m.InfoFetched = TRUE
                  AND m.DurationSeconds > 0";

            if (!string.IsNullOrWhiteSpace(gameMode))
                teamMatchSubquery += " AND m.GameMode = @gameMode";
            else
                teamMatchSubquery += " AND m.GameMode != 'ARAM'";

            foreach (var puuId in puuIds)
            {
                cmd.Parameters.Clear();

                var sql = $@"
                    SELECT
                        SUM(p.DoubleKills) as DoubleKills,
                        SUM(p.TripleKills) as TripleKills,
                        SUM(p.QuadraKills) as QuadraKills,
                        SUM(p.PentaKills) as PentaKills
                    FROM LolMatchParticipant p
                    WHERE p.Puuid = @puuid
                      AND p.MatchId IN ({teamMatchSubquery})";

                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@puuid", puuId);
                for (int i = 0; i < puuIds.Length; i++)
                    cmd.Parameters.AddWithValue($"@teamPuuid{i}", puuIds[i]);
                if (!string.IsNullOrWhiteSpace(gameMode))
                    cmd.Parameters.AddWithValue("@gameMode", gameMode);

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    records.Add(new PlayerMultiKillRecord(
                        PuuId: puuId,
                        DoubleKills: reader.IsDBNull(reader.GetOrdinal("DoubleKills")) ? 0 : reader.GetInt32("DoubleKills"),
                        TripleKills: reader.IsDBNull(reader.GetOrdinal("TripleKills")) ? 0 : reader.GetInt32("TripleKills"),
                        QuadraKills: reader.IsDBNull(reader.GetOrdinal("QuadraKills")) ? 0 : reader.GetInt32("QuadraKills"),
                        PentaKills: reader.IsDBNull(reader.GetOrdinal("PentaKills")) ? 0 : reader.GetInt32("PentaKills")
                    ));
                }
            }

            return records;
        });
    }

    /// <summary>
    /// Gets detailed information about the most recent game where all team members played together.
    /// </summary>
    public async Task<LatestGameTogetherRecord?> GetLatestGameTogetherByTeamPuuIdsAsync(string[] puuIds)
    {
        if (puuIds == null || puuIds.Length < 2)
            return null;

        return await ExecuteWithConnectionAsync(async (conn, cmd) =>
        {
            // Build dynamic SQL to find the latest match where ALL players participated on the same team
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
                SELECT p0.MatchId, m.GameEndTimestamp, p0.Win, p0.TeamId
                FROM LolMatchParticipant p0
                {string.Join("", joinClauses)}
                INNER JOIN LolMatch m ON p0.MatchId = m.MatchId
                WHERE p0.Puuid = @puuid0
                  AND m.InfoFetched = TRUE
                  AND m.GameEndTimestamp IS NOT NULL
                ORDER BY m.GameEndTimestamp DESC
                LIMIT 1";

            cmd.CommandText = sql;
            for (int i = 0; i < puuIds.Length; i++)
            {
                cmd.Parameters.AddWithValue($"@puuid{i}", puuIds[i]);
            }

            string? matchId = null;
            DateTime gameEndTimestamp = DateTime.MinValue;
            bool win = false;
            int teamId = 0;

            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    matchId = reader.GetString("MatchId");
                    gameEndTimestamp = reader.GetDateTime("GameEndTimestamp");
                    win = reader.GetBoolean("Win");
                    teamId = reader.GetInt32("TeamId");
                }
            }

            if (matchId == null)
                return null;

            // Build IN clause for puuids and filter by team
            cmd.Parameters.Clear();
            var puuidParams = string.Join(", ", puuIds.Select((_, i) => $"@playerPuuid{i}"));
            var playersSql = $@"
                SELECT DISTINCT
                    p.Puuid,
                    p.Win,
                    COALESCE(NULLIF(p.TeamPosition, ''), 'UNKNOWN') as Role,
                    p.ChampionId,
                    p.ChampionName,
                    p.Kills,
                    p.Deaths,
                    p.Assists
                FROM LolMatchParticipant p
                WHERE p.MatchId = @matchId
                  AND p.TeamId = @teamId
                  AND p.Puuid IN ({puuidParams})";

            cmd.CommandText = playersSql;
            cmd.Parameters.AddWithValue("@matchId", matchId);
            cmd.Parameters.AddWithValue("@teamId", teamId);
            for (int i = 0; i < puuIds.Length; i++)
            {
                cmd.Parameters.AddWithValue($"@playerPuuid{i}", puuIds[i]);
            }

            var players = new List<LatestGameTogetherPlayerRecord>();
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    players.Add(new LatestGameTogetherPlayerRecord(
                        Puuid: reader.GetString("Puuid"),
                        Win: reader.GetBoolean("Win"),
                        Role: reader.GetString("Role"),
                        ChampionId: reader.GetInt32("ChampionId"),
                        ChampionName: reader.GetString("ChampionName"),
                        Kills: reader.GetInt32("Kills"),
                        Deaths: reader.GetInt32("Deaths"),
                        Assists: reader.GetInt32("Assists")
                    ));
                }
            }

            return new LatestGameTogetherRecord(gameEndTimestamp, win, players);
        });
    }
}

