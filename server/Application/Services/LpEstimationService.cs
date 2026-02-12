using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Core.QueryModels;

namespace Mongoose.Api.Application.Services;

/// <summary>
/// Estimates historical LP values for ranked matches by working backwards
/// from current known LP. Uses community-sourced averages for LP gain/loss.
/// Stops when hitting a match that already has LP data (never overwrites).
/// </summary>
public sealed class LpEstimationService : ILpEstimationService
{
    private readonly IParticipantsRepository _participantsRepo;
    private readonly ILogger<LpEstimationService> _logger;

    // Remake detection
    private const int RemakeThresholdSeconds = 210; // 3.5 minutes

    // Base LP changes (from community data)
    private const int BaseLpGain = 20;
    private const int BaseLpLoss = 17;

    // Streak bonuses
    private const int StreakBonusPerWin = 2;
    private const int MaxStreakBonus = 10;
    private const int StreakPenaltyPerLoss = 2;
    private const int MaxStreakPenalty = 8;

    // Rank transition estimates
    private const int PromotionEstimateLp = 75;
    private const int DemotionEstimateLp = 25;

    // Estimation limits
    private const int MaxEstimatesPerRun = 20;

    // Master+ tiers don't have divisions
    private static readonly HashSet<string> ApexTiers = new(StringComparer.OrdinalIgnoreCase)
        { "MASTER", "GRANDMASTER", "CHALLENGER" };

    public LpEstimationService(IParticipantsRepository participantsRepo, ILogger<LpEstimationService> logger)
    {
        _participantsRepo = participantsRepo;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<int> EstimateLpForRecentMatchesAsync(
        string puuid, int queueId, int currentLp, string currentTier, string currentDivision, int maxMatches = 20)
    {
        // Skip Master+ tiers - LP gain/loss is too variable for estimation
        if (ApexTiers.Contains(currentTier))
        {
            _logger.LogDebug("Skipping LP estimation for {Puuid} - apex tier {Tier}", puuid, currentTier);
            return 0;
        }

        // Fetch recent ranked matches
        var matches = await _participantsRepo.GetRecentRankedMatchesForLpEstimationAsync(puuid, queueId, maxMatches);
        if (matches.Count == 0)
        {
            _logger.LogDebug("No ranked matches found for LP estimation for {Puuid}", puuid);
            return 0;
        }

        // Pre-calculate streaks for all matches
        var streaks = CalculateStreaks(matches);

        // Walk backwards from current LP (newest match first)
        var lp = currentLp;
        var tier = currentTier;
        var division = currentDivision;
        var estimates = new List<(string matchId, string puuid, int lpAfter, string tierAfter, string rankAfter)>();

        // Track the current season from the first match we process
        string? currentSeason = null;

        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];

            // Initialize current season from the first match
            currentSeason ??= match.SeasonCode;

            // Stop if we've reached a different season (LP resets between seasons)
            if (currentSeason != null && match.SeasonCode != null && match.SeasonCode != currentSeason)
            {
                _logger.LogDebug("Hit season boundary at match {MatchId} (current: {CurrentSeason}, match: {MatchSeason}), stopping estimation",
                    match.MatchId, currentSeason, match.SeasonCode);
                break;
            }

            // Stop if we've reached the max estimates per run
            if (estimates.Count >= MaxEstimatesPerRun)
            {
                _logger.LogDebug("Reached max estimates ({Max}) at match {MatchId}, stopping estimation",
                    MaxEstimatesPerRun, match.MatchId);
                break;
            }

            // If match has LP data, check if it's actual or estimated
            if (match.LpAfter.HasValue)
            {
                if (!match.IsLpEstimated)
                {
                    // Actual LP data from Riot API - use as anchor and STOP
                    lp = match.LpAfter.Value;
                    tier = match.TierAfter ?? tier;
                    division = match.RankAfter ?? division;
                    _logger.LogDebug("Hit actual LP data at match {MatchId} ({Tier} {Division} {LP} LP), stopping estimation",
                        match.MatchId, tier, division, lp);
                    break;
                }
                else
                {
                    // Previously estimated LP - use as anchor but CONTINUE to backfill older matches
                    lp = match.LpAfter.Value;
                    tier = match.TierAfter ?? tier;
                    division = match.RankAfter ?? division;
                    _logger.LogDebug("Using estimated LP as anchor at match {MatchId} ({Tier} {Division} {LP} LP), continuing backwards",
                        match.MatchId, tier, division, lp);
                    continue;
                }
            }

            // Record estimate for this match (LP AFTER this match = current state)
            estimates.Add((match.MatchId, match.Puuid, lp, tier, division));

            // Calculate LP BEFORE this match (reverse the change)
            int lpChange;
            if (IsRemake(match))
            {
                lpChange = 0;
            }
            else if (match.Win)
            {
                // Reverse a win: subtract LP gain
                var gain = BaseLpGain + Math.Min(streaks[i].ConsecutiveWins * StreakBonusPerWin, MaxStreakBonus);
                lpChange = -gain;
            }
            else
            {
                // Reverse a loss: add back LP loss
                var loss = BaseLpLoss + Math.Min(streaks[i].ConsecutiveLosses * StreakPenaltyPerLoss, MaxStreakPenalty);
                lpChange = loss;
            }

            var newLp = lp + lpChange;

            // Handle rank transitions
            if (newLp > 100)
            {
                // Reverse promotion: player was in previous division before this match
                (tier, division) = GetPreviousDivision(tier, division);
                newLp = PromotionEstimateLp;
                _logger.LogDebug("Detected promotion boundary at {MatchId}: now {Tier} {Division}", match.MatchId, tier, division);
            }
            else if (newLp < 0)
            {
                // Reverse demotion: player was in next division before this match
                (tier, division) = GetNextDivision(tier, division);
                newLp = DemotionEstimateLp;
                _logger.LogDebug("Detected demotion boundary at {MatchId}: now {Tier} {Division}", match.MatchId, tier, division);
            }

            lp = Math.Clamp(newLp, 0, 100);
        }

        // Write estimates to database
        if (estimates.Count > 0)
        {
            var updated = await _participantsRepo.BatchUpdateLpEstimatesAsync(estimates);
            _logger.LogInformation("Estimated LP for {Count} matches for {Puuid} (queue {QueueId}), {Updated} rows updated",
                estimates.Count, puuid, queueId, updated);
            return updated;
        }

        return 0;
    }

    /// <summary>
    /// Pre-calculates win/loss streaks for each match position.
    /// Streak counts how many consecutive same-result games came BEFORE this match
    /// (looking at more recent matches, since the list is ordered newest first).
    /// </summary>
    private static List<(int ConsecutiveWins, int ConsecutiveLosses)> CalculateStreaks(IList<LpEstimationMatch> matches)
    {
        var streaks = new List<(int ConsecutiveWins, int ConsecutiveLosses)>();

        for (int i = 0; i < matches.Count; i++)
        {
            int consecutiveWins = 0;
            int consecutiveLosses = 0;

            // Count streak by looking at matches BEFORE this one (lower index = more recent)
            for (int j = i - 1; j >= 0; j--)
            {
                if (IsRemake(matches[j])) continue; // Skip remakes in streak calculation

                if (matches[j].Win)
                {
                    if (consecutiveLosses > 0) break; // Streak broken
                    consecutiveWins++;
                }
                else
                {
                    if (consecutiveWins > 0) break; // Streak broken
                    consecutiveLosses++;
                }
            }

            streaks.Add((consecutiveWins, consecutiveLosses));
        }

        return streaks;
    }

    private static bool IsRemake(LpEstimationMatch match)
    {
        return match.GameDurationSec < RemakeThresholdSeconds;
    }

    /// <summary>
    /// Gets the division BELOW the current one (for reversing a promotion).
    /// E.g., Gold IV → Silver I, Gold III → Gold IV
    /// </summary>
    private static (string tier, string division) GetPreviousDivision(string tier, string division)
    {
        var divisionOrder = new[] { "IV", "III", "II", "I" };
        var tierOrder = new[] { "IRON", "BRONZE", "SILVER", "GOLD", "PLATINUM", "EMERALD", "DIAMOND" };

        var divIndex = Array.IndexOf(divisionOrder, division);
        if (divIndex > 0)
        {
            // Move down one division within same tier (e.g., Gold III → Gold IV)
            return (tier, divisionOrder[divIndex - 1]);
        }

        // At division IV, move to previous tier's division I
        var tierIndex = Array.IndexOf(tierOrder, tier.ToUpperInvariant());
        if (tierIndex > 0)
        {
            return (tierOrder[tierIndex - 1], "I");
        }

        // Already at Iron IV, can't go lower
        return (tier, division);
    }

    /// <summary>
    /// Gets the division ABOVE the current one (for reversing a demotion).
    /// E.g., Silver I → Gold IV, Gold II → Gold I
    /// </summary>
    private static (string tier, string division) GetNextDivision(string tier, string division)
    {
        var divisionOrder = new[] { "IV", "III", "II", "I" };
        var tierOrder = new[] { "IRON", "BRONZE", "SILVER", "GOLD", "PLATINUM", "EMERALD", "DIAMOND" };

        var divIndex = Array.IndexOf(divisionOrder, division);
        if (divIndex < divisionOrder.Length - 1)
        {
            // Move up one division within same tier (e.g., Gold II → Gold I)
            return (tier, divisionOrder[divIndex + 1]);
        }

        // At division I, move to next tier's division IV
        var tierIndex = Array.IndexOf(tierOrder, tier.ToUpperInvariant());
        if (tierIndex < tierOrder.Length - 1)
        {
            return (tierOrder[tierIndex + 1], "IV");
        }

        // Already at Diamond I, next would be Master (apex) - stay put
        return (tier, division);
    }
}
