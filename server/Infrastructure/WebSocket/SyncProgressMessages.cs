using System.Text.Json.Serialization;

namespace RiotProxy.Infrastructure.WebSocket;

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

