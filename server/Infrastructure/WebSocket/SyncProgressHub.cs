using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RiotProxy.Infrastructure.External.Database.Repositories.V2;

namespace RiotProxy.Infrastructure.WebSocket;

/// <summary>
/// Manages WebSocket connections for sync progress updates.
/// Handles client subscribe/unsubscribe and broadcasts progress to subscribed clients.
/// </summary>
public sealed class SyncProgressHub : ISyncProgressBroadcaster
{
    private readonly ILogger<SyncProgressHub> _logger;
    private readonly V2RiotAccountsRepository _riotAccountsRepo;

    // Maximum message size in bytes (4KB should be plenty for JSON messages)
    private const int MaxMessageSize = 4096;

    // Connected clients: ConnectionId -> ClientConnection
    private readonly ConcurrentDictionary<string, ClientConnection> _connections = new();

    // Subscriptions: Puuid -> Set of ConnectionIds
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _subscriptions = new(StringComparer.OrdinalIgnoreCase);

    public SyncProgressHub(ILogger<SyncProgressHub> logger, V2RiotAccountsRepository riotAccountsRepo)
    {
        _logger = logger;
        _riotAccountsRepo = riotAccountsRepo;
    }

    /// <summary>
    /// Handles a new WebSocket connection. Runs for the lifetime of the connection.
    /// </summary>
    public async Task HandleConnectionAsync(System.Net.WebSockets.WebSocket webSocket, long userId, CancellationToken ct)
    {
        var connectionId = Guid.NewGuid().ToString("N");
        var connection = new ClientConnection(connectionId, webSocket, userId);

        _connections[connectionId] = connection;
        _logger.LogDebug("WebSocket connected: {ConnectionId} for user {UserId}", connectionId, userId);

        try
        {
            await ReceiveMessagesAsync(connection, ct);
        }
        finally
        {
            // Cleanup: remove from all subscriptions
            foreach (var riotAccountId in connection.SubscribedAccounts)
            {
                Unsubscribe(connectionId, riotAccountId);
            }
            _connections.TryRemove(connectionId, out _);
            connection.Dispose();
            _logger.LogDebug("WebSocket disconnected: {ConnectionId}", connectionId);
        }
    }

    private async Task ReceiveMessagesAsync(ClientConnection connection, CancellationToken ct)
    {
        var buffer = new byte[1024];
        using var messageBuffer = new MemoryStream();

        while (!ct.IsCancellationRequested && connection.WebSocket.State == WebSocketState.Open)
        {
            try
            {
                messageBuffer.SetLength(0);
                WebSocketReceiveResult result;

                // Accumulate frames until EndOfMessage is true
                do
                {
                    result = await connection.WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await connection.WebSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Client requested close",
                            CancellationToken.None);
                        return;
                    }

                    messageBuffer.Write(buffer, 0, result.Count);

                    // Guard against excessively large messages
                    if (messageBuffer.Length > MaxMessageSize)
                    {
                        _logger.LogWarning("WebSocket message too large from {ConnectionId}, closing connection", connection.ConnectionId);
                        await connection.WebSocket.CloseAsync(
                            WebSocketCloseStatus.MessageTooBig,
                            "Message exceeds maximum size",
                            CancellationToken.None);
                        return;
                    }
                } while (!result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(messageBuffer.GetBuffer(), 0, (int)messageBuffer.Length);
                    await HandleClientMessageAsync(connection, message);
                }
            }
            catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
            {
                // Client disconnected abruptly
                break;
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task HandleClientMessageAsync(ClientConnection connection, string messageJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(messageJson);
            var type = doc.RootElement.GetProperty("type").GetString();

            switch (type)
            {
                case "subscribe":
                    var subscribePuuid = doc.RootElement.GetProperty("puuid").GetString();
                    if (!string.IsNullOrEmpty(subscribePuuid))
                    {
                        var subscribed = await TrySubscribeAsync(connection.ConnectionId, connection.UserId, subscribePuuid);
                        if (subscribed)
                        {
                            connection.SubscribedAccounts.Add(subscribePuuid);
                        }
                    }
                    break;

                case "unsubscribe":
                    var unsubscribePuuid = doc.RootElement.GetProperty("puuid").GetString();
                    if (!string.IsNullOrEmpty(unsubscribePuuid))
                    {
                        Unsubscribe(connection.ConnectionId, unsubscribePuuid);
                        connection.SubscribedAccounts.Remove(unsubscribePuuid);
                    }
                    break;

                default:
                    _logger.LogWarning("Unknown WebSocket message type: {Type}", type);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse WebSocket message: {Message}", messageJson);
        }
    }

    /// <summary>
    /// Attempts to subscribe a connection to a puuid after verifying ownership.
    /// Returns true if subscription was successful, false if user doesn't own the account.
    /// </summary>
    private async Task<bool> TrySubscribeAsync(string connectionId, long userId, string puuid)
    {
        // Verify user owns this Riot account
        var account = await _riotAccountsRepo.GetByPuuidAsync(puuid);
        if (account == null || account.UserId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to subscribe to unowned account {Puuid}", userId, puuid);
            return false;
        }

        var subscribers = _subscriptions.GetOrAdd(puuid, _ => new ConcurrentDictionary<string, byte>());
        subscribers[connectionId] = 0;
        _logger.LogDebug("Connection {ConnectionId} subscribed to account {Puuid}", connectionId, puuid);
        return true;
    }

    private void Unsubscribe(string connectionId, string puuid)
    {
        if (_subscriptions.TryGetValue(puuid, out var subscribers))
        {
            subscribers.TryRemove(connectionId, out _);
            _logger.LogDebug("Connection {ConnectionId} unsubscribed from account {Puuid}", connectionId, puuid);
        }
    }

    // ISyncProgressBroadcaster implementation
    public async Task BroadcastProgressAsync(string puuid, int progress, int total, string? currentMatchId = null)
    {
        var message = new SyncProgressMessage
        {
            Puuid = puuid,
            Status = "syncing",
            Progress = progress,
            Total = total,
            MatchId = currentMatchId
        };
        await BroadcastToSubscribersAsync(puuid, message);
    }

    public async Task BroadcastCompleteAsync(string puuid, int totalSynced)
    {
        var message = new SyncCompleteMessage
        {
            Puuid = puuid,
            Status = "completed",
            TotalSynced = totalSynced
        };
        await BroadcastToSubscribersAsync(puuid, message);
    }

    public async Task BroadcastErrorAsync(string puuid, string error)
    {
        var message = new SyncErrorMessage
        {
            Puuid = puuid,
            Status = "failed",
            Error = error
        };
        await BroadcastToSubscribersAsync(puuid, message);
    }

    private async Task BroadcastToSubscribersAsync<T>(string puuid, T message) where T : SyncServerMessage
    {
        if (!_subscriptions.TryGetValue(puuid, out var subscribers))
            return;

        var json = JsonSerializer.Serialize(message, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var bytes = Encoding.UTF8.GetBytes(json);
        var segment = new ArraySegment<byte>(bytes);

        // Get open connections explicitly
        var openConnections = subscribers.Keys
            .Select(id => _connections.TryGetValue(id, out var conn) ? conn : null)
            .Where(conn => conn != null && conn.WebSocket.State == WebSocketState.Open);

        foreach (var connection in openConnections)
        {
            try
            {
                await connection!.SendLock.WaitAsync();
                try
                {
                    await connection.WebSocket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
                }
                finally
                {
                    connection.SendLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send WebSocket message to {ConnectionId}", connection!.ConnectionId);
            }
        }
    }

    private sealed class ClientConnection : IDisposable
    {
        public string ConnectionId { get; }
        public System.Net.WebSockets.WebSocket WebSocket { get; }
        public long UserId { get; }
        public HashSet<string> SubscribedAccounts { get; } = new(StringComparer.OrdinalIgnoreCase);
        public SemaphoreSlim SendLock { get; } = new(1, 1);

        public ClientConnection(string connectionId, System.Net.WebSockets.WebSocket webSocket, long userId)
        {
            ConnectionId = connectionId;
            WebSocket = webSocket;
            UserId = userId;
        }

        public void Dispose()
        {
            SendLock.Dispose();
        }
    }
}

