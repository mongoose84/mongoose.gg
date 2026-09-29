using Microsoft.AspNetCore.Mvc;
using Mongoose.Api.Application.DTOs.Solo;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Application.Services;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Core.QueryModels;
using Mongoose.Api.Core.Services.Solo;

namespace Mongoose.Api.Application.Endpoints.Solo;

/// <summary>
/// Solo stat trends: the six match-deciding stats over the range, and the "Your focus" pick
/// (features/solo-trends.spec.md FR13–FR20).
/// </summary>
public sealed class SoloStatTrendsEndpoint : IEndpoint
{
    public string Route { get; }

    public SoloStatTrendsEndpoint(string basePath)
    {
        Route = basePath + "/solo/stat-trends/{userId}";
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
            [FromServices] ILogger<SoloStatTrendsEndpoint> logger
        ) =>
        {
            try
            {
                var (error, scope) = await SoloTrendsRequest.ResolveAsync(
                    httpContext, userId, queueType, range, accountId, puuidResolutionService, soloTrendsRepo, logger);
                if (error != null) return error;

                logger.LogInformation("Solo stat trends request: userId={UserId}, accountCount={AccountCount}, queueType={Queue}, range={Range}",
                    LogSanitizer.Sanitize(scope!.UserId.ToString()), scope.Puuids.Count,
                    LogSanitizer.Sanitize(scope.QueueType), LogSanitizer.Sanitize(scope.RangeKey));

                var rows = await soloTrendsRepo.GetMatchRowsAsync(scope.Puuids, scope.QueueType, scope.Range);

                // FR16: the benchmark is the season average in the same queue scope.
                var seasonRows = scope.Range == SoloRange.Season
                    ? rows
                    : await soloTrendsRepo.GetMatchRowsAsync(scope.Puuids, scope.QueueType, SoloRange.Season);

                var trends = StatTrendCalculator.Calculate(rows, seasonRows);
                var factors = WinFactorCalculator.Calculate(rows);
                var focus = SoloFocusPicker.Pick(rows, trends, factors);

                return Results.Ok(new SoloTrendsDto.StatTrendsResponse(
                    rows.Count,
                    scope.QueueType,
                    scope.RangeKey,
                    trends.Select(SoloTrendsDto.ToDto).ToList(),
                    SoloTrendsDto.ToDto(focus)));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Solo stat trends: unhandled error");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });

        endpoint.RequireAuthorization();
    }
}
