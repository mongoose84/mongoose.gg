using System.Text.Json.Serialization;

namespace Mongoose.Api.Infrastructure.WebSocket;

/// <summary>
/// Message types sent from server to client via WebSocket.
/// </summary>
public abstract record SyncServerMessage
{
    [JsonPropertyName("type")]
    public abstract string Type { get; }

    /// <summary>
    /// The Riot account PUUID (primary key in riot_accounts table).
    /// </summary>
    [JsonPropertyName("puuid")]
    public required string Puuid { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }
}

public sealed record SyncProgressMessage : SyncServerMessage
{
    public override string Type => "sync_progress";
    
    [JsonPropertyName("progress")]
    public required int Progress { get; init; }
    
    [JsonPropertyName("total")]
    public required int Total { get; init; }
    
    [JsonPropertyName("matchId")]
    public string? MatchId { get; init; }
}

public sealed record SyncCompleteMessage : SyncServerMessage
{
    public override string Type => "sync_complete";
    
    [JsonPropertyName("totalSynced")]
    public required int TotalSynced { get; init; }
}

public sealed record SyncErrorMessage : SyncServerMessage
{
    public override string Type => "sync_error";

    [JsonPropertyName("error")]
    public required string Error { get; init; }
}

/// <summary>
/// TEMPORARY: Message indicating sync is waiting due to Riot API rate limiting.
/// TODO: Remove this once we have a more sophisticated rate limiting UX.
/// </summary>
public sealed record SyncRateLimitedMessage : SyncServerMessage
{
    public override string Type => "sync_rate_limited";
}

/// <summary>
/// User-scoped aggregate messages: a single combined view across all of a user's
/// linked accounts during an "Analyze all" run. Sent to every connection authenticated
/// to that user (no per-PUUID subscription). Kept separate from <see cref="SyncServerMessage"/>
/// because aggregate progress has no single PUUID.
/// </summary>
public abstract record SyncAggregateMessage
{
    [JsonPropertyName("type")]
    public abstract string Type { get; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }
}

public sealed record SyncAggregateProgressMessage : SyncAggregateMessage
{
    public override string Type => "sync_aggregate_progress";

    /// <summary>Combined matches processed across all accounts in the run.</summary>
    [JsonPropertyName("progress")]
    public required int Progress { get; init; }

    /// <summary>
    /// Combined match total. Grows as each account is enumerated from Riot, so clients
    /// should treat the run as indeterminate until <c>accountsDone == accountsTotal</c>.
    /// </summary>
    [JsonPropertyName("total")]
    public required int Total { get; init; }

    [JsonPropertyName("accountsTotal")]
    public required int AccountsTotal { get; init; }

    [JsonPropertyName("accountsDone")]
    public required int AccountsDone { get; init; }

    [JsonPropertyName("matchId")]
    public string? MatchId { get; init; }
}

public sealed record SyncAggregateCompleteMessage : SyncAggregateMessage
{
    public override string Type => "sync_aggregate_complete";

    [JsonPropertyName("totalSynced")]
    public required int TotalSynced { get; init; }

    [JsonPropertyName("accountsTotal")]
    public required int AccountsTotal { get; init; }
}

public sealed record SyncAggregateErrorMessage : SyncAggregateMessage
{
    public override string Type => "sync_aggregate_error";

    [JsonPropertyName("error")]
    public required string Error { get; init; }
}

/// <summary>
/// Message types sent from client to server via WebSocket.
/// </summary>
public abstract record SyncClientMessage
{
    [JsonPropertyName("type")]
    public abstract string Type { get; }
}

public sealed record SubscribeMessage : SyncClientMessage
{
    public override string Type => "subscribe";

    /// <summary>
    /// The Riot account PUUID to subscribe to.
    /// </summary>
    [JsonPropertyName("puuid")]
    public required string Puuid { get; init; }
}

public sealed record UnsubscribeMessage : SyncClientMessage
{
    public override string Type => "unsubscribe";

    /// <summary>
    /// The Riot account PUUID to unsubscribe from.
    /// </summary>
    [JsonPropertyName("puuid")]
    public required string Puuid { get; init; }
}

