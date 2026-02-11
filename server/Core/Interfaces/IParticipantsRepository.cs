using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.QueryModels;

namespace Mongoose.Api.Core.Interfaces;

public interface IParticipantsRepository
{
    Task<long> InsertAsync(Participant participant);
    Task<IList<Participant>> GetByMatchAsync(string matchId);
    Task UpdateLpDataAsync(string matchId, string puuid, int? lp, string? tier, string? rank);
    Task<ISet<string>> GetMatchIdsForPuuidAsync(string puuid);
    Task<IList<Participant>> GetRecentByPuuidAsync(string puuid, int? queueId, int limit);

    /// <summary>
    /// Gets recent ranked matches for LP estimation.
    /// Returns lightweight models with only the fields needed for the estimation algorithm.
    /// Ordered by game_start_time DESC (newest first).
    /// </summary>
    Task<IList<LpEstimationMatch>> GetRecentRankedMatchesForLpEstimationAsync(string puuid, int queueId, int limit);

    /// <summary>
    /// Batch updates estimated LP data for multiple matches.
    /// Only updates rows where lp_after IS NULL (never overwrites existing LP data).
    /// </summary>
    Task<int> BatchUpdateLpEstimatesAsync(IList<(string matchId, string puuid, int lpAfter, string tierAfter, string rankAfter)> estimates);
}

