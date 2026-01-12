using MySqlConnector;
using RiotProxy.External.Domain.Entities.V2;

namespace RiotProxy.Infrastructure.External.Database.Repositories.V2;

public class V2RiotAccountsRepository : RepositoryBase
{
    public V2RiotAccountsRepository(IV2DbConnectionFactory factory) : base(factory) {}

    public virtual Task UpsertAsync(V2RiotAccount account)
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

    public virtual Task<IList<V2RiotAccount>> GetByUserIdAsync(long userId)
    {
        const string sql = "SELECT puuid, user_id, game_name, tag_line, summoner_name, region, is_primary, sync_status, sync_progress, sync_total, last_sync_at, created_at, updated_at FROM riot_accounts WHERE user_id = @user_id ORDER BY is_primary DESC, created_at ASC";
        return ExecuteListAsync(sql, Map, ("@user_id", userId));
    }

    public virtual async Task<V2RiotAccount?> GetByPuuidAsync(string puuid)
    {
        const string sql = "SELECT puuid, user_id, game_name, tag_line, summoner_name, region, is_primary, sync_status, sync_progress, sync_total, last_sync_at, created_at, updated_at FROM riot_accounts WHERE puuid = @puuid";
        var results = await ExecuteListAsync(sql, Map, ("@puuid", puuid));
        return results.FirstOrDefault();
    }

    public virtual async Task<bool> ExistsByPuuidAsync(string puuid)
    {
        const string sql = "SELECT COUNT(*) FROM riot_accounts WHERE puuid = @puuid";
        var count = await ExecuteScalarAsync<long>(sql, ("@puuid", puuid));
        return count > 0;
    }

    public virtual Task DeleteAsync(string puuid, long userId)
    {
        const string sql = "DELETE FROM riot_accounts WHERE puuid = @puuid AND user_id = @user_id";
        return ExecuteNonQueryAsync(sql, ("@puuid", puuid), ("@user_id", userId));
    }

    /// <summary>
    /// Updates the sync status and optionally the last sync timestamp.
    /// Pass lastSyncAt explicitly when sync completes successfully.
    /// Pass null to preserve the existing last_sync_at value (e.g., when setting status to 'syncing' or 'failed').
    /// </summary>
    public virtual Task UpdateSyncStatusAsync(string puuid, string syncStatus, DateTime? lastSyncAt = null)
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

    /// <summary>
    /// Atomically claims the next pending account for sync.
    /// Uses UPDATE ... WHERE to prevent race conditions.
    /// Returns null if no pending accounts or if another worker claimed it first.
    /// </summary>
    public virtual async Task<V2RiotAccount?> ClaimNextPendingForSyncAsync()
    {
        return await ExecuteWithConnectionAsync(async conn =>
        {
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // Find one pending account (oldest first by updated_at)
                const string selectSql = @"
                    SELECT puuid FROM riot_accounts
                    WHERE sync_status = 'pending'
                    ORDER BY updated_at ASC
                    LIMIT 1
                    FOR UPDATE";

                await using var selectCmd = new MySqlCommand(selectSql, conn, tx);
                var puuid = (string?)await selectCmd.ExecuteScalarAsync();

                if (puuid == null)
                {
                    await tx.RollbackAsync();
                    return null;
                }

                // Atomically claim it (WHERE ensures we only claim if still pending)
                const string updateSql = @"
                    UPDATE riot_accounts
                    SET sync_status = 'syncing', updated_at = @now
                    WHERE puuid = @puuid AND sync_status = 'pending'";

                await using var updateCmd = new MySqlCommand(updateSql, conn, tx);
                updateCmd.Parameters.AddWithValue("@puuid", puuid);
                updateCmd.Parameters.AddWithValue("@now", DateTime.UtcNow);
                var affected = await updateCmd.ExecuteNonQueryAsync();

                if (affected == 0)
                {
                    // Race condition: someone else claimed it
                    await tx.RollbackAsync();
                    return null;
                }

                await tx.CommitAsync();

                // Fetch the full account (outside transaction)
                return await GetByPuuidAsync(puuid);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        });
    }

    /// <summary>
    /// Reset accounts stuck in 'syncing' state (crash recovery).
    /// Accounts that have been syncing for longer than the threshold are reset to 'pending'.
    /// </summary>
    public virtual Task ResetStuckSyncingAccountsAsync(TimeSpan threshold)
    {
        var cutoff = DateTime.UtcNow - threshold;
        const string sql = @"
            UPDATE riot_accounts
            SET sync_status = 'pending', updated_at = @now
            WHERE sync_status = 'syncing' AND updated_at < @cutoff";

        return ExecuteNonQueryAsync(sql,
            ("@now", DateTime.UtcNow),
            ("@cutoff", cutoff));
    }

    /// <summary>
    /// Updates sync progress for an account.
    /// </summary>
    public virtual Task UpdateSyncProgressAsync(string puuid, int progress, int total)
    {
        const string sql = @"
            UPDATE riot_accounts
            SET sync_progress = @progress, sync_total = @total, updated_at = @now
            WHERE puuid = @puuid";

        return ExecuteNonQueryAsync(sql,
            ("@puuid", puuid),
            ("@progress", progress),
            ("@total", total),
            ("@now", DateTime.UtcNow));
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
        SyncProgress = r.GetInt32(8),
        SyncTotal = r.GetInt32(9),
        LastSyncAt = r.IsDBNull(10) ? null : r.GetDateTime(10),
        CreatedAt = r.GetDateTime(11),
        UpdatedAt = r.GetDateTime(12)
    };
}
