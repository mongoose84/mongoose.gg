using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RiotProxy.Application.DTOs.Matches;
using RiotProxy.Core.QueryModels;
using RiotProxy.Infrastructure.Database.Repositories;

namespace RiotProxy.Application.Endpoints.Matches;

/// <summary>
/// Match Narrative Endpoint
/// Returns lane matchups for all 5 roles with detailed stats for storytelling.
/// </summary>
public sealed class MatchNarrativeEndpoint : IEndpoint
{
    private const string DataDragonVersion = "16.1.1";
    public string Route { get; }

    public MatchNarrativeEndpoint(string basePath)
    {
        Route = basePath + "/matches/{matchId}/narrative";
    }

    public void Configure(WebApplication app)
    {
        var endpoint = app.MapGet(Route, async (
            HttpContext httpContext,
            [FromRoute] string matchId,
            [FromQuery] string? puuid,
            [FromServices] MatchesRepository matchesRepo,
            [FromServices] RiotAccountsRepository riotAccountsRepo,
            [FromServices] ILogger<MatchNarrativeEndpoint> logger
        ) =>
        {
            try
            {
                if (httpContext.User?.Identity?.IsAuthenticated != true)
                    return Results.Unauthorized();

                if (string.IsNullOrWhiteSpace(matchId))
                {
                    return Results.BadRequest(new { error = "matchId is required" });
                }

                if (string.IsNullOrWhiteSpace(puuid))
                {
                    return Results.BadRequest(new { error = "puuid query parameter is required" });
                }

                // Verify the puuid belongs to the authenticated user
                var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
                {
                    return Results.Unauthorized();
                }

                var userRiotAccounts = await riotAccountsRepo.GetByUserIdAsync(userId);
                if (userRiotAccounts == null || !userRiotAccounts.Any(a => a.Puuid == puuid))
                {
                    logger.LogWarning("Match narrative: user {UserId} attempted to access data for unowned puuid {Puuid}",
                        userId, puuid);
                    return Results.Forbid();
                }

                logger.LogInformation("Match narrative request: matchId={MatchId}, puuid={Puuid}",
                    matchId, puuid);

                // Fetch all participants for this match
                var participants = await matchesRepo.GetMatchParticipantsAsync(matchId);

                if (participants.Count == 0)
                {
                    logger.LogWarning("Match narrative: no participants found for matchId {MatchId}", matchId);
                    return Results.NotFound(new { error = "Match not found or no participant data" });
                }

                // Find the user's team
                var userParticipant = participants.FirstOrDefault(p => p.Puuid == puuid);
                if (userParticipant == null)
                {
                    logger.LogWarning("Match narrative: user puuid {Puuid} not found in match {MatchId}", puuid, matchId);
                    return Results.NotFound(new { error = "User not found in this match" });
                }

                var userTeamId = userParticipant.TeamId;
                var userRole = userParticipant.Role ?? "UNKNOWN";

                // Group participants by role and create lane matchups
                var laneMatchups = CreateLaneMatchups(participants, userTeamId);

                var response = new MatchNarrativeResponse(matchId, userRole, laneMatchups);
                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Match narrative: unhandled error for matchId {MatchId}", matchId);
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });

        endpoint.RequireAuthorization();
    }

    private LaneMatchup[] CreateLaneMatchups(IList<MatchupParticipantRaw> participants, int userTeamId)
    {
        var roles = new[] { "TOP", "JUNGLE", "MIDDLE", "BOTTOM", "UTILITY" };
        var matchups = new List<LaneMatchup>();

        foreach (var role in roles)
        {
            var ally = participants.FirstOrDefault(p => p.TeamId == userTeamId && p.Role == role);
            var enemy = participants.FirstOrDefault(p => p.TeamId != userTeamId && p.Role == role);

            if (ally == null || enemy == null)
                continue;

            var laneWinner = DetermineLaneWinner(ally.GoldDiffAt10, enemy.GoldDiffAt10);

            matchups.Add(new LaneMatchup(
                Role: role,
                AllyParticipant: ToMatchupParticipant(ally),
                EnemyParticipant: ToMatchupParticipant(enemy),
                LaneWinner: laneWinner
            ));
        }

        return matchups.ToArray();
    }

    private static string DetermineLaneWinner(int? allyGoldDiff, int? enemyGoldDiff)
    {
        // Prefer ally's gold diff (positive means ally ahead)
        if (allyGoldDiff.HasValue)
        {
            // Use 300g threshold for 10-minute checkpoint (lower than 15m threshold)
            return allyGoldDiff.Value switch
            {
                >= 300 => "ally",
                <= -300 => "enemy",
                _ => "even"
            };
        }

        // Fall back to enemy's gold diff (inverted: positive enemy diff means ally behind)
        if (enemyGoldDiff.HasValue)
        {
            return enemyGoldDiff.Value switch
            {
                >= 300 => "enemy",  // Enemy ahead means ally lost lane
                <= -300 => "ally",  // Enemy behind means ally won lane
                _ => "even"
            };
        }

        // No data available for either side
        return "even";
    }

    private MatchupParticipant ToMatchupParticipant(MatchupParticipantRaw raw) => new(
        Puuid: raw.Puuid,
        SummonerName: "", // Not available in current data - could be added later
        ChampionId: raw.ChampionId,
        ChampionName: raw.ChampionName,
        ChampionIconUrl: GetChampionIconUrl(raw.ChampionName),
        TeamId: raw.TeamId,
        Win: raw.Win,
        Kills: raw.Kills,
        Deaths: raw.Deaths,
        Assists: raw.Assists,
        GoldAt10: raw.GoldAt10,
        CsAt10: raw.CsAt10,
        GoldDiffAt10: raw.GoldDiffAt10,
        CsDiffAt10: raw.CsDiffAt10,
        DeathsPre10: raw.DeathsPre10,
        SoloKills: 0, // TODO: Add solo kills calculation
        DamageShare: (double)raw.DamageShare,
        KillParticipation: (double)raw.KillParticipation,
        VisionScore: raw.VisionScore,
        CreepScore: raw.CreepScore,
        GoldEarned: raw.GoldEarned
    );

    private static string GetChampionIconUrl(string championName)
    {
        var normalized = championName.Replace(" ", "").Replace("'", "");
        return $"https://ddragon.leagueoflegends.com/cdn/{DataDragonVersion}/img/champion/{normalized}.png";
    }
}

