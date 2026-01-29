using RiotProxy.Application.DTOs.Matches;
using RiotProxy.Core.Entities;

namespace RiotProxy.Core.Interfaces;

// MatchListItem, RoleBaseline from MatchListDto.cs
// MatchupParticipantRaw from MatchNarrativeDto.cs

public interface IMatchesRepository
{
    Task UpsertAsync(Match match);
    Task<long> GetTotalMatchCountAsync();
    Task<IList<Match>> GetRecentMatchHeadersAsync(string puuid, int? queueId, int limit);
    Task<IList<MatchListItem>> GetMatchListAsync(string puuid, string queueFilter, int limit = 20, Dictionary<string, RoleBaseline>? baselines = null);
    Task<Dictionary<string, RoleBaseline>> GetRoleBaselinesAsync(string puuid, string queueFilter);
    Task<IList<MatchupParticipantRaw>> GetMatchParticipantsAsync(string matchId);
}

