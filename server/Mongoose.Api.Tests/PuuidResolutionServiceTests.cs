using Microsoft.Extensions.Logging.Abstractions;
using Mongoose.Api.Application.Services;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;
using Xunit;

namespace Mongoose.Api.Tests;

public sealed class PuuidResolutionServiceTests
{
    [Fact]
    public async Task ResolvePrimaryAccountAsync_ReturnsFirstAccount_WhenNoPrimaryIsMarked()
    {
        var linkedAccounts = new List<(UserRiotAccountLink Link, RiotAccount Account)>
        {
            (
                new UserRiotAccountLink { UserId = 1, Puuid = "puuid-1", IsPrimary = false, LinkedAt = DateTime.UtcNow },
                new RiotAccount { Puuid = "puuid-1", GameName = "PlayerOne", TagLine = "NA1", SummonerName = "PlayerOne", Region = "na1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            ),
            (
                new UserRiotAccountLink { UserId = 1, Puuid = "puuid-2", IsPrimary = false, LinkedAt = DateTime.UtcNow },
                new RiotAccount { Puuid = "puuid-2", GameName = "PlayerTwo", TagLine = "NA1", SummonerName = "PlayerTwo", Region = "na1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            )
        };

        var repository = new FakeUserRiotAccountsRepository(linkedAccounts);
        var service = new PuuidResolutionService(repository, NullLogger<PuuidResolutionService>.Instance);

        var (errorResult, account) = await service.ResolvePrimaryAccountAsync(1);

        Assert.Null(errorResult);
        Assert.NotNull(account);
        Assert.Equal("puuid-1", account!.Account.Puuid);
        Assert.False(account.IsPrimary);
    }

    [Fact]
    public async Task ResolvePrimaryAccountAsync_FallsBackToFirstAccount_WhenLinkMetadataIsNull()
    {
        var linkedAccounts = new List<(UserRiotAccountLink Link, RiotAccount Account)>
        {
            (
                null!,
                new RiotAccount { Puuid = "puuid-1", GameName = "PlayerOne", TagLine = "NA1", SummonerName = "PlayerOne", Region = "na1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            ),
            (
                null!,
                new RiotAccount { Puuid = "puuid-2", GameName = "PlayerTwo", TagLine = "NA1", SummonerName = "PlayerTwo", Region = "na1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            )
        };

        var repository = new FakeUserRiotAccountsRepository(linkedAccounts);
        var service = new PuuidResolutionService(repository, NullLogger<PuuidResolutionService>.Instance);

        var (errorResult, account) = await service.ResolvePrimaryAccountAsync(1);

        Assert.Null(errorResult);
        Assert.NotNull(account);
        Assert.Equal("puuid-1", account!.Account.Puuid);
        Assert.False(account.IsPrimary);
    }

    [Fact]
    public async Task ResolveAllAccountsAsync_TreatsNullLinkMetadataAsNonPrimary()
    {
        var linkedAccounts = new List<(UserRiotAccountLink Link, RiotAccount Account)>
        {
            (
                null!,
                new RiotAccount { Puuid = "puuid-1", GameName = "PlayerOne", TagLine = "NA1", SummonerName = "PlayerOne", Region = "na1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            ),
            (
                new UserRiotAccountLink { UserId = 1, Puuid = "puuid-2", IsPrimary = true, LinkedAt = DateTime.UtcNow },
                new RiotAccount { Puuid = "puuid-2", GameName = "PlayerTwo", TagLine = "NA1", SummonerName = "PlayerTwo", Region = "na1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            )
        };

        var repository = new FakeUserRiotAccountsRepository(linkedAccounts);
        var service = new PuuidResolutionService(repository, NullLogger<PuuidResolutionService>.Instance);

        var (errorResult, accounts) = await service.ResolveAllAccountsAsync(1);

        Assert.Null(errorResult);
        Assert.NotNull(accounts);
        Assert.Equal(2, accounts!.Count);
        Assert.False(accounts[0].IsPrimary);
        Assert.True(accounts[1].IsPrimary);
    }

    private sealed class FakeUserRiotAccountsRepository : IUserRiotAccountsRepository
    {
        private readonly IList<(UserRiotAccountLink Link, RiotAccount Account)> _linkedAccounts;

        public FakeUserRiotAccountsRepository(IList<(UserRiotAccountLink Link, RiotAccount Account)> linkedAccounts)
        {
            _linkedAccounts = linkedAccounts;
        }

        public Task<IList<(UserRiotAccountLink Link, RiotAccount Account)>> GetByUserIdAsync(long userId)
            => Task.FromResult(_linkedAccounts);

        public Task<bool> IsLinkedAsync(long userId, string puuid)
            => Task.FromResult(_linkedAccounts.Any(la => la.Account.Puuid == puuid));

        public Task LinkAsync(long userId, string puuid, bool isPrimary) => throw new NotImplementedException();
        public Task UnlinkAsync(long userId, string puuid) => throw new NotImplementedException();
        public Task<IList<long>> GetUserIdsByPuuidAsync(string puuid) => throw new NotImplementedException();
        public Task SetPrimaryAsync(long userId, string puuid) => throw new NotImplementedException();
        public Task<(UserRiotAccountLink Link, RiotAccount Account)?> GetPrimaryByUserIdAsync(long userId) => throw new NotImplementedException();
        public Task<bool> HasAnyLinksAsync(string puuid) => throw new NotImplementedException();
        public Task<int> GetLinkCountAsync(string puuid) => throw new NotImplementedException();
    }
}
