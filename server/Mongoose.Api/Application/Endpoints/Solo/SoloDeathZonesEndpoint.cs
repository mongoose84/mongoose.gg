using Microsoft.AspNetCore.Mvc;
using Mongoose.Api.Application.DTOs.Solo;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Application.Services;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Core.Services.Solo;

namespace Mongoose.Api.Application.Endpoints.Solo;

/// <summary>
/// Solo death zones: where the player's deaths happen and which ones cost objectives, with the
/// phase, how and cost breakdowns, and the death detail backfill's progress while older matches
/// are still being read (features/solo-trends.spec.md FR 31-39).
/// </summary>
public sealed class SoloDeathZonesEndpoint : IEndpoint
{
    public string Route { get; }

    public SoloDeathZonesEndpoint(string basePath)
    {
        Route = basePath + "/solo/death-zones/{userId}";
    }

    public void Configure(WebApplication app)
    {
        var endpoint = app.MapGet(Route, async (
            HttpContext httpContext,
            [FromRoute] string userId,
            [FromQuery] string? queueType,
            [FromQuery] string? range,
            [FromQuery] string? accountId,
            [FromServices] PuuidResolutionService puuidResolutionService,
            [FromServices] ISoloTrendsRepository soloTrendsRepo,
            [FromServices] IDeathDetailBackfillState backfillState,
            [FromServices] ILogger<SoloDeathZonesEndpoint> logger
        ) =>
        {
            try
            {
                var (error, scope) = await SoloTrendsRequest.ResolveAsync(
                    httpContext, userId, queueType, range, accountId, puuidResolutionService, soloTrendsRepo, logger);
                if (error != null) return error;

                logger.LogInformation("Solo death zones request: userId={UserId}, accountCount={AccountCount}, queueType={Queue}, range={Range}",
                    LogSanitizer.Sanitize(scope!.UserId.ToString()), scope.Puuids.Count,
                    LogSanitizer.Sanitize(scope.QueueType), LogSanitizer.Sanitize(scope.RangeKey));

                var rows = await soloTrendsRepo.GetMatchRowsAsync(scope.Puuids, scope.QueueType, scope.Range);
                var data = await soloTrendsRepo.GetDeathDataAsync(scope.Puuids, rows.Select(r => r.MatchId).ToList());
                var zones = DeathZonesCalculator.Calculate(data.Deaths, data.Participants, data.Objectives);

                var backfill = zones.MissingDetail == 0
                    ? null
                    : Backfill(backfillState, await soloTrendsRepo.GetDeathDetailPendingAccountsAsync(scope.Puuids));

                return Results.Ok(SoloTrendsDto.ToDto(zones, rows.Count, scope.QueueType, scope.RangeKey, backfill));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Solo death zones: unhandled error");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });

        endpoint.RequireAuthorization();
    }

    /// <summary>
    /// FR 39, called when deaths in range lack their detail: the job's progress for an account in
    /// scope whose backfill isn't done, or "queued" when the job hasn't reached it yet. Null when
    /// every account is done: the job only reads the last 50 matches and skips timelines Riot no
    /// longer serves, so older deaths can stay without detail for good. Asking for the zones moves
    /// the pending accounts to the front of the backfill (FR 38).
    /// </summary>
    private static SoloTrendsDto.BackfillDto? Backfill(IDeathDetailBackfillState state, IReadOnlyList<string> pendingPuuids)
    {
        if (pendingPuuids.Count == 0) return null;

        foreach (var puuid in pendingPuuids) state.Prioritize(puuid);

        var progress = pendingPuuids.Select(state.Get).FirstOrDefault(p => p != null)
            ?? new DeathDetailBackfillProgress(DeathDetailBackfillProgress.Queued, 0, 0);
        return new SoloTrendsDto.BackfillDto(progress.Status, progress.Done, progress.Total, progress.RetryAt);
    }
}
