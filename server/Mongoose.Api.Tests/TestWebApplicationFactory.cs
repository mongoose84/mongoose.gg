using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Core.QueryModels;
using Mongoose.Api.Infrastructure.Database.Repositories;
using Mongoose.Api.Infrastructure.Security;
using Mongoose.Api.Infrastructure.Email;
using Mongoose.Api.Infrastructure.Jobs;
using Mongoose.Api.Infrastructure.Riot;
using Mongoose.Api.Application.DTOs;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Text.Json;
using static Mongoose.Api.Application.DTOs.SoloPerformanceDto;
using static Mongoose.Api.Application.DTOs.SoloMatchupsDto;
using static Mongoose.Api.Application.DTOs.ChampionSelectDto;
using static Mongoose.Api.Application.DTOs.TrendDto;
using static Mongoose.Api.Application.DTOs.Solo.RadarChartDto;

namespace Mongoose.Api.Tests;

internal sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly IDictionary<string, string?> _overrides;
    private readonly FakeUsersRepository _usersRepository;
    private readonly FakeVerificationTokensRepository _tokensRepository;
    private readonly FakeEmailService _emailService;
    private readonly FakeRiotApiClient _riotApiClient;
    private readonly FakeRiotAccountsRepository _riotAccountsRepository;
    private readonly FakeUserRiotAccountsRepository _userRiotAccountsRepository;
    private readonly FakeOverviewStatsRepository _overviewStatsRepository;
    private readonly FakeAnalyticsEventsRepository _analyticsEventsRepository;
    private readonly FakeGitHubService _gitHubService;
    private readonly FakeMatchesRepository _matchesRepository;
    private readonly FakeSoloPerformanceRepository _soloPerformanceRepository;
    private readonly FakeMatchupRepository _matchupRepository;
    private readonly FakeChampionSelectRepository _championSelectRepository;
    private readonly FakeTrendRepository _trendRepository;
    private readonly FakeRadarChartRepository _radarChartRepository;
    private readonly FakeDeathPositionsRepository _deathPositionsRepository;

    public FakeUsersRepository UsersRepository => _usersRepository;
    public FakeVerificationTokensRepository TokensRepository => _tokensRepository;
    public FakeEmailService EmailService => _emailService;
    public FakeRiotApiClient RiotApiClient => _riotApiClient;
    public FakeRiotAccountsRepository RiotAccountsRepository => _riotAccountsRepository;
    public FakeUserRiotAccountsRepository UserRiotAccountsRepository => _userRiotAccountsRepository;
    public FakeOverviewStatsRepository OverviewStatsRepository => _overviewStatsRepository;
    public FakeAnalyticsEventsRepository AnalyticsEventsRepository => _analyticsEventsRepository;
    public FakeGitHubService GitHubService => _gitHubService;
    public FakeMatchesRepository MatchesRepository => _matchesRepository;
    public FakeSoloPerformanceRepository SoloPerformanceRepository => _soloPerformanceRepository;
    public FakeMatchupRepository MatchupRepository => _matchupRepository;
    public FakeChampionSelectRepository ChampionSelectRepository => _championSelectRepository;
    public FakeTrendRepository TrendRepository => _trendRepository;
    public FakeRadarChartRepository RadarChartRepository => _radarChartRepository;
    public FakeDeathPositionsRepository DeathPositionsRepository => _deathPositionsRepository;

    public TestWebApplicationFactory(IDictionary<string, string?>? overrides = null)
    {
        _overrides = overrides ?? new Dictionary<string, string?>();
        _usersRepository = new FakeUsersRepository();
        _tokensRepository = new FakeVerificationTokensRepository();
        _emailService = new FakeEmailService();
        _riotApiClient = new FakeRiotApiClient();
        _riotAccountsRepository = new FakeRiotAccountsRepository();
        _userRiotAccountsRepository = new FakeUserRiotAccountsRepository(_riotAccountsRepository);
        _overviewStatsRepository = new FakeOverviewStatsRepository();
        _analyticsEventsRepository = new FakeAnalyticsEventsRepository();
        _gitHubService = new FakeGitHubService();
        _matchesRepository = new FakeMatchesRepository(_riotAccountsRepository);
        _soloPerformanceRepository = new FakeSoloPerformanceRepository();
        _matchupRepository = new FakeMatchupRepository();
        _championSelectRepository = new FakeChampionSelectRepository();
        _trendRepository = new FakeTrendRepository();
        _radarChartRepository = new FakeRadarChartRepository();
        _deathPositionsRepository = new FakeDeathPositionsRepository();
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Ensure process environment reflects testing to allow Secrets.Initialize reinitialization
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Testing");

        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            // Test encryption key — computed at runtime so static scanners do not flag it as a secret
            var testEncryptionSecret = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("test-only-not-a-real-secret-1234"));

            var defaults = new Dictionary<string, string?>
            {
                ["Auth:EnableMvpLogin"] = "true",
                ["Auth:CookieName"] = "mongoose-auth",
                ["Auth:SessionTimeout"] = "30",
                ["Jobs:EnableMatchHistorySync"] = "false",
                ["Jobs:EnableMatchCleanup"] = "false",
                ["RIOT_API_KEY"] = "test-key",
                ["Database_test"] = "Server=localhost;Port=3306;Database=test;User Id=test;Password=test;",
                ["Security:EncryptionSecret"] = testEncryptionSecret
            };

            config.AddInMemoryCollection(defaults);
            if (_overrides.Count > 0)
            {
                config.AddInMemoryCollection(_overrides);
            }
        });

        builder.ConfigureServices(services =>
        {
            // Remove background jobs in integration tests to avoid noisy logs and side effects
            for (var index = services.Count - 1; index >= 0; index--)
            {
                var descriptor = services[index];
                if (descriptor.ServiceType != typeof(IHostedService))
                {
                    continue;
                }

                if (descriptor.ImplementationType == typeof(MatchHistorySyncJob)
                    || descriptor.ImplementationType == typeof(MatchCleanupJob))
                {
                    services.RemoveAt(index);
                }
            }

            // Replace UsersRepository with a fake to avoid real DB connections
            services.RemoveAll<IUsersRepository>();
            services.AddSingleton<IUsersRepository>(_usersRepository);

            // Replace VerificationTokensRepository with a fake
            services.RemoveAll<IVerificationTokensRepository>();
            services.AddSingleton<IVerificationTokensRepository>(_tokensRepository);

            // Replace IEmailService with a fake
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService>(_emailService);

            // Replace IRiotApiClient with a fake
            services.RemoveAll<IRiotApiClient>();
            services.AddSingleton<IRiotApiClient>(_riotApiClient);

            // Replace RiotAccountsRepository with a fake
            services.RemoveAll<IRiotAccountsRepository>();
            services.AddSingleton<IRiotAccountsRepository>(_riotAccountsRepository);

            // Replace UserRiotAccountsRepository with a fake
            services.RemoveAll<IUserRiotAccountsRepository>();
            services.AddSingleton<IUserRiotAccountsRepository>(_userRiotAccountsRepository);

            // Replace OverviewStatsRepository with a fake
            services.RemoveAll<IOverviewStatsRepository>();
            services.AddSingleton<IOverviewStatsRepository>(_overviewStatsRepository);

            // Replace AnalyticsEventsRepository with a fake
            services.RemoveAll<IAnalyticsEventsRepository>();
            services.AddSingleton<IAnalyticsEventsRepository>(_analyticsEventsRepository);

            // Replace IGitHubService with a fake
            services.RemoveAll<IGitHubService>();
            services.AddSingleton<IGitHubService>(_gitHubService);

            // Replace MatchesRepository with a fake (only register as IMatchesRepository since
            // FakeMatchesRepository implements the interface directly, not the concrete class)
            services.RemoveAll<IMatchesRepository>();
            services.AddSingleton<IMatchesRepository>(_matchesRepository);

            // Replace ISoloPerformanceRepository with a fake
            services.RemoveAll<ISoloPerformanceRepository>();
            services.AddSingleton<ISoloPerformanceRepository>(_soloPerformanceRepository);

            // Replace IMatchupRepository with a fake
            services.RemoveAll<IMatchupRepository>();
            services.AddSingleton<IMatchupRepository>(_matchupRepository);

            // Replace IChampionSelectRepository with a fake
            services.RemoveAll<IChampionSelectRepository>();
            services.AddSingleton<IChampionSelectRepository>(_championSelectRepository);

            // Replace ITrendRepository with a fake
            services.RemoveAll<ITrendRepository>();
            services.AddSingleton<ITrendRepository>(_trendRepository);

            // Replace IRadarChartRepository with a fake
            services.RemoveAll<IRadarChartRepository>();
            services.AddSingleton<IRadarChartRepository>(_radarChartRepository);

            // Replace IDeathPositionsRepository with a fake
            services.RemoveAll<IDeathPositionsRepository>();
            services.AddSingleton<IDeathPositionsRepository>(_deathPositionsRepository);
        });

        return base.CreateHost(builder);
    }

    internal sealed class FakeUsersRepository : UsersRepository
    {
        private readonly ConcurrentDictionary<string, User> _usersByUsername = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, User> _usersByEmail = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<long, User> _usersById = new();
        private long _nextId = 1;

        public FakeUsersRepository() : base(null!, new FakeEncryptor())
        {
            // Pre-populate with a test user (password: "test-password")
            var testUser = new User
            {
                UserId = _nextId++,
                Username = "tester",
                Email = "tester@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("test-password"),
                SecurityStamp = Guid.NewGuid().ToString(),
                EmailVerified = true,
                IsActive = true,
                Tier = "free",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _usersByUsername["tester"] = testUser;
            _usersByEmail["tester@test.com"] = testUser;
            _usersById[testUser.UserId] = testUser;
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
            _usersById[user.UserId] = user;
            return Task.FromResult(user.UserId);
        }

        public override Task<User?> GetByIdAsync(long userId)
        {
            _usersById.TryGetValue(userId, out var user);
            return Task.FromResult(user);
        }

        public override Task UpdateEmailVerifiedAsync(long userId, bool verified)
        {
            if (_usersById.TryGetValue(userId, out var user))
            {
                user.EmailVerified = verified;
            }
            return Task.CompletedTask;
        }

        public override Task UpdatePasswordHashAsync(long userId, string passwordHash)
        {
            if (_usersById.TryGetValue(userId, out var user))
            {
                user.PasswordHash = passwordHash;
                user.SecurityStamp = Guid.NewGuid().ToString();
            }
            return Task.CompletedTask;
        }

        public override Task<string?> GetSecurityStampAsync(long userId)
        {
            _usersById.TryGetValue(userId, out var user);
            return Task.FromResult(user?.SecurityStamp);
        }

        public override Task<bool> DeleteUserAsync(long userId)
        {
            if (!_usersById.TryRemove(userId, out var removedUser))
            {
                return Task.FromResult(false);
            }

            _usersByUsername.TryRemove(removedUser.Username, out _);
            _usersByEmail.TryRemove(removedUser.Email, out _);
            return Task.FromResult(true);
        }

        public void AddUnverifiedUser(string username, string email, string password)
        {
            var user = new User
            {
                UserId = _nextId++,
                Username = username,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                SecurityStamp = Guid.NewGuid().ToString(),
                EmailVerified = false,
                IsActive = true,
                Tier = "free",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _usersByUsername[username] = user;
            _usersByEmail[email] = user;
            _usersById[user.UserId] = user;
        }

        public void AddInactiveUser(string username, string email, string password)
        {
            var user = new User
            {
                UserId = _nextId++,
                Username = username,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                SecurityStamp = Guid.NewGuid().ToString(),
                EmailVerified = true,
                IsActive = false,
                Tier = "free",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _usersByUsername[username] = user;
            _usersByEmail[email] = user;
            _usersById[user.UserId] = user;
        }

        public void SetSecurityStamp(string username, string securityStamp)
        {
            if (_usersByUsername.TryGetValue(username, out var user))
            {
                user.SecurityStamp = securityStamp;
            }
        }

        public void SetTier(string username, string tier)
        {
            if (_usersByUsername.TryGetValue(username, out var user))
            {
                user.Tier = tier;
            }
        }

        public override Task<bool> UsernameExistsAsync(string username)
        {
            return Task.FromResult(_usersByUsername.ContainsKey(username));
        }

        public override Task<bool> EmailExistsAsync(string email)
        {
            return Task.FromResult(_usersByEmail.ContainsKey(email));
        }

        public override Task<long> GetActiveUserCountAsync()
        {
            var count = _usersById.Values.Count(u => u.IsActive);
            return Task.FromResult((long)count);
        }
    }

    /// <summary>
    /// Fake encryptor for testing that doesn't actually encrypt.
    /// Just passes through the value as-is (or with a simple marker).
    /// </summary>
    private sealed class FakeEncryptor : IEncryptor
    {
        public string Encrypt(string input) => $"encrypted:{input.ToLowerInvariant().Trim()}";
        public string EncryptPreserveCase(string input) => $"encrypted:{input.Trim()}";
        public string Decrypt(string encryptedInput) =>
            encryptedInput.StartsWith("encrypted:")
                ? encryptedInput.Substring("encrypted:".Length)
                : encryptedInput;
    }

    /// <summary>
    /// Fake verification tokens repository for testing.
    /// </summary>
    internal sealed class FakeVerificationTokensRepository : VerificationTokensRepository
    {
        private readonly ConcurrentDictionary<long, VerificationToken> _tokens = new();
        private long _nextId = 1;

        public FakeVerificationTokensRepository() : base(null!) { }

        public override Task<long> CreateTokenAsync(long userId, string tokenType, string code, DateTime expiresAt)
        {
            var token = new VerificationToken
            {
                Id = _nextId++,
                UserId = userId,
                TokenType = tokenType,
                Code = code,
                ExpiresAt = expiresAt,
                UsedAt = null,
                Attempts = 0,
                CreatedAt = DateTime.UtcNow
            };
            _tokens[token.Id] = token;
            return Task.FromResult(token.Id);
        }

        public override Task<VerificationToken?> GetActiveTokenAsync(long userId, string tokenType)
        {
            var token = _tokens.Values
                .Where(t => t.UserId == userId && t.TokenType == tokenType && t.UsedAt == null && t.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefault();
            return Task.FromResult(token);
        }

        public override Task MarkTokenAsUsedAsync(long tokenId)
        {
            if (_tokens.TryGetValue(tokenId, out var token))
            {
                token.UsedAt = DateTime.UtcNow;
            }
            return Task.CompletedTask;
        }

        public override Task IncrementAttemptsAsync(long tokenId)
        {
            if (_tokens.TryGetValue(tokenId, out var token))
            {
                token.Attempts++;
            }
            return Task.CompletedTask;
        }

        public override Task<int> CountRecentTokensAsync(long userId, string tokenType, int seconds)
        {
            var since = DateTime.UtcNow.AddSeconds(-seconds);
            var count = _tokens.Values.Count(t => t.UserId == userId && t.TokenType == tokenType && t.CreatedAt > since);
            return Task.FromResult(count);
        }

        public override Task InvalidateActiveTokensAsync(long userId, string tokenType)
        {
            foreach (var token in _tokens.Values.Where(t => t.UserId == userId && t.TokenType == tokenType && t.UsedAt == null))
            {
                token.UsedAt = DateTime.UtcNow;
            }
            return Task.CompletedTask;
        }

        public void AddToken(long userId, string tokenType, string code, DateTime expiresAt)
        {
            var token = new VerificationToken
            {
                Id = _nextId++,
                UserId = userId,
                TokenType = tokenType,
                Code = code,
                ExpiresAt = expiresAt,
                UsedAt = null,
                Attempts = 0,
                CreatedAt = DateTime.UtcNow
            };
            _tokens[token.Id] = token;
        }

        public VerificationToken? GetToken(long tokenId)
        {
            _tokens.TryGetValue(tokenId, out var token);
            return token;
        }

        public IEnumerable<VerificationToken> GetAllTokensForUser(long userId)
        {
            return _tokens.Values.Where(t => t.UserId == userId);
        }

        /// <summary>
        /// Helper method for testing: sets the attempt count on a token.
        /// </summary>
        public void SetTokenAttempts(long tokenId, int attempts)
        {
            if (_tokens.TryGetValue(tokenId, out var token))
            {
                token.Attempts = attempts;
            }
        }
    }

    /// <summary>
    /// Fake email service for testing.
    /// </summary>
    internal sealed class FakeEmailService : IEmailService
    {
        private readonly List<SentEmail> _sentEmails = new();
        private readonly List<SentPasswordResetEmail> _sentPasswordResetEmails = new();
        private readonly SemaphoreSlim _verificationEmailSignal = new(0);

        public IReadOnlyList<SentEmail> SentEmails => _sentEmails;
        public IReadOnlyList<SentPasswordResetEmail> SentPasswordResetEmails => _sentPasswordResetEmails;

        public Task SendVerificationEmailAsync(string toEmail, string username, string verificationCode)
        {
            _sentEmails.Add(new SentEmail(toEmail, username, verificationCode));
            _verificationEmailSignal.Release();
            return Task.CompletedTask;
        }

        public Task SendPasswordResetEmailAsync(string toEmail, string username, string resetCode)
        {
            _sentPasswordResetEmails.Add(new SentPasswordResetEmail(toEmail, username, resetCode));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Waits until a verification email has been recorded. Use this instead of Task.Delay
        /// to create a deterministic sync point for fire-and-forget email sends.
        /// </summary>
        public Task WaitForVerificationEmailAsync(TimeSpan? timeout = null) =>
            _verificationEmailSignal.WaitAsync(timeout ?? TimeSpan.FromSeconds(5));

        public void Clear()
        {
            _sentEmails.Clear();
            _sentPasswordResetEmails.Clear();
        }

        public record SentEmail(string ToEmail, string Username, string VerificationCode);
        public record SentPasswordResetEmail(string ToEmail, string Username, string ResetCode);
    }

    /// <summary>
    /// Fake Riot API client for testing.
    /// </summary>
    internal sealed class FakeRiotApiClient : IRiotApiClient
    {
        private readonly ConcurrentDictionary<string, string> _puuidByRiotId = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, byte> _notFoundRiotIds = new(StringComparer.OrdinalIgnoreCase);

        public event EventHandler<RateLimitWaitEventArgs>? RateLimitWaitStarted;

        private void NotifyRateLimitWaitStarted(RateLimitWaitEventArgs args)
        {
            RateLimitWaitStarted?.Invoke(this, args);
        }

        public Task<double> GetWinrateAsync(string puuid)
        {
            return Task.FromResult(50.0);
        }

        public Task<string> GetPuuIdAsync(string gameName, string tagLine, CancellationToken ct = default)
        {
            var key = BuildRiotIdKey(gameName, tagLine);
            if (_notFoundRiotIds.ContainsKey(key))
            {
                throw new HttpRequestException("Not Found", null, System.Net.HttpStatusCode.NotFound);
            }

            if (_puuidByRiotId.TryGetValue(key, out var mappedPuuid))
            {
                return Task.FromResult(mappedPuuid);
            }

            var generated = $"test-puuid-{gameName.Trim().ToLowerInvariant()}-{tagLine.Trim().ToLowerInvariant()}";
            _puuidByRiotId[key] = generated;
            return Task.FromResult(generated);
        }

        public Task<JsonDocument> GetMatchHistoryAsync(string puuid, int start = 0, int count = 100, long? startTime = null, CancellationToken ct = default)
        {
            return Task.FromResult(JsonDocument.Parse("[]"));
        }

        public Task<JsonDocument> GetMatchInfoAsync(string matchId, CancellationToken ct = default)
        {
            return Task.FromResult(JsonDocument.Parse("{}"));
        }

        public Task<JsonDocument> GetMatchTimelineAsync(string matchId, CancellationToken ct = default)
        {
            return Task.FromResult(JsonDocument.Parse("{}"));
        }

        public Task<JsonDocument> GetSummonerByPuuIdAsync(string tagline, string puuid, CancellationToken ct = default)
        {
            return Task.FromResult(JsonDocument.Parse("{}"));
        }

        public Task<JsonDocument> GetLeagueEntriesBySummonerIdAsync(string region, string summonerId, CancellationToken ct = default)
        {
            return Task.FromResult(JsonDocument.Parse("[]"));
        }

        public Task<JsonDocument> GetLeagueEntriesByPuuidAsync(string region, string puuid, CancellationToken ct = default)
        {
            return Task.FromResult(JsonDocument.Parse("[]"));
        }

        public Task<string> GetLolVersionAsync(CancellationToken ct = default)
        {
            return Task.FromResult("14.1.1");
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public void MapRiotIdToPuuid(string gameName, string tagLine, string puuid)
        {
            _puuidByRiotId[BuildRiotIdKey(gameName, tagLine)] = puuid;
        }

        public void SimulateRiotNotFound(string gameName, string tagLine)
        {
            _notFoundRiotIds[BuildRiotIdKey(gameName, tagLine)] = 0;
        }

        private static string BuildRiotIdKey(string gameName, string tagLine)
        {
            return $"{gameName.Trim().ToLowerInvariant()}#{tagLine.Trim().ToLowerInvariant()}";
        }
    }

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

    /// <summary>
    /// Fake overview stats repository for testing.
    /// </summary>
    internal sealed class FakeOverviewStatsRepository : OverviewStatsRepository
    {
        private readonly ConcurrentDictionary<string, List<MatchResultData>> _matchesByPuuid = new();
        private readonly ConcurrentDictionary<string, LastMatchData> _lastMatchByPuuid = new();
        private readonly ConcurrentDictionary<string, MostPlayedChampionData> _mostPlayedChampionByPuuid = new();
        private int _defaultQueueId = 420;
        private string _defaultQueueLabel = "Ranked Solo/Duo";

        public FakeOverviewStatsRepository() : base(null!, null!) { }

        public override Task<(int QueueId, string QueueLabel, int MatchCount)> GetPrimaryQueueAsync(string puuid)
        {
            if (_matchesByPuuid.TryGetValue(puuid, out var matches))
            {
                return Task.FromResult((_defaultQueueId, _defaultQueueLabel, matches.Count));
            }
            return Task.FromResult((_defaultQueueId, _defaultQueueLabel, 0));
        }

        public override Task<(int QueueId, string QueueLabel, int MatchCount)> GetPrimaryQueueAsync(IReadOnlyList<string> puuids)
        {
            var matchCount = puuids
                .Where(puuid => _matchesByPuuid.TryGetValue(puuid, out _))
                .Sum(puuid => _matchesByPuuid[puuid].Count);

            return Task.FromResult((_defaultQueueId, _defaultQueueLabel, matchCount));
        }

        public override Task<List<MatchResultData>> GetLast20MatchesAsync(string puuid, int queueId)
        {
            if (_matchesByPuuid.TryGetValue(puuid, out var matches))
            {
                return Task.FromResult(matches.Take(20).ToList());
            }
            return Task.FromResult(new List<MatchResultData>());
        }

        public override Task<List<MatchResultData>> GetLast20MatchesAsync(IReadOnlyList<string> puuids, int queueId)
        {
            var matches = puuids
                .Where(puuid => _matchesByPuuid.TryGetValue(puuid, out _))
                .SelectMany(puuid => _matchesByPuuid[puuid])
                .OrderByDescending(match => match.GameStartTime)
                .Take(20)
                .ToList();

            return Task.FromResult(matches);
        }

        public override Task<LastMatchData?> GetLastMatchAsync(string puuid)
        {
            _lastMatchByPuuid.TryGetValue(puuid, out var lastMatch);
            return Task.FromResult(lastMatch);
        }

        public override Task<LastMatchData?> GetLastMatchAsync(IReadOnlyList<string> puuids)
        {
            var match = puuids
                .Where(puuid => _lastMatchByPuuid.TryGetValue(puuid, out _))
                .Select(puuid => _lastMatchByPuuid[puuid])
                .OrderByDescending(lastMatch => lastMatch.GameStartTime)
                .FirstOrDefault();

            return Task.FromResult(match);
        }

        public override Task<MostPlayedChampionData?> GetMostPlayedChampionAsync(string puuid)
        {
            _mostPlayedChampionByPuuid.TryGetValue(puuid, out var mostPlayedChampion);
            return Task.FromResult(mostPlayedChampion);
        }

        public override Task<MostPlayedChampionData?> GetMostPlayedChampionAsync(IReadOnlyList<string> puuids)
        {
            foreach (var puuid in puuids)
            {
                if (_mostPlayedChampionByPuuid.TryGetValue(puuid, out var mostPlayedChampion))
                {
                    return Task.FromResult<MostPlayedChampionData?>(mostPlayedChampion);
                }
            }

            return Task.FromResult<MostPlayedChampionData?>(null);
        }

        public override Task<int?> GetCurrentLpAsync(string puuid, int queueId)
        {
            if (_matchesByPuuid.TryGetValue(puuid, out var matches) && matches.Count > 0)
            {
                return Task.FromResult(matches.First().LpAfter);
            }
            return Task.FromResult<int?>(null);
        }

        /// <summary>
        /// Sets the default queue for the fake repository.
        /// </summary>
        public void SetDefaultQueue(int queueId, string queueLabel)
        {
            _defaultQueueId = queueId;
            _defaultQueueLabel = queueLabel;
        }

        /// <summary>
        /// Adds match result data for a player.
        /// </summary>
        public void AddMatchResult(string puuid, string matchId, bool win, int? lpAfter, long gameStartTime)
        {
            if (!_matchesByPuuid.TryGetValue(puuid, out var matches))
            {
                matches = new List<MatchResultData>();
                _matchesByPuuid[puuid] = matches;
            }
            matches.Add(new MatchResultData(matchId, win, lpAfter, gameStartTime));
        }

        /// <summary>
        /// Sets the last match for a player.
        /// </summary>
        public void SetLastMatch(string puuid, string matchId, int championId, string championName,
            bool win, int kills, int deaths, int assists, long gameStartTime, int queueId = 420)
        {
            _lastMatchByPuuid[puuid] = new LastMatchData(matchId, championId, championName, win, kills, deaths, assists, gameStartTime, queueId);
        }

        /// <summary>
        /// Sets the most played champion for a player.
        /// </summary>
        public void SetMostPlayedChampion(string puuid, string championName, int gamesPlayed)
        {
            _mostPlayedChampionByPuuid[puuid] = new MostPlayedChampionData(championName, gamesPlayed);
        }
    }

    /// <summary>
    /// Fake analytics events repository for testing.
    /// </summary>
    internal sealed class FakeAnalyticsEventsRepository : AnalyticsEventsRepository
    {
        private readonly ConcurrentDictionary<long, AnalyticsEvent> _events = new();
        private long _nextId = 1;

        public FakeAnalyticsEventsRepository() : base(null!) { }

        public override Task<int> InsertAsync(AnalyticsEvent evt)
        {
            evt.Id = _nextId++;
            _events[evt.Id] = evt;
            return Task.FromResult(1);
        }

        public override Task<int> InsertBatchAsync(IEnumerable<AnalyticsEvent> events)
        {
            var count = 0;
            foreach (var evt in events)
            {
                evt.Id = _nextId++;
                _events[evt.Id] = evt;
                count++;
            }
            return Task.FromResult(count);
        }

        public override Task<long> GetEventCountAsync(string eventName, DateTime from, DateTime to)
        {
            var count = _events.Values.Count(e =>
                e.EventName == eventName &&
                e.CreatedAt >= from &&
                e.CreatedAt <= to);
            return Task.FromResult((long)count);
        }

        public override Task<long> GetUniqueUserCountAsync(string eventName, DateTime from, DateTime to)
        {
            var count = _events.Values
                .Where(e => e.EventName == eventName &&
                           e.CreatedAt >= from &&
                           e.CreatedAt <= to &&
                           e.UserId != null)
                .Select(e => e.UserId)
                .Distinct()
                .Count();
            return Task.FromResult((long)count);
        }

        public IReadOnlyCollection<AnalyticsEvent> GetAllEvents() => _events.Values.ToList();

        public void Clear() => _events.Clear();
    }

    /// <summary>
    /// Fake GitHub service for testing feedback endpoint.
    /// </summary>
    internal sealed class FakeGitHubService : IGitHubService
    {
        private readonly List<CreatedIssue> _createdIssues = new();
        private bool _shouldFail;
        private string? _failureMessage;
        private bool _isConfigured = true;

        public bool IsConfigured => _isConfigured;

        public IReadOnlyList<CreatedIssue> CreatedIssues => _createdIssues;

        public Task<GitHubIssueResult> CreateIssueAsync(string title, string body, IEnumerable<string> labels)
        {
            if (!_isConfigured)
            {
                return Task.FromResult(new GitHubIssueResult(false, "GitHub integration is not configured"));
            }

            if (_shouldFail)
            {
                return Task.FromResult(new GitHubIssueResult(false, _failureMessage ?? "Simulated failure"));
            }

            _createdIssues.Add(new CreatedIssue(title, body, labels.ToArray()));
            return Task.FromResult(new GitHubIssueResult(true));
        }

        /// <summary>
        /// Configures the fake to return a failure on the next issue creation.
        /// </summary>
        public void SetupFailure(string? message = null)
        {
            _shouldFail = true;
            _failureMessage = message;
        }

        /// <summary>
        /// Configures the fake to simulate an unconfigured GitHub service.
        /// </summary>
        public void SetupNotConfigured()
        {
            _isConfigured = false;
        }

        /// <summary>
        /// Resets the fake to its default state.
        /// </summary>
        public void Reset()
        {
            _shouldFail = false;
            _failureMessage = null;
            _isConfigured = true;
            _createdIssues.Clear();
        }

        public record CreatedIssue(string Title, string Body, string[] Labels);
    }

    /// <summary>
    /// Storage record for fake match data in tests.
    /// </summary>
    public record FakeMatchData(
        string MatchId,
        int QueueId,
        long GameStartTime,
        int GameDurationSec
    );

    /// <summary>
    /// Storage record for fake participant data in tests.
    /// </summary>
    public record FakeParticipantData(
        string MatchId,
        string Puuid,
        int ChampionId,
        string ChampionName,
        string Role,
        string? Lane,
        bool Win,
        int Kills,
        int Deaths,
        int Assists,
        int CreepScore,
        int GoldEarned,
        int TeamId,
        int? GoldDiffAt10 = null,
        int? GoldAt10 = null,
        int? CsAt10 = null,
        int? CsDiffAt10 = null,
        int DamageDealt = 0,
        int DamageTaken = 0,
        int VisionScore = 0,
        decimal KillParticipation = 0,
        decimal DamageShare = 0,
        int DeathsPre10 = 0
    );

    /// <summary>
    /// Fake matches repository for testing match endpoints.
    /// Implements IMatchesRepository directly since MatchesRepository methods aren't virtual.
    /// </summary>
    internal sealed class FakeMatchesRepository : IMatchesRepository
    {
        private readonly ConcurrentDictionary<string, FakeMatchData> _matches = new();
        private readonly ConcurrentDictionary<string, List<FakeParticipantData>> _participants = new();
        private readonly ConcurrentDictionary<string, Dictionary<string, RoleBaseline>> _baselines = new();
        private readonly FakeRiotAccountsRepository _riotAccountsRepo;

        public FakeMatchesRepository(FakeRiotAccountsRepository riotAccountsRepo)
        {
            _riotAccountsRepo = riotAccountsRepo;
        }

        /// <summary>
        /// Helper to add a match for testing.
        /// </summary>
        public void AddMatch(string matchId, int queueId = 420, long? gameStartTime = null, int gameDurationSec = 1800)
        {
            var match = new FakeMatchData(
                MatchId: matchId,
                QueueId: queueId,
                GameStartTime: gameStartTime ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                GameDurationSec: gameDurationSec
            );
            _matches[matchId] = match;
            _participants[matchId] = new List<FakeParticipantData>();
        }

        /// <summary>
        /// Helper to add a participant to a match.
        /// </summary>
        public void AddParticipant(FakeParticipantData participant)
        {
            if (!_participants.TryGetValue(participant.MatchId, out var list))
            {
                list = new List<FakeParticipantData>();
                _participants[participant.MatchId] = list;
            }
            list.Add(participant);
        }

        /// <summary>
        /// Helper to set role baselines for a puuid.
        /// </summary>
        public void SetBaselines(string puuid, Dictionary<string, RoleBaseline> baselines)
        {
            _baselines[puuid] = baselines;
        }

        public Task<IList<MatchListSummaryItem>> GetMatchListSummaryAsync(
            string puuid, string queueFilter, int limit = 20, Dictionary<string, RoleBaseline>? baselines = null)
        {
            var result = _matches
                .OrderByDescending(m => m.Value.GameStartTime)
                .Select(matchKvp =>
                {
                    var match = matchKvp.Value;
                    if (!_participants.TryGetValue(match.MatchId, out var participants))
                        return null;

                    var participant = participants.FirstOrDefault(p => p.Puuid == puuid);
                    if (participant == null)
                        return null;

                    // Apply queue filter
                    if (!MatchesQueueFilter(match.QueueId, queueFilter))
                        return null;

                    var durationMin = match.GameDurationSec / 60.0;
                    var csPerMin = durationMin > 0 ? Math.Round(participant.CreepScore / durationMin, 1) : 0;
                    var goldPerMin = durationMin > 0 ? Math.Round(participant.GoldEarned / durationMin, 0) : 0;

                    return new MatchListSummaryItem(
                        MatchId: match.MatchId,
                        AccountGameName: null,
                        AccountTagLine: null,
                        AccountRegion: null,
                        QueueId: match.QueueId,
                        QueueType: GetQueueType(match.QueueId),
                        ChampionId: participant.ChampionId,
                        ChampionName: participant.ChampionName,
                        ChampionIconUrl: $"https://cdn.example.com/{participant.ChampionName}.png",
                        Role: participant.Role,
                        Lane: participant.Lane,
                        Win: participant.Win,
                        Kills: participant.Kills,
                        Deaths: participant.Deaths,
                        Assists: participant.Assists,
                        CreepScore: participant.CreepScore,
                        GoldEarned: participant.GoldEarned,
                        GameDurationSec: match.GameDurationSec,
                        GameStartTime: match.GameStartTime,
                        CsPerMin: csPerMin,
                        GoldPerMin: goldPerMin,
                        TrendBadge: null
                    );
                })
                .Where(item => item != null)
                .Take(limit)
                .ToList();

            return Task.FromResult<IList<MatchListSummaryItem>>(result!);
        }

        public Task<IList<MatchListSummaryItem>> GetMatchListSummaryAsync(
            IReadOnlyList<string> puuids, string queueFilter, int limit = 20, Dictionary<string, RoleBaseline>? baselines = null)
        {
            if (puuids.Count == 0)
                return Task.FromResult<IList<MatchListSummaryItem>>(new List<MatchListSummaryItem>());

            var allowed = puuids.ToHashSet(StringComparer.Ordinal);
            var result = _matches
                .OrderByDescending(m => m.Value.GameStartTime)
                .Select(matchKvp =>
                {
                    var match = matchKvp.Value;
                    if (!_participants.TryGetValue(match.MatchId, out var participants))
                        return null;

                    var participant = participants.FirstOrDefault(p => allowed.Contains(p.Puuid));
                    if (participant == null)
                        return null;

                    if (!MatchesQueueFilter(match.QueueId, queueFilter))
                        return null;

                    var durationMin = match.GameDurationSec / 60.0;
                    var csPerMin = durationMin > 0 ? Math.Round(participant.CreepScore / durationMin, 1) : 0;
                    var goldPerMin = durationMin > 0 ? Math.Round(participant.GoldEarned / durationMin, 0) : 0;

                    // Look up account info — only populate when multiple accounts are in scope
                    var account = _riotAccountsRepo.GetByPuuid(participant.Puuid);
                    var accountGameName = allowed.Count > 1 ? account?.GameName : null;
                    var accountTagLine = allowed.Count > 1 ? account?.TagLine : null;
                    var accountRegion = allowed.Count > 1 ? account?.Region : null;

                    return new MatchListSummaryItem(
                        MatchId: match.MatchId,
                        AccountGameName: accountGameName,
                        AccountTagLine: accountTagLine,
                        AccountRegion: accountRegion,
                        QueueId: match.QueueId,
                        QueueType: GetQueueType(match.QueueId),
                        ChampionId: participant.ChampionId,
                        ChampionName: participant.ChampionName,
                        ChampionIconUrl: $"https://cdn.example.com/{participant.ChampionName}.png",
                        Role: participant.Role,
                        Lane: participant.Lane,
                        Win: participant.Win,
                        Kills: participant.Kills,
                        Deaths: participant.Deaths,
                        Assists: participant.Assists,
                        CreepScore: participant.CreepScore,
                        GoldEarned: participant.GoldEarned,
                        GameDurationSec: match.GameDurationSec,
                        GameStartTime: match.GameStartTime,
                        CsPerMin: csPerMin,
                        GoldPerMin: goldPerMin,
                        TrendBadge: null
                    );
                })
                .Where(item => item != null)
                .Take(limit)
                .ToList();

            return Task.FromResult<IList<MatchListSummaryItem>>(result!);
        }

        public Task<MatchDetailsItem?> GetMatchDetailsAsync(string matchId, string puuid)
        {
            if (!_matches.TryGetValue(matchId, out var match))
                return Task.FromResult<MatchDetailsItem?>(null);

            if (!_participants.TryGetValue(matchId, out var participants))
                return Task.FromResult<MatchDetailsItem?>(null);

            var participant = participants.FirstOrDefault(p => p.Puuid == puuid);
            if (participant == null)
                return Task.FromResult<MatchDetailsItem?>(null);

            var durationMin = match.GameDurationSec / 60.0;
            var csPerMin = durationMin > 0 ? Math.Round(participant.CreepScore / durationMin, 1) : 0;
            var goldPerMin = durationMin > 0 ? Math.Round(participant.GoldEarned / durationMin, 0) : 0;

            // Calculate team stats
            var allyParticipants = participants.Where(p => p.TeamId == participant.TeamId).ToList();
            var enemyParticipants = participants.Where(p => p.TeamId != participant.TeamId).ToList();
            var teamKills = allyParticipants.Sum(p => p.Kills);
            var enemyTeamKills = enemyParticipants.Sum(p => p.Kills);
            var teamDamage = allyParticipants.Sum(p => p.DamageDealt);
            var enemyTeamDamage = enemyParticipants.Sum(p => p.DamageDealt);

            var result = new MatchDetailsItem(
                MatchId: match.MatchId,
                QueueId: match.QueueId,
                QueueType: GetQueueType(match.QueueId),
                ChampionId: participant.ChampionId,
                ChampionName: participant.ChampionName,
                ChampionIconUrl: $"https://cdn.example.com/{participant.ChampionName}.png",
                Role: participant.Role,
                Lane: participant.Lane,
                Win: participant.Win,
                Kills: participant.Kills,
                Deaths: participant.Deaths,
                Assists: participant.Assists,
                CreepScore: participant.CreepScore,
                GoldEarned: participant.GoldEarned,
                GameDurationSec: match.GameDurationSec,
                GameStartTime: match.GameStartTime,
                DamageDealt: participant.DamageDealt,
                DamageTaken: participant.DamageTaken,
                VisionScore: participant.VisionScore,
                KillParticipation: (double)participant.KillParticipation,
                DamageShare: (double)participant.DamageShare,
                DeathsPre10: participant.DeathsPre10,
                CsPerMin: csPerMin,
                GoldPerMin: goldPerMin,
                TeamKills: teamKills,
                EnemyTeamKills: enemyTeamKills,
                GoldDiffAt15: participant.GoldDiffAt10,
                TeamTotalDamage: teamDamage,
                EnemyTeamTotalDamage: enemyTeamDamage,
                TeamGoldLeadAt15: null,
                TeamDragons: 0,
                EnemyTeamDragons: 0,
                TeamBarons: 0,
                EnemyTeamBarons: 0,
                TeamTowers: 0,
                EnemyTeamTowers: 0
            );

            return Task.FromResult<MatchDetailsItem?>(result);
        }

        public Task<Dictionary<string, RoleBaseline>> GetRoleBaselinesAsync(string puuid, string queueFilter)
        {
            if (_baselines.TryGetValue(puuid, out var baselines))
                return Task.FromResult(baselines);

            return Task.FromResult(new Dictionary<string, RoleBaseline>());
        }

        public Task<Dictionary<string, RoleBaseline>> GetRoleBaselinesAsync(IReadOnlyList<string> puuids, string queueFilter)
        {
            foreach (var puuid in puuids)
            {
                if (_baselines.TryGetValue(puuid, out var baselines))
                    return Task.FromResult(baselines);
            }

            return Task.FromResult(new Dictionary<string, RoleBaseline>());
        }

        public Task<IList<MatchupParticipantRaw>> GetMatchParticipantsAsync(string matchId)
        {
            if (!_participants.TryGetValue(matchId, out var participants))
                return Task.FromResult<IList<MatchupParticipantRaw>>(new List<MatchupParticipantRaw>());

            var result = participants.Select((p, i) => new MatchupParticipantRaw(
                ParticipantId: i + 1,
                Puuid: p.Puuid,
                ChampionId: p.ChampionId,
                ChampionName: p.ChampionName,
                TeamId: p.TeamId,
                Role: p.Role,
                Win: p.Win,
                Kills: p.Kills,
                Deaths: p.Deaths,
                Assists: p.Assists,
                CreepScore: p.CreepScore,
                GoldEarned: p.GoldEarned,
                KillParticipation: p.KillParticipation,
                DamageShare: p.DamageShare,
                VisionScore: p.VisionScore,
                DeathsPre10: p.DeathsPre10,
                GoldAt10: p.GoldAt10,
                CsAt10: p.CsAt10,
                GoldDiffAt10: p.GoldDiffAt10,
                CsDiffAt10: p.CsDiffAt10
            )).ToList();

            return Task.FromResult<IList<MatchupParticipantRaw>>(result);
        }

        // Required interface methods with minimal implementation
        public Task UpsertAsync(Match match) => Task.CompletedTask;
        public Task<long> GetTotalMatchCountAsync() => Task.FromResult((long)_matches.Count);
        public Task<IList<Match>> GetRecentMatchHeadersAsync(string puuid, int? queueId, int limit)
            => Task.FromResult<IList<Match>>(new List<Match>());
        public Task<IList<MatchListItem>> GetMatchListAsync(string puuid, string queueFilter, int limit = 20, Dictionary<string, RoleBaseline>? baselines = null)
            => Task.FromResult<IList<MatchListItem>>(new List<MatchListItem>());
        public Task<IList<MatchListItem>> GetMatchListAsync(IReadOnlyList<string> puuids, string queueFilter, int limit = 20, Dictionary<string, RoleBaseline>? baselines = null)
            => Task.FromResult<IList<MatchListItem>>(new List<MatchListItem>());
        public Task<int> DeleteOldMatchesAsync(long cutoffTimestamp, int batchSize) => Task.FromResult(0);

        private static bool MatchesQueueFilter(int queueId, string queueFilter)
        {
            if (string.IsNullOrEmpty(queueFilter) || queueFilter == "AND 1=1")
                return true;
            if (queueFilter.Contains("420") && queueId == 420) return true;
            if (queueFilter.Contains("440") && queueId == 440) return true;
            if (queueFilter.Contains("450") && queueId == 450) return true;
            if (queueFilter.Contains("400") && queueId == 400) return true;
            return queueFilter == "AND 1=1";
        }

        private static string GetQueueType(int queueId) => queueId switch
        {
            420 => "ranked_solo",
            440 => "ranked_flex",
            450 => "aram",
            400 => "normal",
            _ => "other"
        };
    }

    /// <summary>
    /// Fake implementation of ISoloPerformanceRepository for testing.
    /// </summary>
    internal sealed class FakeSoloPerformanceRepository : ISoloPerformanceRepository
    {
        private readonly ConcurrentDictionary<string, SoloPerformanceResponse> _performanceData = new();

        public void SetPerformanceData(string puuid, SoloPerformanceResponse data)
        {
            _performanceData[puuid] = data;
        }

        public void Clear()
        {
            _performanceData.Clear();
        }

        public Task<SoloPerformanceResponse?> GetSoloPerformanceAsync(string puuid, string? queueType = null, string? timeRange = null)
        {
            _performanceData.TryGetValue(puuid, out var data);
            return Task.FromResult(data);
        }

        public Task<SoloPerformanceResponse?> GetSoloPerformanceAsync(IReadOnlyList<string> puuids, string? queueType = null, string? timeRange = null)
        {
            var aggregate = puuids
                .Select(puuid => _performanceData.TryGetValue(puuid, out var data) ? data : null)
                .FirstOrDefault(data => data != null);

            return Task.FromResult(aggregate);
        }
    }

    /// <summary>
    /// Fake implementation of IMatchupRepository for testing.
    /// </summary>
    internal sealed class FakeMatchupRepository : IMatchupRepository
    {
        private readonly ConcurrentDictionary<string, ChampionMatchupsResponse> _matchupData = new();

        public void SetMatchupData(string puuid, ChampionMatchupsResponse data)
        {
            _matchupData[puuid] = data;
        }

        public void Clear()
        {
            _matchupData.Clear();
        }

        public Task<ChampionMatchupsResponse> GetChampionMatchupsAsync(string puuid, string? queueType = null, string? timeRange = null)
        {
            if (_matchupData.TryGetValue(puuid, out var data))
                return Task.FromResult(data);

            // Return empty response if no data
            return Task.FromResult(new ChampionMatchupsResponse(
                Matchups: Array.Empty<ChampionMatchup>(),
                QueueType: queueType ?? "all",
                TimeRange: timeRange ?? "all"
            ));
        }
    }

    /// <summary>
    /// Fake implementation of IChampionSelectRepository for testing.
    /// </summary>
    internal sealed class FakeChampionSelectRepository : IChampionSelectRepository
    {
        private readonly ConcurrentDictionary<string, ChampionSelectResponse> _championSelectData = new();

        public void SetChampionSelectData(string puuid, ChampionSelectResponse data)
        {
            _championSelectData[puuid] = data;
        }

        public void Clear()
        {
            _championSelectData.Clear();
        }

        public Task<ChampionSelectResponse?> GetChampionSelectDataAsync(string puuid, string? queueType = null, string? timeRange = null)
        {
            _championSelectData.TryGetValue(puuid, out var data);
            return Task.FromResult(data);
        }
    }

    /// <summary>
    /// Fake implementation of ITrendRepository for testing.
    /// </summary>
    internal sealed class FakeTrendRepository : ITrendRepository
    {
        private readonly ConcurrentDictionary<string, (DragonParticipationTrendPoint[] DataPoints, double AverageParticipation, double OverallAverage, string Trend)> _dragonParticipationData = new();
        private readonly ConcurrentDictionary<string, (VisionScoreTrendPoint[] DataPoints, double AverageVisionPerMinute, double OverallAverage, double RoleTarget, string Trend)> _visionScoreData = new();
        private readonly ConcurrentDictionary<string, WinrateTrendPoint[]> _winrateData = new();
        private readonly ConcurrentDictionary<string, GoldAt15TrendPoint[]> _goldAt15Data = new();
        private readonly ConcurrentDictionary<string, CsPerMinuteTrendPoint[]> _csPerMinuteData = new();
        private readonly ConcurrentDictionary<string, (DeathsTrendPoint[] DataPoints, double AverageDeaths, double OverallAverage, string Trend)> _deathsData = new();

        public void SetDragonParticipationData(string puuid, DragonParticipationTrendPoint[] dataPoints, double averageParticipation, double overallAverage, string trend)
        {
            _dragonParticipationData[puuid] = (dataPoints, averageParticipation, overallAverage, trend);
        }

        public void SetVisionScoreData(string puuid, VisionScoreTrendPoint[] dataPoints, double averageVisionPerMinute, double overallAverage, double roleTarget, string trend)
        {
            _visionScoreData[puuid] = (dataPoints, averageVisionPerMinute, overallAverage, roleTarget, trend);
        }

        public void SetWinrateData(string puuid, WinrateTrendPoint[] dataPoints)
        {
            _winrateData[puuid] = dataPoints;
        }

        public void SetGoldAt15Data(string puuid, GoldAt15TrendPoint[] dataPoints)
        {
            _goldAt15Data[puuid] = dataPoints;
        }

        public void SetCsPerMinuteData(string puuid, CsPerMinuteTrendPoint[] dataPoints)
        {
            _csPerMinuteData[puuid] = dataPoints;
        }

        public void SetDeathsData(string puuid, DeathsTrendPoint[] dataPoints, double averageDeaths, double overallAverage, string trend)
        {
            _deathsData[puuid] = (dataPoints, averageDeaths, overallAverage, trend);
        }

        public void Clear()
        {
            _dragonParticipationData.Clear();
            _visionScoreData.Clear();
            _winrateData.Clear();
            _goldAt15Data.Clear();
            _csPerMinuteData.Clear();
            _deathsData.Clear();
        }

        public Task<(DragonParticipationTrendPoint[] DataPoints, double AverageParticipation, double OverallAverage, string Trend)> GetDragonParticipationTrendAsync(string puuid, string? queueType = null, string? timeRange = null, int? limit = null)
        {
            if (_dragonParticipationData.TryGetValue(puuid, out var data))
                return Task.FromResult(data);

            // Return empty result if no data
            return Task.FromResult<(DragonParticipationTrendPoint[] DataPoints, double AverageParticipation, double OverallAverage, string Trend)>(
                (Array.Empty<DragonParticipationTrendPoint>(), 0, 0, "neutral"));
        }

        public Task<(DragonParticipationTrendPoint[] DataPoints, double AverageParticipation, double OverallAverage, string Trend)> GetDragonParticipationTrendAsync(IReadOnlyList<string> puuids, string? queueType = null, string? timeRange = null, int? limit = null, IReadOnlyDictionary<string, string>? puuidToGameName = null)
        {
            var combinedPoints = new List<DragonParticipationTrendPoint>();
            (double AverageParticipation, double OverallAverage, string Trend)? summary = null;

            foreach (var puuid in puuids)
            {
                if (_dragonParticipationData.TryGetValue(puuid, out var data))
                {
                    if (summary == null)
                    {
                        summary = (data.AverageParticipation, data.OverallAverage, data.Trend);
                    }

                    string? accountGameName = null;
                    if (puuidToGameName != null)
                    {
                        puuidToGameName.TryGetValue(puuid, out accountGameName);
                    }

                    var labeledPoints = data.DataPoints
                        .Select(point => point with { AccountGameName = point.AccountGameName ?? accountGameName });

                    combinedPoints.AddRange(labeledPoints);
                }
            }

            if (summary != null)
            {
                return Task.FromResult((
                    combinedPoints.ToArray(),
                    summary.Value.AverageParticipation,
                    summary.Value.OverallAverage,
                    summary.Value.Trend));
            }

            return Task.FromResult<(DragonParticipationTrendPoint[] DataPoints, double AverageParticipation, double OverallAverage, string Trend)>(
                (Array.Empty<DragonParticipationTrendPoint>(), 0, 0, "neutral"));
        }

        public Task<WinrateTrendPoint[]> GetWinrateTrendAsync(string puuid, string? queueType = null, string? timeRange = null, int? limit = null)
        {
            if (_winrateData.TryGetValue(puuid, out var data))
                return Task.FromResult(data);

            return Task.FromResult(Array.Empty<WinrateTrendPoint>());
        }

        public Task<WinrateTrendPoint[]> GetWinrateTrendAsync(IReadOnlyList<string> puuids, string? queueType = null, string? timeRange = null, int? limit = null, IReadOnlyDictionary<string, string>? puuidToGameName = null)
        {
            var combinedPoints = new List<WinrateTrendPoint>();

            foreach (var puuid in puuids)
            {
                if (_winrateData.TryGetValue(puuid, out var data))
                {
                    string? accountGameName = null;
                    if (puuidToGameName != null)
                    {
                        puuidToGameName.TryGetValue(puuid, out accountGameName);
                    }

                    var labeledPoints = data
                        .Select(point => point with { AccountGameName = point.AccountGameName ?? accountGameName });

                    combinedPoints.AddRange(labeledPoints);
                }
            }

            return Task.FromResult(combinedPoints.ToArray());
        }

        public Task<GoldAt15TrendPoint[]> GetGoldAt15TrendAsync(string puuid, string? queueType = null, string? timeRange = null, int? limit = null)
        {
            if (_goldAt15Data.TryGetValue(puuid, out var data))
                return Task.FromResult(data);

            return Task.FromResult(Array.Empty<GoldAt15TrendPoint>());
        }

        public Task<GoldAt15TrendPoint[]> GetGoldAt15TrendAsync(IReadOnlyList<string> puuids, string? queueType = null, string? timeRange = null, int? limit = null, IReadOnlyDictionary<string, string>? puuidToGameName = null)
        {
            var combinedPoints = new List<GoldAt15TrendPoint>();

            foreach (var puuid in puuids)
            {
                if (_goldAt15Data.TryGetValue(puuid, out var data))
                {
                    string? accountGameName = null;
                    if (puuidToGameName != null)
                    {
                        puuidToGameName.TryGetValue(puuid, out accountGameName);
                    }

                    var labeledPoints = data
                        .Select(point => point with { AccountGameName = point.AccountGameName ?? accountGameName });

                    combinedPoints.AddRange(labeledPoints);
                }
            }

            return Task.FromResult(combinedPoints.ToArray());
        }

        public Task<CsPerMinuteTrendPoint[]> GetCsPerMinuteTrendAsync(string puuid, string? queueType = null, string? timeRange = null, int? limit = null)
        {
            if (_csPerMinuteData.TryGetValue(puuid, out var data))
                return Task.FromResult(data);

            return Task.FromResult(Array.Empty<CsPerMinuteTrendPoint>());
        }

        public Task<CsPerMinuteTrendPoint[]> GetCsPerMinuteTrendAsync(IReadOnlyList<string> puuids, string? queueType = null, string? timeRange = null, int? limit = null, IReadOnlyDictionary<string, string>? puuidToGameName = null)
        {
            var combinedPoints = new List<CsPerMinuteTrendPoint>();

            foreach (var puuid in puuids)
            {
                if (_csPerMinuteData.TryGetValue(puuid, out var data))
                {
                    string? accountGameName = null;
                    if (puuidToGameName != null)
                    {
                        puuidToGameName.TryGetValue(puuid, out accountGameName);
                    }

                    var labeledPoints = data
                        .Select(point => point with { AccountGameName = point.AccountGameName ?? accountGameName });

                    combinedPoints.AddRange(labeledPoints);
                }
            }

            return Task.FromResult(combinedPoints.ToArray());
        }

        public Task<(DeathsTrendPoint[] DataPoints, double AverageDeaths, double OverallAverage, string Trend)> GetDeathsTrendAsync(string puuid, string? queueType = null, string? timeRange = null, int? limit = null)
        {
            if (_deathsData.TryGetValue(puuid, out var data))
                return Task.FromResult(data);

            return Task.FromResult<(DeathsTrendPoint[] DataPoints, double AverageDeaths, double OverallAverage, string Trend)>(
                (Array.Empty<DeathsTrendPoint>(), 0, 0, "neutral"));
        }

        public Task<(DeathsTrendPoint[] DataPoints, double AverageDeaths, double OverallAverage, string Trend)> GetDeathsTrendAsync(IReadOnlyList<string> puuids, string? queueType = null, string? timeRange = null, int? limit = null, IReadOnlyDictionary<string, string>? puuidToGameName = null)
        {
            var combinedPoints = new List<DeathsTrendPoint>();
            (double AverageDeaths, double OverallAverage, string Trend)? summary = null;

            foreach (var puuid in puuids)
            {
                if (_deathsData.TryGetValue(puuid, out var data))
                {
                    if (summary == null)
                    {
                        summary = (data.AverageDeaths, data.OverallAverage, data.Trend);
                    }

                    string? accountGameName = null;
                    if (puuidToGameName != null)
                    {
                        puuidToGameName.TryGetValue(puuid, out accountGameName);
                    }

                    var labeledPoints = data.DataPoints
                        .Select(point => point with { AccountGameName = point.AccountGameName ?? accountGameName });

                    combinedPoints.AddRange(labeledPoints);
                }
            }

            if (summary != null)
            {
                return Task.FromResult((
                    combinedPoints.ToArray(),
                    summary.Value.AverageDeaths,
                    summary.Value.OverallAverage,
                    summary.Value.Trend));
            }

            return Task.FromResult<(DeathsTrendPoint[] DataPoints, double AverageDeaths, double OverallAverage, string Trend)>(
                (Array.Empty<DeathsTrendPoint>(), 0, 0, "neutral"));
        }

        public Task<(VisionScoreTrendPoint[] DataPoints, double AverageVisionPerMinute, double OverallAverage, double RoleTarget, string Trend)> GetVisionScoreTrendAsync(string puuid, string? queueType = null, string? timeRange = null, int? limit = null)
        {
            if (_visionScoreData.TryGetValue(puuid, out var data))
                return Task.FromResult(data);

            // Return empty result if no data
            return Task.FromResult<(VisionScoreTrendPoint[] DataPoints, double AverageVisionPerMinute, double OverallAverage, double RoleTarget, string Trend)>(
                (Array.Empty<VisionScoreTrendPoint>(), 0, 0, 1.0, "neutral"));
        }

        public Task<(VisionScoreTrendPoint[] DataPoints, double AverageVisionPerMinute, double OverallAverage, double RoleTarget, string Trend)> GetVisionScoreTrendAsync(IReadOnlyList<string> puuids, string? queueType = null, string? timeRange = null, int? limit = null, IReadOnlyDictionary<string, string>? puuidToGameName = null)
        {
            var combinedPoints = new List<VisionScoreTrendPoint>();
            (double AverageVisionPerMinute, double OverallAverage, double RoleTarget, string Trend)? summary = null;

            foreach (var puuid in puuids)
            {
                if (_visionScoreData.TryGetValue(puuid, out var data))
                {
                    if (summary == null)
                    {
                        summary = (data.AverageVisionPerMinute, data.OverallAverage, data.RoleTarget, data.Trend);
                    }

                    string? accountGameName = null;
                    if (puuidToGameName != null)
                    {
                        puuidToGameName.TryGetValue(puuid, out accountGameName);
                    }
                    var labeledPoints = data.DataPoints
                        .Select(point => point with { AccountGameName = point.AccountGameName ?? accountGameName });

                    combinedPoints.AddRange(labeledPoints);
                }
            }

            if (summary != null)
            {
                return Task.FromResult((
                    combinedPoints.ToArray(),
                    summary.Value.AverageVisionPerMinute,
                    summary.Value.OverallAverage,
                    summary.Value.RoleTarget,
                    summary.Value.Trend));
            }

            return Task.FromResult<(VisionScoreTrendPoint[] DataPoints, double AverageVisionPerMinute, double OverallAverage, double RoleTarget, string Trend)>(
                (Array.Empty<VisionScoreTrendPoint>(), 0, 0, 1.0, "neutral"));
        }

        private readonly ConcurrentDictionary<string, Dictionary<string, int>> _dailyCounts = new();

        public void SetDailyMatchCounts(string puuid, Dictionary<string, int> counts)
        {
            _dailyCounts[puuid] = counts;
        }

        public Task<Dictionary<string, int>> GetDailyMatchCountsAsync(string puuid, int daysBack = 91)
        {
            _dailyCounts.TryGetValue(puuid, out var counts);
            return Task.FromResult(counts ?? new Dictionary<string, int>());
        }

        public Task<Dictionary<string, int>> GetDailyMatchCountsAsync(IReadOnlyList<string> puuids, int daysBack = 91)
        {
            var merged = new Dictionary<string, int>();
            foreach (var puuid in puuids)
            {
                if (_dailyCounts.TryGetValue(puuid, out var counts))
                {
                    foreach (var (date, count) in counts)
                    {
                        merged[date] = merged.TryGetValue(date, out var existing) ? existing + count : count;
                    }
                }
            }
            return Task.FromResult(merged);
        }
    }

    /// <summary>
    /// Fake implementation of IRadarChartRepository for testing.
    /// </summary>
    internal sealed class FakeRadarChartRepository : IRadarChartRepository
    {
        private readonly ConcurrentDictionary<string, RadarChartResponse> _radarData = new();

        public void SetRadarData(string puuid, RadarChartResponse response)
        {
            _radarData[puuid] = response;
        }

        public void Clear()
        {
            _radarData.Clear();
        }

        public Task<RadarChartResponse?> GetRadarChartAsync(string puuid, string? queueType = null, string? timeRange = null)
        {
            _radarData.TryGetValue(puuid, out var data);
            return Task.FromResult(data);
        }

        public Task<RadarChartResponse?> GetRadarChartAsync(IReadOnlyList<string> puuids, string? queueType = null, string? timeRange = null)
        {
            foreach (var puuid in puuids)
            {
                if (_radarData.TryGetValue(puuid, out var data))
                    return Task.FromResult<RadarChartResponse?>(data);
            }

            return Task.FromResult<RadarChartResponse?>(null);
        }
    }

    /// <summary>
    /// Fake implementation of IDeathPositionsRepository for testing.
    /// </summary>
    internal sealed class FakeDeathPositionsRepository : IDeathPositionsRepository
    {
        private readonly ConcurrentDictionary<string, Core.QueryModels.DeathPositionsResult> _deathPositionsData = new();

        public void SetDeathPositionsData(string puuid, Core.QueryModels.DeathPositionsResult data)
        {
            _deathPositionsData[puuid] = data;
        }

        public void Clear()
        {
            _deathPositionsData.Clear();
        }

        public Task<Core.QueryModels.DeathPositionsResult?> GetDeathPositionsAsync(
            string puuid, 
            string? queueType = null, 
            string? timeRange = null, 
            string? side = null)
        {
            _deathPositionsData.TryGetValue(puuid, out var data);
            return Task.FromResult(data);
        }

        public Task<Core.QueryModels.DeathPositionsResult?> GetDeathPositionsAsync(
            IReadOnlyList<string> puuids,
            string? queueType = null,
            string? timeRange = null,
            string? side = null)
        {
            foreach (var puuid in puuids)
            {
                if (_deathPositionsData.TryGetValue(puuid, out var data))
                    return Task.FromResult<Core.QueryModels.DeathPositionsResult?>(data);
            }

            return Task.FromResult<Core.QueryModels.DeathPositionsResult?>(null);
        }
    }
}

