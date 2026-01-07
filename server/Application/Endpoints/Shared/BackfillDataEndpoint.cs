using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Backfill;
using RiotProxy.Infrastructure.External.Database.Repositories;
using RiotProxy.Infrastructure.External.Riot;

namespace RiotProxy.Application.Endpoints
{
    public sealed class BackfillDataEndpoint : IEndpoint
    {
        public string Route { get; }

        public BackfillDataEndpoint(string basePath)
        {
            Route = basePath + "/admin/backfill/queue-id";
        }

        public void Configure(WebApplication app)
        {
            app.MapPost(Route, async (
                [FromServices] LolMatchRepository matchRepository,
                [FromServices] LolMatchParticipantRepository participantRepository,
                [FromServices] IRiotApiClient riotApiClient,
                CancellationToken ct
                ) =>
            {
                try
                {
                    // NOTE: This endpoint runs backfill jobs synchronously within the HTTP request.
                    // For large datasets, this operation is long-running and may approach or exceed HTTP timeout limits.
                    // Consider implementing as a background job (e.g., Hangfire) for production use with very large datasets.
                    var runner = new BackfillJobRunner(
                        batchSize: 10,
                        delayBetweenBatches: TimeSpan.FromMilliseconds(500)
                    );

                    var processedJob = new ProcessedMatchMetadataBackfillJob(matchRepository, riotApiClient);
                    var processedResult = await runner.RunAsync(processedJob, ct);

                    var unprocessedJob = new UnprocessedMatchBackfillJob(matchRepository, participantRepository, riotApiClient);
                    var unprocessedResult = await runner.RunAsync(unprocessedJob, ct);

                    return Results.Ok(new
                    {
                        processed = new
                        {
                            jobName = processedResult.JobName,
                            status = processedResult.Status.ToString(),
                            totalItems = processedResult.TotalItems,
                            itemsProcessed = processedResult.ItemsProcessed,
                            durationSeconds = processedResult.DurationSeconds,
                            error = processedResult.Error
                        },
                        unprocessed = new
                        {
                            jobName = unprocessedResult.JobName,
                            status = unprocessedResult.Status.ToString(),
                            totalItems = unprocessedResult.TotalItems,
                            itemsProcessed = unprocessedResult.ItemsProcessed,
                            durationSeconds = unprocessedResult.DurationSeconds,
                            error = unprocessedResult.Error
                        }
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        detail: ex.Message,
                        statusCode: 500,
                        title: "Backfill job failed"
                    );
                }
            })
            .WithName("BackfillMatchMetadata")
            .WithTags("Admin", "Backfill")
            .WithDescription("Backfills QueueId/metadata for processed matches and fully hydrates unprocessed matches. " +
                "WARNING: This is a long-running synchronous operation. Large datasets may take several minutes and could exceed HTTP timeout limits. " +
                "For very large datasets, adjust client/server timeout settings accordingly.");
        }
    }
}
