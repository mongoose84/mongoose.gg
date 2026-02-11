using MySqlConnector;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Core.QueryModels;

namespace Mongoose.Api.Infrastructure.Database.Repositories;

public class ParticipantsRepository : RepositoryBase, IParticipantsRepository
{
    public ParticipantsRepository(IDbConnectionFactory factory) : base(factory) {}

    public Task<long> InsertAsync(Participant p)
    {
        const string sql = @"INSERT INTO participants
            (match_id, puuid, team_id, role, lane, champion_id, champion_name, win, kills, deaths, assists, creep_score, gold_earned, time_dead_sec, lp_after, tier_after, rank_after, is_lp_estimated, created_at)
            VALUES (@match_id, @puuid, @team_id, @role, @lane, @champion_id, @champion_name, @win, @kills, @deaths, @assists, @creep_score, @gold_earned, @time_dead_sec, @lp_after, @tier_after, @rank_after, @is_lp_estimated, @created_at) AS new
            ON DUPLICATE KEY UPDATE
                team_id = new.team_id,
                role = new.role,
                lane = new.lane,
                champion_id = new.champion_id,
                champion_name = new.champion_name,
                win = new.win,
                kills = new.kills,
                deaths = new.deaths,
                assists = new.assists,
                creep_score = new.creep_score,
                gold_earned = new.gold_earned,
                time_dead_sec = new.time_dead_sec,
                lp_after = new.lp_after,
                tier_after = new.tier_after,
                rank_after = new.rank_after,
                is_lp_estimated = new.is_lp_estimated;";

        return ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@match_id", p.MatchId);
            cmd.Parameters.AddWithValue("@puuid", p.Puuid);
            cmd.Parameters.AddWithValue("@team_id", p.TeamId);
            cmd.Parameters.AddWithValue("@role", p.Role ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@lane", p.Lane ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@champion_id", p.ChampionId);
            cmd.Parameters.AddWithValue("@champion_name", p.ChampionName);
            cmd.Parameters.AddWithValue("@win", p.Win);
            cmd.Parameters.AddWithValue("@kills", p.Kills);
            cmd.Parameters.AddWithValue("@deaths", p.Deaths);
            cmd.Parameters.AddWithValue("@assists", p.Assists);
            cmd.Parameters.AddWithValue("@creep_score", p.CreepScore);
            cmd.Parameters.AddWithValue("@gold_earned", p.GoldEarned);
            cmd.Parameters.AddWithValue("@time_dead_sec", p.TimeDeadSec);
            cmd.Parameters.AddWithValue("@lp_after", p.LpAfter ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@tier_after", p.TierAfter ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@rank_after", p.RankAfter ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@is_lp_estimated", p.IsLpEstimated);
            cmd.Parameters.AddWithValue("@created_at", p.CreatedAt == default ? DateTime.UtcNow : p.CreatedAt);
            await cmd.ExecuteNonQueryAsync();
            if (cmd.LastInsertedId != 0)
            {
                return cmd.LastInsertedId;
            }
            // If duplicate, fetch the existing participant's ID
            const string idSql = "SELECT id FROM participants WHERE match_id = @match_id AND puuid = @puuid LIMIT 1";
            await using var idCmd = new MySqlCommand(idSql, conn);
            idCmd.Parameters.AddWithValue("@match_id", p.MatchId);
            idCmd.Parameters.AddWithValue("@puuid", p.Puuid);
            var result = await idCmd.ExecuteScalarAsync();
            if (result != null && result != DBNull.Value)
            {
                return Convert.ToInt64(result);
            }
            throw new InvalidOperationException($"Failed to insert or find participant for match_id={p.MatchId}, puuid={p.Puuid}");
        });
    }

    public Task<IList<Participant>> GetByMatchAsync(string matchId)
    {
        const string sql = "SELECT * FROM participants WHERE match_id = @match_id";
        return ExecuteListAsync(sql, Map, ("@match_id", matchId));
    }

    /// <summary>
    /// Updates LP and rank data for a participant record.
    /// Used to set LP/rank after syncing a ranked match.
    /// </summary>
    public Task UpdateLpDataAsync(string matchId, string puuid, int? lp, string? tier, string? rank)
    {
        const string sql = @"UPDATE participants
            SET lp_after = @lp_after, tier_after = @tier_after, rank_after = @rank_after
            WHERE match_id = @match_id AND puuid = @puuid";

        return ExecuteNonQueryAsync(sql,
            ("@match_id", matchId),
            ("@puuid", puuid),
            ("@lp_after", lp ?? (object)DBNull.Value),
            ("@tier_after", tier ?? (object)DBNull.Value),
            ("@rank_after", rank ?? (object)DBNull.Value));
    }

    public virtual async Task<ISet<string>> GetMatchIdsForPuuidAsync(string puuid)
    {
        const string sql = "SELECT match_id FROM participants WHERE puuid = @puuid";
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", puuid);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                ids.Add(reader.GetString(0));
            }
            return 0; // dummy return to satisfy signature
        });
        return ids;
    }

    public Task<IList<Participant>> GetRecentByPuuidAsync(string puuid, int? queueId, int limit)
    {
        var sql = @"SELECT p.* FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            WHERE p.puuid = @puuid";
        if (queueId.HasValue)
        {
            sql += " AND m.queue_id = @queue_id";
        }
        sql += " ORDER BY m.game_start_time DESC LIMIT @limit";

        var parameters = new List<(string, object?)> { ("@puuid", puuid), ("@limit", limit) };
        if (queueId.HasValue)
        {
            parameters.Add(("@queue_id", queueId.Value));
        }
        return ExecuteListAsync(sql, Map, parameters.ToArray());
    }

    /// <summary>
    /// Gets recent ranked matches for LP estimation.
    /// Returns lightweight models with only the fields needed for the estimation algorithm.
    /// </summary>
    public virtual Task<IList<LpEstimationMatch>> GetRecentRankedMatchesForLpEstimationAsync(string puuid, int queueId, int limit)
    {
        const string sql = @"SELECT p.match_id, p.puuid, p.win, m.game_duration_sec,
                p.lp_after, p.tier_after, p.rank_after, p.is_lp_estimated
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            WHERE p.puuid = @puuid AND m.queue_id = @queue_id
            ORDER BY m.game_start_time DESC
            LIMIT @limit";

        return ExecuteListAsync(sql, MapLpEstimation,
            ("@puuid", puuid),
            ("@queue_id", queueId),
            ("@limit", limit));
    }

    /// <summary>
    /// Batch updates estimated LP data for multiple matches.
    /// Only updates rows where lp_after IS NULL (never overwrites existing LP data).
    /// </summary>
    public virtual async Task<int> BatchUpdateLpEstimatesAsync(IList<(string matchId, string puuid, int lpAfter, string tierAfter, string rankAfter)> estimates)
    {
        if (estimates.Count == 0) return 0;

        int totalUpdated = 0;

        await ExecuteWithConnectionAsync(async conn =>
        {
            foreach (var (matchId, puuid, lpAfter, tierAfter, rankAfter) in estimates)
            {
                const string sql = @"UPDATE participants
                    SET lp_after = @lp_after, tier_after = @tier_after, rank_after = @rank_after, is_lp_estimated = TRUE
                    WHERE match_id = @match_id AND puuid = @puuid AND lp_after IS NULL";

                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@match_id", matchId);
                cmd.Parameters.AddWithValue("@puuid", puuid);
                cmd.Parameters.AddWithValue("@lp_after", lpAfter);
                cmd.Parameters.AddWithValue("@tier_after", tierAfter);
                cmd.Parameters.AddWithValue("@rank_after", rankAfter);

                totalUpdated += await cmd.ExecuteNonQueryAsync();
            }

            return totalUpdated;
        });

        return totalUpdated;
    }

    private static LpEstimationMatch MapLpEstimation(MySqlDataReader r) => new()
    {
        MatchId = r.GetString(0),
        Puuid = r.GetString(1),
        Win = r.GetBoolean(2),
        GameDurationSec = r.GetInt32(3),
        LpAfter = r.IsDBNull(4) ? null : r.GetInt32(4),
        TierAfter = r.IsDBNull(5) ? null : r.GetString(5),
        RankAfter = r.IsDBNull(6) ? null : r.GetString(6),
        IsLpEstimated = r.GetBoolean(7)
    };

    private static Participant Map(MySqlDataReader r) => new()
    {
        Id = r.GetInt64(0),
        MatchId = r.GetString(1),
        Puuid = r.GetString(2),
        TeamId = r.GetInt32(3),
        Role = r.IsDBNull(4) ? null : r.GetString(4),
        Lane = r.IsDBNull(5) ? null : r.GetString(5),
        ChampionId = r.GetInt32(6),
        ChampionName = r.GetString(7),
        Win = r.GetBoolean(8),
        Kills = r.GetInt32(9),
        Deaths = r.GetInt32(10),
        Assists = r.GetInt32(11),
        CreepScore = r.GetInt32(12),
        GoldEarned = r.GetInt32(13),
        TimeDeadSec = r.GetInt32(14),
        LpAfter = r.IsDBNull(15) ? null : r.GetInt32(15),
        TierAfter = r.IsDBNull(16) ? null : r.GetString(16),
        RankAfter = r.IsDBNull(17) ? null : r.GetString(17),
        IsLpEstimated = r.GetBoolean(18),
        CreatedAt = r.GetDateTimeUtc(19)
    };
}
