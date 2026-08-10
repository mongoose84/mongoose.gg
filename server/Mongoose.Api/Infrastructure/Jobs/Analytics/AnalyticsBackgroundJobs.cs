namespace Mongoose.Api.Infrastructure.Jobs.Analytics;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Infrastructure.Services.Analytics;

/// <summary>
/// Base class for analytics background jobs
/// </summary>
public abstract class AnalyticsBackgroundJob : BackgroundService
{
    protected readonly ILogger<AnalyticsBackgroundJob> Logger;
    protected readonly string JobName;

    protected AnalyticsBackgroundJob(ILogger<AnalyticsBackgroundJob> logger, string jobName)
    {
        Logger = logger;
        JobName = jobName;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Logger.LogInformation($"{JobName} background job started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var nextRunTime = await GetNextRunTimeAsync();
                var delayMs = (int)(nextRunTime - DateTime.UtcNow).TotalMilliseconds;

                if (delayMs > 0)
                {
                    await Task.Delay(delayMs, stoppingToken);
                }

                Logger.LogInformation($"{JobName} executing");
                await ExecuteJobAsync(stoppingToken);
                Logger.LogInformation($"{JobName} completed successfully");
            }
            catch (OperationCanceledException)
            {
                Logger.LogInformation($"{JobName} was cancelled");
                break;
            }
            catch (Exception ex)
            {
                Logger.LogError($"{JobName} failed: {ex.Message}");
                // Don't throw - continue running and retry next interval
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        Logger.LogInformation($"{JobName} background job stopped");
    }

    /// <summary>
    /// Get the next scheduled run time
    /// </summary>
    protected abstract Task<DateTime> GetNextRunTimeAsync();

    /// <summary>
    /// Execute the job logic
    /// </summary>
    protected abstract Task ExecuteJobAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Job to extract dimensions from raw events (runs every 5 minutes)
/// </summary>
public class DimensionExtractionBackgroundJob : AnalyticsBackgroundJob
{
    private DateTime _lastRunTime = DateTime.UtcNow;
    private readonly IServiceScopeFactory _scopeFactory;
    private const int RunIntervalMinutes = 5;

    public DimensionExtractionBackgroundJob(
        IServiceScopeFactory scopeFactory,
        ILogger<DimensionExtractionBackgroundJob> logger)
        : base(logger, "DimensionExtraction")
    {
        _scopeFactory = scopeFactory;
    }

    protected override Task<DateTime> GetNextRunTimeAsync()
    {
        var nextRun = _lastRunTime.AddMinutes(RunIntervalMinutes);
        return Task.FromResult(nextRun > DateTime.UtcNow ? nextRun : DateTime.UtcNow);
    }

    protected override async Task ExecuteJobAsync(CancellationToken cancellationToken)
    {
        _lastRunTime = DateTime.UtcNow;

        using var scope = _scopeFactory.CreateScope();
        var dimensionService = scope.ServiceProvider.GetRequiredService<DimensionExtractionService>();

        // Implementation would:
        // 1. Query for unprocessed events from analytics_events_v2
        // 2. Call dimensionService.ExtractDimensionsAsync()
        // 3. Mark events as processed
        // 4. Log progress

        await Task.CompletedTask; // Placeholder
        Logger.LogInformation("Dimension extraction batch completed");
    }
}

/// <summary>
/// Job to compute hourly rollups from dimension data (runs every hour at :05)
/// </summary>
public class RollupAggregationBackgroundJob : AnalyticsBackgroundJob
{
    private DateTime _lastRunTime = DateTime.UtcNow;
    private readonly IServiceScopeFactory _scopeFactory;

    public RollupAggregationBackgroundJob(
        IServiceScopeFactory scopeFactory,
        ILogger<RollupAggregationBackgroundJob> logger)
        : base(logger, "RollupAggregation")
    {
        _scopeFactory = scopeFactory;
    }

    protected override Task<DateTime> GetNextRunTimeAsync()
    {
        // Calculate next hour:05
        var now = DateTime.UtcNow;
        var nextHour = now.AddHours(1);
        var nextRun = new DateTime(nextHour.Year, nextHour.Month, nextHour.Day, nextHour.Hour, 5, 0, DateTimeKind.Utc);

        return Task.FromResult(nextRun);
    }

    protected override async Task ExecuteJobAsync(CancellationToken cancellationToken)
    {
        _lastRunTime = DateTime.UtcNow;

        using var scope = _scopeFactory.CreateScope();
        var aggregationService = scope.ServiceProvider.GetRequiredService<AggregationService>();

        // Aggregate last hour's events
        await aggregationService.AggregateLastHourAsync();

        Logger.LogInformation("Hourly rollup aggregation completed");
    }
}

/// <summary>
/// Job to purge old events based on retention policies (runs daily at 02:00 UTC)
/// </summary>
public class RetentionAndPurgeBackgroundJob : AnalyticsBackgroundJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private const int RunHourUtc = 2;
    private const int RunMinuteUtc = 0;

    public RetentionAndPurgeBackgroundJob(
        IServiceScopeFactory scopeFactory,
        ILogger<RetentionAndPurgeBackgroundJob> logger)
        : base(logger, "RetentionAndPurge")
    {
        _scopeFactory = scopeFactory;
    }

    protected override Task<DateTime> GetNextRunTimeAsync()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var nextRun = new DateTime(today.Year, today.Month, today.Day, RunHourUtc, RunMinuteUtc, 0, DateTimeKind.Utc);

        // If already past today's run time, schedule for tomorrow
        if (nextRun <= now)
        {
            nextRun = nextRun.AddDays(1);
        }

        return Task.FromResult(nextRun);
    }

    protected override async Task ExecuteJobAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Starting retention and purge job");

        try
        {
            // Purge old events
            using var scope = _scopeFactory.CreateScope();
            var eventsRepository = scope.ServiceProvider.GetRequiredService<IAnalyticsEventsV2Repository>();
            var purgedCount = await eventsRepository.DeleteOlderThanAsync(
                DateTime.UtcNow.AddDays(-365)); // Keep max 365 days

            Logger.LogInformation($"Purged {purgedCount} old events from analytics_events_v2");

            // Purge from dimensions table
            var dimensionsPurged = await PurgeDimensionsAsync(cancellationToken);
            Logger.LogInformation($"Purged {dimensionsPurged} records from analytics_event_dimensions");

            // Purge from journey table
            var journeysPurged = await PurgeJourneysAsync(cancellationToken);
            Logger.LogInformation($"Purged {journeysPurged} records from analytics_journey_steps");

            // Purge from funnel table
            var funnelsPurged = await PurefunnelsAsync(cancellationToken);
            Logger.LogInformation($"Purged {funnelsPurged} records from analytics_funnel_steps");

            Logger.LogInformation("Retention and purge job completed successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Retention and purge job failed: {ex.Message}");
            throw;
        }
    }

    private Task<int> PurgeDimensionsAsync(CancellationToken cancellationToken)
    {
        // Would call repository to purge dimensions older than max retention
        return Task.FromResult(0); // Placeholder
    }

    private Task<int> PurgeJourneysAsync(CancellationToken cancellationToken)
    {
        // Would call repository to purge journey steps older than max retention
        return Task.FromResult(0); // Placeholder
    }

    private Task<int> PurefunnelsAsync(CancellationToken cancellationToken)
    {
        // Would call repository to purge funnel steps older than max retention
        return Task.FromResult(0); // Placeholder
    }
}
