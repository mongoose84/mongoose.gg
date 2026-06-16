using System.Collections.Concurrent;
using Mongoose.Api.Core.Interfaces;

namespace Mongoose.Api.Infrastructure.WebSocket;

/// <summary>
/// In-memory, per-user aggregator for the "Analyze all" flow. Holds each run's per-account
/// slots and a reverse PUUID-&gt;users index so per-account events resolve affected runs with
/// no database calls. Combined progress is recomputed under a per-run lock and broadcast via
/// <see cref="SyncProgressHub.BroadcastToUserAsync"/>.
///
/// Singleton. State is intentionally ephemeral — on restart, runs are dropped and the UI falls
/// back to stored per-account status (and the job's stuck-job recovery resumes any 'syncing' rows).
/// </summary>
public sealed class SyncProgressAggregator : ISyncProgressAggregator
{
    private readonly IUserSyncBroadcaster _hub;
    private readonly ILogger<SyncProgressAggregator> _logger;

    private readonly ConcurrentDictionary<long, UserRun> _runs = new();
    // Reverse index: puuid -> set of userIds with an active run containing it (M:M accounts).
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<long, byte>> _puuidToUsers =
        new(StringComparer.OrdinalIgnoreCase);

    public SyncProgressAggregator(IUserSyncBroadcaster hub, ILogger<SyncProgressAggregator> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task StartRunAsync(long userId, IReadOnlyList<string> puuids)
    {
        if (puuids.Count == 0)
            return;

        // Replace any prior run for this user (a new click supersedes the old one).
        if (_runs.TryRemove(userId, out var previous))
        {
            RemoveReverseIndex(userId, previous);
        }

        var run = new UserRun(userId, puuids);
        _runs[userId] = run;
        foreach (var puuid in run.Slots.Keys)
        {
            _puuidToUsers.GetOrAdd(puuid, _ => new ConcurrentDictionary<long, byte>())[userId] = 0;
        }

        SyncAggregateMessage message;
        lock (run.Gate)
        {
            message = BuildProgress(run, matchId: null);
        }

        await _hub.BroadcastToUserAsync(userId, message);
    }

    public Task OnProgressAsync(string puuid, int progress, int total, string? matchId) =>
        ApplyToRunsAsync(puuid, slot =>
        {
            slot.Status = SlotStatus.Syncing;
            slot.Progress = progress;
            slot.Total = total;
        }, matchId);

    public Task OnCompleteAsync(string puuid, int totalSynced) =>
        ApplyToRunsAsync(puuid, slot =>
        {
            slot.Status = SlotStatus.Completed;
            slot.TotalSynced = totalSynced;
            slot.Progress = slot.Total; // fill the account's portion of the bar
        }, matchId: null);

    public Task OnErrorAsync(string puuid, string error) =>
        ApplyToRunsAsync(puuid, slot =>
        {
            slot.Status = SlotStatus.Failed;
            slot.Error = error;
        }, matchId: null);

    /// <summary>
    /// Applies a slot mutation to every active run containing the PUUID, then broadcasts the
    /// recomputed aggregate. Mutation + message build happen under the run lock; the await-ing
    /// broadcast and run removal happen outside it.
    /// </summary>
    private async Task ApplyToRunsAsync(string puuid, Action<AccountSlot> mutate, string? matchId)
    {
        if (!_puuidToUsers.TryGetValue(puuid, out var users))
            return;

        foreach (var userId in users.Keys)
        {
            if (!_runs.TryGetValue(userId, out var run))
                continue;

            SyncAggregateMessage? message = null;
            var settled = false;

            lock (run.Gate)
            {
                if (!run.Slots.TryGetValue(puuid, out var slot))
                    continue;

                mutate(slot);

                if (AllSettled(run))
                {
                    settled = true;
                    message = BuildTerminal(run);
                }
                else
                {
                    message = BuildProgress(run, matchId);
                }
            }

            if (settled)
            {
                // Only remove if this is still the current run (guards against a concurrent
                // StartRun having superseded it).
                if (_runs.TryGetValue(userId, out var current) && ReferenceEquals(current, run))
                {
                    _runs.TryRemove(userId, out _);
                    RemoveReverseIndex(userId, run);
                }
            }

            await _hub.BroadcastToUserAsync(userId, message);
        }
    }

    private static bool AllSettled(UserRun run) =>
        run.Slots.Values.All(s => s.Status is SlotStatus.Completed or SlotStatus.Failed);

    private static SyncAggregateProgressMessage BuildProgress(UserRun run, string? matchId)
    {
        var slots = run.Slots.Values;
        return new SyncAggregateProgressMessage
        {
            Status = "syncing",
            Progress = slots.Sum(s => s.Progress),
            Total = slots.Sum(s => s.Total),
            AccountsTotal = run.Slots.Count,
            AccountsDone = slots.Count(s => s.Status is SlotStatus.Completed or SlotStatus.Failed),
            MatchId = matchId
        };
    }

    private SyncAggregateMessage BuildTerminal(UserRun run)
    {
        var slots = run.Slots.Values;
        var firstError = slots.FirstOrDefault(s => s.Status == SlotStatus.Failed)?.Error;

        if (firstError != null)
        {
            _logger.LogInformation("Aggregate run for user {UserId} settled with at least one failure", run.UserId);
            return new SyncAggregateErrorMessage
            {
                Status = "failed",
                Error = firstError
            };
        }

        return new SyncAggregateCompleteMessage
        {
            Status = "completed",
            TotalSynced = slots.Sum(s => s.TotalSynced),
            AccountsTotal = run.Slots.Count
        };
    }

    private void RemoveReverseIndex(long userId, UserRun run)
    {
        foreach (var puuid in run.Slots.Keys)
        {
            if (_puuidToUsers.TryGetValue(puuid, out var users))
            {
                users.TryRemove(userId, out _);
                if (users.IsEmpty)
                {
                    _puuidToUsers.TryRemove(puuid, out _);
                }
            }
        }
    }

    private enum SlotStatus { Pending, Syncing, Completed, Failed }

    private sealed class AccountSlot
    {
        public SlotStatus Status = SlotStatus.Pending;
        public int Progress;
        public int Total;
        public int TotalSynced;
        public string? Error;
    }

    private sealed class UserRun
    {
        public long UserId { get; }
        public object Gate { get; } = new();
        public Dictionary<string, AccountSlot> Slots { get; }

        public UserRun(long userId, IReadOnlyList<string> puuids)
        {
            UserId = userId;
            Slots = new Dictionary<string, AccountSlot>(StringComparer.OrdinalIgnoreCase);
            foreach (var puuid in puuids)
            {
                Slots[puuid] = new AccountSlot();
            }
        }
    }
}
