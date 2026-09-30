using Microsoft.AspNetCore.Mvc;
using Mongoose.Api.Application.DTOs.Solo;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Application.Services;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Core.Services.Solo;

namespace Mongoose.Api.Application.Endpoints.Solo;

/// <summary>
/// Solo climb: LP or win-rate mode, the ladder or rolling win rate, promotions, the biggest drop,
/// LP per champion and the current rank (features/solo-trends.spec.md FR 5–12, FR 24).
/// </summary>
public sealed class SoloClimbEndpoint : IEndpoint
{
    public string Route { get; }

    public SoloClimbEndpoint(string basePath)
    {
        Route = basePath + "/solo/climb/{userId}";
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
            [FromServices] ILogger<SoloClimbEndpoint> logger
        ) =>
        {
            try
            {
                var (error, scope) = await SoloTrendsRequest.ResolveAsync(
                    httpContext, userId, queueType, range, accountId, puuidResolutionService, soloTrendsRepo, logger);
                if (error != null) return error;

                logger.LogInformation("Solo climb request: userId={UserId}, accountCount={AccountCount}, queueType={Queue}, range={Range}",
                    LogSanitizer.Sanitize(scope!.UserId.ToString()), scope.Puuids.Count,
                    LogSanitizer.Sanitize(scope.QueueType), LogSanitizer.Sanitize(scope.RangeKey));

                var rows = await soloTrendsRepo.GetMatchRowsAsync(scope.Puuids, scope.QueueType, scope.Range);
                var climb = ClimbCalculator.Calculate(rows, scope.QueueType, scope.SingleAccount);

                return Results.Ok(SoloTrendsDto.ToDto(climb, rows.Count, scope.QueueType, scope.RangeKey));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Solo climb: unhandled error");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });

        endpoint.RequireAuthorization();
    }
}
