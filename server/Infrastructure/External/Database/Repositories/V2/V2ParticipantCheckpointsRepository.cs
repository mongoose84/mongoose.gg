using MySqlConnector;
using RiotProxy.External.Domain.Entities.V2;
using System.Text;

namespace RiotProxy.Infrastructure.External.Database.Repositories.V2;

public class V2ParticipantCheckpointsRepository : RepositoryBase
{
    public V2ParticipantCheckpointsRepository(IV2DbConnectionFactory factory) : base(factory) {}

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

    public async Task UpsertBatchAsync(IEnumerable<V2ParticipantCheckpoint> checkpoints)
    {
        var cps = checkpoints?.ToList() ?? [];
        if (cps.Count == 0) return;

        const string sqlPrefix = @"INSERT INTO participant_checkpoints
            (participant_id, minute_mark, gold, cs, xp, gold_diff_vs_lane, cs_diff_vs_lane, is_ahead, created_at)
            VALUES ";

        var sb = new System.Text.StringBuilder();
        sb.Append(sqlPrefix);

        var parameters = new List<(string name, object? value)>();
        for (int i = 0; i < cps.Count; i++)
        {
            var cp = cps[i];
            var suffix = i == cps.Count - 1 ? "" : ",";
            sb.Append($"(@p{i}_participant_id, @p{i}_minute_mark, @p{i}_gold, @p{i}_cs, @p{i}_xp, @p{i}_gold_diff_vs_lane, @p{i}_cs_diff_vs_lane, @p{i}_is_ahead, @p{i}_created_at){suffix}");

            parameters.Add(($"@p{i}_participant_id", cp.ParticipantId));
            parameters.Add(($"@p{i}_minute_mark", cp.MinuteMark));
            parameters.Add(($"@p{i}_gold", cp.Gold));
            parameters.Add(($"@p{i}_cs", cp.Cs));
            parameters.Add(($"@p{i}_xp", cp.Xp));
            parameters.Add(($"@p{i}_gold_diff_vs_lane", cp.GoldDiffVsLane ?? (object)DBNull.Value));
            parameters.Add(($"@p{i}_cs_diff_vs_lane", cp.CsDiffVsLane ?? (object)DBNull.Value));
            parameters.Add(($"@p{i}_is_ahead", cp.IsAhead ?? (object)DBNull.Value));
            parameters.Add(($"@p{i}_created_at", cp.CreatedAt == default ? DateTime.UtcNow : cp.CreatedAt));
        }

        sb.Append(@" AS new
            ON DUPLICATE KEY UPDATE
                gold = new.gold,
                cs = new.cs,
                xp = new.xp,
                gold_diff_vs_lane = new.gold_diff_vs_lane,
                cs_diff_vs_lane = new.cs_diff_vs_lane,
                is_ahead = new.is_ahead;");

        await ExecuteNonQueryAsync(sb.ToString(), parameters.ToArray());
    }

    public async Task<IList<V2ParticipantCheckpoint>> GetByParticipantIdsAsync(IEnumerable<long> participantIds)
    {
        var ids = participantIds?.Distinct().ToList() ?? [];
        if (ids.Count == 0) return Array.Empty<V2ParticipantCheckpoint>();

        return await ExecuteWithConnectionAsync(async (conn, cmd) =>
        {
            var sb = new StringBuilder();
            sb.Append("SELECT * FROM participant_checkpoints WHERE participant_id IN (");
            for (int i = 0; i < ids.Count; i++)
            {
                var suffix = i == ids.Count - 1 ? ") ORDER BY participant_id, minute_mark" : ", ";
                var param = $"@p{i}";
                sb.Append(param);
                cmd.Parameters.AddWithValue(param, ids[i]);
            }

            cmd.CommandText = sb.ToString();

            var results = new List<V2ParticipantCheckpoint>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(Map(reader));
            }

            return (IList<V2ParticipantCheckpoint>)results;
        });
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
