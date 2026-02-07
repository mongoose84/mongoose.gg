using Mongoose.Api.Core.Entities;

namespace Mongoose.Api.Core.Interfaces;

public interface IAiSnapshotsRepository
{
    Task<long> InsertAsync(AiSnapshot snapshot);
    Task<AiSnapshot?> GetLatestAsync(string puuid, string contextType, int? queueId);
}

