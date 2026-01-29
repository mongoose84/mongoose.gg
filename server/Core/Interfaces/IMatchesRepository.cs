using RiotProxy.Core.Entities;
using RiotProxy.Core.QueryModels;

namespace RiotProxy.Core.Interfaces;

public interface IMatchesRepository
{
    Task UpsertAsync(Match match);
    Task<long> GetTotalMatchCountAsync();
    Task<IList<Match>> GetRecentMatchHeadersAsync(string puuid, int? queueId, int limit);
    Task<IList<MatchListItem>> GetMatchListAsync(string puuid, string queueFilter, int limit = 20, Dictionary<string, RoleBaseline>? baselines = null);
    Task<Dictionary<string, RoleBaseline>> GetRoleBaselinesAsync(string puuid, string queueFilter);
    Task<IList<MatchupParticipantRaw>> GetMatchParticipantsAsync(string matchId);
}

