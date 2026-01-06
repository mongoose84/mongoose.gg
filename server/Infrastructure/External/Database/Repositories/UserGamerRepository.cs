namespace RiotProxy.Infrastructure.External.Database.Repositories
{
    public class UserGamerRepository : RepositoryBase
    {
        public UserGamerRepository(IDbConnectionFactory factory) : base(factory)
        {
        }

        public async Task<bool> LinkGamerToUserAsync(int userId, string puuid)
        {
            const string sql = "INSERT IGNORE INTO UserGamer (UserId, PuuId) VALUES (@userId, @puuid)";
            await ExecuteNonQueryAsync(sql, ("@userId", userId), ("@puuid", puuid));
            return true; // Always return true as INSERT IGNORE won't fail on duplicates
        }

        public async Task<IList<string>> GetGamersPuuIdByUserIdAsync(int userId)
        {
            const string sql = "SELECT PuuId FROM UserGamer WHERE UserId = @userId";
            return await ExecuteListAsync(sql, r => r.GetString(0), ("@userId", userId));
        }
    }
}