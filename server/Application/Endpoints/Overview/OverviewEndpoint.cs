using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RiotProxy.Application.DTOs.Overview;
using RiotProxy.Infrastructure.External.Database.Repositories;

namespace RiotProxy.Application.Endpoints.Overview;

/// <summary>
/// Overview Endpoint
/// Returns aggregated dashboard data for the Overview page.
/// Includes player header, rank snapshot, last match, active goals, and suggested actions.
/// Primary queue is auto-selected based on highest match count in recent window.
/// </summary>
public sealed class OverviewEndpoint : IEndpoint
{
    public string Route { get; }
    
    // Data Dragon version for icon URLs
    private const string DataDragonVersion = "16.1.1";

    public OverviewEndpoint(string basePath)
    {
        Route = basePath + "/overview/{userId}";
    }

    public void Configure(WebApplication app)
    {
        var endpoint = app.MapGet(Route, async (
            HttpContext httpContext,
            [FromRoute] string userId,
            [FromServices] RiotAccountsRepository riotAccountRepo,
            [FromServices] OverviewStatsRepository overviewStatsRepo,
            [FromServices] ILpSnapshotsRepository lpSnapshotsRepo,
            [FromServices] ILogger<OverviewEndpoint> logger
        ) =>
        {
            try
            {
                if (httpContext.User?.Identity?.IsAuthenticated != true)
                    return Results.Unauthorized();

                // Parse userId
                if (!int.TryParse(userId, out var userIdInt))
                {
                    logger.LogWarning("Overview: invalid userId format {UserId}", userId);
                    return Results.BadRequest(new { error = "Invalid userId format" });
                }

                // Get riot accounts for the user
                var riotAccounts = await riotAccountRepo.GetByUserIdAsync(userIdInt);
                if (riotAccounts == null || riotAccounts.Count == 0)
                {
                    logger.LogWarning("Overview: no Riot accounts found for userId {UserId}", userIdInt);
                    return Results.NotFound(new { error = "No linked Riot accounts found" });
                }

                // Use primary account or first account
                var primaryAccount = riotAccounts.FirstOrDefault(a => a.IsPrimary) ?? riotAccounts[0];
                var primaryPuuid = primaryAccount.Puuid;

                logger.LogInformation("Overview request: userId={UserId}, puuid={Puuid}", userIdInt, primaryPuuid);

                // Build player header
                var profileIconUrl = BuildProfileIconUrl(primaryAccount.ProfileIconId);
                var activeContexts = DetermineActiveContexts(riotAccounts.Count);
                var playerHeader = new PlayerHeader(
                    SummonerName: primaryAccount.SummonerName,
                    Level: primaryAccount.SummonerLevel ?? 0,
                    Region: primaryAccount.Region.ToUpperInvariant(),
                    ProfileIconUrl: profileIconUrl,
                    ActiveContexts: activeContexts
                );

                // Determine primary queue
                var (primaryQueueId, primaryQueueLabel, _) = await overviewStatsRepo.GetPrimaryQueueAsync(primaryPuuid);

                // Get last 20 matches for primary queue
                var last20Matches = await overviewStatsRepo.GetLast20MatchesAsync(primaryPuuid, primaryQueueId);

                // Calculate rank snapshot using LP snapshots for accurate delta
                var rankSnapshot = await BuildRankSnapshotAsync(
                    primaryAccount,
                    primaryQueueId,
                    primaryQueueLabel,
                    last20Matches,
                    lpSnapshotsRepo
                );

                // Get last match
                var lastMatchData = await overviewStatsRepo.GetLastMatchAsync(primaryPuuid);
                var lastMatch = lastMatchData != null ? BuildLastMatch(lastMatchData) : null;

                // Active goals (placeholder - no goals table yet, return empty)
                var activeGoals = Array.Empty<GoalPreview>();

                // Suggested actions (placeholder - return empty for now)
                var suggestedActions = Array.Empty<SuggestedAction>();

                var response = new OverviewResponse(
                    PlayerHeader: playerHeader,
                    RankSnapshot: rankSnapshot,
                    LastMatch: lastMatch,
                    ActiveGoals: activeGoals,
                    SuggestedActions: suggestedActions
                );

                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Overview: unhandled error for userId {UserId}", userId);
                return Results.Problem("An unexpected error occurred");
            }
        }).RequireAuthorization();
    }

    private static string BuildProfileIconUrl(int? profileIconId)
    {
        var iconId = profileIconId ?? 29; // Default icon if not set
        return $"https://ddragon.leagueoflegends.com/cdn/{DataDragonVersion}/img/profileicon/{iconId}.png";
    }

    private static string[] DetermineActiveContexts(int accountCount)
    {
        // Solo is always active if there's at least one account
        // Duo and Team badges will be added when those features are available
        var contexts = new List<string> { "Solo" };
        
        // Could check for duo partners or team memberships in the future
        // For now, just return Solo
        
        return contexts.ToArray();
    }

    private static async Task<RankSnapshot> BuildRankSnapshotAsync(
        RiotProxy.External.Domain.Entities.RiotAccount account,
        int primaryQueueId,
        string primaryQueueLabel,
        List<MatchResultData> last20Matches,
        ILpSnapshotsRepository lpSnapshotsRepo)
    {
        // Get current rank based on primary queue
        string? rank = null;
        int? currentLp = null;
        string? queueType = null;
        string? currentTier = null;
        string? currentDivision = null;

        if (primaryQueueId == 420) // Ranked Solo/Duo
        {
            queueType = "RANKED_SOLO_5x5";
            if (!string.IsNullOrEmpty(account.SoloTier) && !string.IsNullOrEmpty(account.SoloRank))
            {
                rank = $"{account.SoloTier} {account.SoloRank}";
                currentLp = account.SoloLp;
                currentTier = account.SoloTier;
                currentDivision = account.SoloRank;
            }
        }
        else if (primaryQueueId == 440) // Ranked Flex
        {
            queueType = "RANKED_FLEX_SR";
            if (!string.IsNullOrEmpty(account.FlexTier) && !string.IsNullOrEmpty(account.FlexRank))
            {
                rank = $"{account.FlexTier} {account.FlexRank}";
                currentLp = account.FlexLp;
                currentTier = account.FlexTier;
                currentDivision = account.FlexRank;
            }
        }

        // Calculate wins and losses from last 20
        var last20Wins = last20Matches.Count(m => m.Win);
        var last20Losses = last20Matches.Count(m => !m.Win);

        // Build W/L array (newest first, true = win, false = loss)
        var wlLast20 = last20Matches.Select(m => m.Win).ToArray();

        // Calculate LP delta using LP snapshots
        // Find the LP snapshot closest to the oldest match in the last 20
        var lpDeltaLast20 = await CalculateLpDeltaFromSnapshotsAsync(
            account.Puuid,
            queueType,
            currentTier,
            currentDivision,
            currentLp,
            last20Matches,
            lpSnapshotsRepo
        );

        // We no longer calculate per-match LP deltas since we don't have accurate per-match LP data
        // Return an empty array - the UI should handle this gracefully
        var lpDeltasLast20 = Array.Empty<int>();

        return new RankSnapshot(
            PrimaryQueueLabel: primaryQueueLabel,
            Rank: rank,
            Lp: currentLp,
            LpDeltaLast20: lpDeltaLast20,
            Last20Wins: last20Wins,
            Last20Losses: last20Losses,
            LpDeltasLast20: lpDeltasLast20,
            WlLast20: wlLast20
        );
    }

    /// <summary>
    /// Calculates LP delta by comparing current rank/LP to the LP snapshot from around the time
    /// of the oldest match in the last 20. Accounts for tier/division changes by converting
    /// to absolute LP values.
    /// Falls back to the oldest available snapshot if no snapshot exists before the oldest match.
    /// </summary>
    private static async Task<int> CalculateLpDeltaFromSnapshotsAsync(
        string puuid,
        string? queueType,
        string? currentTier,
        string? currentDivision,
        int? currentLp,
        List<MatchResultData> last20Matches,
        ILpSnapshotsRepository lpSnapshotsRepo)
    {
        // If no current LP or no queue type, we can't calculate delta
        if (currentLp == null || queueType == null || last20Matches.Count == 0)
            return 0;

        // Find the oldest match in the last 20 (matches are ordered newest first)
        var oldestMatch = last20Matches.OrderBy(m => m.GameStartTime).First();

        // Convert game start time (epoch milliseconds) to DateTime
        var oldestMatchTime = DateTimeOffset.FromUnixTimeMilliseconds(oldestMatch.GameStartTime).UtcDateTime;

        // Get the LP snapshot closest to (but not after) the oldest match
        var oldSnapshot = await lpSnapshotsRepo.GetSnapshotAtOrBeforeAsync(puuid, queueType, oldestMatchTime);

        if (oldSnapshot == null)
        {
            // No snapshot before the oldest match - fall back to the oldest available snapshot
            // This handles the case where LP tracking started after matches were already played
            oldSnapshot = await lpSnapshotsRepo.GetOldestByPuuidAndQueueAsync(puuid, queueType);

            if (oldSnapshot == null)
            {
                // No snapshots at all - can't calculate delta
                return 0;
            }
        }

        // Convert both current and old rank to absolute LP for accurate comparison
        var currentAbsoluteLp = CalculateAbsoluteLp(currentTier, currentDivision, currentLp.Value);
        var oldAbsoluteLp = CalculateAbsoluteLp(oldSnapshot.Tier, oldSnapshot.Division, oldSnapshot.Lp);

        return currentAbsoluteLp - oldAbsoluteLp;
    }

    /// <summary>
    /// Converts tier + division + LP to an absolute LP value for comparison across ranks.
    /// Each division is worth 100 LP, each tier is worth 400 LP (4 divisions).
    /// Master+ tiers don't have divisions, so LP can exceed 100.
    /// </summary>
    private static int CalculateAbsoluteLp(string? tier, string? division, int lp)
    {
        var tierValue = GetTierValue(tier);
        var divisionValue = GetDivisionValue(division);

        // For Master+ (no divisions), just add LP directly to tier base
        // For other tiers, add division offset + LP within division
        return tierValue + divisionValue + lp;
    }

    /// <summary>
    /// Gets the base LP value for a tier.
    /// Each tier below Master is worth 400 LP (4 divisions × 100 LP each).
    /// </summary>
    private static int GetTierValue(string? tier)
    {
        return tier?.ToUpperInvariant() switch
        {
            "IRON" => 0,
            "BRONZE" => 400,
            "SILVER" => 800,
            "GOLD" => 1200,
            "PLATINUM" => 1600,
            "EMERALD" => 2000,
            "DIAMOND" => 2400,
            "MASTER" => 2800,
            "GRANDMASTER" => 2800, // Same base as Master, differentiated by LP
            "CHALLENGER" => 2800,  // Same base as Master, differentiated by LP
            _ => 0
        };
    }

    /// <summary>
    /// Gets the LP offset for a division within a tier.
    /// IV = 0, III = 100, II = 200, I = 300.
    /// Master+ don't have divisions (returns 0).
    /// </summary>
    private static int GetDivisionValue(string? division)
    {
        return division?.ToUpperInvariant() switch
        {
            "IV" => 0,
            "III" => 100,
            "II" => 200,
            "I" => 300,
            _ => 0 // Master+ don't have divisions
        };
    }

    private static LastMatch BuildLastMatch(LastMatchData data)
    {
        var championIconUrl = BuildChampionIconUrl(data.ChampionName);
        var result = data.Win ? "Victory" : "Defeat";
        var kda = $"{data.Kills}/{data.Deaths}/{data.Assists}";
        var queueType = OverviewStatsRepository.GetQueueLabel(data.QueueId);

        return new LastMatch(
            MatchId: data.MatchId,
            ChampionIconUrl: championIconUrl,
            ChampionName: data.ChampionName,
            Result: result,
            Kda: kda,
            Timestamp: data.GameStartTime,
            QueueType: queueType
        );
    }

    private static string BuildChampionIconUrl(string championName)
    {
        // Normalize champion name for Data Dragon URL
        var normalized = championName.Replace(" ", "").Replace("'", "");
        return $"https://ddragon.leagueoflegends.com/cdn/{DataDragonVersion}/img/champion/{normalized}.png";
    }
}

