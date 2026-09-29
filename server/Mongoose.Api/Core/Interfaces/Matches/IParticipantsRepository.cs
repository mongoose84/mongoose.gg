using Mongoose.Api.Core.Entities;

namespace Mongoose.Api.Core.Interfaces;

public interface IParticipantsRepository
{
    Task<long> InsertAsync(Participant participant);
    Task<IList<Participant>> GetByMatchAsync(string matchId);
    Task UpdateLpDataAsync(string matchId, string puuid, int? lp, string? tier, string? rank);
    Task<ISet<string>> GetMatchIdsForPuuidAsync(string puuid);
    Task<IList<Participant>> GetRecentByPuuidAsync(string puuid, int? queueId, int limit);

    /// <summary>Sets Riot's participantId (1-10) on a match's rows, keyed by PUUID.</summary>
    Task SetRiotParticipantIdsAsync(string matchId, IReadOnlyDictionary<string, int> participantIds);
}

