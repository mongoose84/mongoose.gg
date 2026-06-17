using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Infrastructure.Database.Repositories;

namespace Mongoose.Api.Infrastructure.WebSocket;

/// <summary>
/// Manages WebSocket connections for sync progress updates.
/// Handles client subscribe/unsubscribe and broadcasts progress to subscribed clients.
/// </summary>
public sealed class SyncProgressHub : ISyncProgressBroadcaster, IUserSyncBroadcaster
{
    private readonly ILogger<SyncProgressHub> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    // Maximum message size in bytes (4KB should be plenty for JSON messages)
    private const int MaxMessageSize = 4096;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // Connected clients: ConnectionId -> ClientConnection
    private readonly ConcurrentDictionary<string, ClientConnection> _connections = new();

    // Subscriptions: Puuid -> Set of ConnectionIds
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _subscriptions = new(StringComparer.OrdinalIgnoreCase);

    public SyncProgressHub(ILogger<SyncProgressHub> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Handles a new WebSocket connection. Runs for the lifetime of the connection.
    /// </summary>
    public async Task HandleConnectionAsync(System.Net.WebSockets.WebSocket webSocket, long userId, CancellationToken ct)
    {
        var connectionId = Guid.NewGuid().ToString("N");
        var connection = new ClientConnection(connectionId, webSocket, userId);

        _connections[connectionId] = connection;
        _logger.LogDebug("WebSocket connected: {ConnectionId} for user {UserId}", LogSanitizer.Sanitize(connectionId), LogSanitizer.Sanitize(userId.ToString()));

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
            _logger.LogDebug("WebSocket disconnected: {ConnectionId}", LogSanitizer.Sanitize(connectionId));
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
                        _logger.LogWarning("WebSocket message too large from {ConnectionId}, closing connection", LogSanitizer.Sanitize(connection.ConnectionId));
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
                    _logger.LogWarning("Unknown WebSocket message type: {Type}", LogSanitizer.Sanitize(type));
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse WebSocket message: {Message}", LogSanitizer.Sanitize(messageJson));
        }
    }

    /// <summary>
    /// Attempts to subscribe a connection to a puuid after verifying ownership.
    /// Returns true if subscription was successful, false if user doesn't own the account.
    /// </summary>
    private async Task<bool> TrySubscribeAsync(string connectionId, long userId, string puuid)
    {
        // Verify user has this Riot account linked (resolve scoped repository per call)
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IUserRiotAccountsRepository>();
        var isLinked = await repo.IsLinkedAsync(userId, puuid);
        if (!isLinked)
        {
            _logger.LogWarning("User {UserId} attempted to subscribe to unlinked account {Puuid}", LogSanitizer.Sanitize(userId.ToString()), LogSanitizer.HashForLog(puuid));
            return false;
        }

        var subscribers = _subscriptions.GetOrAdd(puuid, _ => new ConcurrentDictionary<string, byte>());
        subscribers[connectionId] = 0;
        _logger.LogDebug("Connection {ConnectionId} subscribed to account {Puuid}", LogSanitizer.Sanitize(connectionId), LogSanitizer.HashForLog(puuid));
        return true;
    }

    private void Unsubscribe(string connectionId, string puuid)
    {
        if (_subscriptions.TryGetValue(puuid, out var subscribers))
        {
            subscribers.TryRemove(connectionId, out _);
            _logger.LogDebug("Connection {ConnectionId} unsubscribed from account {Puuid}", LogSanitizer.Sanitize(connectionId), LogSanitizer.HashForLog(puuid));
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

    /// <summary>
    /// TEMPORARY: Broadcasts that sync is waiting due to Riot API rate limiting.
    /// TODO: Remove this once we have a more sophisticated rate limiting UX.
    /// </summary>
    public async Task BroadcastRateLimitedAsync(string puuid)
    {
        var message = new SyncRateLimitedMessage
        {
            Puuid = puuid,
            Status = "rate_limited"
        };
        await BroadcastToSubscribersAsync(puuid, message);
    }

    /// <summary>
    /// Broadcasts a user-scoped aggregate message to every open connection authenticated
    /// to <paramref name="userId"/>. No per-PUUID subscription is required — the connection
    /// is already authenticated to the user, so pushing their own aggregate is inherently safe.
    /// </summary>
    public async Task BroadcastToUserAsync(long userId, SyncAggregateMessage message)
    {
        var bytes = Serialize(message);
        var openConnections = _connections.Values
            .Where(conn => conn.UserId == userId && conn.WebSocket.State == WebSocketState.Open);
        await SendToConnectionsAsync(openConnections, bytes);
    }

    private async Task BroadcastToSubscribersAsync<T>(string puuid, T message) where T : SyncServerMessage
    {
        if (!_subscriptions.TryGetValue(puuid, out var subscribers))
            return;

        var bytes = Serialize(message);
        var openConnections = subscribers.Keys
            .Select(id => _connections.TryGetValue(id, out var conn) ? conn : null)
            .Where(conn => conn != null && conn.WebSocket.State == WebSocketState.Open)
            .Select(conn => conn!);

        await SendToConnectionsAsync(openConnections, bytes);
    }

    private static byte[] Serialize<T>(T message) =>
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, JsonOptions));

    private async Task SendToConnectionsAsync(IEnumerable<ClientConnection> connections, byte[] bytes)
    {
        var segment = new ArraySegment<byte>(bytes);

        foreach (var connection in connections)
        {
            try
            {
                await connection.SendLock.WaitAsync();
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
                _logger.LogWarning(ex, "Failed to send WebSocket message to {ConnectionId}", LogSanitizer.Sanitize(connection.ConnectionId));
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

