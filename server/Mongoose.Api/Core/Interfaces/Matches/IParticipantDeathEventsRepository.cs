using Mongoose.Api.Core.Entities;

namespace Mongoose.Api.Core.Interfaces;

public interface IParticipantDeathEventsRepository
{
    Task InsertAsync(ParticipantDeathEvent deathEvent);
    Task InsertBatchAsync(IEnumerable<ParticipantDeathEvent> deathEvents);

    /// <summary>
    /// Replaces every death event of the match's participants with <paramref name="deathEvents"/>, in
    /// one transaction, so a timeline read again (sync or backfill) never duplicates deaths.
    /// </summary>
    Task ReplaceForMatchAsync(string matchId, IReadOnlyList<ParticipantDeathEvent> deathEvents);
}
