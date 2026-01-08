using MySqlConnector;
using RiotProxy.External.Domain.Entities.V2;

namespace RiotProxy.Infrastructure.External.Database.Repositories.V2;

public class V2ParticipantObjectivesRepository : RepositoryBase
{
    public V2ParticipantObjectivesRepository(IDbConnectionFactory factory) : base(factory) {}

    public Task UpsertAsync(V2ParticipantObjective o)
    {
        const string sql = @"INSERT INTO participant_objectives (participant_id, dragons_participated, heralds_participated, barons_participated, towers_participated, created_at)
            VALUES (@participant_id, @dragons, @heralds, @barons, @towers, @created_at) AS new
            ON DUPLICATE KEY UPDATE
                dragons_participated = new.dragons_participated,
                heralds_participated = new.heralds_participated,
                barons_participated = new.barons_participated,
                towers_participated = new.towers_participated;";

        return ExecuteNonQueryAsync(sql,
            ("@participant_id", o.ParticipantId),
            ("@dragons", o.DragonsParticipated),
            ("@heralds", o.HeraldsParticipated),
            ("@barons", o.BaronsParticipated),
            ("@towers", o.TowersParticipated),
            ("@created_at", o.CreatedAt == default ? DateTime.UtcNow : o.CreatedAt));
    }

    public Task<V2ParticipantObjective?> GetByParticipantIdAsync(long participantId)
    {
        const string sql = "SELECT * FROM participant_objectives WHERE participant_id = @participant_id LIMIT 1";
        return ExecuteSingleAsync(sql, Map, ("@participant_id", participantId));
    }

    private static V2ParticipantObjective Map(MySqlDataReader r) => new()
    {
        Id = r.GetInt64("id"),
        ParticipantId = r.GetInt64("participant_id"),
        DragonsParticipated = r.GetInt32("dragons_participated"),
        HeraldsParticipated = r.GetInt32("heralds_participated"),
        BaronsParticipated = r.GetInt32("barons_participated"),
        TowersParticipated = r.GetInt32("towers_participated"),
        CreatedAt = r.GetDateTime("created_at")
    };
}
