using System.Net;
using System.Text.Json;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Infrastructure.Riot.Mappers;
using Mongoose.Api.Infrastructure.Services;
using Mongoose.Api.Infrastructure.WebSocket;

namespace Mongoose.Api.Infrastructure.Jobs;

/// <summary>
/// Re-fetches the match-v5 timeline of older matches to add the death detail and objective events
/// that sync stores for new matches (features/solo-trends.spec.md FR 38-39). The last 50 Summoner's
/// Rift matches of active accounts, newest first, one account at a time, at the lowest priority:
/// it steps aside while a match sync is queued or running or rank readings are being taken, spends
/// at most 20 of the 50 timeline requests Riot allows per 2 minutes, and goes through the same
/// rate limiter as every Riot call. A match counts as done once written, so it resumes anywhere.
/// </summary>
public class DeathDetailBackfillJob : BackgroundService
{
    public const int MatchesPerAccount = 50;
    public const string SkipNotFound = "not_found";
    public const string SkipUnreadable = "unreadable";

    // FR 38: 20 timeline requests per 2 minutes, so a sync started meanwhile always finds tokens
    public const int BudgetRequests = 20;
    public static readonly TimeSpan BudgetWindow = TimeSpan.FromMinutes(2);

    // FR 39: progress goes out every 5 matches and on every status change
    public const int BroadcastEvery = 5;

    private static readonly TimeSpan YieldDelay = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan IdleDelay = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RateLimitBackoff = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan TimeoutBackoff = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan ThrottlePoll = TimeSpan.FromSeconds(1);
    private const int CandidateAccounts = 20;

    private readonly IServiceProvider _serviceProvider;
    private readonly DeathDetailBackfillState _state;
    private readonly RiotBackgroundActivity _activity;
    private readonly IRiotThrottleState _throttle;
    private readonly ILogger<DeathDetailBackfillJob> _logger;
    private readonly TimeSpan _activeWindow;
    private readonly Queue<DateTime> _recentRequests = new();

    public DeathDetailBackfillJob(
        IServiceProvider serviceProvider,
        DeathDetailBackfillState state,
        RiotBackgroundActivity activity,
        IRiotThrottleState throttle,
        ILogger<DeathDetailBackfillJob> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _state = state;
        _activity = activity;
        _throttle = throttle;
        _logger = logger;
        // Same "active" window as RankSnapshotJob
        _activeWindow = TimeSpan.FromDays(Math.Max(1, configuration.GetValue("Jobs:RankSnapshotActiveDays", 7)));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DeathDetailBackfillJob starting ({Budget} timeline requests per {Window})", BudgetRequests, BudgetWindow);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = YieldDelay;
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
                _logger.LogError(ex, "DeathDetailBackfillJob step failed");
            }

            if (delay <= TimeSpan.Zero) continue;
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
    /// One step: backfill one match of the account at the front, or finish that account, or wait.
    /// Returns how long to wait before the next step (zero to go on at once).
    /// </summary>
    public async Task<TimeSpan> RunOnceAsync(DateTime nowUtc, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var services = scope.ServiceProvider;
        var backfillRepo = services.GetRequiredService<IDeathDetailBackfillRepository>();

        var puuid = await NextAccountAsync(backfillRepo, nowUtc);
        if (puuid is null) return IdleDelay;

        var matches = await backfillRepo.GetRecentMatchesAsync(puuid, MatchesPerAccount);
        var done = matches.Count(m => m.Done);
        var next = matches.FirstOrDefault(m => !m.Done);

        if (next is null)
        {
            await backfillRepo.MarkAccountDoneAsync(puuid, nowUtc);
            _state.Clear(puuid);
            await ReportAsync(services, puuid, new DeathDetailBackfillProgress(DeathDetailBackfillProgress.Complete, done, matches.Count), force: true);
            _logger.LogInformation("Death detail backfill done for {Puuid} ({Matches} matches)", LogSanitizer.HashForLog(puuid), matches.Count);
            return TimeSpan.Zero;
        }

        // Lowest priority: a match sync or rank readings go first
        if (_activity.RankSnapshotsRunning || await backfillRepo.IsSyncActiveAsync())
        {
            await ReportAsync(services, puuid, new DeathDetailBackfillProgress(DeathDetailBackfillProgress.Queued, done, matches.Count));
            return YieldDelay;
        }

        // Our own budget, well inside Riot's 50 per 2 minutes
        while (_recentRequests.Count > 0 && nowUtc - _recentRequests.Peek() >= BudgetWindow) _recentRequests.Dequeue();
        if (_recentRequests.Count >= BudgetRequests)
        {
            return _recentRequests.Peek() + BudgetWindow - nowUtc;
        }

        await ReportAsync(services, puuid, new DeathDetailBackfillProgress(DeathDetailBackfillProgress.Running, done, matches.Count));
        _recentRequests.Enqueue(nowUtc);

        var riot = services.GetRequiredService<IRiotApiClient>();
        JsonDocument timeline;
        try
        {
            timeline = await FetchWatchingThrottleAsync(services, riot, puuid, next.MatchId, done, matches.Count, ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Riot no longer serves it: record it so it isn't retried, and move on
            await backfillRepo.MarkSkippedAsync(next.MatchId, SkipNotFound, nowUtc);
            await ReportProgressAsync(services, puuid, done + 1, matches.Count);
            return TimeSpan.Zero;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            _logger.LogWarning("Riot rate limit reached during the death detail backfill; pausing for {Backoff}", RateLimitBackoff);
            await ReportAsync(services, puuid, new DeathDetailBackfillProgress(DeathDetailBackfillProgress.Waiting, done, matches.Count, nowUtc + RateLimitBackoff));
            return RateLimitBackoff;
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            // An HTTP timeout, not a shutdown
            _logger.LogWarning("Timeline request timed out during the death detail backfill; pausing for {Backoff}", TimeoutBackoff);
            await ReportAsync(services, puuid, new DeathDetailBackfillProgress(DeathDetailBackfillProgress.Waiting, done, matches.Count, nowUtc + TimeoutBackoff));
            return TimeoutBackoff;
        }

        using (timeline)
        {
            if (!await WriteAsync(services, next.MatchId, timeline.RootElement))
            {
                await backfillRepo.MarkSkippedAsync(next.MatchId, SkipUnreadable, nowUtc);
            }
        }

        await ReportProgressAsync(services, puuid, done + 1, matches.Count);
        return TimeSpan.Zero;
    }

    /// <summary>The account a player asked for most recently, else the most recently active one.</summary>
    private async Task<string?> NextAccountAsync(IDeathDetailBackfillRepository repo, DateTime nowUtc)
    {
        var candidates = await repo.GetAccountsToBackfillAsync(nowUtc - _activeWindow, CandidateAccounts);
        var waiting = candidates.ToHashSet(StringComparer.Ordinal);
        return _state.Prioritized().FirstOrDefault(waiting.Contains) ?? candidates.FirstOrDefault();
    }

    /// <summary>
    /// Fetches the timeline, reporting "waiting" (FR 39) while the rate limiter has held the request
    /// for more than 2 seconds.
    /// </summary>
    private async Task<JsonDocument> FetchWatchingThrottleAsync(
        IServiceProvider services, IRiotApiClient riot, string puuid, string matchId, int done, int total, CancellationToken ct)
    {
        var fetch = riot.GetMatchTimelineAsync(matchId, ct);
        var waited = false;
        while (!fetch.IsCompleted)
        {
            await Task.WhenAny(fetch, Task.Delay(ThrottlePoll, ct));
            if (!fetch.IsCompleted && _throttle.WaitingUntilUtc is { } until)
            {
                waited = true;
                await ReportAsync(services, puuid, new DeathDetailBackfillProgress(DeathDetailBackfillProgress.Waiting, done, total, until));
            }
        }

        var timeline = await fetch;
        if (waited)
        {
            await ReportAsync(services, puuid, new DeathDetailBackfillProgress(DeathDetailBackfillProgress.Running, done, total));
        }
        return timeline;
    }

    /// <summary>Writes the match's death detail; false when the timeline can't be read.</summary>
    private async Task<bool> WriteAsync(IServiceProvider services, string matchId, JsonElement timeline)
    {
        var participantsRepo = services.GetRequiredService<IParticipantsRepository>();
        var writer = services.GetRequiredService<IDeathDetailWriter>();

        Dictionary<string, int> timelineIds;
        try
        {
            timelineIds = RiotTimelineMapper.ExtractParticipantIds(timeline);
        }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException)
        {
            _logger.LogWarning("Timeline for {MatchId} has no readable participants; skipping", LogSanitizer.Sanitize(matchId));
            return false;
        }

        var rows = await participantsRepo.GetByMatchAsync(matchId);
        var participants = rows
            .Where(p => timelineIds.ContainsKey(p.Puuid))
            .ToDictionary(p => timelineIds[p.Puuid], p => new TimelineParticipant(p.Id, p.ChampionId));
        if (participants.Count == 0)
        {
            _logger.LogWarning("Timeline for {MatchId} matches none of its stored participants; skipping", LogSanitizer.Sanitize(matchId));
            return false;
        }

        await writer.WriteAsync(matchId, timeline, participants);
        // Last: the ID marks the match done, so a failure above retries it
        await participantsRepo.SetRiotParticipantIdsAsync(matchId,
            rows.Where(p => timelineIds.ContainsKey(p.Puuid)).ToDictionary(p => p.Puuid, p => timelineIds[p.Puuid]));
        return true;
    }

    private Task ReportProgressAsync(IServiceProvider services, string puuid, int done, int total)
    {
        var progress = new DeathDetailBackfillProgress(DeathDetailBackfillProgress.Running, done, total);
        return ReportAsync(services, puuid, progress, force: done % BroadcastEvery == 0 || done == total);
    }

    /// <summary>Stores the progress and sends it on a status change (or when forced), to every linked user.</summary>
    private async Task ReportAsync(IServiceProvider services, string puuid, DeathDetailBackfillProgress progress, bool force = false)
    {
        var previous = _state.Get(puuid);
        if (progress.Status != DeathDetailBackfillProgress.Complete) _state.Set(puuid, progress);

        var changed = previous is null || previous.Status != progress.Status || previous.RetryAt != progress.RetryAt;
        if (!changed && !force) return;

        var broadcaster = services.GetService<IUserSyncBroadcaster>();
        if (broadcaster is null) return;

        var message = new DetailBackfillProgressMessage
        {
            Status = progress.Status,
            Done = progress.Done,
            Total = progress.Total,
            RetryAt = progress.RetryAt
        };

        var userIds = await services.GetRequiredService<IUserRiotAccountsRepository>().GetUserIdsByPuuidAsync(puuid);
        foreach (var userId in userIds)
        {
            await broadcaster.BroadcastToUserAsync(userId, message);
        }
    }
}
