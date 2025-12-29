using MySqlConnector;
using RiotProxy.External.Domain.Entities;
using RiotProxy.Infrastructure.External.Database.Records;

namespace RiotProxy.Infrastructure.External.Database.Repositories
{
    public class LolMatchParticipantRepository
    {
        private readonly IDbConnectionFactory _factory;

        public LolMatchParticipantRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task AddParticipantIfNotExistsAsync(LolMatchParticipant participant)
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            const string sql = "INSERT IGNORE INTO LolMatchParticipant (MatchId, Puuid, TeamId, Win, Role, TeamPosition, Lane, ChampionId, ChampionName, Kills, Deaths, Assists, DoubleKills, TripleKills, QuadraKills, PentaKills, GoldEarned, TimeBeingDeadSeconds, CreepScore) " +
                               "VALUES (@matchId, @puuid, @teamId, @win, @role, @teamPosition, @lane, @championId, @championName, @kills, @deaths, @assists, @doubleKills, @tripleKills, @quadraKills, @pentaKills, @goldEarned, @timeBeingDeadSeconds, @creepScore)";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@matchId", participant.MatchId);
            cmd.Parameters.AddWithValue("@puuid", participant.PuuId);
            cmd.Parameters.AddWithValue("@teamId", participant.TeamId);
            cmd.Parameters.AddWithValue("@win", participant.Win);
            cmd.Parameters.AddWithValue("@role", participant.Role ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@teamPosition", participant.TeamPosition ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@lane", participant.Lane ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@championId", participant.ChampionId);
            cmd.Parameters.AddWithValue("@championName", participant.ChampionName);
            cmd.Parameters.AddWithValue("@kills", participant.Kills);
            cmd.Parameters.AddWithValue("@deaths", participant.Deaths);
            cmd.Parameters.AddWithValue("@assists", participant.Assists);
            cmd.Parameters.AddWithValue("@doubleKills", participant.DoubleKills);
            cmd.Parameters.AddWithValue("@tripleKills", participant.TripleKills);
            cmd.Parameters.AddWithValue("@quadraKills", participant.QuadraKills);
            cmd.Parameters.AddWithValue("@pentaKills", participant.PentaKills);
            cmd.Parameters.AddWithValue("@goldEarned", participant.GoldEarned);
            cmd.Parameters.AddWithValue("@timeBeingDeadSeconds", participant.TimeBeingDeadSeconds);
            cmd.Parameters.AddWithValue("@creepScore", participant.CreepScore);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<IList<string>> GetMatchIdsForPuuidAsync(string puuId)
        {
            var matchIds = new List<string>();
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            const string sql = "SELECT MatchId FROM LolMatchParticipant WHERE Puuid = @puuid";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuId);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                matchIds.Add(reader.GetString(0));
            }
            return matchIds;
        }

        internal async Task<long> GetTotalDurationPlayedByPuuidAsync(string puuId)
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            const string sql = "SELECT SUM(DurationSeconds) FROM LolMatch WHERE MatchId IN (SELECT MatchId FROM LolMatchParticipant WHERE Puuid = @puuid)";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuId);
            var result = await cmd.ExecuteScalarAsync();
            return result != DBNull.Value ? Convert.ToInt64(result) : 0L;
        }

        internal async Task<int> GetWinsByPuuIdAsync(string puuId)
        {
            const string sql = "SELECT COUNT(*) FROM LolMatchParticipant WHERE Puuid = @puuid AND Win = TRUE";
            var wins = await GetIntegerValueFromPuuIdAsync(puuId, sql);
            return wins;
        }

        internal async Task<int> GetMatchesCountByPuuIdAsync(string puuId)
        {
            const string sql = "SELECT COUNT(*) FROM LolMatchParticipant WHERE Puuid = @puuid";
            var totalMatches = await GetIntegerValueFromPuuIdAsync(puuId, sql);
            return totalMatches;
        }

        internal async Task<int> GetTotalAssistsByPuuIdAsync(string puuId)
        {
            const string sql = "SELECT SUM(Assists) FROM LolMatchParticipant WHERE Puuid = @puuid";
            var totalAssists = await GetIntegerValueFromPuuIdAsync(puuId, sql);
            return totalAssists;
        }

        internal async Task<int> GetTotalDeathsByPuuIdAsync(string puuId)
        {
            const string sql = "SELECT SUM(Deaths) FROM LolMatchParticipant WHERE Puuid = @puuid";
            var totalDeaths = await GetIntegerValueFromPuuIdAsync(puuId, sql);
            return totalDeaths;
        }

        internal async Task<int> GetTotalKillsByPuuIdAsync(string puuId)
        {
            const string sql = "SELECT SUM(Kills) FROM LolMatchParticipant WHERE Puuid = @puuid";
            var totalKills = await GetIntegerValueFromPuuIdAsync(puuId, sql);
            return totalKills;
        }

        internal async Task<int> GetTotalCreepScoreByPuuIdAsync(string puuId)
        {
            const string sql = "SELECT SUM(CreepScore) FROM LolMatchParticipant WHERE Puuid = @puuid";
            var totalCreepScore = await GetIntegerValueFromPuuIdAsync(puuId, sql);
            return totalCreepScore;
        }

        internal async Task<int> GetTotalGoldEarnedByPuuIdAsync(string puuId)
        {
            const string sql = "SELECT SUM(GoldEarned) FROM LolMatchParticipant WHERE Puuid = @puuid";
            var totalGoldEarned = await GetIntegerValueFromPuuIdAsync(puuId, sql);
            return totalGoldEarned;
        }

        internal async Task<int> GetTotalTimeBeingDeadSecondsByPuuIdAsync(string puuId)
        {
            const string sql = "SELECT SUM(TimeBeingDeadSeconds) FROM LolMatchParticipant WHERE Puuid = @puuid";
            var totalTimeBeingDeadSeconds = await GetIntegerValueFromPuuIdAsync(puuId, sql);
            return totalTimeBeingDeadSeconds;
        }

        /// <summary>
        /// Gets the timestamp of the most recent game played by a specific player.
        /// </summary>
        internal async Task<DateTime?> GetLatestGameTimestampByPuuIdAsync(string puuId)
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                SELECT MAX(m.GameEndTimestamp)
                FROM LolMatchParticipant p
                INNER JOIN LolMatch m ON p.MatchId = m.MatchId
                WHERE p.Puuid = @puuid
                  AND m.InfoFetched = TRUE
                  AND m.GameEndTimestamp IS NOT NULL";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuId);
            var result = await cmd.ExecuteScalarAsync();

            if (result == null || result == DBNull.Value)
                return null;

            return Convert.ToDateTime(result);
        }

        /// <summary>
        /// Gets detailed information about the most recent game played by a specific player.
        /// </summary>
        internal async Task<LatestGameRecord?> GetLatestGameDetailsByPuuIdAsync(string puuId)
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                SELECT
                    m.GameEndTimestamp,
                    p.Win,
                    COALESCE(NULLIF(p.TeamPosition, ''), 'UNKNOWN') as Role,
                    p.ChampionId,
                    p.ChampionName,
                    p.Kills,
                    p.Deaths,
                    p.Assists
                FROM LolMatchParticipant p
                INNER JOIN LolMatch m ON p.MatchId = m.MatchId
                WHERE p.Puuid = @puuid
                  AND m.InfoFetched = TRUE
                  AND m.GameEndTimestamp IS NOT NULL
                ORDER BY m.GameEndTimestamp DESC
                LIMIT 1";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuId);
            await using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new LatestGameRecord(
                    GameEndTimestamp: reader.GetDateTime("GameEndTimestamp"),
                    Win: reader.GetBoolean("Win"),
                    Role: reader.GetString("Role"),
                    ChampionId: reader.GetInt32("ChampionId"),
                    ChampionName: reader.GetString("ChampionName"),
                    Kills: reader.GetInt32("Kills"),
                    Deaths: reader.GetInt32("Deaths"),
                    Assists: reader.GetInt32("Assists")
                );
            }

            return null;
        }

        /// <summary>
        /// Gets detailed information about the most recent game where two players played together.
        /// </summary>
        internal async Task<LatestGameTogetherRecord?> GetLatestGameTogetherByDuoPuuIdsAsync(string puuId1, string puuId2)
        {
            if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2))
                return null;

            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            // First, find the latest match where both players played together
            const string matchSql = @"
                SELECT p1.MatchId, m.GameEndTimestamp, p1.Win
                FROM LolMatchParticipant p1
                INNER JOIN LolMatchParticipant p2
                    ON p1.MatchId = p2.MatchId
                    AND p1.TeamId = p2.TeamId
                    AND p1.Puuid != p2.Puuid
                INNER JOIN LolMatch m ON p1.MatchId = m.MatchId
                WHERE p1.Puuid = @puuid1
                  AND p2.Puuid = @puuid2
                  AND m.InfoFetched = TRUE
                  AND m.GameEndTimestamp IS NOT NULL
                ORDER BY m.GameEndTimestamp DESC
                LIMIT 1";

            await using var matchCmd = new MySqlCommand(matchSql, conn);
            matchCmd.Parameters.AddWithValue("@puuid1", puuId1);
            matchCmd.Parameters.AddWithValue("@puuid2", puuId2);

            string? matchId = null;
            DateTime gameEndTimestamp = DateTime.MinValue;
            bool win = false;

            await using (var reader = await matchCmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    matchId = reader.GetString("MatchId");
                    gameEndTimestamp = reader.GetDateTime("GameEndTimestamp");
                    win = reader.GetBoolean("Win");
                }
            }

            if (matchId == null)
                return null;

            // Now get each player's details for that match
            const string playersSql = @"
                SELECT
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
                  AND p.Puuid IN (@puuid1, @puuid2)";

            await using var playersCmd = new MySqlCommand(playersSql, conn);
            playersCmd.Parameters.AddWithValue("@matchId", matchId);
            playersCmd.Parameters.AddWithValue("@puuid1", puuId1);
            playersCmd.Parameters.AddWithValue("@puuid2", puuId2);

            var players = new List<LatestGameTogetherPlayerRecord>();
            await using (var reader = await playersCmd.ExecuteReaderAsync())
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
        }

        /// <summary>
        /// Gets detailed information about the most recent game where all team members played together.
        /// </summary>
        internal async Task<LatestGameTogetherRecord?> GetLatestGameTogetherByTeamPuuIdsAsync(string[] puuIds)
        {
            if (puuIds == null || puuIds.Length < 2)
                return null;

            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

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
                SELECT p0.MatchId, m.GameEndTimestamp, p0.Win
                FROM LolMatchParticipant p0
                {string.Join("", joinClauses)}
                INNER JOIN LolMatch m ON p0.MatchId = m.MatchId
                WHERE p0.Puuid = @puuid0
                  AND m.InfoFetched = TRUE
                  AND m.GameEndTimestamp IS NOT NULL
                ORDER BY m.GameEndTimestamp DESC
                LIMIT 1";

            await using var matchCmd = new MySqlCommand(sql, conn);
            for (int i = 0; i < puuIds.Length; i++)
            {
                matchCmd.Parameters.AddWithValue($"@puuid{i}", puuIds[i]);
            }

            string? matchId = null;
            DateTime gameEndTimestamp = DateTime.MinValue;
            bool win = false;

            await using (var reader = await matchCmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    matchId = reader.GetString("MatchId");
                    gameEndTimestamp = reader.GetDateTime("GameEndTimestamp");
                    win = reader.GetBoolean("Win");
                }
            }

            if (matchId == null)
                return null;

            // Build IN clause for puuids
            var puuidParams = string.Join(", ", puuIds.Select((_, i) => $"@playerPuuid{i}"));
            var playersSql = $@"
                SELECT
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
                  AND p.Puuid IN ({puuidParams})";

            await using var playersCmd = new MySqlCommand(playersSql, conn);
            playersCmd.Parameters.AddWithValue("@matchId", matchId);
            for (int i = 0; i < puuIds.Length; i++)
            {
                playersCmd.Parameters.AddWithValue($"@playerPuuid{i}", puuIds[i]);
            }

            var players = new List<LatestGameTogetherPlayerRecord>();
            await using (var reader = await playersCmd.ExecuteReaderAsync())
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
        }

        private async Task<int> GetIntegerValueFromPuuIdAsync(string puuId, string sqlQuery)
        {
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand(sqlQuery, conn);
            cmd.Parameters.AddWithValue("@puuid", puuId);
            var result = await cmd.ExecuteScalarAsync();
            if (result == null || result == DBNull.Value)
                return 0;
            return Convert.ToInt32(result);
        }

        /// <summary>
        /// Gets per-match performance data for a player, ordered by game end time.
        /// Used for performance timeline charts.
        /// </summary>
        /// <param name="puuId">Player's PUUID</param>
        /// <param name="fromDate">Start date filter (null for all time)</param>
        /// <param name="limit">Maximum number of matches to return (null for no limit). Returns the most recent matches.</param>
        /// <returns>List of match performance records ordered oldest to newest</returns>
        public async Task<IList<MatchPerformanceRecord>> GetMatchPerformanceTimelineAsync(string puuId, DateTime? fromDate = null, int? limit = null)
        {
            var records = new List<MatchPerformanceRecord>();
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            // If limit is specified, we need to get the most recent N matches first, then reverse them
            // We'll use a subquery to get the latest N matches, then order them chronologically
            string sql;

            if (limit.HasValue)
            {
                // Get the latest N matches ordered chronologically (oldest to newest)
                // Using a subquery to first get the most recent N matches, then order them chronologically
                sql = @"
                    SELECT * FROM (
                        SELECT
                            p.Win,
                            p.GoldEarned,
                            p.CreepScore,
                            m.DurationSeconds,
                            m.GameEndTimestamp
                        FROM LolMatchParticipant p
                        INNER JOIN LolMatch m ON p.MatchId = m.MatchId
                        WHERE p.Puuid = @puuid
                          AND m.InfoFetched = TRUE
                          AND m.DurationSeconds > 0";

                if (fromDate.HasValue)
                {
                    sql += " AND m.GameEndTimestamp >= @fromDate";
                }

                sql += @"
                        ORDER BY m.GameEndTimestamp DESC
                        LIMIT @limit
                    ) AS recent_matches
                    ORDER BY GameEndTimestamp ASC";
            }
            else
            {
                // No limit - get all matches
                sql = @"
                    SELECT
                        p.Win,
                        p.GoldEarned,
                        p.CreepScore,
                        m.DurationSeconds,
                        m.GameEndTimestamp
                    FROM LolMatchParticipant p
                    INNER JOIN LolMatch m ON p.MatchId = m.MatchId
                    WHERE p.Puuid = @puuid
                      AND m.InfoFetched = TRUE
                      AND m.DurationSeconds > 0";

                if (fromDate.HasValue)
                {
                    sql += " AND m.GameEndTimestamp >= @fromDate";
                }

                sql += " ORDER BY m.GameEndTimestamp ASC";
            }

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuId);
            if (fromDate.HasValue)
            {
                cmd.Parameters.AddWithValue("@fromDate", fromDate.Value);
            }
            if (limit.HasValue)
            {
                cmd.Parameters.AddWithValue("@limit", limit.Value);
            }

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var durationSeconds = reader.GetInt64(3);
                var durationMinutes = durationSeconds / 60.0;

                records.Add(new MatchPerformanceRecord(
                    Win: reader.GetBoolean(0),
                    GoldEarned: reader.GetInt32(1),
                    CreepScore: reader.GetInt32(2),
                    DurationMinutes: durationMinutes,
                    GameEndTimestamp: reader.IsDBNull(4) ? DateTime.MinValue : reader.GetDateTime(4)
                ));
            }

            // No need to reverse - the SQL query now handles ordering correctly
            // Both limited and unlimited queries return results in chronological order (oldest to newest)
            return records;
        }

        /// <summary>
        /// Get champion statistics (games played, wins) grouped by champion for a specific puuid.
        /// </summary>
        internal async Task<IList<ChampionStatsRecord>> GetChampionStatsByPuuIdAsync(string puuId)
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
                var championId = reader.GetInt32("ChampionId");
                var championName = reader.GetString("ChampionName");
                var gamesPlayed = reader.GetInt32("GamesPlayed");
                var wins = reader.GetInt32("Wins");

                records.Add(new ChampionStatsRecord(championId, championName, gamesPlayed, wins));
            }

            return records;
        }

        /// <summary>
        /// Get role/position distribution for a specific puuid.
        /// Returns count of games played in each role/position.
        /// </summary>
        internal async Task<IList<RoleDistributionRecord>> GetRoleDistributionByPuuIdAsync(string puuId)
        {
            var records = new List<RoleDistributionRecord>();
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            // Use TeamPosition as it's more reliable than Role or Lane in modern League
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
                var position = reader.GetString("Position");
                var gamesPlayed = reader.GetInt32("GamesPlayed");

                records.Add(new RoleDistributionRecord(position, gamesPlayed));
            }

            return records;
        }

        /// <summary>
        /// Get side statistics (blue/red) for a specific puuid.
        /// TeamId 100 = Blue side, TeamId 200 = Red side.
        /// </summary>
        internal async Task<SideStatsRecord> GetSideStatsByPuuIdAsync(string puuId)
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
                    BlueGames: reader.GetInt32("BlueGames"),
                    BlueWins: reader.GetInt32("BlueWins"),
                    RedGames: reader.GetInt32("RedGames"),
                    RedWins: reader.GetInt32("RedWins")
                );
            }

            return new SideStatsRecord(0, 0, 0, 0);
        }

        /// <summary>
        /// Get side statistics (blue/red) for duo games (where both players are on the same team).
        /// </summary>
        internal async Task<SideStatsRecord> GetDuoSideStatsByPuuIdsAsync(string puuId1, string puuId2)
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
        /// Get side statistics (blue/red) for team games (where all specified players are on the same team).
        /// </summary>
        internal async Task<SideStatsRecord> GetTeamSideStatsByPuuIdsAsync(string[] puuIds)
        {
            if (puuIds == null || puuIds.Length < 3)
            {
                return new SideStatsRecord(0, 0, 0, 0);
            }

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
            {
                cmd.Parameters.AddWithValue($"@puuid{i}", puuIds[i]);
            }
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
        /// Get match duration statistics (wins/total games) grouped by duration buckets for a specific puuid.
        /// </summary>
        internal async Task<IList<DurationBucketRecord>> GetDurationStatsByPuuIdAsync(string puuId)
        {
            var records = new List<DurationBucketRecord>();
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            // Group matches into 5-minute buckets
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
                var gamesPlayed = reader.GetInt32("GamesPlayed");
                var wins = reader.GetInt32("Wins");

                records.Add(new DurationBucketRecord(minMinutes, minMinutes + 5, gamesPlayed, wins));
            }

            return records;
        }

        /// <summary>
        /// Get champion matchup statistics for multiple puuids.
        /// Returns data grouped by champion+role showing performance against each opponent champion.
        /// </summary>
        internal async Task<IList<ChampionMatchupRecord>> GetChampionMatchupsByPuuIdsAsync(string[] puuIds)
        {
            if (puuIds == null || puuIds.Length == 0)
            {
                return new List<ChampionMatchupRecord>();
            }

            var records = new List<ChampionMatchupRecord>();
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            // Build parameterized query for multiple puuids
            var puuidParams = string.Join(",", puuIds.Select((_, i) => $"@puuid{i}"));

            // This query finds all matches where the player played a champion in a role,
            // then joins to find the opponent in the same role on the enemy team
            // Filters out UNKNOWN roles (empty or null TeamPosition)
            var sql = $@"
                SELECT
                    player.ChampionId,
                    player.ChampionName,
                    player.TeamPosition as Role,
                    opponent.ChampionId as OpponentChampionId,
                    opponent.ChampionName as OpponentChampionName,
                    COUNT(*) as GamesPlayed,
                    SUM(CASE WHEN player.Win = 1 THEN 1 ELSE 0 END) as Wins
                FROM LolMatchParticipant player
                INNER JOIN LolMatchParticipant opponent
                    ON player.MatchId = opponent.MatchId
                    AND player.TeamId != opponent.TeamId
                    AND player.TeamPosition = opponent.TeamPosition
                WHERE player.Puuid IN ({puuidParams})
                    AND player.TeamPosition IS NOT NULL
                    AND player.TeamPosition != ''
                GROUP BY player.ChampionId, player.ChampionName, Role, opponent.ChampionId, opponent.ChampionName
                ORDER BY player.ChampionName, Role, GamesPlayed DESC";

            await using var cmd = new MySqlCommand(sql, conn);
            for (int i = 0; i < puuIds.Length; i++)
            {
                cmd.Parameters.AddWithValue($"@puuid{i}", puuIds[i]);
            }

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var championId = reader.GetInt32("ChampionId");
                var championName = reader.GetString("ChampionName");
                var role = reader.GetString("Role");
                var opponentChampionId = reader.GetInt32("OpponentChampionId");
                var opponentChampionName = reader.GetString("OpponentChampionName");
                var gamesPlayed = reader.GetInt32("GamesPlayed");
                var wins = reader.GetInt32("Wins");

                records.Add(new ChampionMatchupRecord(
                    championId,
                    championName,
                    role,
                    opponentChampionId,
                    opponentChampionName,
                    gamesPlayed,
                    wins
                ));
            }

            return records;
        }

        /// <summary>
        /// Get statistics for games where two players played together (on the same team).
        /// Returns total games played together, wins, and the most common queue type.
        /// </summary>
        internal async Task<DuoStatsRecord?> GetDuoStatsByPuuIdsAsync(string puuId1, string puuId2)
        {
            if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2))
            {
                return null;
            }

            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            // Find matches where both players participated on the same team
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
                var gamesPlayed = reader.GetInt32("GamesPlayed");
                var wins = reader.GetInt32("Wins");
                var gameMode = reader.IsDBNull(reader.GetOrdinal("GameMode"))
                    ? "Unknown"
                    : reader.GetString("GameMode");

                return new DuoStatsRecord(gamesPlayed, wins, gameMode);
            }

            return null;
        }

        /// <summary>
        /// Get performance statistics for duo games (when two players play together).
        /// </summary>
        internal async Task<PlayerPerformanceRecord?> GetDuoPerformanceByPuuIdsAsync(string puuId1, string puuId2, string targetPuuId, string? gameMode = null)
        {
            if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2) || string.IsNullOrWhiteSpace(targetPuuId))
            {
                return null;
            }

            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            // Get performance stats for targetPuuId in games where both players were on the same team
            var sql = @"
                SELECT
                    COUNT(DISTINCT p1.MatchId) as GamesPlayed,
                    SUM(CASE WHEN p1.Win = 1 THEN 1 ELSE 0 END) as Wins,
                    SUM(p1.Kills) as TotalKills,
                    SUM(p1.Deaths) as TotalDeaths,
                    SUM(p1.Assists) as TotalAssists,
                    SUM(p1.GoldEarned) as TotalGoldEarned,
                    SUM(m.DurationSeconds) as TotalDurationSeconds
                FROM LolMatchParticipant p1
                INNER JOIN LolMatchParticipant p2
                    ON p1.MatchId = p2.MatchId
                    AND p1.TeamId = p2.TeamId
                    AND p1.Puuid != p2.Puuid
                INNER JOIN LolMatch m ON p1.MatchId = m.MatchId
                WHERE p1.Puuid = @targetPuuid
                  AND p2.Puuid = @otherPuuid
                  AND m.InfoFetched = TRUE
                  AND m.DurationSeconds > 0";

            // Add game mode filter if specified
            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                sql += " AND m.GameMode = @gameMode";
            }

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@targetPuuid", targetPuuId);
            cmd.Parameters.AddWithValue("@otherPuuid", targetPuuId == puuId1 ? puuId2 : puuId1);

            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                cmd.Parameters.AddWithValue("@gameMode", gameMode);
            }

            await using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var gamesPlayed = reader.GetInt32("GamesPlayed");
                if (gamesPlayed == 0) return null;

                return new PlayerPerformanceRecord(
                    GamesPlayed: gamesPlayed,
                    Wins: reader.GetInt32("Wins"),
                    TotalKills: reader.GetInt32("TotalKills"),
                    TotalDeaths: reader.GetInt32("TotalDeaths"),
                    TotalAssists: reader.GetInt32("TotalAssists"),
                    TotalGoldEarned: reader.GetInt64("TotalGoldEarned"),
                    TotalDurationSeconds: reader.GetInt64("TotalDurationSeconds")
                );
            }

            return null;
        }

        /// <summary>
        /// Get performance statistics for solo games (when a player plays without a specific partner).
        /// </summary>
        internal async Task<PlayerPerformanceRecord?> GetSoloPerformanceByPuuIdAsync(string puuId, string excludePuuId, string? gameMode = null)
        {
            if (string.IsNullOrWhiteSpace(puuId))
            {
                return null;
            }

            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            // Get performance stats for puuId in games where excludePuuId was NOT on the same team
            var sql = @"
                SELECT
                    COUNT(DISTINCT p.MatchId) as GamesPlayed,
                    SUM(CASE WHEN p.Win = 1 THEN 1 ELSE 0 END) as Wins,
                    SUM(p.Kills) as TotalKills,
                    SUM(p.Deaths) as TotalDeaths,
                    SUM(p.Assists) as TotalAssists,
                    SUM(p.GoldEarned) as TotalGoldEarned,
                    SUM(m.DurationSeconds) as TotalDurationSeconds
                FROM LolMatchParticipant p
                INNER JOIN LolMatch m ON p.MatchId = m.MatchId
                WHERE p.Puuid = @puuid
                  AND m.InfoFetched = TRUE
                  AND m.DurationSeconds > 0
                  AND NOT EXISTS (
                      SELECT 1 FROM LolMatchParticipant p2
                      WHERE p2.MatchId = p.MatchId
                        AND p2.TeamId = p.TeamId
                        AND p2.Puuid = @excludePuuid
                  )";

            // Add game mode filter if specified
            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                sql += " AND m.GameMode = @gameMode";
            }

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuId);
            cmd.Parameters.AddWithValue("@excludePuuid", excludePuuId);

            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                cmd.Parameters.AddWithValue("@gameMode", gameMode);
            }

            await using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var gamesPlayed = reader.GetInt32("GamesPlayed");
                if (gamesPlayed == 0) return null;

                return new PlayerPerformanceRecord(
                    GamesPlayed: gamesPlayed,
                    Wins: reader.GetInt32("Wins"),
                    TotalKills: reader.GetInt32("TotalKills"),
                    TotalDeaths: reader.GetInt32("TotalDeaths"),
                    TotalAssists: reader.GetInt32("TotalAssists"),
                    TotalGoldEarned: reader.GetInt64("TotalGoldEarned"),
                    TotalDurationSeconds: reader.GetInt64("TotalDurationSeconds")
                );
            }

            return null;
        }

        /// <summary>
        /// Get champion synergy statistics for duo games.
        /// Returns win rates for each champion pair combination when two players play together.
        /// </summary>
        internal async Task<IList<ChampionSynergyRecord>> GetChampionSynergyByPuuIdsAsync(string puuId1, string puuId2, string? gameMode = null)
        {
            if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2))
            {
                return new List<ChampionSynergyRecord>();
            }

            var records = new List<ChampionSynergyRecord>();
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            // Find all champion combinations when both players were on the same team
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
            {
                sql += " AND m.GameMode = @gameMode";
            }

            sql += @"
                GROUP BY p1.ChampionId, p1.ChampionName, p2.ChampionId, p2.ChampionName
                ORDER BY GamesPlayed DESC";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid1", puuId1);
            cmd.Parameters.AddWithValue("@puuid2", puuId2);

            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                cmd.Parameters.AddWithValue("@gameMode", gameMode);
            }

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                records.Add(new ChampionSynergyRecord(
                    ChampionId1: reader.GetInt32("ChampionId1"),
                    ChampionName1: reader.GetString("ChampionName1"),
                    ChampionId2: reader.GetInt32("ChampionId2"),
                    ChampionName2: reader.GetString("ChampionName2"),
                    GamesPlayed: reader.GetInt32("GamesPlayed"),
                    Wins: reader.GetInt32("Wins")
                ));
            }

            return records;
        }

        /// <summary>
        /// Get duo champion combination vs enemy champion statistics.
        /// Returns win rates for duo champion combos vs specific enemy lane champions.
        /// </summary>
        internal async Task<IList<DuoVsEnemyRecord>> GetDuoVsEnemyByPuuIdsAsync(string puuId1, string puuId2, string? gameMode = null)
        {
            if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2))
            {
                return new List<DuoVsEnemyRecord>();
            }

            var records = new List<DuoVsEnemyRecord>();
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            // Find duo champion combinations and their performance vs enemy lane champions
            var sql = @"
                SELECT
                    p1.ChampionId as DuoChampionId1,
                    p1.ChampionName as DuoChampionName1,
                    COALESCE(NULLIF(p1.TeamPosition, ''), 'UNKNOWN') as DuoLane1,
                    p2.ChampionId as DuoChampionId2,
                    p2.ChampionName as DuoChampionName2,
                    COALESCE(NULLIF(p2.TeamPosition, ''), 'UNKNOWN') as DuoLane2,
                    enemy.TeamPosition as EnemyLane,
                    enemy.ChampionId as EnemyChampionId,
                    enemy.ChampionName as EnemyChampionName,
                    COUNT(DISTINCT p1.MatchId) as GamesPlayed,
                    SUM(CASE WHEN p1.Win = 1 THEN 1 ELSE 0 END) as Wins
                FROM LolMatchParticipant p1
                INNER JOIN LolMatchParticipant p2
                    ON p1.MatchId = p2.MatchId
                    AND p1.TeamId = p2.TeamId
                    AND p1.Puuid != p2.Puuid
                INNER JOIN LolMatchParticipant enemy
                    ON p1.MatchId = enemy.MatchId
                    AND p1.TeamId != enemy.TeamId
                INNER JOIN LolMatch m ON p1.MatchId = m.MatchId
                WHERE p1.Puuid = @puuid1
                  AND p2.Puuid = @puuid2
                  AND m.InfoFetched = TRUE
                  AND enemy.TeamPosition IS NOT NULL
                  AND enemy.TeamPosition != ''";

            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                sql += " AND m.GameMode = @gameMode";
            }

            sql += @"
                GROUP BY p1.ChampionId, p1.ChampionName, DuoLane1,
                         p2.ChampionId, p2.ChampionName, DuoLane2,
                         enemy.TeamPosition, enemy.ChampionId, enemy.ChampionName
                ORDER BY GamesPlayed DESC";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid1", puuId1);
            cmd.Parameters.AddWithValue("@puuid2", puuId2);

            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                cmd.Parameters.AddWithValue("@gameMode", gameMode);
            }

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                records.Add(new DuoVsEnemyRecord(
                    DuoChampionId1: reader.GetInt32("DuoChampionId1"),
                    DuoChampionName1: reader.GetString("DuoChampionName1"),
                    DuoLane1: reader.GetString("DuoLane1"),
                    DuoChampionId2: reader.GetInt32("DuoChampionId2"),
                    DuoChampionName2: reader.GetString("DuoChampionName2"),
                    DuoLane2: reader.GetString("DuoLane2"),
                    EnemyLane: reader.GetString("EnemyLane"),
                    EnemyChampionId: reader.GetInt32("EnemyChampionId"),
                    EnemyChampionName: reader.GetString("EnemyChampionName"),
                    GamesPlayed: reader.GetInt32("GamesPlayed"),
                    Wins: reader.GetInt32("Wins")
                ));
            }

            return records;
        }

        /// <summary>
        /// Get role distribution for duo games only (when two players play together).
        /// Returns role distribution for each player in duo games.
        /// </summary>
        internal async Task<IList<RoleDistributionRecord>> GetDuoRoleDistributionByPuuIdsAsync(string puuId1, string puuId2, string targetPuuId, string? gameMode = null)
        {
            if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2) || string.IsNullOrWhiteSpace(targetPuuId))
            {
                return new List<RoleDistributionRecord>();
            }

            var records = new List<RoleDistributionRecord>();
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            var sql = @"
                SELECT
                    COALESCE(NULLIF(p1.TeamPosition, ''), 'UNKNOWN') as Position,
                    COUNT(*) as GamesPlayed
                FROM LolMatchParticipant p1
                INNER JOIN LolMatchParticipant p2
                    ON p1.MatchId = p2.MatchId
                    AND p1.TeamId = p2.TeamId
                    AND p1.Puuid != p2.Puuid
                INNER JOIN LolMatch m ON p1.MatchId = m.MatchId
                WHERE p1.Puuid = @targetPuuid
                  AND p2.Puuid = @otherPuuid
                  AND m.InfoFetched = TRUE";

            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                sql += " AND m.GameMode = @gameMode";
            }

            sql += @"
                GROUP BY Position
                ORDER BY GamesPlayed DESC";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@targetPuuid", targetPuuId);
            cmd.Parameters.AddWithValue("@otherPuuid", puuId1 == targetPuuId ? puuId2 : puuId1);

            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                cmd.Parameters.AddWithValue("@gameMode", gameMode);
            }

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var position = reader.GetString("Position");
                var gamesPlayed = reader.GetInt32("GamesPlayed");

                records.Add(new RoleDistributionRecord(position, gamesPlayed));
            }

            return records;
        }

        /// <summary>
        /// Get lane combination statistics for duo games.
        /// Returns games played and wins for each lane combination.
        /// </summary>
        internal async Task<IList<DuoLaneComboRecord>> GetDuoLaneCombosByPuuIdsAsync(string puuId1, string puuId2, string? gameMode = null)
        {
            if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2))
            {
                return new List<DuoLaneComboRecord>();
            }

            var records = new List<DuoLaneComboRecord>();
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            var sql = @"
                SELECT
                    COALESCE(NULLIF(p1.TeamPosition, ''), 'UNKNOWN') as Lane1,
                    COALESCE(NULLIF(p2.TeamPosition, ''), 'UNKNOWN') as Lane2,
                    COUNT(*) as GamesPlayed,
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
            {
                sql += " AND m.GameMode = @gameMode";
            }

            sql += @"
                GROUP BY Lane1, Lane2
                ORDER BY GamesPlayed DESC";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid1", puuId1);
            cmd.Parameters.AddWithValue("@puuid2", puuId2);

            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                cmd.Parameters.AddWithValue("@gameMode", gameMode);
            }

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var lane1 = reader.GetString("Lane1");
                var lane2 = reader.GetString("Lane2");
                var gamesPlayed = reader.GetInt32("GamesPlayed");
                var wins = reader.GetInt32("Wins");

                records.Add(new DuoLaneComboRecord(lane1, lane2, gamesPlayed, wins));
            }

            return records;
        }

        /// <summary>
        /// Get kill efficiency statistics for duo games.
        /// Returns kill participation and death share for each player.
        /// </summary>
        internal async Task<DuoKillEfficiencyRecord?> GetDuoKillEfficiencyByPuuIdsAsync(string puuId1, string puuId2, string targetPuuId, string? gameMode = null)
        {
            if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2) || string.IsNullOrWhiteSpace(targetPuuId))
            {
                return null;
            }

            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            // Get kill participation (player's kills + assists / team's total kills)
            // and death share in losses (player's deaths / team's total deaths in lost games)
            var sql = @"
                SELECT
                    @targetPuuid as PuuId,
                    SUM(p.Kills) as TotalKills,
                    SUM(p.Assists) as TotalAssists,
                    SUM(team_kills.TeamKills) as TeamKills,
                    SUM(CASE WHEN p.Win = 0 THEN p.Deaths ELSE 0 END) as DeathsInLosses,
                    SUM(CASE WHEN p.Win = 0 THEN team_deaths.TeamDeaths ELSE 0 END) as TeamDeathsInLosses
                FROM LolMatchParticipant p
                INNER JOIN LolMatchParticipant p2
                    ON p.MatchId = p2.MatchId
                    AND p.TeamId = p2.TeamId
                    AND p.Puuid != p2.Puuid
                INNER JOIN LolMatch m ON p.MatchId = m.MatchId
                INNER JOIN (
                    SELECT MatchId, TeamId, SUM(Kills) as TeamKills
                    FROM LolMatchParticipant
                    GROUP BY MatchId, TeamId
                ) team_kills ON p.MatchId = team_kills.MatchId AND p.TeamId = team_kills.TeamId
                INNER JOIN (
                    SELECT MatchId, TeamId, SUM(Deaths) as TeamDeaths
                    FROM LolMatchParticipant
                    GROUP BY MatchId, TeamId
                ) team_deaths ON p.MatchId = team_deaths.MatchId AND p.TeamId = team_deaths.TeamId
                WHERE p.Puuid = @targetPuuid
                  AND p2.Puuid = @otherPuuid
                  AND m.InfoFetched = TRUE";

            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                sql += " AND m.GameMode = @gameMode";
            }

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@targetPuuid", targetPuuId);
            cmd.Parameters.AddWithValue("@otherPuuid", puuId1 == targetPuuId ? puuId2 : puuId1);

            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                cmd.Parameters.AddWithValue("@gameMode", gameMode);
            }

            await using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var puuId = reader.GetString("PuuId");
                var totalKills = reader.GetInt32("TotalKills");
                var totalAssists = reader.GetInt32("TotalAssists");
                var teamKills = reader.GetInt32("TeamKills");
                var deathsInLosses = reader.GetInt32("DeathsInLosses");
                var teamDeathsInLosses = reader.GetInt32("TeamDeathsInLosses");

                return new DuoKillEfficiencyRecord(puuId, totalKills, totalAssists, teamKills, deathsInLosses, teamDeathsInLosses);
            }

            return null;
        }

        /// <summary>
        /// Get match duration statistics for duo games (when two players play together).
        /// Returns wins/total games grouped by duration buckets.
        /// </summary>
        internal async Task<IList<DurationBucketRecord>> GetDuoDurationStatsByPuuIdsAsync(string puuId1, string puuId2, string? gameMode = null)
        {
            if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2))
            {
                return new List<DurationBucketRecord>();
            }

            var records = new List<DurationBucketRecord>();
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            // Group matches into 5-minute buckets for duo games
            var sql = @"
                SELECT
                    FLOOR(m.DurationSeconds / 300) * 5 as MinMinutes,
                    COUNT(*) as GamesPlayed,
                    SUM(CASE WHEN p1.Win = 1 THEN 1 ELSE 0 END) as Wins
                FROM LolMatchParticipant p1
                INNER JOIN LolMatchParticipant p2
                    ON p1.MatchId = p2.MatchId
                    AND p1.TeamId = p2.TeamId
                    AND p1.Puuid != p2.Puuid
                INNER JOIN LolMatch m ON p1.MatchId = m.MatchId
                WHERE p1.Puuid = @puuid1
                  AND p2.Puuid = @puuid2
                  AND m.InfoFetched = TRUE
                  AND m.DurationSeconds > 0";

            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                sql += " AND m.GameMode = @gameMode";
            }

            sql += @"
                GROUP BY MinMinutes
                ORDER BY MinMinutes ASC";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid1", puuId1);
            cmd.Parameters.AddWithValue("@puuid2", puuId2);

            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                cmd.Parameters.AddWithValue("@gameMode", gameMode);
            }

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var minMinutes = reader.GetInt32("MinMinutes");
                var maxMinutes = minMinutes + 5;
                var gamesPlayed = reader.GetInt32("GamesPlayed");
                var wins = reader.GetInt32("Wins");

                records.Add(new DurationBucketRecord(minMinutes, maxMinutes, gamesPlayed, wins));
            }

            return records;
        }

        /// <summary>
        /// Get multi-kill statistics for each player in duo games.
        /// </summary>
        internal async Task<IList<DuoMultiKillRecord>> GetDuoMultiKillsAsync(string puuId1, string puuId2, string? gameMode = null)
        {
            if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2))
            {
                return new List<DuoMultiKillRecord>();
            }

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
                {
                    sql += " AND m.GameMode = @gameMode";
                }

                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@puuid", puuId);
                cmd.Parameters.AddWithValue("@otherPuuid", puuId == puuId1 ? puuId2 : puuId1);
                if (!string.IsNullOrWhiteSpace(gameMode))
                {
                    cmd.Parameters.AddWithValue("@gameMode", gameMode);
                }

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
        /// Get duo kills grouped by game duration buckets.
        /// </summary>
        internal async Task<IList<DuoKillsByDurationRecord>> GetDuoKillsByDurationAsync(string puuId1, string puuId2, string? gameMode = null)
        {
            if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2))
            {
                return new List<DuoKillsByDurationRecord>();
            }

            var records = new List<DuoKillsByDurationRecord>();
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            var sql = @"
                SELECT
                    CASE
                        WHEN m.DurationSeconds < 1200 THEN 'under20'
                        WHEN m.DurationSeconds < 1500 THEN '20-25'
                        WHEN m.DurationSeconds < 1800 THEN '25-30'
                        WHEN m.DurationSeconds < 2100 THEN '30-35'
                        WHEN m.DurationSeconds < 2400 THEN '35-40'
                        ELSE '40+'
                    END as DurationBucket,
                    COUNT(*) as GamesPlayed,
                    SUM(p1.Kills + p2.Kills) as TotalTeamKills,
                    SUM(CASE WHEN p1.Win = 1 THEN 1 ELSE 0 END) as Wins
                FROM LolMatchParticipant p1
                INNER JOIN LolMatchParticipant p2 ON p1.MatchId = p2.MatchId AND p1.TeamId = p2.TeamId AND p1.Puuid != p2.Puuid
                INNER JOIN LolMatch m ON p1.MatchId = m.MatchId
                WHERE p1.Puuid = @puuid1
                  AND p2.Puuid = @puuid2
                  AND m.InfoFetched = TRUE
                  AND m.DurationSeconds > 0";

            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                sql += " AND m.GameMode = @gameMode";
            }

            sql += " GROUP BY DurationBucket";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid1", puuId1);
            cmd.Parameters.AddWithValue("@puuid2", puuId2);
            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                cmd.Parameters.AddWithValue("@gameMode", gameMode);
            }

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                records.Add(new DuoKillsByDurationRecord(
                    DurationBucket: reader.GetString("DurationBucket"),
                    GamesPlayed: reader.GetInt32("GamesPlayed"),
                    TotalTeamKills: reader.IsDBNull(reader.GetOrdinal("TotalTeamKills")) ? 0 : reader.GetInt32("TotalTeamKills"),
                    Wins: reader.GetInt32("Wins")
                ));
            }

            return records;
        }

        /// <summary>
        /// Get kill participation for each player in duo games.
        /// </summary>
        internal async Task<IList<DuoKillParticipationRecord>> GetDuoKillParticipationAsync(string puuId1, string puuId2, string? gameMode = null)
        {
            if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2))
            {
                return new List<DuoKillParticipationRecord>();
            }

            var records = new List<DuoKillParticipationRecord>();
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            foreach (var puuId in new[] { puuId1, puuId2 })
            {
                var sql = @"
                    SELECT
                        SUM(p.Kills) as TotalKills,
                        SUM(p.Assists) as TotalAssists,
                        SUM(p.Kills + p2.Kills) as TotalTeamKills,
                        COUNT(*) as GamesPlayed
                    FROM LolMatchParticipant p
                    INNER JOIN LolMatchParticipant p2 ON p.MatchId = p2.MatchId AND p.TeamId = p2.TeamId AND p.Puuid != p2.Puuid
                    INNER JOIN LolMatch m ON p.MatchId = m.MatchId
                    WHERE p.Puuid = @puuid
                      AND p2.Puuid = @otherPuuid
                      AND m.InfoFetched = TRUE
                      AND m.DurationSeconds > 0";

                if (!string.IsNullOrWhiteSpace(gameMode))
                {
                    sql += " AND m.GameMode = @gameMode";
                }

                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@puuid", puuId);
                cmd.Parameters.AddWithValue("@otherPuuid", puuId == puuId1 ? puuId2 : puuId1);
                if (!string.IsNullOrWhiteSpace(gameMode))
                {
                    cmd.Parameters.AddWithValue("@gameMode", gameMode);
                }

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    records.Add(new DuoKillParticipationRecord(
                        PuuId: puuId,
                        TotalKills: reader.IsDBNull(reader.GetOrdinal("TotalKills")) ? 0 : reader.GetInt32("TotalKills"),
                        TotalAssists: reader.IsDBNull(reader.GetOrdinal("TotalAssists")) ? 0 : reader.GetInt32("TotalAssists"),
                        TotalTeamKills: reader.IsDBNull(reader.GetOrdinal("TotalTeamKills")) ? 0 : reader.GetInt32("TotalTeamKills"),
                        GamesPlayed: reader.GetInt32("GamesPlayed")
                    ));
                }
            }

            return records;
        }

        /// <summary>
        /// Get kills trend for duo games (per match data).
        /// </summary>
        internal async Task<IList<DuoKillsTrendRecord>> GetDuoKillsTrendAsync(string puuId1, string puuId2, string? gameMode = null, int limit = 50)
        {
            if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2))
            {
                return new List<DuoKillsTrendRecord>();
            }

            var records = new List<DuoKillsTrendRecord>();
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            var sql = @"
                SELECT
                    p1.MatchId,
                    (p1.Kills + p2.Kills) as TeamKills,
                    p1.Win,
                    m.GameEndTimestamp
                FROM LolMatchParticipant p1
                INNER JOIN LolMatchParticipant p2 ON p1.MatchId = p2.MatchId AND p1.TeamId = p2.TeamId AND p1.Puuid != p2.Puuid
                INNER JOIN LolMatch m ON p1.MatchId = m.MatchId
                WHERE p1.Puuid = @puuid1
                  AND p2.Puuid = @puuid2
                  AND m.InfoFetched = TRUE
                  AND m.DurationSeconds > 0";

            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                sql += " AND m.GameMode = @gameMode";
            }

            sql += " ORDER BY m.GameEndTimestamp ASC LIMIT @limit";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid1", puuId1);
            cmd.Parameters.AddWithValue("@puuid2", puuId2);
            cmd.Parameters.AddWithValue("@limit", limit);
            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                cmd.Parameters.AddWithValue("@gameMode", gameMode);
            }

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                records.Add(new DuoKillsTrendRecord(
                    MatchId: reader.GetString("MatchId"),
                    TeamKills: reader.GetInt32("TeamKills"),
                    Win: reader.GetBoolean("Win"),
                    GameDate: reader.GetDateTime("GameEndTimestamp")
                ));
            }

            return records;
        }

        /// <summary>
        /// Get death timer stats for duo games.
        /// </summary>
        internal async Task<IList<DuoDeathTimerRecord>> GetDuoDeathTimerStatsAsync(string puuId1, string puuId2, string? gameMode = null)
        {
            if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2))
            {
                return new List<DuoDeathTimerRecord>();
            }

            var records = new List<DuoDeathTimerRecord>();
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            foreach (var puuId in new[] { puuId1, puuId2 })
            {
                var sql = @"
                    SELECT
                        AVG(CASE WHEN p.Win = 1 THEN p.TimeBeingDeadSeconds ELSE NULL END) as AvgTimeDeadWins,
                        AVG(CASE WHEN p.Win = 0 THEN p.TimeBeingDeadSeconds ELSE NULL END) as AvgTimeDeadLosses,
                        SUM(CASE WHEN p.Win = 1 THEN 1 ELSE 0 END) as WinGames,
                        SUM(CASE WHEN p.Win = 0 THEN 1 ELSE 0 END) as LossGames
                    FROM LolMatchParticipant p
                    INNER JOIN LolMatchParticipant p2 ON p.MatchId = p2.MatchId AND p.TeamId = p2.TeamId AND p.Puuid != p2.Puuid
                    INNER JOIN LolMatch m ON p.MatchId = m.MatchId
                    WHERE p.Puuid = @puuid
                      AND p2.Puuid = @otherPuuid
                      AND m.InfoFetched = TRUE
                      AND m.DurationSeconds > 0";

                if (!string.IsNullOrWhiteSpace(gameMode))
                {
                    sql += " AND m.GameMode = @gameMode";
                }

                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@puuid", puuId);
                cmd.Parameters.AddWithValue("@otherPuuid", puuId == puuId1 ? puuId2 : puuId1);
                if (!string.IsNullOrWhiteSpace(gameMode))
                {
                    cmd.Parameters.AddWithValue("@gameMode", gameMode);
                }

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    records.Add(new DuoDeathTimerRecord(
                        PuuId: puuId,
                        AvgTimeDeadWins: reader.IsDBNull(reader.GetOrdinal("AvgTimeDeadWins")) ? 0 : reader.GetDouble("AvgTimeDeadWins"),
                        AvgTimeDeadLosses: reader.IsDBNull(reader.GetOrdinal("AvgTimeDeadLosses")) ? 0 : reader.GetDouble("AvgTimeDeadLosses"),
                        WinGames: reader.GetInt32("WinGames"),
                        LossGames: reader.GetInt32("LossGames")
                    ));
                }
            }

            return records;
        }

        /// <summary>
        /// Get deaths by duration for duo games.
        /// </summary>
        internal async Task<IList<DuoDeathsByDurationRecord>> GetDuoDeathsByDurationAsync(string puuId1, string puuId2, string? gameMode = null)
        {
            if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2))
            {
                return new List<DuoDeathsByDurationRecord>();
            }

            var records = new List<DuoDeathsByDurationRecord>();
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            var sql = @"
                SELECT
                    CASE
                        WHEN m.DurationSeconds < 1200 THEN 'under20'
                        WHEN m.DurationSeconds < 1500 THEN '20-25'
                        WHEN m.DurationSeconds < 1800 THEN '25-30'
                        WHEN m.DurationSeconds < 2100 THEN '30-35'
                        WHEN m.DurationSeconds < 2400 THEN '35-40'
                        ELSE '40+'
                    END as DurationBucket,
                    COUNT(*) as GamesPlayed,
                    SUM(p1.Deaths + p2.Deaths) as TotalTeamDeaths,
                    SUM(CASE WHEN p1.Win = 1 THEN 1 ELSE 0 END) as Wins
                FROM LolMatchParticipant p1
                INNER JOIN LolMatchParticipant p2 ON p1.MatchId = p2.MatchId AND p1.TeamId = p2.TeamId AND p1.Puuid != p2.Puuid
                INNER JOIN LolMatch m ON p1.MatchId = m.MatchId
                WHERE p1.Puuid = @puuid1
                  AND p2.Puuid = @puuid2
                  AND m.InfoFetched = TRUE
                  AND m.DurationSeconds > 0";

            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                sql += " AND m.GameMode = @gameMode";
            }

            sql += " GROUP BY DurationBucket";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid1", puuId1);
            cmd.Parameters.AddWithValue("@puuid2", puuId2);
            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                cmd.Parameters.AddWithValue("@gameMode", gameMode);
            }

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                records.Add(new DuoDeathsByDurationRecord(
                    DurationBucket: reader.GetString("DurationBucket"),
                    GamesPlayed: reader.GetInt32("GamesPlayed"),
                    TotalTeamDeaths: reader.IsDBNull(reader.GetOrdinal("TotalTeamDeaths")) ? 0 : reader.GetInt32("TotalTeamDeaths"),
                    Wins: reader.GetInt32("Wins")
                ));
            }

            return records;
        }

        /// <summary>
        /// Get death share for each player in duo games.
        /// </summary>
        internal async Task<IList<DuoDeathShareRecord>> GetDuoDeathShareAsync(string puuId1, string puuId2, string? gameMode = null)
        {
            if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2))
            {
                return new List<DuoDeathShareRecord>();
            }

            var records = new List<DuoDeathShareRecord>();
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            foreach (var puuId in new[] { puuId1, puuId2 })
            {
                var sql = @"
                    SELECT
                        SUM(p.Deaths) as TotalDeaths,
                        COUNT(*) as GamesPlayed
                    FROM LolMatchParticipant p
                    INNER JOIN LolMatchParticipant p2 ON p.MatchId = p2.MatchId AND p.TeamId = p2.TeamId AND p.Puuid != p2.Puuid
                    INNER JOIN LolMatch m ON p.MatchId = m.MatchId
                    WHERE p.Puuid = @puuid
                      AND p2.Puuid = @otherPuuid
                      AND m.InfoFetched = TRUE
                      AND m.DurationSeconds > 0";

                if (!string.IsNullOrWhiteSpace(gameMode))
                {
                    sql += " AND m.GameMode = @gameMode";
                }

                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@puuid", puuId);
                cmd.Parameters.AddWithValue("@otherPuuid", puuId == puuId1 ? puuId2 : puuId1);
                if (!string.IsNullOrWhiteSpace(gameMode))
                {
                    cmd.Parameters.AddWithValue("@gameMode", gameMode);
                }

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    records.Add(new DuoDeathShareRecord(
                        PuuId: puuId,
                        TotalDeaths: reader.IsDBNull(reader.GetOrdinal("TotalDeaths")) ? 0 : reader.GetInt32("TotalDeaths"),
                        GamesPlayed: reader.GetInt32("GamesPlayed")
                    ));
                }
            }

            return records;
        }

        /// <summary>
        /// Get deaths trend for duo games (per match data).
        /// </summary>
        internal async Task<IList<DuoDeathsTrendRecord>> GetDuoDeathsTrendAsync(string puuId1, string puuId2, string? gameMode = null, int limit = 50)
        {
            if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2))
            {
                return new List<DuoDeathsTrendRecord>();
            }

            var records = new List<DuoDeathsTrendRecord>();
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            var sql = @"
                SELECT
                    p1.MatchId,
                    (p1.Deaths + p2.Deaths) as TeamDeaths,
                    p1.Win,
                    m.GameEndTimestamp
                FROM LolMatchParticipant p1
                INNER JOIN LolMatchParticipant p2 ON p1.MatchId = p2.MatchId AND p1.TeamId = p2.TeamId AND p1.Puuid != p2.Puuid
                INNER JOIN LolMatch m ON p1.MatchId = m.MatchId
                WHERE p1.Puuid = @puuid1
                  AND p2.Puuid = @puuid2
                  AND m.InfoFetched = TRUE
                  AND m.DurationSeconds > 0";

            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                sql += " AND m.GameMode = @gameMode";
            }

            sql += " ORDER BY m.GameEndTimestamp ASC LIMIT @limit";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid1", puuId1);
            cmd.Parameters.AddWithValue("@puuid2", puuId2);
            cmd.Parameters.AddWithValue("@limit", limit);
            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                cmd.Parameters.AddWithValue("@gameMode", gameMode);
            }

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                records.Add(new DuoDeathsTrendRecord(
                    MatchId: reader.GetString("MatchId"),
                    TeamDeaths: reader.GetInt32("TeamDeaths"),
                    Win: reader.GetBoolean("Win"),
                    GameDate: reader.GetDateTime("GameEndTimestamp")
                ));
            }

            return records;
        }

        /// <summary>
        /// Get win rate trend for duo games (per match data).
        /// </summary>
        internal async Task<IList<DuoWinRateTrendRecord>> GetDuoWinRateTrendAsync(string puuId1, string puuId2, string? gameMode = null, int limit = 50)
        {
            if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2))
            {
                return new List<DuoWinRateTrendRecord>();
            }

            var records = new List<DuoWinRateTrendRecord>();
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            var sql = @"
                SELECT
                    p1.MatchId,
                    p1.Win,
                    m.GameEndTimestamp
                FROM LolMatchParticipant p1
                INNER JOIN LolMatchParticipant p2 ON p1.MatchId = p2.MatchId AND p1.TeamId = p2.TeamId AND p1.Puuid != p2.Puuid
                INNER JOIN LolMatch m ON p1.MatchId = m.MatchId
                WHERE p1.Puuid = @puuid1
                  AND p2.Puuid = @puuid2
                  AND m.InfoFetched = TRUE
                  AND m.DurationSeconds > 0";

            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                sql += " AND m.GameMode = @gameMode";
            }

            sql += " ORDER BY m.GameEndTimestamp ASC LIMIT @limit";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid1", puuId1);
            cmd.Parameters.AddWithValue("@puuid2", puuId2);
            cmd.Parameters.AddWithValue("@limit", limit);
            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                cmd.Parameters.AddWithValue("@gameMode", gameMode);
            }

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                records.Add(new DuoWinRateTrendRecord(
                    MatchId: reader.GetString("MatchId"),
                    Win: reader.GetBoolean("Win"),
                    GameDate: reader.GetDateTime("GameEndTimestamp")
                ));
            }

            return records;
        }

        /// <summary>
        /// Get combined performance radar metrics for duo games.
        /// </summary>
        internal async Task<DuoPerformanceRadarRecord?> GetDuoPerformanceRadarAsync(string puuId1, string puuId2, string? gameMode = null)
        {
            if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2))
            {
                return null;
            }

            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            var sql = @"
                SELECT
                    AVG((p1.Kills + p2.Kills) / 2.0) as AvgKills,
                    AVG((p1.Deaths + p2.Deaths) / 2.0) as AvgDeaths,
                    AVG((p1.Assists + p2.Assists) / 2.0) as AvgAssists,
                    AVG((p1.CreepScore + p2.CreepScore) / 2.0) as AvgCs,
                    AVG((p1.GoldEarned + p2.GoldEarned) / 2.0) as AvgGoldEarned,
                    COUNT(*) as GamesPlayed,
                    SUM(CASE WHEN p1.Win = 1 THEN 1 ELSE 0 END) as Wins
                FROM LolMatchParticipant p1
                INNER JOIN LolMatchParticipant p2 ON p1.MatchId = p2.MatchId AND p1.TeamId = p2.TeamId AND p1.Puuid != p2.Puuid
                INNER JOIN LolMatch m ON p1.MatchId = m.MatchId
                WHERE p1.Puuid = @puuid1
                  AND p2.Puuid = @puuid2
                  AND m.InfoFetched = TRUE
                  AND m.DurationSeconds > 0";

            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                sql += " AND m.GameMode = @gameMode";
            }

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid1", puuId1);
            cmd.Parameters.AddWithValue("@puuid2", puuId2);
            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                cmd.Parameters.AddWithValue("@gameMode", gameMode);
            }

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var gamesPlayed = reader.GetInt32("GamesPlayed");
                if (gamesPlayed == 0)
                {
                    return null;
                }

                return new DuoPerformanceRadarRecord(
                    AvgKills: reader.IsDBNull(reader.GetOrdinal("AvgKills")) ? 0 : reader.GetDouble("AvgKills"),
                    AvgDeaths: reader.IsDBNull(reader.GetOrdinal("AvgDeaths")) ? 0 : reader.GetDouble("AvgDeaths"),
                    AvgAssists: reader.IsDBNull(reader.GetOrdinal("AvgAssists")) ? 0 : reader.GetDouble("AvgAssists"),
                    AvgCs: reader.IsDBNull(reader.GetOrdinal("AvgCs")) ? 0 : reader.GetDouble("AvgCs"),
                    AvgGoldEarned: reader.IsDBNull(reader.GetOrdinal("AvgGoldEarned")) ? 0 : reader.GetDouble("AvgGoldEarned"),
                    GamesPlayed: gamesPlayed,
                    Wins: reader.GetInt32("Wins")
                );
            }

            return null;
        }

        /// <summary>
        /// Get win/loss streak data for duo games.
        /// </summary>
        internal async Task<DuoStreakRecord?> GetDuoStreakAsync(string puuId1, string puuId2, string? gameMode = null)
        {
            if (string.IsNullOrWhiteSpace(puuId1) || string.IsNullOrWhiteSpace(puuId2))
            {
                return null;
            }

            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            var sql = @"
                SELECT
                    p1.Win,
                    m.GameEndTimestamp
                FROM LolMatchParticipant p1
                INNER JOIN LolMatchParticipant p2 ON p1.MatchId = p2.MatchId AND p1.TeamId = p2.TeamId AND p1.Puuid != p2.Puuid
                INNER JOIN LolMatch m ON p1.MatchId = m.MatchId
                WHERE p1.Puuid = @puuid1
                  AND p2.Puuid = @puuid2
                  AND m.InfoFetched = TRUE
                  AND m.DurationSeconds > 0";

            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                sql += " AND m.GameMode = @gameMode";
            }

            sql += " ORDER BY m.GameEndTimestamp DESC";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid1", puuId1);
            cmd.Parameters.AddWithValue("@puuid2", puuId2);
            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                cmd.Parameters.AddWithValue("@gameMode", gameMode);
            }

            var results = new List<bool>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(reader.GetBoolean("Win"));
            }

            if (results.Count == 0)
            {
                return null;
            }

            // Calculate current streak
            var currentStreak = 0;
            var isWinStreak = results[0];
            foreach (var win in results)
            {
                if (win == isWinStreak)
                {
                    currentStreak++;
                }
                else
                {
                    break;
                }
            }

            // Calculate longest win streak and longest loss streak
            var longestWinStreak = 0;
            var longestLossStreak = 0;
            var tempStreak = 0;
            bool? lastResult = null;

            foreach (var win in results.AsEnumerable().Reverse())
            {
                if (lastResult == null || lastResult == win)
                {
                    tempStreak++;
                }
                else
                {
                    if (lastResult == true)
                    {
                        longestWinStreak = Math.Max(longestWinStreak, tempStreak);
                    }
                    else
                    {
                        longestLossStreak = Math.Max(longestLossStreak, tempStreak);
                    }
                    tempStreak = 1;
                }
                lastResult = win;
            }

            // Handle the last streak
            if (lastResult == true)
            {
                longestWinStreak = Math.Max(longestWinStreak, tempStreak);
            }
            else if (lastResult == false)
            {
                longestLossStreak = Math.Max(longestLossStreak, tempStreak);
            }

            return new DuoStreakRecord(
                CurrentStreak: currentStreak,
                IsWinStreak: isWinStreak,
                LongestWinStreak: longestWinStreak,
                LongestLossStreak: longestLossStreak
            );
        }

        // ========================
        // Team Statistics Methods (3+ players)
        // ========================

        /// <summary>
        /// Get statistics for games where all specified players played together on the same team.
        /// </summary>
        internal async Task<TeamStatsRecord?> GetTeamStatsByPuuIdsAsync(string[] puuIds)
        {
            if (puuIds == null || puuIds.Length < 3)
            {
                return null;
            }

            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            // Build dynamic SQL to find matches where ALL players participated on the same team
            var joinClauses = new List<string>();
            var whereClauses = new List<string>();

            for (int i = 0; i < puuIds.Length; i++)
            {
                if (i == 0)
                {
                    whereClauses.Add($"p0.Puuid = @puuid0");
                }
                else
                {
                    joinClauses.Add($@"
                        INNER JOIN LolMatchParticipant p{i}
                            ON p0.MatchId = p{i}.MatchId
                            AND p0.TeamId = p{i}.TeamId
                            AND p{i}.Puuid = @puuid{i}");
                }
            }

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
                {string.Join("", joinClauses)}
                INNER JOIN LolMatch m ON p0.MatchId = m.MatchId
                WHERE {whereClauses[0]}
                  AND m.InfoFetched = TRUE
                  AND m.DurationSeconds > 0
                GROUP BY m.GameMode
                ORDER BY GamesPlayed DESC
                LIMIT 1";

            await using var cmd = new MySqlCommand(sql, conn);
            for (int i = 0; i < puuIds.Length; i++)
            {
                cmd.Parameters.AddWithValue($"@puuid{i}", puuIds[i]);
            }

            await using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new TeamStatsRecord(
                    GamesPlayed: reader.GetInt32("GamesPlayed"),
                    Wins: reader.GetInt32("Wins"),
                    TotalKills: reader.GetInt32("TotalKills"),
                    TotalDeaths: reader.GetInt32("TotalDeaths"),
                    TotalAssists: reader.GetInt32("TotalAssists"),
                    AvgDurationSeconds: reader.GetDouble("AvgDurationSeconds"),
                    MostCommonGameMode: reader.IsDBNull(reader.GetOrdinal("GameMode")) ? "Unknown" : reader.GetString("GameMode")
                );
            }

            return null;
        }

        /// <summary>
        /// Get pairwise synergy statistics for all player pairs in team games.
        /// </summary>
        internal async Task<IList<PlayerPairSynergyRecord>> GetTeamPairSynergyByPuuIdsAsync(string[] puuIds, string? gameMode = null)
        {
            if (puuIds == null || puuIds.Length < 3)
            {
                return new List<PlayerPairSynergyRecord>();
            }

            var records = new List<PlayerPairSynergyRecord>();
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            // For each pair of players, find their games together within team games
            for (int i = 0; i < puuIds.Length; i++)
            {
                for (int j = i + 1; j < puuIds.Length; j++)
                {
                    var sql = @"
                        SELECT
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
                    {
                        sql += " AND m.GameMode = @gameMode";
                    }

                    await using var cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@puuid1", puuIds[i]);
                    cmd.Parameters.AddWithValue("@puuid2", puuIds[j]);
                    if (!string.IsNullOrWhiteSpace(gameMode))
                    {
                        cmd.Parameters.AddWithValue("@gameMode", gameMode);
                    }

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
        }

        /// <summary>
        /// Get role distribution for each player in team games.
        /// </summary>
        internal async Task<IList<TeamPlayerRoleRecord>> GetTeamRoleDistributionByPuuIdsAsync(string[] puuIds, string? gameMode = null)
        {
            if (puuIds == null || puuIds.Length < 3)
            {
                return new List<TeamPlayerRoleRecord>();
            }

            var records = new List<TeamPlayerRoleRecord>();
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            // Build the team match subquery
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
            {
                teamMatchSubquery += " AND m.GameMode = @gameMode";
            }

            // For each player, get their role distribution in team games
            foreach (var puuId in puuIds)
            {
                var sql = $@"
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

                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@puuid", puuId);
                for (int i = 0; i < puuIds.Length; i++)
                {
                    cmd.Parameters.AddWithValue($"@teamPuuid{i}", puuIds[i]);
                }
                if (!string.IsNullOrWhiteSpace(gameMode))
                {
                    cmd.Parameters.AddWithValue("@gameMode", gameMode);
                }

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
        }

        /// <summary>
        /// Get individual performance for each player in team games.
        /// </summary>
        internal async Task<IList<TeamPlayerPerformanceRecord>> GetTeamPlayerPerformanceByPuuIdsAsync(string[] puuIds, string? gameMode = null)
        {
            if (puuIds == null || puuIds.Length < 3)
            {
                return new List<TeamPlayerPerformanceRecord>();
            }

            var records = new List<TeamPlayerPerformanceRecord>();
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            // Build the team match subquery
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
            {
                teamMatchSubquery += " AND m.GameMode = @gameMode";
            }

            // For each player, get their performance in team games
            foreach (var puuId in puuIds)
            {
                var sql = $@"
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

                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@puuid", puuId);
                for (int i = 0; i < puuIds.Length; i++)
                {
                    cmd.Parameters.AddWithValue($"@teamPuuid{i}", puuIds[i]);
                }
                if (!string.IsNullOrWhiteSpace(gameMode))
                {
                    cmd.Parameters.AddWithValue("@gameMode", gameMode);
                }

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
        }

        /// <summary>
        /// Get team kills and deaths for kill participation calculation.
        /// </summary>
        internal async Task<TeamKillsDeathsRecord?> GetTeamKillsDeathsByPuuIdsAsync(string[] puuIds, string? gameMode = null)
        {
            if (puuIds == null || puuIds.Length < 3)
            {
                return null;
            }

            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            // Build join clauses directly with p0, p1, p2, etc. aliases
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
            {
                sql += " AND m.GameMode = @gameMode";
            }

            await using var cmd = new MySqlCommand(sql, conn);
            for (int i = 0; i < puuIds.Length; i++)
            {
                cmd.Parameters.AddWithValue($"@puuid{i}", puuIds[i]);
            }
            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                cmd.Parameters.AddWithValue("@gameMode", gameMode);
            }

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new TeamKillsDeathsRecord(
                    TeamKills: reader.IsDBNull(reader.GetOrdinal("TeamKills")) ? 0 : reader.GetInt32("TeamKills"),
                    TeamDeaths: reader.IsDBNull(reader.GetOrdinal("TeamDeaths")) ? 0 : reader.GetInt32("TeamDeaths")
                );
            }

            return null;
        }

        /// <summary>
        /// Get team match results for win rate trend analysis.
        /// </summary>
        internal async Task<IList<TeamMatchResultRecord>> GetTeamMatchResultsByPuuIdsAsync(string[] puuIds, int limit = 50, string? gameMode = null)
        {
            if (puuIds == null || puuIds.Length < 3)
            {
                return new List<TeamMatchResultRecord>();
            }

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

            var sql = $@"
                SELECT
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
            {
                sql += " AND m.GameMode = @gameMode";
            }

            sql += " ORDER BY m.GameEndTimestamp DESC LIMIT @limit";

            await using var cmd = new MySqlCommand(sql, conn);
            for (int i = 0; i < puuIds.Length; i++)
            {
                cmd.Parameters.AddWithValue($"@puuid{i}", puuIds[i]);
            }
            cmd.Parameters.AddWithValue("@limit", limit);
            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                cmd.Parameters.AddWithValue("@gameMode", gameMode);
            }

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
        }

        /// <summary>
        /// Get team game duration statistics for duration analysis.
        /// </summary>
        internal async Task<IList<TeamDurationRecord>> GetTeamDurationStatsByPuuIdsAsync(string[] puuIds, string? gameMode = null)
        {
            if (puuIds == null || puuIds.Length < 3)
            {
                return new List<TeamDurationRecord>();
            }

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

            // Use 5-minute intervals: <20, 20-25, 25-30, 30-35, 35-40, 40+
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
                    COUNT(*) as GamesPlayed,
                    SUM(CASE WHEN p0.Win = 1 THEN 1 ELSE 0 END) as Wins
                FROM LolMatchParticipant p0
                {string.Join("", joinClauses)}
                INNER JOIN LolMatch m ON p0.MatchId = m.MatchId
                WHERE p0.Puuid = @puuid0
                  AND m.InfoFetched = TRUE
                  AND m.DurationSeconds > 0";

            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                sql += " AND m.GameMode = @gameMode";
            }

            sql += " GROUP BY DurationBucket ORDER BY DurationBucket";

            await using var cmd = new MySqlCommand(sql, conn);
            for (int i = 0; i < puuIds.Length; i++)
            {
                cmd.Parameters.AddWithValue($"@puuid{i}", puuIds[i]);
            }
            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                cmd.Parameters.AddWithValue("@gameMode", gameMode);
            }

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
        }

        /// <summary>
        /// Get team champion combinations for combo analysis.
        /// </summary>
        internal async Task<IList<TeamChampionComboRecord>> GetTeamChampionCombosByPuuIdsAsync(string[] puuIds, int limit = 10, string? gameMode = null)
        {
            if (puuIds == null || puuIds.Length < 3)
            {
                return new List<TeamChampionComboRecord>();
            }

            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            // Build join clauses and select columns for all team members
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
            {
                sql += " AND m.GameMode = @gameMode";
            }

            // Group by all champion IDs and Puuids
            var groupByColumns = Enumerable.Range(0, puuIds.Length).Select(i => $"p{i}.ChampionId, p{i}.ChampionName, p{i}.Puuid");
            sql += $" GROUP BY {string.Join(", ", groupByColumns)}";
            // Sort by win rate (descending), then by games played (descending) as tiebreaker
            // This ensures combinations with higher win rates appear first
            sql += " ORDER BY (SUM(CASE WHEN p0.Win = 1 THEN 1 ELSE 0 END) / COUNT(*)) DESC, GamesPlayed DESC LIMIT @limit";

            await using var cmd = new MySqlCommand(sql, conn);
            for (int i = 0; i < puuIds.Length; i++)
            {
                cmd.Parameters.AddWithValue($"@puuid{i}", puuIds[i]);
            }
            cmd.Parameters.AddWithValue("@limit", limit);
            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                cmd.Parameters.AddWithValue("@gameMode", gameMode);
            }

            var results = new List<TeamChampionComboRecord>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                // GameName is looked up separately in the endpoint from the Gamer table
                var champions = new List<(string Puuid, int ChampionId, string ChampionName, string GameName)>();
                for (int i = 0; i < puuIds.Length; i++)
                {
                    champions.Add((
                        reader.GetString($"Puuid{i}"),
                        reader.GetInt32($"ChampionId{i}"),
                        reader.GetString($"ChampionName{i}"),
                        "" // GameName is populated by endpoint using gamerNames lookup
                    ));
                }

                results.Add(new TeamChampionComboRecord(
                    Champions: champions,
                    GamesPlayed: reader.GetInt32("GamesPlayed"),
                    Wins: reader.GetInt32("Wins")
                ));
            }

            return results;
        }

        /// <summary>
        /// Get role pair effectiveness statistics.
        /// </summary>
        internal async Task<IList<TeamRolePairRecord>> GetTeamRolePairStatsByPuuIdsAsync(string[] puuIds, string? gameMode = null)
        {
            if (puuIds == null || puuIds.Length < 3)
            {
                return new List<TeamRolePairRecord>();
            }

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

            // We need to query all matches first, then aggregate role pairs in code
            // because role pairs are dynamic based on which players filled which roles
            var selectColumns = new List<string> { "p0.TeamPosition as Role0" };
            for (int i = 1; i < puuIds.Length; i++)
            {
                selectColumns.Add($"p{i}.TeamPosition as Role{i}");
            }

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
            {
                sql += " AND m.GameMode = @gameMode";
            }

            await using var cmd = new MySqlCommand(sql, conn);
            for (int i = 0; i < puuIds.Length; i++)
            {
                cmd.Parameters.AddWithValue($"@puuid{i}", puuIds[i]);
            }
            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                cmd.Parameters.AddWithValue("@gameMode", gameMode);
            }

            // Aggregate role pair stats in code
            var rolePairStats = new Dictionary<string, (int Games, int Wins)>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var roles = new List<string>();
                for (int i = 0; i < puuIds.Length; i++)
                {
                    var role = reader.IsDBNull(reader.GetOrdinal($"Role{i}")) ? "" : reader.GetString($"Role{i}");
                    if (!string.IsNullOrEmpty(role))
                    {
                        roles.Add(role);
                    }
                }

                var win = reader.GetBoolean("Win");

                // Generate all unique role pairs from this match
                for (int i = 0; i < roles.Count; i++)
                {
                    for (int j = i + 1; j < roles.Count; j++)
                    {
                        // Sort roles to ensure consistent key
                        var pair = string.Compare(roles[i], roles[j]) <= 0
                            ? $"{roles[i]}|{roles[j]}"
                            : $"{roles[j]}|{roles[i]}";

                        if (!rolePairStats.ContainsKey(pair))
                        {
                            rolePairStats[pair] = (0, 0);
                        }

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
        }

        // ========================
        // Death Analysis Methods
        // ========================

        /// <summary>
        /// Get death timer stats per player in wins vs losses for team games.
        /// </summary>
        internal async Task<IList<PlayerDeathTimerRecord>> GetTeamDeathTimerStatsByPuuIdsAsync(string[] puuIds, string? gameMode = null)
        {
            if (puuIds == null || puuIds.Length < 3)
            {
                return new List<PlayerDeathTimerRecord>();
            }

            var records = new List<PlayerDeathTimerRecord>();
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

            // Query for each player's death stats in wins vs losses
            foreach (var targetPuuId in puuIds)
            {
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
                {
                    sql += " AND m.GameMode = @gameMode";
                }

                await using var cmd = new MySqlCommand(sql, conn);
                for (int i = 0; i < puuIds.Length; i++)
                {
                    cmd.Parameters.AddWithValue($"@puuid{i}", puuIds[i]);
                }
                cmd.Parameters.AddWithValue("@targetPuuid", targetPuuId);
                if (!string.IsNullOrWhiteSpace(gameMode))
                {
                    cmd.Parameters.AddWithValue("@gameMode", gameMode);
                }

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
        }

        /// <summary>
        /// Get team deaths grouped by game duration buckets (5-minute intervals).
        /// </summary>
        internal async Task<IList<TeamDeathsByDurationRecord>> GetTeamDeathsByDurationAsync(string[] puuIds, string? gameMode = null)
        {
            if (puuIds == null || puuIds.Length < 3)
            {
                return new List<TeamDeathsByDurationRecord>();
            }

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

            // Sum deaths for all team members
            var deathsSum = string.Join(" + ", Enumerable.Range(0, puuIds.Length).Select(i => $"p{i}.Deaths"));

            // Use 5-minute intervals: <20, 20-25, 25-30, 30-35, 35-40, 40+
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
                    SUM(CASE WHEN p0.Win = 1 THEN 1 ELSE 0 END) as Wins,
                    SUM({deathsSum}) as TotalTeamDeaths
                FROM LolMatchParticipant p0
                {string.Join("", joinClauses)}
                INNER JOIN LolMatch m ON p0.MatchId = m.MatchId
                WHERE p0.Puuid = @puuid0
                  AND m.InfoFetched = TRUE";

            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                sql += " AND m.GameMode = @gameMode";
            }

            sql += " GROUP BY DurationBucket ORDER BY DurationBucket";

            await using var cmd = new MySqlCommand(sql, conn);
            for (int i = 0; i < puuIds.Length; i++)
            {
                cmd.Parameters.AddWithValue($"@puuid{i}", puuIds[i]);
            }
            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                cmd.Parameters.AddWithValue("@gameMode", gameMode);
            }

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
        }

        /// <summary>
        /// Get team match results with death counts for trend analysis.
        /// </summary>
        internal async Task<IList<TeamMatchDeathRecord>> GetTeamMatchDeathsAsync(string[] puuIds, int limit = 50, string? gameMode = null)
        {
            if (puuIds == null || puuIds.Length < 3)
            {
                return new List<TeamMatchDeathRecord>();
            }

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

            // Sum deaths for all team members
            var deathsSum = string.Join(" + ", Enumerable.Range(0, puuIds.Length).Select(i => $"p{i}.Deaths"));

            var sql = $@"
                SELECT
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
            {
                sql += " AND m.GameMode = @gameMode";
            }

            sql += " ORDER BY m.GameEndTimestamp ASC LIMIT @limit";

            await using var cmd = new MySqlCommand(sql, conn);
            for (int i = 0; i < puuIds.Length; i++)
            {
                cmd.Parameters.AddWithValue($"@puuid{i}", puuIds[i]);
            }
            cmd.Parameters.AddWithValue("@limit", limit);
            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                cmd.Parameters.AddWithValue("@gameMode", gameMode);
            }

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
        }

        // ========================
        // Kill Analysis Methods
        // ========================

        /// <summary>
        /// Get team match results with kill counts for trend analysis.
        /// </summary>
        internal async Task<IList<TeamMatchKillRecord>> GetTeamMatchKillsAsync(string[] puuIds, int limit = 50, string? gameMode = null)
        {
            if (puuIds == null || puuIds.Length < 3)
            {
                return new List<TeamMatchKillRecord>();
            }

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

            // Sum kills for all team members
            var killsSum = string.Join(" + ", Enumerable.Range(0, puuIds.Length).Select(i => $"p{i}.Kills"));

            var sql = $@"
                SELECT
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
            {
                sql += " AND m.GameMode = @gameMode";
            }

            sql += " ORDER BY m.GameEndTimestamp ASC LIMIT @limit";

            await using var cmd = new MySqlCommand(sql, conn);
            for (int i = 0; i < puuIds.Length; i++)
            {
                cmd.Parameters.AddWithValue($"@puuid{i}", puuIds[i]);
            }
            cmd.Parameters.AddWithValue("@limit", limit);
            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                cmd.Parameters.AddWithValue("@gameMode", gameMode);
            }

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
        }

        /// <summary>
        /// Get team kills grouped by game duration buckets (5-minute intervals).
        /// </summary>
        internal async Task<IList<TeamKillsByDurationRecord>> GetTeamKillsByDurationAsync(string[] puuIds, string? gameMode = null)
        {
            if (puuIds == null || puuIds.Length < 3)
            {
                return new List<TeamKillsByDurationRecord>();
            }

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

            // Sum kills for all team members
            var killsSum = string.Join(" + ", Enumerable.Range(0, puuIds.Length).Select(i => $"p{i}.Kills"));

            // Use 5-minute intervals: <20, 20-25, 25-30, 30-35, 35-40, 40+
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
                    COUNT(*) as GamesPlayed,
                    SUM(CASE WHEN p0.Win = 1 THEN 1 ELSE 0 END) as Wins,
                    SUM({killsSum}) as TotalTeamKills
                FROM LolMatchParticipant p0
                {string.Join("", joinClauses)}
                INNER JOIN LolMatch m ON p0.MatchId = m.MatchId
                WHERE p0.Puuid = @puuid0
                  AND m.InfoFetched = TRUE
                  AND m.DurationSeconds > 0";

            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                sql += " AND m.GameMode = @gameMode";
            }

            sql += " GROUP BY DurationBucket ORDER BY DurationBucket";

            await using var cmd = new MySqlCommand(sql, conn);
            for (int i = 0; i < puuIds.Length; i++)
            {
                cmd.Parameters.AddWithValue($"@puuid{i}", puuIds[i]);
            }
            if (!string.IsNullOrWhiteSpace(gameMode))
            {
                cmd.Parameters.AddWithValue("@gameMode", gameMode);
            }

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
        }

        /// <summary>
        /// Get multi-kill statistics for each player in team games.
        /// </summary>
        internal async Task<IList<PlayerMultiKillRecord>> GetTeamMultiKillsAsync(string[] puuIds, string? gameMode = null)
        {
            if (puuIds == null || puuIds.Length < 3)
            {
                return new List<PlayerMultiKillRecord>();
            }

            var records = new List<PlayerMultiKillRecord>();
            await using var conn = _factory.CreateConnection();
            await conn.OpenAsync();

            // Build the team match subquery
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
            {
                teamMatchSubquery += " AND m.GameMode = @gameMode";
            }

            // For each player, get their multi-kill stats in team games
            foreach (var puuId in puuIds)
            {
                var sql = $@"
                    SELECT
                        SUM(p.DoubleKills) as DoubleKills,
                        SUM(p.TripleKills) as TripleKills,
                        SUM(p.QuadraKills) as QuadraKills,
                        SUM(p.PentaKills) as PentaKills
                    FROM LolMatchParticipant p
                    WHERE p.Puuid = @puuid
                      AND p.MatchId IN ({teamMatchSubquery})";

                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@puuid", puuId);
                for (int i = 0; i < puuIds.Length; i++)
                {
                    cmd.Parameters.AddWithValue($"@teamPuuid{i}", puuIds[i]);
                }
                if (!string.IsNullOrWhiteSpace(gameMode))
                {
                    cmd.Parameters.AddWithValue("@gameMode", gameMode);
                }

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
        }
    }
}