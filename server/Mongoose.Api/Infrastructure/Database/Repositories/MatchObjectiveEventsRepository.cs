using System.Text;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;

namespace Mongoose.Api.Infrastructure.Database.Repositories;

public class MatchObjectiveEventsRepository : RepositoryBase, IMatchObjectiveEventsRepository
{
    public MatchObjectiveEventsRepository(IDbConnectionFactory factory) : base(factory) {}

    /// <inheritdoc />
    public Task ReplaceForMatchAsync(string matchId, IReadOnlyList<MatchObjectiveEvent> events)
    {
        return ExecuteTransactionAsync(async (conn, tx) =>
        {
            await ExecuteNonQueryWithConnectionAsync(conn, tx,
                "DELETE FROM match_objective_events WHERE match_id = @match_id",
                ("@match_id", matchId));

            if (events.Count == 0) return;

            var sb = new StringBuilder(@"INSERT INTO match_objective_events
                (match_id, team_id, type, subtype, timestamp_sec, killer_participant_id, created_at) VALUES ");
            var parameters = new List<(string name, object? value)> { ("@match_id", matchId) };

            for (var i = 0; i < events.Count; i++)
            {
                var evt = events[i];
                sb.Append(i == 0 ? "" : ",");
                sb.Append($"(@match_id, @e{i}_team_id, @e{i}_type, @e{i}_subtype, @e{i}_timestamp_sec, @e{i}_killer_participant_id, @e{i}_created_at)");

                parameters.Add(($"@e{i}_team_id", evt.TeamId));
                parameters.Add(($"@e{i}_type", evt.Type));
                parameters.Add(($"@e{i}_subtype", evt.Subtype ?? (object)DBNull.Value));
                parameters.Add(($"@e{i}_timestamp_sec", evt.TimestampSec));
                parameters.Add(($"@e{i}_killer_participant_id", evt.KillerParticipantId ?? (object)DBNull.Value));
                parameters.Add(($"@e{i}_created_at", evt.CreatedAt == default ? DateTime.UtcNow : evt.CreatedAt));
            }

            await ExecuteNonQueryWithConnectionAsync(conn, tx, sb.ToString(), parameters.ToArray());
        });
    }
}
