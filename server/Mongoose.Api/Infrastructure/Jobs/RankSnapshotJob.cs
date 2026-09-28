using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Application.Services;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;

namespace Mongoose.Api.Infrastructure.Jobs;

/// <summary>
/// Reads the rank of every linked account of an active user every few minutes (League-v4), so that
/// almost every ranked match falls alone between two readings and gets its LP. A reading after
/// exactly one new match queues a sync; the reading is written onto that match once it is stored.
/// See .github/specs/features/rank-snapshots.spec.md.
/// </summary>
public class RankSnapshotJob : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan RateLimitBackoff = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan RetentionInterval = TimeSpan.FromDays(1);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RankSnapshotJob> _logger;
    private readonly TimeSpan _interval;
    private readonly TimeSpan _activeWindow;
    private readonly int _maxCallsPerTick;
    private readonly TimeSpan _retention;
    private DateTime _lastRetentionRunUtc = DateTime.MinValue;

    public RankSnapshotJob(IServiceProvider serviceProvider, ILogger<RankSnapshotJob> logger, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _interval = TimeSpan.FromMinutes(Math.Max(1, configuration.GetValue("Jobs:RankSnapshotIntervalMinutes", 20)));
        _activeWindow = TimeSpan.FromDays(Math.Max(1, configuration.GetValue("Jobs:RankSnapshotActiveDays", 7)));
        // The job ticks once a minute, so the per-minute cap is the per-tick cap
        _maxCallsPerTick = Math.Max(1, configuration.GetValue("Jobs:RankSnapshotMaxCallsPerMinute", 10));
        _retention = TimeSpan.FromDays(Math.Max(1, configuration.GetValue("Jobs:RankSnapshotRetentionDays", 30)));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RankSnapshotJob starting (interval {Interval}, active {ActiveDays} days, {MaxCalls} calls per minute)",
            _interval, _activeWindow.TotalDays, _maxCallsPerTick);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = TickInterval;
            try
            {
                delay = await RunOnceAsync(DateTime.UtcNow, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RankSnapshotJob tick failed");
            }

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    /// <summary>
    /// One tick: attribute pending snapshots, read the rank of due accounts, and prune old snapshots
    /// once a day. Returns how long to wait before the next tick.
    /// </summary>
    public async Task<TimeSpan> RunOnceAsync(DateTime nowUtc, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var services = scope.ServiceProvider;
        var rankSnapshots = services.GetRequiredService<RankSnapshotService>();
        var snapshotsRepo = services.GetRequiredService<IRankSnapshotsRepository>();
        var riotAccountsRepo = services.GetRequiredService<IRiotAccountsRepository>();
        var riotApiClient = services.GetRequiredService<IRiotApiClient>();

        // Matches synced since the last tick may complete waiting readings
        await rankSnapshots.AttributePendingAsync(null, nowUtc, queueSyncWhenMissing: true);

        var due = await snapshotsRepo.GetDueAccountPuuidsAsync(nowUtc - _activeWindow, nowUtc - _interval, _maxCallsPerTick);
        foreach (var puuid in due)
        {
            ct.ThrowIfCancellationRequested();
            var account = await riotAccountsRepo.GetByPuuidAsync(puuid);
            if (account is null) continue;

            try
            {
                var matchPlayed = await rankSnapshots.CaptureAsync(account, RankSnapshotSource.Poll, riotApiClient, ct);
                if (matchPlayed && await rankSnapshots.QueueSyncAsync(puuid))
                {
                    _logger.LogInformation("Queued sync for {Puuid}: a ranked match ended since the last rank reading",
                        LogSanitizer.HashForLog(puuid));
                }
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("Riot rate limit reached while reading ranks; pausing for {Backoff}", RateLimitBackoff);
                return RateLimitBackoff;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Mark it read anyway so one failing account doesn't block the rest every tick
                _logger.LogWarning(ex, "Failed to read rank for {Puuid}", LogSanitizer.HashForLog(puuid));
                await snapshotsRepo.MarkRankCheckedAsync(puuid, nowUtc);
            }
        }

        if (nowUtc - _lastRetentionRunUtc >= RetentionInterval)
        {
            var deleted = await snapshotsRepo.DeleteOlderThanAsync(nowUtc - _retention);
            _lastRetentionRunUtc = nowUtc;
            if (deleted > 0)
            {
                _logger.LogInformation("Deleted {Count} rank snapshots older than {RetentionDays} days", deleted, _retention.TotalDays);
            }
        }

        return TickInterval;
    }
}
