using MySqlConnector;
using RiotProxy.External.Domain.Entities.V2;

namespace RiotProxy.Infrastructure.External.Database.Repositories.V2;

public class V2TeamRoleResponsibilitiesRepository : RepositoryBase
{
    public V2TeamRoleResponsibilitiesRepository(IDbConnectionFactory factory) : base(factory) {}

    public Task UpsertAsync(V2TeamRoleResponsibility r)
    {
        const string sql = @"INSERT INTO team_role_responsibility (match_id, team_id, role, deaths_share_pct, gold_share_pct, damage_share_pct, created_at)
            VALUES (@match_id, @team_id, @role, @deaths_pct, @gold_pct, @damage_pct, @created_at) AS new
            ON DUPLICATE KEY UPDATE
                deaths_share_pct = new.deaths_share_pct,
                gold_share_pct = new.gold_share_pct,
                damage_share_pct = new.damage_share_pct;";

        return ExecuteNonQueryAsync(sql,
            ("@match_id", r.MatchId),
            ("@team_id", r.TeamId),
            ("@role", r.Role),
            ("@deaths_pct", r.DeathsSharePct),
            ("@gold_pct", r.GoldSharePct),
            ("@damage_pct", r.DamageSharePct),
            ("@created_at", r.CreatedAt == default ? DateTime.UtcNow : r.CreatedAt));
    }

    public Task<IList<V2TeamRoleResponsibility>> GetByMatchAsync(string matchId)
    {
        const string sql = "SELECT * FROM team_role_responsibility WHERE match_id = @match_id";
        return ExecuteListAsync(sql, Map, ("@match_id", matchId));
    }

    private static V2TeamRoleResponsibility Map(MySqlDataReader r) => new()
    {
        Id = r.GetInt64(0),
        MatchId = r.GetString(1),
        TeamId = r.GetInt32(2),
        Role = r.GetString(3),
        DeathsSharePct = r.GetDecimal(4),
        GoldSharePct = r.GetDecimal(5),
        DamageSharePct = r.GetDecimal(6),
        CreatedAt = r.GetDateTime(7)
    };
}
