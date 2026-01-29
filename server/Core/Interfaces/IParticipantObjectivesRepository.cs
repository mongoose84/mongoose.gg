using RiotProxy.Core.Entities;

namespace RiotProxy.Core.Interfaces;

public interface IParticipantObjectivesRepository
{
    Task UpsertAsync(ParticipantObjective o);
    Task<ParticipantObjective?> GetByParticipantIdAsync(long participantId);
}

