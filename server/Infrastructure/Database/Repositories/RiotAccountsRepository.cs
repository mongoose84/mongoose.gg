using MySqlConnector;
using RiotProxy.Core.Entities;
using RiotProxy.Core.Interfaces;

namespace RiotProxy.Infrastructure.Database.Repositories;

/// <summary>
/// Repository for Riot account data (shared across users).
/// User-specific linking is handled by UserRiotAccountsRepository.
/// </summary>
public class RiotAccountsRepository : RepositoryBase, IRiotAccountsRepository
{
    public RiotAccountsRepository(IDbConnectionFactory factory) : base(factory) { }

    public virtual Task UpsertAsync(RiotAccount account)
    {
        const string sql = @"INSERT INTO riot_accounts
            (puuid, game_name, tag_line, summoner_name, region, summoner_id, sync_status, profile_icon_id, summoner_level, solo_tier, solo_rank, solo_lp, flex_tier, flex_rank, flex_lp, last_sync_at, created_at, updated_at)
            VALUES (@puuid, @game_name, @tag_line, @summoner_name, @region, @summoner_id, @sync_status, @profile_icon_id, @summoner_level, @solo_tier, @solo_rank, @solo_lp, @flex_tier, @flex_rank, @flex_lp, @last_sync_at, @created_at, @updated_at) AS new
            ON DUPLICATE KEY UPDATE
                game_name = new.game_name,
                tag_line = new.tag_line,
                summoner_name = new.summoner_name,
                region = new.region,
                summoner_id = COALESCE(new.summoner_id, riot_accounts.summoner_id),
                sync_status = new.sync_status,
                profile_icon_id = COALESCE(new.profile_icon_id, riot_accounts.profile_icon_id),
                summoner_level = COALESCE(new.summoner_level, riot_accounts.summoner_level),
                solo_tier = COALESCE(new.solo_tier, riot_accounts.solo_tier),
                solo_rank = COALESCE(new.solo_rank, riot_accounts.solo_rank),
                solo_lp = COALESCE(new.solo_lp, riot_accounts.solo_lp),
                flex_tier = COALESCE(new.flex_tier, riot_accounts.flex_tier),
                flex_rank = COALESCE(new.flex_rank, riot_accounts.flex_rank),
                flex_lp = COALESCE(new.flex_lp, riot_accounts.flex_lp),
                last_sync_at = new.last_sync_at,
                updated_at = new.updated_at;";

        return ExecuteNonQueryAsync(sql,
            ("@puuid", account.Puuid),
            ("@game_name", account.GameName),
            ("@tag_line", account.TagLine),
            ("@summoner_name", account.SummonerName),
            ("@region", account.Region),
            ("@summoner_id", (object?)account.SummonerId ?? DBNull.Value),
            ("@sync_status", account.SyncStatus),
            ("@profile_icon_id", (object?)account.ProfileIconId ?? DBNull.Value),
            ("@summoner_level", (object?)account.SummonerLevel ?? DBNull.Value),
            ("@solo_tier", (object?)account.SoloTier ?? DBNull.Value),
            ("@solo_rank", (object?)account.SoloRank ?? DBNull.Value),
            ("@solo_lp", (object?)account.SoloLp ?? DBNull.Value),
            ("@flex_tier", (object?)account.FlexTier ?? DBNull.Value),
            ("@flex_rank", (object?)account.FlexRank ?? DBNull.Value),
            ("@flex_lp", (object?)account.FlexLp ?? DBNull.Value),
            ("@last_sync_at", account.LastSyncAt),
            ("@created_at", account.CreatedAt == default ? DateTime.UtcNow : account.CreatedAt),
            ("@updated_at", DateTime.UtcNow));
    }

    private const string SelectColumns = @"puuid, game_name, tag_line, summoner_name, region, summoner_id,
        sync_status, sync_progress, sync_total, profile_icon_id, summoner_level,
        solo_tier, solo_rank, solo_lp, flex_tier, flex_rank, flex_lp,
        last_sync_at, created_at, updated_at";

    public virtual async Task<RiotAccount?> GetByPuuidAsync(string puuid)
    {
        var sql = $"SELECT {SelectColumns} FROM riot_accounts WHERE puuid = @puuid";
        var results = await ExecuteListAsync(sql, Map, ("@puuid", puuid));
        return results.FirstOrDefault();
    }

    public virtual async Task<bool> ExistsByPuuidAsync(string puuid)
    {
        const string sql = "SELECT COUNT(*) FROM riot_accounts WHERE puuid = @puuid";
        var count = await ExecuteScalarAsync<long>(sql, ("@puuid", puuid));
        return count > 0;
    }

    public virtual Task DeleteAsync(string puuid)
    {
        const string sql = "DELETE FROM riot_accounts WHERE puuid = @puuid";
        return ExecuteNonQueryAsync(sql, ("@puuid", puuid));
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

    /// <summary>
    /// Atomically claims the next pending account for sync.
    /// Uses UPDATE ... WHERE to prevent race conditions.
    /// Returns null if no pending accounts or if another worker claimed it first.
    /// </summary>
    public virtual async Task<RiotAccount?> ClaimNextPendingForSyncAsync()
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

    /// <summary>
    /// Updates profile data (icon, level) for an account.
    /// </summary>
    public virtual Task UpdateProfileDataAsync(string puuid, int? profileIconId, int? summonerLevel)
    {
        const string sql = @"
            UPDATE riot_accounts
            SET profile_icon_id = @profile_icon_id,
                summoner_level = @summoner_level,
                updated_at = @now
            WHERE puuid = @puuid";

        return ExecuteNonQueryAsync(sql,
            ("@puuid", puuid),
            ("@profile_icon_id", (object?)profileIconId ?? DBNull.Value),
            ("@summoner_level", (object?)summonerLevel ?? DBNull.Value),
            ("@now", DateTime.UtcNow));
    }

    /// <summary>
    /// Updates rank data (solo and flex) for an account.
    /// </summary>
    public virtual Task UpdateRankDataAsync(
        string puuid,
        string? summonerId,
        string? soloTier, string? soloRank, int? soloLp,
        string? flexTier, string? flexRank, int? flexLp)
    {
        const string sql = @"
            UPDATE riot_accounts
            SET summoner_id = COALESCE(@summoner_id, summoner_id),
                solo_tier = @solo_tier,
                solo_rank = @solo_rank,
                solo_lp = @solo_lp,
                flex_tier = @flex_tier,
                flex_rank = @flex_rank,
                flex_lp = @flex_lp,
                updated_at = @now
            WHERE puuid = @puuid";

        return ExecuteNonQueryAsync(sql,
            ("@puuid", puuid),
            ("@summoner_id", (object?)summonerId ?? DBNull.Value),
            ("@solo_tier", (object?)soloTier ?? DBNull.Value),
            ("@solo_rank", (object?)soloRank ?? DBNull.Value),
            ("@solo_lp", (object?)soloLp ?? DBNull.Value),
            ("@flex_tier", (object?)flexTier ?? DBNull.Value),
            ("@flex_rank", (object?)flexRank ?? DBNull.Value),
            ("@flex_lp", (object?)flexLp ?? DBNull.Value),
            ("@now", DateTime.UtcNow));
    }

    // Column order: puuid, game_name, tag_line, summoner_name, region, summoner_id,
    //               sync_status, sync_progress, sync_total, profile_icon_id, summoner_level,
    //               solo_tier, solo_rank, solo_lp, flex_tier, flex_rank, flex_lp,
    //               last_sync_at, created_at, updated_at
    private static RiotAccount Map(MySqlDataReader r) => new()
    {
        Puuid = r.GetString(0),
        GameName = r.GetString(1),
        TagLine = r.GetString(2),
        SummonerName = r.GetString(3),
        Region = r.GetString(4),
        SummonerId = r.IsDBNull(5) ? null : r.GetString(5),
        SyncStatus = r.GetString(6),
        SyncProgress = r.GetInt32(7),
        SyncTotal = r.GetInt32(8),
        ProfileIconId = r.IsDBNull(9) ? null : r.GetInt32(9),
        SummonerLevel = r.IsDBNull(10) ? null : r.GetInt32(10),
        SoloTier = r.IsDBNull(11) ? null : r.GetString(11),
        SoloRank = r.IsDBNull(12) ? null : r.GetString(12),
        SoloLp = r.IsDBNull(13) ? null : r.GetInt32(13),
        FlexTier = r.IsDBNull(14) ? null : r.GetString(14),
        FlexRank = r.IsDBNull(15) ? null : r.GetString(15),
        FlexLp = r.IsDBNull(16) ? null : r.GetInt32(16),
        LastSyncAt = r.GetDateTimeUtcOrNull(17),
        CreatedAt = r.GetDateTimeUtc(18),
        UpdatedAt = r.GetDateTimeUtc(19)
    };
}
