using Microsoft.Extensions.Logging;
using RiotProxy.Infrastructure.Database.Repositories;

namespace RiotProxy.Infrastructure.Jobs;

/// <summary>
/// Background job that deletes matches older than the configured retention period.
/// Runs on a daily schedule to manage database growth and maintain optimal performance.
/// Uses batch processing to avoid long-running transactions and database locks.
/// </summary>
public class MatchCleanupJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MatchCleanupJob> _logger;
    private readonly IConfiguration _configuration;
    
    // Configuration keys
    private const string RetentionDaysKey = "Jobs:MatchRetentionDays";
    private const string BatchSizeKey = "Jobs:MatchCleanupBatchSize";
    private const string ScheduleKey = "Jobs:MatchCleanupSchedule";
    
    // Default values
    private const int DefaultRetentionDays = 180; // 6 months
    private const int DefaultBatchSize = 1000;
    private const string DefaultSchedule = "02:00"; // 2:00 AM

    public MatchCleanupJob(
        IServiceProvider serviceProvider, 
        ILogger<MatchCleanupJob> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MatchCleanupJob starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Calculate next execution time
                var nextRun = GetNextExecutionTime();
                var delay = nextRun - DateTime.UtcNow;

                if (delay > TimeSpan.Zero)
                {
                    _logger.LogInformation("Next match cleanup scheduled for {NextRun} UTC ({Delay} from now)", 
                        nextRun, delay);
                    await Task.Delay(delay, stoppingToken);
                }

                // Execute cleanup
                await ExecuteCleanupAsync(stoppingToken);

                // Wait until next day
                await Task.Delay(TimeSpan.FromHours(23), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MatchCleanupJob loop");
                // Wait before retrying to avoid tight error loops
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        _logger.LogInformation("MatchCleanupJob stopped.");
    }

    /// <summary>
    /// Executes the match cleanup process.
    /// Deletes matches older than the retention period in batches.
    /// </summary>
    private async Task ExecuteCleanupAsync(CancellationToken ct)
    {
        var retentionDays = _configuration.GetValue<int>(RetentionDaysKey, DefaultRetentionDays);
        var batchSize = _configuration.GetValue<int>(BatchSizeKey, DefaultBatchSize);

        // Validate configuration values to prevent dangerous operations
        const int MinRetentionDays = 30;    // Minimum 30 days to prevent accidental mass deletion
        const int MaxRetentionDays = 3650;  // Maximum 10 years
        const int MinBatchSize = 1;
        const int MaxBatchSize = 10000;     // Prevent overly large SQL queries

        if (retentionDays < MinRetentionDays || retentionDays > MaxRetentionDays)
        {
            _logger.LogError(
                "Invalid MatchRetentionDays configuration: {RetentionDays}. Must be between {Min} and {Max}. Cleanup skipped.",
                retentionDays, MinRetentionDays, MaxRetentionDays);
            return;
        }

        if (batchSize < MinBatchSize || batchSize > MaxBatchSize)
        {
            _logger.LogError(
                "Invalid MatchCleanupBatchSize configuration: {BatchSize}. Must be between {Min} and {Max}. Cleanup skipped.",
                batchSize, MinBatchSize, MaxBatchSize);
            return;
        }

        // Calculate cutoff timestamp (Unix milliseconds)
        var cutoffDate = DateTimeOffset.UtcNow.AddDays(-retentionDays);
        var cutoffTimestamp = cutoffDate.ToUnixTimeMilliseconds();

        _logger.LogInformation(
            "Starting match cleanup (retention: {RetentionDays} days, cutoff: {CutoffDate}, batch size: {BatchSize})",
            retentionDays, cutoffDate.ToString("yyyy-MM-dd"), batchSize);

        var totalDeleted = 0;
        var batchNumber = 0;
        var startTime = DateTime.UtcNow;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var matchesRepo = scope.ServiceProvider.GetRequiredService<MatchesRepository>();

            // Process in batches until no more old matches exist
            while (!ct.IsCancellationRequested)
            {
                batchNumber++;
                var deletedCount = await matchesRepo.DeleteOldMatchesAsync(cutoffTimestamp, batchSize);

                if (deletedCount == 0)
                {
                    // No more old matches to delete
                    break;
                }

                totalDeleted += deletedCount;
                _logger.LogInformation("Deleted {Count} matches in batch {BatchNumber} (total: {Total})",
                    deletedCount, batchNumber, totalDeleted);

                // Small delay between batches to avoid overwhelming the database
                if (deletedCount == batchSize)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), ct);
                }
            }

            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            
            if (totalDeleted > 0)
            {
                _logger.LogInformation(
                    "Match cleanup completed: {TotalDeleted} matches deleted in {Batches} batches ({Duration}ms)",
                    totalDeleted, batchNumber, duration);
            }
            else
            {
                _logger.LogInformation("Match cleanup completed: No old matches to delete");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during match cleanup (deleted {TotalDeleted} matches before error)", totalDeleted);
            throw;
        }
    }

    /// <summary>
    /// Calculates the next execution time based on the configured schedule.
    /// </summary>
    private DateTime GetNextExecutionTime()
    {
        var scheduleTime = _configuration.GetValue<string>(ScheduleKey, DefaultSchedule);
        
        // Parse schedule time (format: "HH:mm")
        if (!TimeSpan.TryParse(scheduleTime, out var scheduledTime))
        {
            _logger.LogWarning("Invalid schedule time '{ScheduleTime}', using default {Default}", 
                scheduleTime, DefaultSchedule);
            scheduledTime = TimeSpan.Parse(DefaultSchedule);
        }

        var now = DateTime.UtcNow;
        var nextRun = now.Date.Add(scheduledTime);

        // If the scheduled time has already passed today, schedule for tomorrow
        if (nextRun <= now)
        {
            nextRun = nextRun.AddDays(1);
        }

        return nextRun;
    }
}

