using Mongoose.Api.Core.Entities;

namespace Mongoose.Api.Core.Interfaces;

public interface IParticipantMetricsRepository
{
    Task UpsertAsync(ParticipantMetric m);
    Task<ParticipantMetric?> GetByParticipantIdAsync(long participantId);
}

