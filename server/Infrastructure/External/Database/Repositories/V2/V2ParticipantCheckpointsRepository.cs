using MySqlConnector;
using RiotProxy.External.Domain.Entities.V2;

namespace RiotProxy.Infrastructure.External.Database.Repositories.V2;

public class V2ParticipantCheckpointsRepository : RepositoryBase
{
    public V2ParticipantCheckpointsRepository(IDbConnectionFactory factory) : base(factory) {}

    public Task UpsertAsync(V2ParticipantCheckpoint cp)
    {
        const string sql = @"INSERT INTO participant_checkpoints
            (participant_id, minute_mark, gold, cs, xp, gold_diff_vs_lane, cs_diff_vs_lane, is_ahead, created_at)
            VALUES (@participant_id, @minute_mark, @gold, @cs, @xp, @gold_diff_vs_lane, @cs_diff_vs_lane, @is_ahead, @created_at) AS new
            ON DUPLICATE KEY UPDATE
                gold = new.gold,
                cs = new.cs,
                xp = new.xp,
                gold_diff_vs_lane = new.gold_diff_vs_lane,
                cs_diff_vs_lane = new.cs_diff_vs_lane,
                is_ahead = new.is_ahead;";

        return ExecuteNonQueryAsync(sql,
            ("@participant_id", cp.ParticipantId),
            ("@minute_mark", cp.MinuteMark),
            ("@gold", cp.Gold),
            ("@cs", cp.Cs),
            ("@xp", cp.Xp),
            ("@gold_diff_vs_lane", cp.GoldDiffVsLane ?? (object)DBNull.Value),
            ("@cs_diff_vs_lane", cp.CsDiffVsLane ?? (object)DBNull.Value),
            ("@is_ahead", cp.IsAhead ?? (object)DBNull.Value),
            ("@created_at", cp.CreatedAt == default ? DateTime.UtcNow : cp.CreatedAt));
    }

    public Task<IList<V2ParticipantCheckpoint>> GetByParticipantAsync(long participantId)
    {
        const string sql = "SELECT * FROM participant_checkpoints WHERE participant_id = @participant_id ORDER BY minute_mark";
        return ExecuteListAsync(sql, Map, ("@participant_id", participantId));
    }

    private static V2ParticipantCheckpoint Map(MySqlDataReader r) => new()
    {
        Id = r.GetInt64(0),
        ParticipantId = r.GetInt64(1),
        MinuteMark = r.GetInt32(2),
        Gold = r.GetInt32(3),
        Cs = r.GetInt32(4),
        Xp = r.GetInt32(5),
        GoldDiffVsLane = r.IsDBNull(6) ? null : r.GetInt32(6),
        CsDiffVsLane = r.IsDBNull(7) ? null : r.GetInt32(7),
        IsAhead = r.IsDBNull(8) ? null : r.GetBoolean(8),
        CreatedAt = r.GetDateTime(9)
    };
}
