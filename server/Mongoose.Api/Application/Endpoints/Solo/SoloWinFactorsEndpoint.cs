using Microsoft.AspNetCore.Mvc;
using Mongoose.Api.Application.DTOs.Solo;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Application.Services;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Core.QueryModels;
using Mongoose.Api.Core.Services.Solo;

namespace Mongoose.Api.Application.Endpoints.Solo;

/// <summary>
/// Solo win factors and patterns: win rate when the player hits each mark vs misses it, and the session,
/// after-a-loss and match-length patterns (features/solo-trends.spec.md FR21–FR28).
/// </summary>
public sealed class SoloWinFactorsEndpoint : IEndpoint
{
    public string Route { get; }

    public SoloWinFactorsEndpoint(string basePath)
    {
        Route = basePath + "/solo/win-factors/{userId}";
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
            [FromServices] ILogger<SoloWinFactorsEndpoint> logger
        ) =>
        {
            try
            {
                var (error, scope) = await SoloTrendsRequest.ResolveAsync(
                    httpContext, userId, queueType, range, accountId, puuidResolutionService, soloTrendsRepo, logger);
                if (error != null) return error;

                logger.LogInformation("Solo win factors request: userId={UserId}, accountCount={AccountCount}, queueType={Queue}, range={Range}",
                    LogSanitizer.Sanitize(scope!.UserId.ToString()), scope.Puuids.Count,
                    LogSanitizer.Sanitize(scope.QueueType), LogSanitizer.Sanitize(scope.RangeKey));

                var rows = await soloTrendsRepo.GetMatchRowsAsync(scope.Puuids, scope.QueueType, scope.Range);
                var factors = WinFactorCalculator.Calculate(rows);
                var patterns = PatternCalculator.Calculate(rows);

                // FR21: the vision and CS labels show the role's mark only when every match in range was one role.
                var roles = rows.Select(r => r.Role).Distinct().ToList();
                var singleRole = roles.Count == 1 ? roles[0] : null;

                return Results.Ok(new SoloTrendsDto.WinFactorsResponse(
                    rows.Count,
                    scope.QueueType,
                    scope.RangeKey,
                    factors.Select(f => SoloTrendsDto.ToDto(f, singleRole == null ? null : WinFactorCalculator.Mark(f.Key, singleRole))).ToList(),
                    SoloTrendsDto.ToDto(patterns)));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Solo win factors: unhandled error");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });

        endpoint.RequireAuthorization();
    }
}
