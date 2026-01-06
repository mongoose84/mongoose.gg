using MySqlConnector;
using RiotProxy.External.Domain.Entities;
using RiotProxy.Infrastructure.External.Database.Records;

namespace RiotProxy.Infrastructure.External.Database.Repositories
{
    public class LolMatchParticipantRepository : RepositoryBase
    {
        public LolMatchParticipantRepository(IDbConnectionFactory factory) : base(factory)
        {
        }

        public async Task AddParticipantIfNotExistsAsync(LolMatchParticipant participant)
        {
            const string sql = "INSERT IGNORE INTO LolMatchParticipant (MatchId, Puuid, TeamId, Win, Role, TeamPosition, Lane, ChampionId, ChampionName, Kills, Deaths, Assists, DoubleKills, TripleKills, QuadraKills, PentaKills, GoldEarned, TimeBeingDeadSeconds, CreepScore) " +
                               "VALUES (@matchId, @puuid, @teamId, @win, @role, @teamPosition, @lane, @championId, @championName, @kills, @deaths, @assists, @doubleKills, @tripleKills, @quadraKills, @pentaKills, @goldEarned, @timeBeingDeadSeconds, @creepScore)";
            
            await ExecuteNonQueryAsync(sql,
                ("@matchId", participant.MatchId),
                ("@puuid", participant.PuuId),
                ("@teamId", participant.TeamId),
                ("@win", participant.Win),
                ("@role", participant.Role),
                ("@teamPosition", participant.TeamPosition),
                ("@lane", participant.Lane),
                ("@championId", participant.ChampionId),
                ("@championName", participant.ChampionName),
                ("@kills", participant.Kills),
                ("@deaths", participant.Deaths),
                ("@assists", participant.Assists),
                ("@doubleKills", participant.DoubleKills),
                ("@tripleKills", participant.TripleKills),
                ("@quadraKills", participant.QuadraKills),
                ("@pentaKills", participant.PentaKills),
                ("@goldEarned", participant.GoldEarned),
                ("@timeBeingDeadSeconds", participant.TimeBeingDeadSeconds),
                ("@creepScore", participant.CreepScore));
        }

        public async Task<IList<string>> GetMatchIdsForPuuidAsync(string puuId)
        {
            const string sql = "SELECT MatchId FROM LolMatchParticipant WHERE Puuid = @puuid";
            return await ExecuteListAsync(sql, r => r.GetString(0), ("@puuid", puuId));
        }

        /// <summary>
        /// Get all aggregate statistics for a player in a single query.
        /// This consolidates multiple individual stat queries into one database round-trip.
        /// </summary>
        public async Task<PlayerAggregateStatsRecord> GetAggregateStatsByPuuIdAsync(string puuId)
        {
            const string sql = @"
                SELECT
                    -- Basic stats (all games)
                    COUNT(*) as TotalMatches,
                    SUM(CASE WHEN p.Win = 1 THEN 1 ELSE 0 END) as Wins,
                    COALESCE(SUM(p.Kills), 0) as TotalKills,
                    COALESCE(SUM(p.Deaths), 0) as TotalDeaths,
                    COALESCE(SUM(p.Assists), 0) as TotalAssists,
                    COALESCE(SUM(p.CreepScore), 0) as TotalCreepScore,
                    COALESCE(SUM(p.GoldEarned), 0) as TotalGoldEarned,
                    COALESCE(SUM(m.DurationSeconds), 0) as TotalDurationPlayedSeconds,
                    COALESCE(SUM(p.TimeBeingDeadSeconds), 0) as TotalTimeBeingDeadSeconds,
                    -- ARAM-excluded stats (for CS/min and Gold/min calculations)
                    COALESCE(SUM(CASE WHEN m.GameMode != 'ARAM' THEN p.CreepScore ELSE 0 END), 0) as TotalCreepScoreExcludingAram,
                    COALESCE(SUM(CASE WHEN m.GameMode != 'ARAM' THEN p.GoldEarned ELSE 0 END), 0) as TotalGoldEarnedExcludingAram,
                    COALESCE(SUM(CASE WHEN m.GameMode != 'ARAM' THEN m.DurationSeconds ELSE 0 END), 0) as TotalDurationExcludingAramSeconds
                FROM LolMatchParticipant p
                INNER JOIN LolMatch m ON p.MatchId = m.MatchId
                WHERE p.Puuid = @puuid";

            return await ExecuteSingleAsync(sql, r => new PlayerAggregateStatsRecord(
                TotalMatches: r.GetInt32("TotalMatches"),
                Wins: r.GetInt32("Wins"),
                TotalKills: r.GetInt32("TotalKills"),
                TotalDeaths: r.GetInt32("TotalDeaths"),
                TotalAssists: r.GetInt32("TotalAssists"),
                TotalCreepScore: r.GetInt32("TotalCreepScore"),
                TotalGoldEarned: r.GetInt32("TotalGoldEarned"),
                TotalDurationPlayedSeconds: r.GetInt64("TotalDurationPlayedSeconds"),
                TotalTimeBeingDeadSeconds: r.GetInt32("TotalTimeBeingDeadSeconds"),
                TotalCreepScoreExcludingAram: r.GetInt32("TotalCreepScoreExcludingAram"),
                TotalGoldEarnedExcludingAram: r.GetInt32("TotalGoldEarnedExcludingAram"),
                TotalDurationExcludingAramSeconds: r.GetInt64("TotalDurationExcludingAramSeconds")
            ), ("@puuid", puuId)) ?? new PlayerAggregateStatsRecord(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        }

        internal async Task<int> GetMatchesCountByPuuIdAsync(string puuId)
        {
            const string sql = "SELECT COUNT(*) FROM LolMatchParticipant WHERE Puuid = @puuid";
            return await ExecuteScalarAsync<int>(sql, ("@puuid", puuId));
        }

        /// <summary>
        /// Gets the timestamp of the most recent game played by a specific player.
        /// </summary>
        internal async Task<DateTime?> GetLatestGameTimestampByPuuIdAsync(string puuId)
        {
            const string sql = @"
                SELECT MAX(m.GameEndTimestamp)
                FROM LolMatchParticipant p
                INNER JOIN LolMatch m ON p.MatchId = m.MatchId
                WHERE p.Puuid = @puuid
                  AND m.InfoFetched = TRUE
                  AND m.GameEndTimestamp IS NOT NULL";

            return await ExecuteScalarAsync<DateTime?>(sql, ("@puuid", puuId));
        }

        /// <summary>
        /// Gets detailed information about the most recent game played by a specific player.
        /// </summary>
        internal async Task<LatestGameRecord?> GetLatestGameDetailsByPuuIdAsync(string puuId)
        {
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

            return await ExecuteSingleAsync(sql, r => new LatestGameRecord(
                GameEndTimestamp: r.GetDateTime("GameEndTimestamp"),
                Win: r.GetBoolean("Win"),
                Role: r.GetString("Role"),
                ChampionId: r.GetInt32("ChampionId"),
                ChampionName: r.GetString("ChampionName"),
                Kills: r.GetInt32("Kills"),
                Deaths: r.GetInt32("Deaths"),
                Assists: r.GetInt32("Assists")
            ), ("@puuid", puuId));
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
            return await ExecuteWithConnectionAsync(async conn =>
            {
                var records = new List<MatchPerformanceRecord>();

                // Build SQL dynamically based on parameters
                string sql;
                if (limit.HasValue)
                {
                    sql = @"
                        SELECT * FROM (
                            SELECT p.Win, p.GoldEarned, p.CreepScore, m.DurationSeconds, m.GameEndTimestamp
                            FROM LolMatchParticipant p
                            INNER JOIN LolMatch m ON p.MatchId = m.MatchId
                            WHERE p.Puuid = @puuid AND m.InfoFetched = TRUE AND m.DurationSeconds > 0 AND m.GameMode != 'ARAM'";
                    if (fromDate.HasValue) sql += " AND m.GameEndTimestamp >= @fromDate";
                    sql += " ORDER BY m.GameEndTimestamp DESC LIMIT @limit) AS recent_matches ORDER BY GameEndTimestamp ASC";
                }
                else
                {
                    sql = @"
                        SELECT p.Win, p.GoldEarned, p.CreepScore, m.DurationSeconds, m.GameEndTimestamp
                        FROM LolMatchParticipant p
                        INNER JOIN LolMatch m ON p.MatchId = m.MatchId
                        WHERE p.Puuid = @puuid AND m.InfoFetched = TRUE AND m.DurationSeconds > 0 AND m.GameMode != 'ARAM'";
                    if (fromDate.HasValue) sql += " AND m.GameEndTimestamp >= @fromDate";
                    sql += " ORDER BY m.GameEndTimestamp ASC";
                }

                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@puuid", puuId);
                if (fromDate.HasValue) cmd.Parameters.AddWithValue("@fromDate", fromDate.Value);
                if (limit.HasValue) cmd.Parameters.AddWithValue("@limit", limit.Value);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var durationSeconds = reader.GetInt64(3);
                    records.Add(new MatchPerformanceRecord(
                        Win: reader.GetBoolean(0),
                        GoldEarned: reader.GetInt32(1),
                        CreepScore: reader.GetInt32(2),
                        DurationMinutes: durationSeconds / 60.0,
                        GameEndTimestamp: reader.IsDBNull(4) ? DateTime.MinValue : reader.GetDateTime(4)
                    ));
                }
                return records;
            });
        }

        /// <summary>
        /// Get champion statistics (games played, wins) grouped by champion for a specific puuid.
        /// Excludes ARAM games.
        /// </summary>
        internal async Task<IList<ChampionStatsRecord>> GetChampionStatsByPuuIdAsync(string puuId)
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
        /// Returns count of games played in each role/position.
        /// Excludes ARAM games.
        /// </summary>
        internal async Task<IList<RoleDistributionRecord>> GetRoleDistributionByPuuIdAsync(string puuId)
        {
            // Use TeamPosition as it's more reliable than Role or Lane in modern League
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
        /// Get side statistics (blue/red) for a specific puuid.
        /// TeamId 100 = Blue side, TeamId 200 = Red side.
        /// Excludes ARAM games.
        /// </summary>
        internal async Task<SideStatsRecord> GetSideStatsByPuuIdAsync(string puuId)
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
                BlueGames: r.GetInt32("BlueGames"),
                BlueWins: r.GetInt32("BlueWins"),
                RedGames: r.GetInt32("RedGames"),
                RedWins: r.GetInt32("RedWins")
            ), ("@puuid", puuId)) ?? new SideStatsRecord(0, 0, 0, 0);
        }

        /// <summary>
        /// Get match duration statistics (wins/total games) grouped by duration buckets for a specific puuid.
        /// Excludes ARAM games.
        /// </summary>
        internal async Task<IList<DurationBucketRecord>> GetDurationStatsByPuuIdAsync(string puuId)
        {
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
                  AND m.GameMode != 'ARAM'
                GROUP BY MinMinutes
                ORDER BY MinMinutes ASC";

            return await ExecuteListAsync(sql, r =>
            {
                var minMinutes = r.GetInt32("MinMinutes");
                return new DurationBucketRecord(minMinutes, minMinutes + 5, r.GetInt32("GamesPlayed"), r.GetInt32("Wins"));
            }, ("@puuid", puuId));
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

            return await ExecuteWithConnectionAsync(async conn =>
            {
                var records = new List<ChampionMatchupRecord>();
                var puuidParams = string.Join(",", puuIds.Select((_, i) => $"@puuid{i}"));

                var sql = $@"
                    SELECT player.ChampionId, player.ChampionName, player.TeamPosition as Role,
                           opponent.ChampionId as OpponentChampionId, opponent.ChampionName as OpponentChampionName,
                           COUNT(*) as GamesPlayed, SUM(CASE WHEN player.Win = 1 THEN 1 ELSE 0 END) as Wins
                    FROM LolMatchParticipant player
                    INNER JOIN LolMatchParticipant opponent
                        ON player.MatchId = opponent.MatchId AND player.TeamId != opponent.TeamId AND player.TeamPosition = opponent.TeamPosition
                    INNER JOIN LolMatch m ON player.MatchId = m.MatchId
                    WHERE player.Puuid IN ({puuidParams})
                        AND player.TeamPosition IS NOT NULL AND player.TeamPosition != '' AND m.GameMode != 'ARAM'
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
                    records.Add(new ChampionMatchupRecord(
                        reader.GetInt32("ChampionId"),
                        reader.GetString("ChampionName"),
                        reader.GetString("Role"),
                        reader.GetInt32("OpponentChampionId"),
                        reader.GetString("OpponentChampionName"),
                        reader.GetInt32("GamesPlayed"),
                        reader.GetInt32("Wins")
                    ));
                }
                return records;
            });
        }

        /// <summary>
        /// Get performance statistics for solo games (when a player plays without a specific partner).
        /// Excludes ARAM games when gameMode is not specified.
        /// </summary>
        internal async Task<PlayerPerformanceRecord?> GetSoloPerformanceByPuuIdAsync(string puuId, string excludePuuId, string? gameMode = null)
        {
            if (string.IsNullOrWhiteSpace(puuId))
            {
                return null;
            }

            return await ExecuteWithConnectionAsync(async conn =>
            {
                var sql = @"
                    SELECT COUNT(DISTINCT p.MatchId) as GamesPlayed,
                           SUM(CASE WHEN p.Win = 1 THEN 1 ELSE 0 END) as Wins,
                           SUM(p.Kills) as TotalKills, SUM(p.Deaths) as TotalDeaths, SUM(p.Assists) as TotalAssists,
                           SUM(p.GoldEarned) as TotalGoldEarned, SUM(m.DurationSeconds) as TotalDurationSeconds
                    FROM LolMatchParticipant p
                    INNER JOIN LolMatch m ON p.MatchId = m.MatchId
                    WHERE p.Puuid = @puuid AND m.InfoFetched = TRUE AND m.DurationSeconds > 0
                      AND NOT EXISTS (SELECT 1 FROM LolMatchParticipant p2 WHERE p2.MatchId = p.MatchId AND p2.TeamId = p.TeamId AND p2.Puuid = @excludePuuid)";

                sql += !string.IsNullOrWhiteSpace(gameMode) ? " AND m.GameMode = @gameMode" : " AND m.GameMode != 'ARAM'";

                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@puuid", puuId);
                cmd.Parameters.AddWithValue("@excludePuuid", excludePuuId);
                if (!string.IsNullOrWhiteSpace(gameMode)) cmd.Parameters.AddWithValue("@gameMode", gameMode);

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
            });
        }
    }
}