using Mongoose.Api.Core.Entities;

namespace Mongoose.Api.Core.Interfaces;

public interface IParticipantCheckpointsRepository
{
    Task UpsertAsync(ParticipantCheckpoint cp);
    Task UpsertBatchAsync(IEnumerable<ParticipantCheckpoint> checkpoints);
    Task<IList<ParticipantCheckpoint>> GetByParticipantIdsAsync(IEnumerable<long> participantIds);
    Task<IList<ParticipantCheckpoint>> GetByParticipantAsync(long participantId);
}

