using MySqlConnector;
using RiotProxy.External.Domain.Entities.V2;

namespace RiotProxy.Infrastructure.External.Database.Repositories.V2;

public class V2AiSnapshotsRepository : RepositoryBase
{
    public V2AiSnapshotsRepository(IDbConnectionFactory factory) : base(factory) {}

    public Task<long> InsertAsync(V2AiSnapshot s)
    {
        const string sql = @"INSERT INTO ai_snapshots (puuid, context_type, context_puuids, queue_id, summary_text, goals_json, snapshot_date, created_at)
            VALUES (@puuid, @context_type, @context_puuids, @queue_id, @summary_text, @goals_json, @snapshot_date, @created_at);";

        return ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@puuid", s.Puuid);
            cmd.Parameters.AddWithValue("@context_type", s.ContextType);
            cmd.Parameters.AddWithValue("@context_puuids", s.ContextPuuidsJson ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@queue_id", s.QueueId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@summary_text", s.SummaryText);
            cmd.Parameters.AddWithValue("@goals_json", s.GoalsJson ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@snapshot_date", s.SnapshotDate == default ? DateOnly.FromDateTime(DateTime.UtcNow) : s.SnapshotDate);
            cmd.Parameters.AddWithValue("@created_at", s.CreatedAt == default ? DateTime.UtcNow : s.CreatedAt);
            await cmd.ExecuteNonQueryAsync();
            return cmd.LastInsertedId;
        });
    }

    public Task<V2AiSnapshot?> GetLatestAsync(string puuid, string contextType, int? queueId)
    {
        var sql = "SELECT * FROM ai_snapshots WHERE puuid = @puuid AND context_type = @context_type";
        var parameters = new List<(string, object?)>
        {
            ("@puuid", puuid),
            ("@context_type", contextType)
        };
        if (queueId.HasValue)
        {
            sql += " AND queue_id = @queue_id";
            parameters.Add(("@queue_id", queueId.Value));
        }
        sql += " ORDER BY snapshot_date DESC, created_at DESC LIMIT 1";

        return ExecuteSingleAsync(sql, Map, parameters.ToArray());
    }

    private static V2AiSnapshot Map(MySqlDataReader r) => new()
    {
        Id = r.GetInt64(0),
        Puuid = r.GetString(1),
        ContextType = r.GetString(2),
        ContextPuuidsJson = r.IsDBNull(3) ? null : r.GetString(3),
        QueueId = r.IsDBNull(4) ? null : r.GetInt32(4),
        SummaryText = r.GetString(5),
        GoalsJson = r.IsDBNull(6) ? null : r.GetString(6),
        SnapshotDate = DateOnly.FromDateTime(r.GetDateTime(7)),
        CreatedAt = r.GetDateTime(8)
    };
}
