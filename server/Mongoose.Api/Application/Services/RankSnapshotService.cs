using System.Text.Json;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Core.Services;

namespace Mongoose.Api.Application.Services;

/// <summary>
/// Stores rank readings (League-v4) as snapshots and writes a reading onto the one ranked match it
/// belongs to. The only code path that sets participants.lp_after. Rules live in
/// <see cref="RankSnapshotRules"/>; see .github/specs/features/rank-snapshots.spec.md.
/// </summary>
public class RankSnapshotService
{
    private const int PendingBatchSize = 200;

    // A sync this long after a pending reading had its chance to store the match; don't queue another
    private static readonly TimeSpan SyncGraceAfterReading = TimeSpan.FromMinutes(3);

    private readonly IRankSnapshotsRepository _snapshots;
    private readonly IParticipantsRepository _participants;
    private readonly IRiotAccountsRepository _riotAccounts;
    private readonly ISyncQueueSignal _queueSignal;
    private readonly ILogger<RankSnapshotService> _logger;
    private readonly TimeSpan _clockSkew;
    private readonly TimeSpan _pendingTimeout;

    public RankSnapshotService(
        IRankSnapshotsRepository snapshots,
        IParticipantsRepository participants,
        IRiotAccountsRepository riotAccounts,
        ISyncQueueSignal queueSignal,
        IConfiguration configuration,
        ILogger<RankSnapshotService> logger)
    {
        _snapshots = snapshots;
        _participants = participants;
        _riotAccounts = riotAccounts;
        _queueSignal = queueSignal;
        _logger = logger;
        _clockSkew = TimeSpan.FromSeconds(configuration.GetValue("Jobs:RankSnapshotClockSkewSeconds", 120));
        _pendingTimeout = TimeSpan.FromMinutes(configuration.GetValue("Jobs:RankSnapshotPendingTimeoutMinutes", 60));
    }

    /// <summary>The ranked queues in a League-v4 entries answer; other queues and malformed entries are left out.</summary>
    public static IReadOnlyList<LeagueRankReading> ParseLeagueEntries(JsonDocument leagueEntries)
    {
        var readings = new List<LeagueRankReading>();
        if (leagueEntries.RootElement.ValueKind != JsonValueKind.Array) return readings;

        foreach (var entry in leagueEntries.RootElement.EnumerateArray())
        {
            var queueId = RankSnapshotRules.QueueIdFor(
                entry.TryGetProperty("queueType", out var queueType) ? queueType.GetString() : null);
            if (queueId is null) continue;

            if (!entry.TryGetProperty("tier", out var tier) || tier.GetString() is not { Length: > 0 } tierName) continue;
            if (!entry.TryGetProperty("leaguePoints", out var lp) || !lp.TryGetInt32(out var lpValue)) continue;
            if (!entry.TryGetProperty("wins", out var wins) || !wins.TryGetInt32(out var winsValue)) continue;
            if (!entry.TryGetProperty("losses", out var losses) || !losses.TryGetInt32(out var lossesValue)) continue;

            var division = entry.TryGetProperty("rank", out var rank) ? rank.GetString() : null;
            readings.Add(new LeagueRankReading(queueId.Value, tierName, division, lpValue, winsValue, lossesValue));
        }

        return readings;
    }

    /// <summary>
    /// Stores a reading per ranked queue when it differs from the previous one, marks the account's
    /// rank as read, and returns true when a reading is pending (exactly one new match).
    /// </summary>
    public async Task<bool> RecordAsync(string puuid, IReadOnlyList<LeagueRankReading> readings, string source, DateTime capturedAtUtc)
    {
        var anyPending = false;
        foreach (var reading in readings)
        {
            var previous = await _snapshots.GetLatestAsync(puuid, reading.QueueId);
            var outcome = RankSnapshotRules.Classify(previous, reading);
            if (outcome == RankReadingOutcome.Unchanged) continue;

            var snapshot = new RankSnapshotRecord
            {
                Puuid = puuid,
                QueueId = reading.QueueId,
                Tier = reading.Tier,
                Division = reading.Division,
                Lp = reading.Lp,
                Wins = reading.Wins,
                Losses = reading.Losses,
                CapturedAt = capturedAtUtc,
                WindowStartAt = outcome == RankReadingOutcome.Pending ? previous!.CapturedAt : null,
                Source = source,
                Status = outcome switch
                {
                    RankReadingOutcome.Pending => RankSnapshotStatus.Pending,
                    RankReadingOutcome.NoMatch => RankSnapshotStatus.NoMatch,
                    _ => RankSnapshotStatus.Baseline
                }
            };
            await _snapshots.InsertAsync(snapshot);
            anyPending |= outcome == RankReadingOutcome.Pending;
        }

        await _snapshots.MarkRankCheckedAsync(puuid, capturedAtUtc);
        return anyPending;
    }

    /// <summary>
    /// Reads the account's rank from Riot, keeps riot_accounts' rank current, stores the snapshots,
    /// and returns true when a match is waiting to be stored.
    /// </summary>
    public async Task<bool> CaptureAsync(RiotAccount account, string source, IRiotApiClient riotApiClient, CancellationToken ct)
    {
        using var leagueDoc = await riotApiClient.GetLeagueEntriesByPuuidAsync(account.Region, account.Puuid, ct);
        var readings = ParseLeagueEntries(leagueDoc);

        var solo = readings.FirstOrDefault(r => r.QueueId == 420);
        var flex = readings.FirstOrDefault(r => r.QueueId == 440);
        await _riotAccounts.UpdateRankDataAsync(
            account.Puuid, account.SummonerId,
            solo?.Tier, solo?.Division, solo?.Lp,
            flex?.Tier, flex?.Division, flex?.Lp);

        return await RecordAsync(account.Puuid, readings, source, DateTime.UtcNow);
    }

    /// <summary>
    /// Queues a match sync for the account unless one is already queued or running, and wakes the
    /// sync job. Returns true when a sync was queued.
    /// </summary>
    public async Task<bool> QueueSyncAsync(string puuid)
    {
        var account = await _riotAccounts.GetByPuuidAsync(puuid);
        if (account is null || account.SyncStatus is "pending" or "syncing") return false;

        await _riotAccounts.UpdateSyncStatusAsync(puuid, "pending");
        _queueSignal.Notify();
        return true;
    }

    /// <summary>
    /// Tries to attribute pending snapshots (all accounts, or one) to their match. With none stored
    /// yet a snapshot stays pending, and when no sync has run since the reading one is queued; after
    /// the timeout, or with two candidate matches, it is skipped. Returns how many were attributed.
    /// </summary>
    public async Task<int> AttributePendingAsync(string? puuid, DateTime nowUtc, bool queueSyncWhenMissing)
    {
        var attributed = 0;
        var pending = await _snapshots.GetPendingAsync(puuid, PendingBatchSize);

        foreach (var snapshot in pending)
        {
            if (snapshot.WindowStartAt is null)
            {
                await _snapshots.UpdateStatusAsync(snapshot.Id, RankSnapshotStatus.Skipped, null);
                continue;
            }

            var (fromMs, toMs) = RankSnapshotRules.AttributionWindow(snapshot.WindowStartAt.Value, snapshot.CapturedAt, _clockSkew);
            var candidates = await _snapshots.GetMatchIdsEndedBetweenAsync(snapshot.Puuid, snapshot.QueueId, fromMs, toMs);
            var (outcome, matchId) = RankSnapshotRules.ChooseMatch(candidates.ToList());

            switch (outcome)
            {
                case MatchAttributionOutcome.Single:
                    await _participants.UpdateLpDataAsync(matchId!, snapshot.Puuid, snapshot.Lp, snapshot.Tier, snapshot.Division);
                    await _snapshots.UpdateStatusAsync(snapshot.Id, RankSnapshotStatus.Attributed, matchId);
                    attributed++;
                    _logger.LogDebug("Attributed rank snapshot {SnapshotId} to match {MatchId} for {Puuid}",
                        snapshot.Id, LogSanitizer.Sanitize(matchId), LogSanitizer.HashForLog(snapshot.Puuid));
                    break;

                case MatchAttributionOutcome.Ambiguous:
                    await _snapshots.UpdateStatusAsync(snapshot.Id, RankSnapshotStatus.Skipped, null);
                    _logger.LogDebug("Skipped rank snapshot {SnapshotId}: {Count} matches ended in its window",
                        snapshot.Id, candidates.Count);
                    break;

                default:
                    if (nowUtc - snapshot.CapturedAt > _pendingTimeout)
                    {
                        await _snapshots.UpdateStatusAsync(snapshot.Id, RankSnapshotStatus.Skipped, null);
                    }
                    else if (queueSyncWhenMissing && await NeedsSyncAsync(snapshot))
                    {
                        await QueueSyncAsync(snapshot.Puuid);
                    }
                    break;
            }
        }

        return attributed;
    }

    // Match-v5 can list a match minutes after League-v4 counts it: allow syncs until one has run
    // comfortably after the reading, then leave the snapshot to its timeout.
    private async Task<bool> NeedsSyncAsync(RankSnapshotRecord snapshot)
    {
        var account = await _riotAccounts.GetByPuuidAsync(snapshot.Puuid);
        // A failed sync is not retried every tick; the next poll or the user's own sync tries again
        if (account is null || account.SyncStatus is "pending" or "syncing" or "failed") return false;
        return account.LastSyncAt is null || account.LastSyncAt.Value < snapshot.CapturedAt + SyncGraceAfterReading;
    }
}
