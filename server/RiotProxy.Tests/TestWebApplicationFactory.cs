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
using RiotProxy.Infrastructure.Security;
using RiotProxy.External.Domain.Entities;
using RiotProxy.External.Domain.Enums;
using Microsoft.Extensions.Hosting;

namespace RiotProxy.Tests;

internal sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly IDictionary<string, string?> _overrides;
    private readonly FakeV2UsersRepository _v2UsersRepository;

    public TestWebApplicationFactory(IDictionary<string, string?>? overrides = null)
    {
        _overrides = overrides ?? new Dictionary<string, string?>();
        _v2UsersRepository = new FakeV2UsersRepository();
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Ensure process environment reflects testing to allow Secrets.Initialize reinitialization
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Testing");

        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            // Test encryption key (32 bytes base64-encoded) - only for testing
            // "test-encryption-key-32bytes!!!!!" (32 chars) -> base64
            const string testEmailEncryptionKey = "dGVzdC1lbmNyeXB0aW9uLWtleS0zMmJ5dGVzISEhISE=";

            var defaults = new Dictionary<string, string?>
            {
                ["Auth:EnableMvpLogin"] = "true",
                ["Auth:CookieName"] = "pulse-auth",
                ["Auth:SessionTimeout"] = "30",
                ["Jobs:EnableMatchHistorySync"] = "false",
                ["RIOT_API_KEY"] = "test-key",
                ["LOL_DB_CONNECTIONSTRING"] = "Server=localhost;Port=3306;Database=test;User Id=test;Password=test;",
                ["LOL_DB_CONNECTIONSTRING_V2"] = "Server=localhost;Port=3306;Database=test;User Id=test;Password=test;",
                ["Security:EmailEncryptionKey"] = testEmailEncryptionKey
            };

            config.AddInMemoryCollection(defaults);
            if (_overrides.Count > 0)
            {
                config.AddInMemoryCollection(_overrides);
            }
        });

        builder.ConfigureServices(services =>
        {
            // Replace V2UsersRepository with a fake to avoid real DB connections
            services.RemoveAll<UsersRepository>();
            services.AddSingleton<UsersRepository>(_v2UsersRepository);
        });

        return base.CreateHost(builder);
    }

    internal sealed class FakeV2UsersRepository : UsersRepository
    {
        private readonly ConcurrentDictionary<string, User> _usersByUsername = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, User> _usersByEmail = new(StringComparer.OrdinalIgnoreCase);
        private long _nextId = 1;

        public FakeV2UsersRepository() : base(null!, new FakeEmailEncryptor())
        {
            // Pre-populate with a test user (password: "test-password")
            var testUser = new User
            {
                UserId = _nextId++,
                Username = "tester",
                Email = "tester@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("test-password"),
                EmailVerified = true,
                IsActive = true,
                Tier = "free",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _usersByUsername["tester"] = testUser;
            _usersByEmail["tester@test.com"] = testUser;
        }

        public override Task<User?> GetByUsernameAsync(string username)
        {
            _usersByUsername.TryGetValue(username, out var user);
            return Task.FromResult(user);
        }

        public override Task<User?> GetByEmailAsync(string email)
        {
            _usersByEmail.TryGetValue(email, out var user);
            return Task.FromResult(user);
        }

        public override Task<long> UpsertAsync(User user)
        {
            if (user.UserId == 0)
            {
                user.UserId = _nextId++;
            }
            _usersByUsername[user.Username] = user;
            _usersByEmail[user.Email] = user;
            return Task.FromResult(user.UserId);
        }
    }

    /// <summary>
    /// Fake email encryptor for testing that doesn't actually encrypt.
    /// Just passes through the email as-is (or with a simple marker).
    /// </summary>
    private sealed class FakeEmailEncryptor : IEmailEncryptor
    {
        public string Encrypt(string email) => $"encrypted:{email.ToLowerInvariant().Trim()}";
        public string Decrypt(string encryptedEmail) =>
            encryptedEmail.StartsWith("encrypted:")
                ? encryptedEmail.Substring("encrypted:".Length)
                : encryptedEmail;
    }
}
