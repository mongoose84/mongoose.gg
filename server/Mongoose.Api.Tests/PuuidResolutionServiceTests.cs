using Microsoft.Extensions.Logging.Abstractions;
using Mongoose.Api.Application.Services;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;
using Microsoft.AspNetCore.Http;
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
        var usersRepository = new FakeUsersRepository("pro");
        var service = new PuuidResolutionService(repository, usersRepository, NullLogger<PuuidResolutionService>.Instance);

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
        var usersRepository = new FakeUsersRepository("pro");
        var service = new PuuidResolutionService(repository, usersRepository, NullLogger<PuuidResolutionService>.Instance);

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
        var usersRepository = new FakeUsersRepository("pro");
        var service = new PuuidResolutionService(repository, usersRepository, NullLogger<PuuidResolutionService>.Instance);

        var (errorResult, accounts) = await service.ResolveAllAccountsAsync(1);

        Assert.Null(errorResult);
        Assert.NotNull(accounts);
        Assert.Equal(2, accounts!.Count);
        Assert.False(accounts[0].IsPrimary);
        Assert.True(accounts[1].IsPrimary);
    }

    [Fact]
    public async Task VerifyPuuidOwnershipAsync_ReturnsFalse_ForNonPrimaryLinkedAccount_WhenFreeTier()
    {
        var linkedAccounts = new List<(UserRiotAccountLink Link, RiotAccount Account)>
        {
            (
                new UserRiotAccountLink { UserId = 1, Puuid = "puuid-primary", IsPrimary = true, LinkedAt = DateTime.UtcNow },
                new RiotAccount { Puuid = "puuid-primary", GameName = "Primary", TagLine = "NA1", SummonerName = "Primary", Region = "na1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            ),
            (
                new UserRiotAccountLink { UserId = 1, Puuid = "puuid-secondary", IsPrimary = false, LinkedAt = DateTime.UtcNow },
                new RiotAccount { Puuid = "puuid-secondary", GameName = "Secondary", TagLine = "NA1", SummonerName = "Secondary", Region = "na1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            )
        };

        var repository = new FakeUserRiotAccountsRepository(linkedAccounts);
        var usersRepository = new FakeUsersRepository("free");
        var service = new PuuidResolutionService(repository, usersRepository, NullLogger<PuuidResolutionService>.Instance);

        var ownsSecondaryAccount = await service.VerifyPuuidOwnershipAsync(1, "puuid-secondary");

        Assert.False(ownsSecondaryAccount);
    }

    [Fact]
    public async Task VerifyPuuidOwnershipAsync_ReturnsTrue_ForNonPrimaryLinkedAccount_WhenTierIsPremium()
    {
        var linkedAccounts = new List<(UserRiotAccountLink Link, RiotAccount Account)>
        {
            (
                new UserRiotAccountLink { UserId = 1, Puuid = "puuid-primary", IsPrimary = true, LinkedAt = DateTime.UtcNow },
                new RiotAccount { Puuid = "puuid-primary", GameName = "Primary", TagLine = "NA1", SummonerName = "Primary", Region = "na1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            ),
            (
                new UserRiotAccountLink { UserId = 1, Puuid = "puuid-secondary", IsPrimary = false, LinkedAt = DateTime.UtcNow },
                new RiotAccount { Puuid = "puuid-secondary", GameName = "Secondary", TagLine = "NA1", SummonerName = "Secondary", Region = "na1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            )
        };

        var repository = new FakeUserRiotAccountsRepository(linkedAccounts);
        var usersRepository = new FakeUsersRepository("premium");
        var service = new PuuidResolutionService(repository, usersRepository, NullLogger<PuuidResolutionService>.Instance);

        var ownsSecondaryAccount = await service.VerifyPuuidOwnershipAsync(1, "puuid-secondary");

        Assert.True(ownsSecondaryAccount);
    }

    [Fact]
    public async Task ResolveAllAccountsAsync_ReturnsAllAccounts_WhenTierIsUnknown()
    {
        var linkedAccounts = new List<(UserRiotAccountLink Link, RiotAccount Account)>
        {
            (
                new UserRiotAccountLink { UserId = 1, Puuid = "puuid-primary", IsPrimary = true, LinkedAt = DateTime.UtcNow },
                new RiotAccount { Puuid = "puuid-primary", GameName = "Primary", TagLine = "NA1", SummonerName = "Primary", Region = "na1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            ),
            (
                new UserRiotAccountLink { UserId = 1, Puuid = "puuid-secondary", IsPrimary = false, LinkedAt = DateTime.UtcNow },
                new RiotAccount { Puuid = "puuid-secondary", GameName = "Secondary", TagLine = "NA1", SummonerName = "Secondary", Region = "na1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            )
        };

        var repository = new FakeUserRiotAccountsRepository(linkedAccounts);
        var usersRepository = new FakeUsersRepository("weird_tier");
        var service = new PuuidResolutionService(repository, usersRepository, NullLogger<PuuidResolutionService>.Instance);

        var (errorResult, accounts) = await service.ResolveAllAccountsAsync(1);

        Assert.Null(errorResult);
        Assert.NotNull(accounts);
        Assert.Equal(2, accounts!.Count);
        Assert.Equal("puuid-primary", accounts[0].Account.Puuid);
        Assert.True(accounts[0].IsPrimary);
        Assert.Equal("puuid-secondary", accounts[1].Account.Puuid);
        Assert.False(accounts[1].IsPrimary);
    }

    [Fact]
    public async Task ResolveRequestedAccountsAsync_ReturnsPrimary_WhenAccountParamIsNull()
    {
        var linkedAccounts = new List<(UserRiotAccountLink Link, RiotAccount Account)>
        {
            (
                new UserRiotAccountLink { UserId = 1, Puuid = "puuid-primary", IsPrimary = true, LinkedAt = DateTime.UtcNow },
                new RiotAccount { Puuid = "puuid-primary", GameName = "Primary", TagLine = "NA1", SummonerName = "Primary", Region = "na1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            ),
            (
                new UserRiotAccountLink { UserId = 1, Puuid = "puuid-secondary", IsPrimary = false, LinkedAt = DateTime.UtcNow },
                new RiotAccount { Puuid = "puuid-secondary", GameName = "Secondary", TagLine = "NA1", SummonerName = "Secondary", Region = "na1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            )
        };

        var service = new PuuidResolutionService(
            new FakeUserRiotAccountsRepository(linkedAccounts),
            new FakeUsersRepository("pro"),
            NullLogger<PuuidResolutionService>.Instance);

        var (error, accounts) = await service.ResolveRequestedAccountsAsync(1, null);

        Assert.Null(error);
        Assert.NotNull(accounts);
        Assert.Single(accounts!);
        Assert.Equal("puuid-primary", accounts[0].Account.Puuid);
    }

    [Fact]
    public async Task ResolveRequestedAccountsAsync_ReturnsAll_WhenAccountParamIsAll()
    {
        var linkedAccounts = new List<(UserRiotAccountLink Link, RiotAccount Account)>
        {
            (
                new UserRiotAccountLink { UserId = 1, Puuid = "puuid-primary", IsPrimary = true, LinkedAt = DateTime.UtcNow },
                new RiotAccount { Puuid = "puuid-primary", GameName = "Primary", TagLine = "NA1", SummonerName = "Primary", Region = "na1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            ),
            (
                new UserRiotAccountLink { UserId = 1, Puuid = "puuid-secondary", IsPrimary = false, LinkedAt = DateTime.UtcNow },
                new RiotAccount { Puuid = "puuid-secondary", GameName = "Secondary", TagLine = "NA1", SummonerName = "Secondary", Region = "na1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            )
        };

        var service = new PuuidResolutionService(
            new FakeUserRiotAccountsRepository(linkedAccounts),
            new FakeUsersRepository("pro"),
            NullLogger<PuuidResolutionService>.Instance);

        var (error, accounts) = await service.ResolveRequestedAccountsAsync(1, "all");

        Assert.Null(error);
        Assert.NotNull(accounts);
        Assert.Equal(2, accounts!.Count);
    }

    [Fact]
    public async Task ResolveRequestedAccountsAsync_ReturnsSpecificAccount_WhenPuuidIsOwned()
    {
        var linkedAccounts = new List<(UserRiotAccountLink Link, RiotAccount Account)>
        {
            (
                new UserRiotAccountLink { UserId = 1, Puuid = "puuid-primary", IsPrimary = true, LinkedAt = DateTime.UtcNow },
                new RiotAccount { Puuid = "puuid-primary", GameName = "Primary", TagLine = "NA1", SummonerName = "Primary", Region = "na1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            ),
            (
                new UserRiotAccountLink { UserId = 1, Puuid = "puuid-secondary", IsPrimary = false, LinkedAt = DateTime.UtcNow },
                new RiotAccount { Puuid = "puuid-secondary", GameName = "Secondary", TagLine = "NA1", SummonerName = "Secondary", Region = "na1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            )
        };

        var service = new PuuidResolutionService(
            new FakeUserRiotAccountsRepository(linkedAccounts),
            new FakeUsersRepository("pro"),
            NullLogger<PuuidResolutionService>.Instance);

        var (error, accounts) = await service.ResolveRequestedAccountsAsync(1, "puuid-secondary");

        Assert.Null(error);
        Assert.NotNull(accounts);
        Assert.Single(accounts!);
        Assert.Equal("puuid-secondary", accounts[0].Account.Puuid);
    }

    [Fact]
    public async Task ResolveRequestedAccountsAsync_ReturnsForbidden_WhenPuuidIsNotOwned()
    {
        var linkedAccounts = new List<(UserRiotAccountLink Link, RiotAccount Account)>
        {
            (
                new UserRiotAccountLink { UserId = 1, Puuid = "puuid-primary", IsPrimary = true, LinkedAt = DateTime.UtcNow },
                new RiotAccount { Puuid = "puuid-primary", GameName = "Primary", TagLine = "NA1", SummonerName = "Primary", Region = "na1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            )
        };

        var service = new PuuidResolutionService(
            new FakeUserRiotAccountsRepository(linkedAccounts),
            new FakeUsersRepository("pro"),
            NullLogger<PuuidResolutionService>.Instance);

        var (error, accounts) = await service.ResolveRequestedAccountsAsync(1, "someone-elses-puuid");

        Assert.NotNull(error);
        Assert.Null(accounts);
        var statusCodeResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(error);
        Assert.Equal(StatusCodes.Status403Forbidden, statusCodeResult.StatusCode);
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
        public Task<int> GetLinkCountForUserAsync(long userId) => throw new NotImplementedException();
    }

    private sealed class FakeUsersRepository : IUsersRepository
    {
        private readonly string _tier;

        public FakeUsersRepository(string tier)
        {
            _tier = tier;
        }

        public Task<User?> GetByIdAsync(long userId)
        {
            return Task.FromResult<User?>(new User
            {
                UserId = userId,
                Username = "tester",
                Email = "tester@example.com",
                PasswordHash = "hash",
                SecurityStamp = "stamp",
                EmailVerified = true,
                IsActive = true,
                Tier = _tier,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        public Task<long> UpsertAsync(User user) => throw new NotImplementedException();
        public Task<User?> GetByEmailAsync(string email) => throw new NotImplementedException();
        public Task<User?> GetByUsernameAsync(string username) => throw new NotImplementedException();
        public Task<bool> UsernameExistsAsync(string username) => throw new NotImplementedException();
        public Task<bool> EmailExistsAsync(string email) => throw new NotImplementedException();
        public Task<long> GetActiveUserCountAsync() => throw new NotImplementedException();
        public Task UpdateEmailVerifiedAsync(long userId, bool verified) => throw new NotImplementedException();
        public Task UpdatePasswordHashAsync(long userId, string passwordHash) => throw new NotImplementedException();
        public Task<string?> GetSecurityStampAsync(long userId) => throw new NotImplementedException();
        public Task<bool> DeleteUserAsync(long userId) => throw new NotImplementedException();
    }
}
