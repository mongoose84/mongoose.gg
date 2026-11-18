using System.Text.Json;
using Microsoft.Extensions.Logging;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;

namespace Mongoose.Api.Tests;

/// <summary>Records every LP write; honours the "only when empty" rule of the real repository.</summary>
internal sealed class RecordingParticipantsRepository : IParticipantsRepository
{
    public Dictionary<(string MatchId, string Puuid), (int? Lp, string? Tier, string? Rank)> LpData { get; } = new();

    public Task UpdateLpDataAsync(string matchId, string puuid, int? lp, string? tier, string? rank)
    {
        LpData.TryAdd((matchId, puuid), (lp, tier, rank));
        return Task.CompletedTask;
    }

    public Task<long> InsertAsync(Participant participant) => Task.FromResult(0L);
    public Task<IList<Participant>> GetByMatchAsync(string matchId) => Task.FromResult<IList<Participant>>(new List<Participant>());
    public Task<ISet<string>> GetMatchIdsForPuuidAsync(string puuid) => Task.FromResult<ISet<string>>(new HashSet<string>());
    public Task<IList<Participant>> GetRecentByPuuidAsync(string puuid, int? queueId, int limit) => Task.FromResult<IList<Participant>>(new List<Participant>());
    public Task SetRiotParticipantIdsAsync(string matchId, IReadOnlyDictionary<string, int> participantIds) => Task.CompletedTask;
}

/// <summary>Riot accounts in memory with the sync-status and rank writes the snapshot code makes.</summary>
internal sealed class InMemoryRiotAccountsRepository : IRiotAccountsRepository
{
    public Dictionary<string, RiotAccount> Accounts { get; } = new();
    public List<(string Puuid, string? SoloTier, int? SoloLp, string? FlexTier, int? FlexLp)> RankUpdates { get; } = new();

    public RiotAccount Add(string puuid, string syncStatus = "completed", DateTime? lastSyncAt = null)
    {
        var account = new RiotAccount { Puuid = puuid, Region = "euw1", GameName = "Player", TagLine = "EUW", SyncStatus = syncStatus, LastSyncAt = lastSyncAt };
        Accounts[puuid] = account;
        return account;
    }

    public Task<RiotAccount?> GetByPuuidAsync(string puuid) => Task.FromResult(Accounts.GetValueOrDefault(puuid));

    public Task UpdateSyncStatusAsync(string puuid, string syncStatus, DateTime? lastSyncAt = null)
    {
        if (Accounts.TryGetValue(puuid, out var account))
        {
            account.SyncStatus = syncStatus;
            account.LastSyncAt = lastSyncAt ?? account.LastSyncAt;
        }
        return Task.CompletedTask;
    }

    public Task UpdateRankDataAsync(string puuid, string? summonerId, string? soloTier, string? soloRank, int? soloLp, string? flexTier, string? flexRank, int? flexLp)
    {
        RankUpdates.Add((puuid, soloTier, soloLp, flexTier, flexLp));
        return Task.CompletedTask;
    }

    public Task UpsertAsync(RiotAccount account) => Task.CompletedTask;
    public Task<bool> ExistsByPuuidAsync(string puuid) => Task.FromResult(Accounts.ContainsKey(puuid));
    public Task DeleteAsync(string puuid) => Task.CompletedTask;
    public Task<RiotAccount?> ClaimNextPendingForSyncAsync() => Task.FromResult<RiotAccount?>(null);
    public Task ResetStuckSyncingAccountsAsync(TimeSpan threshold) => Task.CompletedTask;
    public Task UpdateSyncProgressAsync(string puuid, int progress, int total) => Task.CompletedTask;
    public Task UpdateProfileDataAsync(string puuid, int? profileIconId, int? summonerLevel) => Task.CompletedTask;
}

internal sealed class CountingQueueSignal : ISyncQueueSignal
{
    public int Notifications { get; private set; }
    public void Notify() => Notifications++;
    public Task WaitAsync(CancellationToken cancellationToken) => Task.Delay(Timeout.Infinite, cancellationToken);
}

/// <summary>Answers League-v4 with a configurable JSON body (or throws); every other call is unused.</summary>
internal sealed class LeagueOnlyRiotApiClient : IRiotApiClient
{
    public Dictionary<string, string> LeagueJsonByPuuid { get; } = new();
    public Dictionary<string, Exception> FailuresByPuuid { get; } = new();
    public List<string> LeagueCalls { get; } = new();

    public event EventHandler<RateLimitWaitEventArgs>? RateLimitWaitStarted { add { } remove { } }

    public Task<JsonDocument> GetLeagueEntriesByPuuidAsync(string region, string puuid, CancellationToken ct = default)
    {
        LeagueCalls.Add(puuid);
        if (FailuresByPuuid.TryGetValue(puuid, out var failure)) throw failure;
        return Task.FromResult(JsonDocument.Parse(LeagueJsonByPuuid.GetValueOrDefault(puuid, "[]")));
    }

    public static string Entry(string queueType, string tier, string rank, int lp, int wins, int losses) =>
        $$"""{"queueType":"{{queueType}}","tier":"{{tier}}","rank":"{{rank}}","leaguePoints":{{lp}},"wins":{{wins}},"losses":{{losses}}}""";

    public Task<double> GetWinrateAsync(string puuid) => throw new NotSupportedException();
    public Task<string> GetPuuIdAsync(string gameName, string tagLine, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<JsonDocument> GetMatchHistoryAsync(string puuid, int start = 0, int count = 100, long? startTime = null, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<JsonDocument> GetMatchInfoAsync(string matchId, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<JsonDocument> GetMatchTimelineAsync(string matchId, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<JsonDocument> GetSummonerByPuuIdAsync(string tagline, string puuid, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<JsonDocument> GetLeagueEntriesBySummonerIdAsync(string region, string summonerId, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<string> GetLolVersionAsync(CancellationToken ct = default) => throw new NotSupportedException();
    public void Dispose() { }
}

/// <summary>Keeps every log entry at or above Debug as (level, rendered message).</summary>
internal sealed class RecordingLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message)> Entries { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => Entries.Add((logLevel, formatter(state, exception)));
}
