using System.Collections.Concurrent;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;

namespace Mongoose.Api.Tests;

internal sealed partial class TestWebApplicationFactory
{
    /// <summary>
    /// Fake user-riot accounts repository for testing the M:M junction table.
    /// </summary>
    internal sealed class FakeUserRiotAccountsRepository : IUserRiotAccountsRepository
    {
        private readonly ConcurrentDictionary<(long UserId, string Puuid), UserRiotAccountLink> _links = new();
        private readonly FakeRiotAccountsRepository _riotAccountsRepo;

        public FakeUserRiotAccountsRepository(FakeRiotAccountsRepository riotAccountsRepo)
        {
            _riotAccountsRepo = riotAccountsRepo;
        }

        public Task LinkAsync(long userId, string puuid, bool isPrimary)
        {
            var link = new UserRiotAccountLink
            {
                UserId = userId,
                Puuid = puuid,
                IsPrimary = isPrimary,
                LinkedAt = DateTime.UtcNow
            };
            _links[(userId, puuid)] = link;
            return Task.CompletedTask;
        }

        public Task UnlinkAsync(long userId, string puuid)
        {
            _links.TryRemove((userId, puuid), out _);
            return Task.CompletedTask;
        }

        public Task<bool> IsLinkedAsync(long userId, string puuid)
        {
            return Task.FromResult(_links.ContainsKey((userId, puuid)));
        }

        public async Task<IList<(UserRiotAccountLink Link, RiotAccount Account)>> GetByUserIdAsync(long userId)
        {
            var results = new List<(UserRiotAccountLink, RiotAccount)>();
            var userLinks = _links.Where(kvp => kvp.Key.UserId == userId)
                .OrderByDescending(kvp => kvp.Value.IsPrimary)
                .ThenBy(kvp => kvp.Value.LinkedAt);

            foreach (var kvp in userLinks)
            {
                var account = await _riotAccountsRepo.GetByPuuidAsync(kvp.Key.Puuid);
                if (account != null)
                {
                    results.Add((kvp.Value, account));
                }
            }

            return results;
        }

        public Task<IList<long>> GetUserIdsByPuuidAsync(string puuid)
        {
            var userIds = _links.Where(kvp => kvp.Key.Puuid == puuid)
                .Select(kvp => kvp.Key.UserId)
                .ToList();
            return Task.FromResult<IList<long>>(userIds);
        }

        public Task SetPrimaryAsync(long userId, string puuid)
        {
            // Unset all primary flags for this user
            foreach (var kvp in _links.Where(kvp => kvp.Key.UserId == userId))
            {
                kvp.Value.IsPrimary = false;
            }

            // Set the specified account as primary
            if (_links.TryGetValue((userId, puuid), out var link))
            {
                link.IsPrimary = true;
            }

            return Task.CompletedTask;
        }

        public async Task<(UserRiotAccountLink Link, RiotAccount Account)?> GetPrimaryByUserIdAsync(long userId)
        {
            var primaryLink = _links.FirstOrDefault(kvp => kvp.Key.UserId == userId && kvp.Value.IsPrimary);
            if (primaryLink.Value == null)
            {
                return null;
            }

            var account = await _riotAccountsRepo.GetByPuuidAsync(primaryLink.Key.Puuid);
            if (account == null)
            {
                return null;
            }

            return (primaryLink.Value, account);
        }

        public Task<bool> HasAnyLinksAsync(string puuid)
        {
            return Task.FromResult(_links.Any(kvp => kvp.Key.Puuid == puuid));
        }

        public Task<int> GetLinkCountAsync(string puuid)
        {
            return Task.FromResult(_links.Count(kvp => kvp.Key.Puuid == puuid));
        }

        public Task<int> GetLinkCountForUserAsync(long userId)
        {
            return Task.FromResult(_links.Count(kvp => kvp.Key.UserId == userId));
        }

        /// <summary>
        /// Helper method to link a Riot account to a user for testing.
        /// </summary>
        public void LinkAccount(long userId, string puuid, bool isPrimary = true)
        {
            LinkAsync(userId, puuid, isPrimary).Wait();
        }
    }
}
