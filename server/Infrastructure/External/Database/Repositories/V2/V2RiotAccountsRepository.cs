using MySqlConnector;
using RiotProxy.External.Domain.Entities.V2;

namespace RiotProxy.Infrastructure.External.Database.Repositories.V2;

public class V2RiotAccountsRepository : RepositoryBase
{
    public V2RiotAccountsRepository(IDbConnectionFactory factory) : base(factory) {}

    public Task UpsertAsync(V2RiotAccount account)
    {
        const string sql = @"INSERT INTO riot_accounts (puuid, user_id, summoner_name, region, is_primary, created_at, updated_at)
            VALUES (@puuid, @user_id, @summoner_name, @region, @is_primary, @created_at, @updated_at) AS new
            ON DUPLICATE KEY UPDATE
                user_id = new.user_id,
                summoner_name = new.summoner_name,
                region = new.region,
                is_primary = new.is_primary,
                updated_at = new.updated_at;";

        return ExecuteNonQueryAsync(sql,
            ("@puuid", account.Puuid),
            ("@user_id", account.UserId),
            ("@summoner_name", account.SummonerName),
            ("@region", account.Region),
            ("@is_primary", account.IsPrimary),
            ("@created_at", account.CreatedAt == default ? DateTime.UtcNow : account.CreatedAt),
            ("@updated_at", DateTime.UtcNow));
    }

    public Task<IList<V2RiotAccount>> GetByUserIdAsync(long userId)
    {
        const string sql = "SELECT * FROM riot_accounts WHERE user_id = @user_id";
        return ExecuteListAsync(sql, Map, ("@user_id", userId));
    }

    private static V2RiotAccount Map(MySqlDataReader r) => new()
    {
        Puuid = r.GetString("puuid"),
        UserId = r.GetInt64("user_id"),
        SummonerName = r.GetString("summoner_name"),
        Region = r.GetString("region"),
        IsPrimary = r.GetBoolean("is_primary"),
        CreatedAt = r.GetDateTime("created_at"),
        UpdatedAt = r.GetDateTime("updated_at")
    };
}
