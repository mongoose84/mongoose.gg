using Mongoose.Api.Core.Entities;

namespace Mongoose.Api.Core.Interfaces;

public interface IParticipantDeathEventsRepository
{
    Task InsertAsync(ParticipantDeathEvent deathEvent);
    Task InsertBatchAsync(IEnumerable<ParticipantDeathEvent> deathEvents);
}
