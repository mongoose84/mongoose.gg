using System.Collections.Concurrent;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Infrastructure.Database.Repositories;

namespace Mongoose.Api.Tests;

internal sealed partial class TestWebApplicationFactory
{
    /// <summary>
    /// Fake Riot accounts repository for testing.
    /// </summary>
    internal sealed class FakeRiotAccountsRepository : RiotAccountsRepository
    {
        private readonly ConcurrentDictionary<string, RiotAccount> _accountsByPuuid = new();

        public FakeRiotAccountsRepository() : base(null!) { }

        public override Task<RiotAccount?> GetByPuuidAsync(string puuid)
        {
            _accountsByPuuid.TryGetValue(puuid, out var account);
            return Task.FromResult(account);
        }

        /// <summary>
        /// Synchronous in-memory lookup. Use this inside other fake repositories
        /// to avoid the sync-over-async anti-pattern.
        /// </summary>
        public RiotAccount? GetByPuuid(string puuid)
        {
            _accountsByPuuid.TryGetValue(puuid, out var account);
            return account;
        }

        public override Task<bool> ExistsByPuuidAsync(string puuid)
        {
            return Task.FromResult(_accountsByPuuid.ContainsKey(puuid));
        }

        public override Task UpsertAsync(RiotAccount account)
        {
            _accountsByPuuid[account.Puuid] = account;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Helper method to add a Riot account for testing.
        /// Note: userId parameter kept for API compatibility but not stored on RiotAccount (use UserRiotAccountLink for user-account relationships).
        /// </summary>
        public void AddRiotAccount(long userId, string puuid, string gameName, string region, string summonerName, int summonerLevel, int profileIconId)
        {
            var account = new RiotAccount
            {
                Puuid = puuid,
                GameName = gameName,
                TagLine = summonerName.Contains('#') ? summonerName.Split('#')[1] : "NA1",
                SummonerName = summonerName,
                Region = region,
                SyncStatus = "synced",
                SummonerLevel = summonerLevel,
                ProfileIconId = profileIconId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            UpsertAsync(account).Wait();
        }

        /// <summary>
        /// Helper method to add a Riot account with rank data for testing.
        /// Note: userId parameter kept for API compatibility but not stored on RiotAccount (use UserRiotAccountLink for user-account relationships).
        /// </summary>
        public void AddRiotAccountWithRank(long userId, string puuid, string gameName, string region, string summonerName,
            int summonerLevel, int profileIconId, string? soloTier, string? soloRank, int? soloLp,
            string? flexTier = null, string? flexRank = null, int? flexLp = null)
        {
            var account = new RiotAccount
            {
                Puuid = puuid,
                GameName = gameName,
                TagLine = summonerName.Contains('#') ? summonerName.Split('#')[1] : "NA1",
                SummonerName = summonerName,
                Region = region,
                SyncStatus = "synced",
                SummonerLevel = summonerLevel,
                ProfileIconId = profileIconId,
                SoloTier = soloTier,
                SoloRank = soloRank,
                SoloLp = soloLp,
                FlexTier = flexTier,
                FlexRank = flexRank,
                FlexLp = flexLp,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            UpsertAsync(account).Wait();
        }

        public override Task UpdateSyncStatusAsync(string puuid, string syncStatus, DateTime? lastSyncAt = null)
        {
            if (_accountsByPuuid.TryGetValue(puuid, out var account))
            {
                account.SyncStatus = syncStatus;
                if (lastSyncAt.HasValue)
                    account.LastSyncAt = lastSyncAt;
                account.UpdatedAt = DateTime.UtcNow;
            }
            return Task.CompletedTask;
        }
    }
}
