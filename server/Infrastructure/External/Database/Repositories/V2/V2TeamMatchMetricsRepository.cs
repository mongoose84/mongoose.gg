using MySqlConnector;
using RiotProxy.External.Domain.Entities.V2;

namespace RiotProxy.Infrastructure.External.Database.Repositories.V2;

public class V2TeamMatchMetricsRepository : RepositoryBase
{
    public V2TeamMatchMetricsRepository(IV2DbConnectionFactory factory) : base(factory) {}

    public Task UpsertAsync(V2TeamMatchMetric t)
    {
        const string sql = @"INSERT INTO team_match_metrics (match_id, team_id, gold_lead_at_15, largest_gold_lead, gold_swing_post_20, win_when_ahead_at_20, created_at)
            VALUES (@match_id, @team_id, @g15, @largest, @swing20, @win_ahead20, @created_at) AS new
            ON DUPLICATE KEY UPDATE
                gold_lead_at_15 = new.gold_lead_at_15,
                largest_gold_lead = new.largest_gold_lead,
                gold_swing_post_20 = new.gold_swing_post_20,
                win_when_ahead_at_20 = new.win_when_ahead_at_20;";

        return ExecuteNonQueryAsync(sql,
            ("@match_id", t.MatchId),
            ("@team_id", t.TeamId),
            ("@g15", t.GoldLeadAt15 ?? (object)DBNull.Value),
            ("@largest", t.LargestGoldLead ?? (object)DBNull.Value),
            ("@swing20", t.GoldSwingPost20 ?? (object)DBNull.Value),
            ("@win_ahead20", t.WinWhenAheadAt20 ?? (object)DBNull.Value),
            ("@created_at", t.CreatedAt == default ? DateTime.UtcNow : t.CreatedAt));
    }

    public Task<V2TeamMatchMetric?> GetAsync(string matchId, int teamId)
    {
        const string sql = "SELECT * FROM team_match_metrics WHERE match_id = @match_id AND team_id = @team_id LIMIT 1";
        return ExecuteSingleAsync(sql, Map, ("@match_id", matchId), ("@team_id", teamId));
    }

    private static V2TeamMatchMetric Map(MySqlDataReader r) => new()
    {
        Id = r.GetInt64(0),
        MatchId = r.GetString(1),
        TeamId = r.GetInt32(2),
        GoldLeadAt15 = r.IsDBNull(3) ? null : r.GetInt32(3),
        LargestGoldLead = r.IsDBNull(4) ? null : r.GetInt32(4),
        GoldSwingPost20 = r.IsDBNull(5) ? null : r.GetInt32(5),
        WinWhenAheadAt20 = r.IsDBNull(6) ? null : r.GetBoolean(6),
        CreatedAt = r.GetDateTime(7)
    };
}
