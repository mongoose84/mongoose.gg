using RiotProxy.Core.Entities;

namespace RiotProxy.Core.Interfaces;

public interface IParticipantMetricsRepository
{
    Task UpsertAsync(ParticipantMetric m);
    Task<ParticipantMetric?> GetByParticipantIdAsync(long participantId);
}

