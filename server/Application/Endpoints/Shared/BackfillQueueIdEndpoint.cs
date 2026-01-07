using Microsoft.AspNetCore.Mvc;
using RiotProxy.Infrastructure.External.Backfill;
using RiotProxy.Infrastructure.External.Database.Repositories;
using RiotProxy.Infrastructure.External.Riot;

namespace RiotProxy.Application.Endpoints
{
    public sealed class BackfillQueueIdEndpoint : IEndpoint
    {
        public string Route { get; }

        public BackfillQueueIdEndpoint(string basePath)
        {
            Route = basePath + "/admin/backfill/queue-id";
        }

        public void Configure(WebApplication app)
        {
            app.MapPost(Route, async (
                [FromServices] LolMatchRepository matchRepository,
                [FromServices] IRiotApiClient riotApiClient,
                CancellationToken ct
                ) =>
            {
                try
                {
                    // Create the backfill job
                    var backfillJob = new QueueIdBackfillJob(matchRepository, riotApiClient);

                    // Create runner with conservative settings to respect rate limits
                    // Batch size of 10 with 500ms delay between batches
                    var runner = new BackfillJobRunner(
                        batchSize: 10,
                        delayBetweenBatches: TimeSpan.FromMilliseconds(500)
                    );

                    // Run the backfill
                    var result = await runner.RunAsync(backfillJob, ct);

                    return Results.Ok(new
                    {
                        jobName = result.JobName,
                        status = result.Status.ToString(),
                        totalItems = result.TotalItems,
                        itemsProcessed = result.ItemsProcessed,
                        durationSeconds = result.DurationSeconds,
                        error = result.Error
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error running QueueId backfill: {ex.Message}");
                    return Results.Problem(
                        detail: ex.Message,
                        statusCode: 500,
                        title: "Backfill job failed"
                    );
                }
            })
            .WithName("BackfillQueueId")
            .WithTags("Admin", "Backfill")
            .WithDescription("Backfills QueueId for matches that are missing it");
        }
    }
}
