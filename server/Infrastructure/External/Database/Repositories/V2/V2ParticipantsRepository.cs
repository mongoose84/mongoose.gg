using MySqlConnector;
using RiotProxy.External.Domain.Entities.V2;

namespace RiotProxy.Infrastructure.External.Database.Repositories.V2;

public class V2ParticipantsRepository : RepositoryBase
{
    public V2ParticipantsRepository(IV2DbConnectionFactory factory) : base(factory) {}

    public Task<long> InsertAsync(V2Participant p)
    {
        const string sql = @"INSERT INTO participants
            (match_id, puuid, team_id, role, lane, champion_id, champion_name, win, kills, deaths, assists, creep_score, gold_earned, time_dead_sec, created_at)
            VALUES (@match_id, @puuid, @team_id, @role, @lane, @champion_id, @champion_name, @win, @kills, @deaths, @assists, @creep_score, @gold_earned, @time_dead_sec, @created_at) AS new
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
                time_dead_sec = new.time_dead_sec;";

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
            cmd.Parameters.AddWithValue("@created_at", p.CreatedAt == default ? DateTime.UtcNow : p.CreatedAt);
            await cmd.ExecuteNonQueryAsync();
            return cmd.LastInsertedId;
        });
    }

    public Task<IList<V2Participant>> GetByMatchAsync(string matchId)
    {
        const string sql = "SELECT * FROM participants WHERE match_id = @match_id";
        return ExecuteListAsync(sql, Map, ("@match_id", matchId));
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

    public Task<IList<V2Participant>> GetRecentByPuuidAsync(string puuid, int? queueId, int limit)
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

    private static V2Participant Map(MySqlDataReader r) => new()
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
        CreatedAt = r.GetDateTime(15)
    };
}
