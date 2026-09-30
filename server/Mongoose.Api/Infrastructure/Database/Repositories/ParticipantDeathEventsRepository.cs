using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;
using System.Text;

namespace Mongoose.Api.Infrastructure.Database.Repositories;

public class ParticipantDeathEventsRepository : RepositoryBase, IParticipantDeathEventsRepository
{
    private const string Columns = @"(participant_id, minute_mark, timestamp_sec, position_x, position_y, killer_champion_id,
            killer_participant_id, assisting_participant_ids, allies_nearby, assist_count, created_at)";

    public ParticipantDeathEventsRepository(IDbConnectionFactory factory) : base(factory) {}

    public Task InsertAsync(ParticipantDeathEvent deathEvent) => InsertBatchAsync([deathEvent]);

    public async Task InsertBatchAsync(IEnumerable<ParticipantDeathEvent> deathEvents)
    {
        var events = deathEvents?.ToList() ?? [];
        if (events.Count == 0) return;

        var (sql, parameters) = BuildInsert(events);
        await ExecuteNonQueryAsync(sql, parameters);
    }

    /// <inheritdoc />
    public Task ReplaceForMatchAsync(string matchId, IReadOnlyList<ParticipantDeathEvent> deathEvents)
    {
        return ExecuteTransactionAsync(async (conn, tx) =>
        {
            await ExecuteNonQueryWithConnectionAsync(conn, tx, @"
                DELETE pde FROM participant_death_events pde
                INNER JOIN participants p ON p.id = pde.participant_id
                WHERE p.match_id = @match_id",
                ("@match_id", matchId));

            if (deathEvents.Count == 0) return;

            var (sql, parameters) = BuildInsert(deathEvents);
            await ExecuteNonQueryWithConnectionAsync(conn, tx, sql, parameters);
        });
    }

    private static (string Sql, (string name, object? value)[] Parameters) BuildInsert(IReadOnlyList<ParticipantDeathEvent> events)
    {
        var sb = new StringBuilder($"INSERT INTO participant_death_events {Columns} VALUES ");
        var parameters = new List<(string name, object? value)>();

        for (var i = 0; i < events.Count; i++)
        {
            var evt = events[i];
            sb.Append(i == 0 ? "" : ",");
            sb.Append($"(@p{i}_participant_id, @p{i}_minute_mark, @p{i}_timestamp_sec, @p{i}_position_x, @p{i}_position_y, " +
                      $"@p{i}_killer_champion_id, @p{i}_killer_participant_id, @p{i}_assisting_participant_ids, " +
                      $"@p{i}_allies_nearby, @p{i}_assist_count, @p{i}_created_at)");

            parameters.Add(($"@p{i}_participant_id", evt.ParticipantId));
            parameters.Add(($"@p{i}_minute_mark", evt.MinuteMark));
            parameters.Add(($"@p{i}_timestamp_sec", evt.TimestampSec ?? (object)DBNull.Value));
            parameters.Add(($"@p{i}_position_x", evt.PositionX));
            parameters.Add(($"@p{i}_position_y", evt.PositionY));
            parameters.Add(($"@p{i}_killer_champion_id", evt.KillerChampionId ?? (object)DBNull.Value));
            parameters.Add(($"@p{i}_killer_participant_id", evt.KillerParticipantId ?? (object)DBNull.Value));
            parameters.Add(($"@p{i}_assisting_participant_ids", evt.AssistingParticipantIds ?? (object)DBNull.Value));
            parameters.Add(($"@p{i}_allies_nearby", evt.AlliesNearby ?? (object)DBNull.Value));
            parameters.Add(($"@p{i}_assist_count", evt.AssistCount));
            parameters.Add(($"@p{i}_created_at", evt.CreatedAt == default ? DateTime.UtcNow : evt.CreatedAt));
        }

        sb.Append(';');
        return (sb.ToString(), parameters.ToArray());
    }
}
