using MySqlConnector;
using RiotProxy.Core.Entities;
using RiotProxy.Core.Interfaces;

namespace RiotProxy.Infrastructure.Database.Repositories;

public class UserRiotAccountsRepository : RepositoryBase, IUserRiotAccountsRepository
{
    public UserRiotAccountsRepository(IDbConnectionFactory factory) : base(factory) { }

    public Task LinkAsync(long userId, string puuid, bool isPrimary)
    {
        const string sql = @"
            INSERT INTO user_riot_accounts (user_id, puuid, is_primary, linked_at)
            VALUES (@user_id, @puuid, @is_primary, @linked_at)
            ON DUPLICATE KEY UPDATE is_primary = @is_primary";

        return ExecuteNonQueryAsync(sql,
            ("@user_id", userId),
            ("@puuid", puuid),
            ("@is_primary", isPrimary),
            ("@linked_at", DateTime.UtcNow));
    }

    public Task UnlinkAsync(long userId, string puuid)
    {
        const string sql = "DELETE FROM user_riot_accounts WHERE user_id = @user_id AND puuid = @puuid";
        return ExecuteNonQueryAsync(sql, ("@user_id", userId), ("@puuid", puuid));
    }

    public async Task<bool> IsLinkedAsync(long userId, string puuid)
    {
        const string sql = "SELECT COUNT(*) FROM user_riot_accounts WHERE user_id = @user_id AND puuid = @puuid";
        var count = await ExecuteScalarAsync<long>(sql, ("@user_id", userId), ("@puuid", puuid));
        return count > 0;
    }

    public async Task<IList<(UserRiotAccountLink Link, RiotAccount Account)>> GetByUserIdAsync(long userId)
    {
        const string sql = @"
            SELECT 
                ura.user_id, ura.puuid, ura.is_primary, ura.linked_at,
                ra.puuid, ra.game_name, ra.tag_line, ra.summoner_name, ra.region, ra.summoner_id,
                ra.sync_status, ra.sync_progress, ra.sync_total, ra.profile_icon_id, ra.summoner_level,
                ra.solo_tier, ra.solo_rank, ra.solo_lp, ra.flex_tier, ra.flex_rank, ra.flex_lp,
                ra.last_sync_at, ra.created_at, ra.updated_at
            FROM user_riot_accounts ura
            INNER JOIN riot_accounts ra ON ura.puuid = ra.puuid
            WHERE ura.user_id = @user_id
            ORDER BY ura.is_primary DESC, ura.linked_at ASC";

        return await ExecuteListAsync(sql, MapLinkWithAccount, ("@user_id", userId));
    }

    public async Task<IList<long>> GetUserIdsByPuuidAsync(string puuid)
    {
        const string sql = "SELECT user_id FROM user_riot_accounts WHERE puuid = @puuid";
        return await ExecuteListAsync(sql, r => r.GetInt64(0), ("@puuid", puuid));
    }

    public async Task SetPrimaryAsync(long userId, string puuid)
    {
        // First, unset all primary flags for this user
        const string unsetSql = "UPDATE user_riot_accounts SET is_primary = FALSE WHERE user_id = @user_id";
        await ExecuteNonQueryAsync(unsetSql, ("@user_id", userId));

        // Then set the specified account as primary
        const string setSql = "UPDATE user_riot_accounts SET is_primary = TRUE WHERE user_id = @user_id AND puuid = @puuid";
        await ExecuteNonQueryAsync(setSql, ("@user_id", userId), ("@puuid", puuid));
    }

    public async Task<(UserRiotAccountLink Link, RiotAccount Account)?> GetPrimaryByUserIdAsync(long userId)
    {
        const string sql = @"
            SELECT 
                ura.user_id, ura.puuid, ura.is_primary, ura.linked_at,
                ra.puuid, ra.game_name, ra.tag_line, ra.summoner_name, ra.region, ra.summoner_id,
                ra.sync_status, ra.sync_progress, ra.sync_total, ra.profile_icon_id, ra.summoner_level,
                ra.solo_tier, ra.solo_rank, ra.solo_lp, ra.flex_tier, ra.flex_rank, ra.flex_lp,
                ra.last_sync_at, ra.created_at, ra.updated_at
            FROM user_riot_accounts ura
            INNER JOIN riot_accounts ra ON ura.puuid = ra.puuid
            WHERE ura.user_id = @user_id AND ura.is_primary = TRUE
            LIMIT 1";

        var results = await ExecuteListAsync(sql, MapLinkWithAccount, ("@user_id", userId));
        return results.FirstOrDefault();
    }

    public async Task<bool> HasAnyLinksAsync(string puuid)
    {
        const string sql = "SELECT COUNT(*) FROM user_riot_accounts WHERE puuid = @puuid";
        var count = await ExecuteScalarAsync<long>(sql, ("@puuid", puuid));
        return count > 0;
    }

    public async Task<int> GetLinkCountAsync(string puuid)
    {
        const string sql = "SELECT COUNT(*) FROM user_riot_accounts WHERE puuid = @puuid";
        return (int)await ExecuteScalarAsync<long>(sql, ("@puuid", puuid));
    }

    private static (UserRiotAccountLink, RiotAccount) MapLinkWithAccount(MySqlDataReader r)
    {
        var link = new UserRiotAccountLink
        {
            UserId = r.GetInt64(0),
            Puuid = r.GetString(1),
            IsPrimary = r.GetBoolean(2),
            LinkedAt = r.GetDateTime(3)
        };

        var account = new RiotAccount
        {
            Puuid = r.GetString(4),
            GameName = r.GetString(5),
            TagLine = r.GetString(6),
            SummonerName = r.GetString(7),
            Region = r.GetString(8),
            SummonerId = r.IsDBNull(9) ? null : r.GetString(9),
            SyncStatus = r.GetString(10),
            SyncProgress = r.GetInt32(11),
            SyncTotal = r.GetInt32(12),
            ProfileIconId = r.IsDBNull(13) ? null : r.GetInt32(13),
            SummonerLevel = r.IsDBNull(14) ? null : r.GetInt32(14),
            SoloTier = r.IsDBNull(15) ? null : r.GetString(15),
            SoloRank = r.IsDBNull(16) ? null : r.GetString(16),
            SoloLp = r.IsDBNull(17) ? null : r.GetInt32(17),
            FlexTier = r.IsDBNull(18) ? null : r.GetString(18),
            FlexRank = r.IsDBNull(19) ? null : r.GetString(19),
            FlexLp = r.IsDBNull(20) ? null : r.GetInt32(20),
            LastSyncAt = r.IsDBNull(21) ? null : r.GetDateTime(21),
            CreatedAt = r.GetDateTime(22),
            UpdatedAt = r.GetDateTime(23)
        };

        return (link, account);
    }
}

