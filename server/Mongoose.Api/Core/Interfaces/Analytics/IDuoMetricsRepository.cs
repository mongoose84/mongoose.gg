using Mongoose.Api.Core.Entities;

namespace Mongoose.Api.Core.Interfaces;

public interface IDuoMetricsRepository
{
    Task<long> InsertAsync(DuoMetric metric);
    Task<IList<DuoMetric>> GetByMatchAsync(string matchId);
}

