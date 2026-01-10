using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RiotProxy.Infrastructure.External.Database.Repositories;
using RiotProxy.Infrastructure.External;
using RiotProxy.External.Domain.Entities;
using RiotProxy.External.Domain.Enums;
using Microsoft.Extensions.Hosting;

namespace RiotProxy.Tests;

internal sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly IDictionary<string, string?> _overrides;
    private readonly IUserRepository _userRepository;

    public TestWebApplicationFactory(
        IDictionary<string, string?>? overrides = null,
        IUserRepository? userRepository = null)
    {
        _overrides = overrides ?? new Dictionary<string, string?>();
        _userRepository = userRepository ?? new FakeUserRepository();
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Ensure process environment reflects testing to allow Secrets.Initialize reinitialization
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Testing");

        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            var defaults = new Dictionary<string, string?>
            {
                ["Auth:EnableMvpLogin"] = "true",
                ["Auth:DevPassword"] = "dev-secret",
                ["Auth:CookieName"] = "pulse-auth",
                ["Auth:SessionTimeout"] = "30",
                ["Jobs:EnableMatchHistorySync"] = "false",
                ["RIOT_API_KEY"] = "test-key",
                ["LOL_DB_CONNECTIONSTRING"] = "Server=localhost;Port=3306;Database=test;User Id=test;Password=test;",
                ["LOL_DB_CONNECTIONSTRING_V2"] = "Server=localhost;Port=3306;Database=test;User Id=test;Password=test;"
            };

            config.AddInMemoryCollection(defaults);
            if (_overrides.Count > 0)
            {
                config.AddInMemoryCollection(_overrides);
            }
        });

        builder.ConfigureServices(services =>
        {
            // Replace the user repository with a test double to avoid DB access.
            services.RemoveAll<IUserRepository>();
            services.AddSingleton(_userRepository);
        });

        return base.CreateHost(builder);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly ConcurrentDictionary<string, User> _users = new(StringComparer.OrdinalIgnoreCase);

        public FakeUserRepository()
        {
            _users.TryAdd("tester", new User { UserId = 1, UserName = "tester", UserType = UserTypeEnum.Solo });
        }

        public Task<IList<User>> GetAllUsersAsync() => Task.FromResult<IList<User>>(_users.Values.ToList());

        public Task<User?> GetByUserNameAsync(string userName)
        {
            _users.TryGetValue(userName, out var user);
            return Task.FromResult<User?>(user);
        }

        public Task<User?> CreateUserAsync(string userName, UserTypeEnum userType)
        {
            var user = new User { UserId = _users.Count + 1, UserName = userName, UserType = userType };
            _users[userName] = user;
            return Task.FromResult<User?>(user);
        }
    }
}
