using MySqlConnector;
using RiotProxy.External.Domain.Entities.V2;

namespace RiotProxy.Infrastructure.External.Database.Repositories.V2;

public class V2DuoMetricsRepository : RepositoryBase
{
    public V2DuoMetricsRepository(IDbConnectionFactory factory) : base(factory) {}

    public Task<long> InsertAsync(V2DuoMetric d)
    {
        const string sql = @"INSERT INTO duo_metrics (match_id, participant_id_1, participant_id_2, early_gold_delta_10, early_gold_delta_15, assist_synergy_pct, shared_objective_participation_pct, win_when_ahead_at_15, created_at)
            VALUES (@match_id, @p1, @p2, @g10, @g15, @synergy, @shared_obj, @win_ahead15, @created_at);";

        return ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@match_id", d.MatchId);
            cmd.Parameters.AddWithValue("@p1", d.ParticipantId1);
            cmd.Parameters.AddWithValue("@p2", d.ParticipantId2);
            cmd.Parameters.AddWithValue("@g10", d.EarlyGoldDelta10 ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@g15", d.EarlyGoldDelta15 ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@synergy", d.AssistSynergyPct ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@shared_obj", d.SharedObjectiveParticipationPct ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@win_ahead15", d.WinWhenAheadAt15 ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@created_at", d.CreatedAt == default ? DateTime.UtcNow : d.CreatedAt);
            await cmd.ExecuteNonQueryAsync();
            return cmd.LastInsertedId;
        });
    }

    public Task<IList<V2DuoMetric>> GetByMatchAsync(string matchId)
    {
        const string sql = "SELECT * FROM duo_metrics WHERE match_id = @match_id";
        return ExecuteListAsync(sql, Map, ("@match_id", matchId));
    }

    private static V2DuoMetric Map(MySqlDataReader r) => new()
    {
        Id = r.GetInt64("id"),
        MatchId = r.GetString("match_id"),
        ParticipantId1 = r.GetInt64("participant_id_1"),
        ParticipantId2 = r.GetInt64("participant_id_2"),
        EarlyGoldDelta10 = r.IsDBNull("early_gold_delta_10") ? null : r.GetInt32("early_gold_delta_10"),
        EarlyGoldDelta15 = r.IsDBNull("early_gold_delta_15") ? null : r.GetInt32("early_gold_delta_15"),
        AssistSynergyPct = r.IsDBNull("assist_synergy_pct") ? null : r.GetDecimal("assist_synergy_pct"),
        SharedObjectiveParticipationPct = r.IsDBNull("shared_objective_participation_pct") ? null : r.GetDecimal("shared_objective_participation_pct"),
        WinWhenAheadAt15 = r.IsDBNull("win_when_ahead_at_15") ? null : r.GetBoolean("win_when_ahead_at_15"),
        CreatedAt = r.GetDateTime("created_at")
    };
}
