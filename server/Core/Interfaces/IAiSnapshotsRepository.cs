using RiotProxy.Core.Entities;

namespace RiotProxy.Core.Interfaces;

public interface IAiSnapshotsRepository
{
    Task<long> InsertAsync(AiSnapshot snapshot);
    Task<AiSnapshot?> GetLatestAsync(string puuid, string contextType, int? queueId);
}

