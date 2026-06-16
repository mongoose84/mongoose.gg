namespace Mongoose.Api.Infrastructure.WebSocket;

/// <summary>
/// Sends user-scoped aggregate sync messages to every connection authenticated to a user.
/// Implemented by <see cref="SyncProgressHub"/>; depended on by <see cref="SyncProgressAggregator"/>
/// so the aggregate combine logic can be unit-tested without real WebSocket connections.
/// </summary>
public interface IUserSyncBroadcaster
{
    Task BroadcastToUserAsync(long userId, SyncAggregateMessage message);
}
