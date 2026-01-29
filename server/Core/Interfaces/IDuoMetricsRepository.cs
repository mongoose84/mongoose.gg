using RiotProxy.Core.Entities;

namespace RiotProxy.Core.Interfaces;

public interface IDuoMetricsRepository
{
    Task<long> InsertAsync(DuoMetric metric);
    Task<IList<DuoMetric>> GetByMatchAsync(string matchId);
}

