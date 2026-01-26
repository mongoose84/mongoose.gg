using MySqlConnector;
using RiotProxy.External.Domain.Entities
;

namespace RiotProxy.Infrastructure.External.Database.Repositories;

public class ParticipantsRepository : RepositoryBase
{
    public ParticipantsRepository(IDbConnectionFactory factory) : base(factory) {}

    public Task<long> InsertAsync(Participant p)
    {
        const string sql = @"INSERT INTO participants
            (match_id, puuid, team_id, role, lane, champion_id, champion_name, win, kills, deaths, assists, creep_score, gold_earned, time_dead_sec, lp_after, tier_after, rank_after, created_at)
            VALUES (@match_id, @puuid, @team_id, @role, @lane, @champion_id, @champion_name, @win, @kills, @deaths, @assists, @creep_score, @gold_earned, @time_dead_sec, @lp_after, @tier_after, @rank_after, @created_at) AS new
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
                rank_after = new.rank_after;";

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
            cmd.Parameters.AddWithValue("@created_at", p.CreatedAt == default ? DateTime.UtcNow : p.CreatedAt);
            await cmd.ExecuteNonQueryAsync();
            return cmd.LastInsertedId;
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
        CreatedAt = r.GetDateTimeUtc(18)
    };
}
