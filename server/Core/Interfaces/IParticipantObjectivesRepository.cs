using Mongoose.Api.Core.Entities;

namespace Mongoose.Api.Core.Interfaces;

public interface IParticipantObjectivesRepository
{
    Task UpsertAsync(ParticipantObjective o);
    Task<ParticipantObjective?> GetByParticipantIdAsync(long participantId);
}

