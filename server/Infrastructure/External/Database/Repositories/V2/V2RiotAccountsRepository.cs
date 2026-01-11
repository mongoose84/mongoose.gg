using MySqlConnector;
using RiotProxy.External.Domain.Entities.V2;

namespace RiotProxy.Infrastructure.External.Database.Repositories.V2;

public class V2RiotAccountsRepository : RepositoryBase
{
    public V2RiotAccountsRepository(IV2DbConnectionFactory factory) : base(factory) {}

    public Task UpsertAsync(V2RiotAccount account)
    {
        const string sql = @"INSERT INTO riot_accounts
            (puuid, user_id, game_name, tag_line, summoner_name, region, is_primary, sync_status, last_sync_at, created_at, updated_at)
            VALUES (@puuid, @user_id, @game_name, @tag_line, @summoner_name, @region, @is_primary, @sync_status, @last_sync_at, @created_at, @updated_at) AS new
            ON DUPLICATE KEY UPDATE
                user_id = new.user_id,
                game_name = new.game_name,
                tag_line = new.tag_line,
                summoner_name = new.summoner_name,
                region = new.region,
                is_primary = new.is_primary,
                sync_status = new.sync_status,
                last_sync_at = new.last_sync_at,
                updated_at = new.updated_at;";

        return ExecuteNonQueryAsync(sql,
            ("@puuid", account.Puuid),
            ("@user_id", account.UserId),
            ("@game_name", account.GameName),
            ("@tag_line", account.TagLine),
            ("@summoner_name", account.SummonerName),
            ("@region", account.Region),
            ("@is_primary", account.IsPrimary),
            ("@sync_status", account.SyncStatus),
            ("@last_sync_at", account.LastSyncAt),
            ("@created_at", account.CreatedAt == default ? DateTime.UtcNow : account.CreatedAt),
            ("@updated_at", DateTime.UtcNow));
    }

    public Task<IList<V2RiotAccount>> GetByUserIdAsync(long userId)
    {
        const string sql = "SELECT puuid, user_id, game_name, tag_line, summoner_name, region, is_primary, sync_status, last_sync_at, created_at, updated_at FROM riot_accounts WHERE user_id = @user_id ORDER BY is_primary DESC, created_at ASC";
        return ExecuteListAsync(sql, Map, ("@user_id", userId));
    }

    public async Task<V2RiotAccount?> GetByPuuidAsync(string puuid)
    {
        const string sql = "SELECT puuid, user_id, game_name, tag_line, summoner_name, region, is_primary, sync_status, last_sync_at, created_at, updated_at FROM riot_accounts WHERE puuid = @puuid";
        var results = await ExecuteListAsync(sql, Map, ("@puuid", puuid));
        return results.FirstOrDefault();
    }

    public async Task<bool> ExistsByPuuidAsync(string puuid)
    {
        const string sql = "SELECT COUNT(*) FROM riot_accounts WHERE puuid = @puuid";
        var count = await ExecuteScalarAsync<long>(sql, ("@puuid", puuid));
        return count > 0;
    }

    public Task DeleteAsync(string puuid, long userId)
    {
        const string sql = "DELETE FROM riot_accounts WHERE puuid = @puuid AND user_id = @user_id";
        return ExecuteNonQueryAsync(sql, ("@puuid", puuid), ("@user_id", userId));
    }

    /// <summary>
    /// Updates the sync status and optionally the last sync timestamp.
    /// Pass lastSyncAt explicitly when sync completes successfully.
    /// Pass null to preserve the existing last_sync_at value (e.g., when setting status to 'syncing' or 'failed').
    /// </summary>
    public Task UpdateSyncStatusAsync(string puuid, string syncStatus, DateTime? lastSyncAt = null)
    {
        // Use COALESCE to preserve existing last_sync_at when null is passed
        const string sql = @"UPDATE riot_accounts
            SET sync_status = @sync_status,
                last_sync_at = COALESCE(@last_sync_at, last_sync_at),
                updated_at = @updated_at
            WHERE puuid = @puuid";
        return ExecuteNonQueryAsync(sql,
            ("@puuid", puuid),
            ("@sync_status", syncStatus),
            ("@last_sync_at", (object?)lastSyncAt ?? DBNull.Value),
            ("@updated_at", DateTime.UtcNow));
    }

    public Task SetPrimaryAsync(string puuid, long userId)
    {
        // First, unset all other accounts as primary for this user, then set the new primary
        const string unsql = "UPDATE riot_accounts SET is_primary = FALSE, updated_at = @updated_at WHERE user_id = @user_id";
        const string setsql = "UPDATE riot_accounts SET is_primary = TRUE, updated_at = @updated_at WHERE puuid = @puuid AND user_id = @user_id";

        return ExecuteTransactionAsync(async (conn, transaction) =>
        {
            await ExecuteNonQueryWithConnectionAsync(conn, transaction, unsql, ("@user_id", userId), ("@updated_at", DateTime.UtcNow));
            await ExecuteNonQueryWithConnectionAsync(conn, transaction, setsql, ("@puuid", puuid), ("@user_id", userId), ("@updated_at", DateTime.UtcNow));
        });
    }

    private static V2RiotAccount Map(MySqlDataReader r) => new()
    {
        Puuid = r.GetString(0),
        UserId = r.GetInt64(1),
        GameName = r.GetString(2),
        TagLine = r.GetString(3),
        SummonerName = r.GetString(4),
        Region = r.GetString(5),
        IsPrimary = r.GetBoolean(6),
        SyncStatus = r.GetString(7),
        LastSyncAt = r.IsDBNull(8) ? null : r.GetDateTime(8),
        CreatedAt = r.GetDateTime(9),
        UpdatedAt = r.GetDateTime(10)
    };
}
