using RiotProxy.Core.Entities;
using RiotProxy.Core.QueryModels;

namespace RiotProxy.Core.Interfaces;

public interface IMatchesRepository
{
    Task UpsertAsync(Match match);
    Task<long> GetTotalMatchCountAsync();
    Task<IList<Match>> GetRecentMatchHeadersAsync(string puuid, int? queueId, int limit);

    /// <summary>
    /// Get lightweight match summaries for the list view.
    /// Only fetches data needed to render match rows (champion, KDA, result, timestamp, trend badge).
    /// </summary>
    Task<IList<MatchListSummaryItem>> GetMatchListSummaryAsync(string puuid, string queueFilter, int limit = 20, Dictionary<string, RoleBaseline>? baselines = null);

    /// <summary>
    /// Get full match details for a single match.
    /// Fetched on-demand when user selects a match from the list.
    /// </summary>
    Task<MatchDetailsItem?> GetMatchDetailsAsync(string matchId, string puuid);

    /// <summary>
    /// @deprecated Use GetMatchListSummaryAsync instead.
    /// </summary>
    Task<IList<MatchListItem>> GetMatchListAsync(string puuid, string queueFilter, int limit = 20, Dictionary<string, RoleBaseline>? baselines = null);

    Task<Dictionary<string, RoleBaseline>> GetRoleBaselinesAsync(string puuid, string queueFilter);
    Task<IList<MatchupParticipantRaw>> GetMatchParticipantsAsync(string matchId);

    /// <summary>
    /// Deletes matches older than the specified cutoff date in batches.
    /// Uses CASCADE DELETE to automatically remove related records from all child tables.
    /// </summary>
    /// <param name="cutoffTimestamp">Unix timestamp in milliseconds (matches older than this will be deleted)</param>
    /// <param name="batchSize">Maximum number of matches to delete in one batch</param>
    /// <returns>Number of matches deleted in this batch</returns>
    Task<int> DeleteOldMatchesAsync(long cutoffTimestamp, int batchSize);
}

