namespace RiotProxy.Infrastructure.WebSocket;

/// <summary>
/// Interface for broadcasting sync progress updates to connected WebSocket clients.
/// Used by V2MatchHistorySyncJob to push real-time updates.
/// </summary>
public interface ISyncProgressBroadcaster
{
    /// <summary>
    /// Broadcasts sync progress update to all clients subscribed to this Riot account.
    /// </summary>
    /// <param name="puuid">The Riot account PUUID (primary key)</param>
    Task BroadcastProgressAsync(string puuid, int progress, int total, string? currentMatchId = null);

    /// <summary>
    /// Broadcasts sync completion to all clients subscribed to this Riot account.
    /// </summary>
    /// <param name="puuid">The Riot account PUUID (primary key)</param>
    Task BroadcastCompleteAsync(string puuid, int totalSynced);

    /// <summary>
    /// Broadcasts sync error to all clients subscribed to this Riot account.
    /// </summary>
    /// <param name="puuid">The Riot account PUUID (primary key)</param>
    Task BroadcastErrorAsync(string puuid, string error);
}

