using MySqlConnector;
using RiotProxy.External.Domain.Entities.V2;

namespace RiotProxy.Infrastructure.External.Database.Repositories.V2;

public class V2ParticipantMetricsRepository : RepositoryBase
{
    public V2ParticipantMetricsRepository(IDbConnectionFactory factory) : base(factory) {}

    public Task UpsertAsync(V2ParticipantMetric m)
    {
        const string sql = @"INSERT INTO participant_metrics
            (participant_id, kill_participation_pct, damage_share_pct, damage_taken, damage_mitigated, vision_score, vision_per_min, deaths_pre_10, deaths_10_20, deaths_20_30, deaths_30_plus, first_death_minute, first_kill_participation_minute, created_at)
            VALUES (@participant_id, @kp, @dmg_share, @dmg_taken, @dmg_mitigated, @vision_score, @vision_per_min, @d_pre10, @d_10_20, @d_20_30, @d_30_plus, @first_death, @first_kp, @created_at)
            ON DUPLICATE KEY UPDATE
                kill_participation_pct = VALUES(kill_participation_pct),
                damage_share_pct = VALUES(damage_share_pct),
                damage_taken = VALUES(damage_taken),
                damage_mitigated = VALUES(damage_mitigated),
                vision_score = VALUES(vision_score),
                vision_per_min = VALUES(vision_per_min),
                deaths_pre_10 = VALUES(deaths_pre_10),
                deaths_10_20 = VALUES(deaths_10_20),
                deaths_20_30 = VALUES(deaths_20_30),
                deaths_30_plus = VALUES(deaths_30_plus),
                first_death_minute = VALUES(first_death_minute),
                first_kill_participation_minute = VALUES(first_kill_participation_minute);";

        return ExecuteNonQueryAsync(sql,
            ("@participant_id", m.ParticipantId),
            ("@kp", m.KillParticipationPct),
            ("@dmg_share", m.DamageSharePct),
            ("@dmg_taken", m.DamageTaken),
            ("@dmg_mitigated", m.DamageMitigated),
            ("@vision_score", m.VisionScore),
            ("@vision_per_min", m.VisionPerMin),
            ("@d_pre10", m.DeathsPre10),
            ("@d_10_20", m.Deaths10To20),
            ("@d_20_30", m.Deaths20To30),
            ("@d_30_plus", m.Deaths30Plus),
            ("@first_death", m.FirstDeathMinute ?? (object)DBNull.Value),
            ("@first_kp", m.FirstKillParticipationMinute ?? (object)DBNull.Value),
            ("@created_at", m.CreatedAt == default ? DateTime.UtcNow : m.CreatedAt));
    }

    public Task<V2ParticipantMetric?> GetByParticipantIdAsync(long participantId)
    {
        const string sql = "SELECT * FROM participant_metrics WHERE participant_id = @participant_id LIMIT 1";
        return ExecuteSingleAsync(sql, Map, ("@participant_id", participantId));
    }

    private static V2ParticipantMetric Map(MySqlDataReader r) => new()
    {
        Id = r.GetInt64("id"),
        ParticipantId = r.GetInt64("participant_id"),
        KillParticipationPct = r.GetDecimal("kill_participation_pct"),
        DamageSharePct = r.GetDecimal("damage_share_pct"),
        DamageTaken = r.GetInt32("damage_taken"),
        DamageMitigated = r.GetInt32("damage_mitigated"),
        VisionScore = r.GetInt32("vision_score"),
        VisionPerMin = r.GetDecimal("vision_per_min"),
        DeathsPre10 = r.GetInt32("deaths_pre_10"),
        Deaths10To20 = r.GetInt32("deaths_10_20"),
        Deaths20To30 = r.GetInt32("deaths_20_30"),
        Deaths30Plus = r.GetInt32("deaths_30_plus"),
        FirstDeathMinute = r.IsDBNull("first_death_minute") ? null : r.GetInt32("first_death_minute"),
        FirstKillParticipationMinute = r.IsDBNull("first_kill_participation_minute") ? null : r.GetInt32("first_kill_participation_minute"),
        CreatedAt = r.GetDateTime("created_at")
    };
}
