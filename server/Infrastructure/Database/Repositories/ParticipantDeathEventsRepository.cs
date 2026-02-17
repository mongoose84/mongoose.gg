using MySqlConnector;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;
using System.Text;

namespace Mongoose.Api.Infrastructure.Database.Repositories;

public class ParticipantDeathEventsRepository : RepositoryBase, IParticipantDeathEventsRepository
{
    public ParticipantDeathEventsRepository(IDbConnectionFactory factory) : base(factory) {}

    public Task InsertAsync(ParticipantDeathEvent deathEvent)
    {
        const string sql = @"INSERT INTO participant_death_events
            (participant_id, minute_mark, position_x, position_y, killer_champion_id, assist_count, created_at)
            VALUES (@participant_id, @minute_mark, @position_x, @position_y, @killer_champion_id, @assist_count, @created_at);";

        return ExecuteNonQueryAsync(sql,
            ("@participant_id", deathEvent.ParticipantId),
            ("@minute_mark", deathEvent.MinuteMark),
            ("@position_x", deathEvent.PositionX),
            ("@position_y", deathEvent.PositionY),
            ("@killer_champion_id", deathEvent.KillerChampionId ?? (object)DBNull.Value),
            ("@assist_count", deathEvent.AssistCount),
            ("@created_at", deathEvent.CreatedAt == default ? DateTime.UtcNow : deathEvent.CreatedAt));
    }

    public async Task InsertBatchAsync(IEnumerable<ParticipantDeathEvent> deathEvents)
    {
        var events = deathEvents?.ToList() ?? [];
        if (events.Count == 0) return;

        const string sqlPrefix = @"INSERT INTO participant_death_events
            (participant_id, minute_mark, position_x, position_y, killer_champion_id, assist_count, created_at)
            VALUES ";

        var sb = new StringBuilder();
        sb.Append(sqlPrefix);

        var parameters = new List<(string name, object? value)>();
        for (int i = 0; i < events.Count; i++)
        {
            var evt = events[i];
            var suffix = i == events.Count - 1 ? ";" : ",";
            sb.Append($"(@p{i}_participant_id, @p{i}_minute_mark, @p{i}_position_x, @p{i}_position_y, @p{i}_killer_champion_id, @p{i}_assist_count, @p{i}_created_at){suffix}");

            parameters.Add(($"@p{i}_participant_id", evt.ParticipantId));
            parameters.Add(($"@p{i}_minute_mark", evt.MinuteMark));
            parameters.Add(($"@p{i}_position_x", evt.PositionX));
            parameters.Add(($"@p{i}_position_y", evt.PositionY));
            parameters.Add(($"@p{i}_killer_champion_id", evt.KillerChampionId ?? (object)DBNull.Value));
            parameters.Add(($"@p{i}_assist_count", evt.AssistCount));
            parameters.Add(($"@p{i}_created_at", evt.CreatedAt == default ? DateTime.UtcNow : evt.CreatedAt));
        }

        await ExecuteNonQueryAsync(sb.ToString(), parameters.ToArray());
    }
}
