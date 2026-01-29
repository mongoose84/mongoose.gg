using RiotProxy.Core.Entities;

namespace RiotProxy.Core.Interfaces;

public interface IParticipantsRepository
{
    Task<long> InsertAsync(Participant participant);
    Task<IList<Participant>> GetByMatchAsync(string matchId);
    Task UpdateLpDataAsync(string matchId, string puuid, int? lp, string? tier, string? rank);
    Task<ISet<string>> GetMatchIdsForPuuidAsync(string puuid);
    Task<IList<Participant>> GetRecentByPuuidAsync(string puuid, int? queueId, int limit);
}

