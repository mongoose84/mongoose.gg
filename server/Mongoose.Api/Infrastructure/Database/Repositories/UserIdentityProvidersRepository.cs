using Mongoose.Api.Core.Interfaces;

namespace Mongoose.Api.Infrastructure.Database.Repositories;

public class UserIdentityProvidersRepository : RepositoryBase, IUserIdentityProvidersRepository
{
    public UserIdentityProvidersRepository(IDbConnectionFactory factory) : base(factory) { }

    public async Task<long?> GetUserIdByProviderIdentityAsync(string provider, string providerUid)
    {
        const string sql = "SELECT user_id FROM user_identity_providers WHERE provider = @provider AND provider_uid = @provider_uid LIMIT 1";
        var userId = await ExecuteScalarAsync<long>(sql, ("@provider", provider), ("@provider_uid", providerUid));
        return userId == default ? null : userId;
    }

    public Task LinkProviderIdentityAsync(long userId, string provider, string providerUid)
    {
        const string sql = @"
            INSERT INTO user_identity_providers (user_id, provider, provider_uid, created_at)
            VALUES (@user_id, @provider, @provider_uid, @created_at)
            ON DUPLICATE KEY UPDATE user_id = @user_id";

        return ExecuteNonQueryAsync(sql,
            ("@user_id", userId),
            ("@provider", provider),
            ("@provider_uid", providerUid),
            ("@created_at", DateTime.UtcNow));
    }
}
