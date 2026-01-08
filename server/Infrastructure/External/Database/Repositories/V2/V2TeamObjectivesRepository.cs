using MySqlConnector;
using RiotProxy.External.Domain.Entities.V2;

namespace RiotProxy.Infrastructure.External.Database.Repositories.V2;

public class V2TeamObjectivesRepository : RepositoryBase
{
    public V2TeamObjectivesRepository(IV2DbConnectionFactory factory) : base(factory) {}

    public Task UpsertAsync(V2TeamObjective t)
    {
        const string sql = @"INSERT INTO team_objectives (match_id, team_id, dragons_taken, heralds_taken, barons_taken, towers_taken, created_at)
            VALUES (@match_id, @team_id, @dragons, @heralds, @barons, @towers, @created_at) AS new
            ON DUPLICATE KEY UPDATE
                dragons_taken = new.dragons_taken,
                heralds_taken = new.heralds_taken,
                barons_taken = new.barons_taken,
                towers_taken = new.towers_taken;";

        return ExecuteNonQueryAsync(sql,
            ("@match_id", t.MatchId),
            ("@team_id", t.TeamId),
            ("@dragons", t.DragonsTaken),
            ("@heralds", t.HeraldsTaken),
            ("@barons", t.BaronsTaken),
            ("@towers", t.TowersTaken),
            ("@created_at", t.CreatedAt == default ? DateTime.UtcNow : t.CreatedAt));
    }

    public Task<V2TeamObjective?> GetAsync(string matchId, int teamId)
    {
        const string sql = "SELECT * FROM team_objectives WHERE match_id = @match_id AND team_id = @team_id LIMIT 1";
        return ExecuteSingleAsync(sql, Map, ("@match_id", matchId), ("@team_id", teamId));
    }

    private static V2TeamObjective Map(MySqlDataReader r) => new()
    {
        Id = r.GetInt64(0),
        MatchId = r.GetString(1),
        TeamId = r.GetInt32(2),
        DragonsTaken = r.GetInt32(3),
        HeraldsTaken = r.GetInt32(4),
        BaronsTaken = r.GetInt32(5),
        TowersTaken = r.GetInt32(6),
        CreatedAt = r.GetDateTime(7)
    };
}
