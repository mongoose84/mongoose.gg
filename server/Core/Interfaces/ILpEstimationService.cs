namespace Mongoose.Api.Core.Interfaces;

/// <summary>
/// Service for estimating historical LP values for ranked matches.
/// Works backwards from current known LP through match history,
/// estimating LP gain/loss per match using community-sourced averages.
/// </summary>
public interface ILpEstimationService
{
    /// <summary>
    /// Estimates LP for recent ranked matches that don't have LP data.
    /// Walks backwards from current LP, stops when hitting a match that already has LP data.
    /// Never overwrites existing LP values.
    /// </summary>
    /// <param name="puuid">Player's PUUID</param>
    /// <param name="queueId">Queue ID (420 = Solo/Duo, 440 = Flex)</param>
    /// <param name="currentLp">Current LP from Riot API</param>
    /// <param name="currentTier">Current tier (e.g., "GOLD")</param>
    /// <param name="currentDivision">Current division (e.g., "II")</param>
    /// <param name="maxMatches">Maximum number of matches to estimate (default 20)</param>
    /// <returns>Number of matches that had LP estimated</returns>
    Task<int> EstimateLpForRecentMatchesAsync(
        string puuid,
        int queueId,
        int currentLp,
        string currentTier,
        string currentDivision,
        int maxMatches = 20);
}

