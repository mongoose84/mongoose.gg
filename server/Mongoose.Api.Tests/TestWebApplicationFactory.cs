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
using Mongoose.Api.Application.DTOs;
using Mongoose.Api.Application.Interfaces;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using static Mongoose.Api.Application.DTOs.SoloPerformanceDto;
using static Mongoose.Api.Application.DTOs.SoloMatchupsDto;
using static Mongoose.Api.Application.DTOs.ChampionSelectDto;
using static Mongoose.Api.Application.DTOs.TrendDto;
using static Mongoose.Api.Application.DTOs.Solo.DeathPositionsDto;

namespace Mongoose.Api.Tests;

internal sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly IDictionary<string, string?> _overrides;
    private readonly FakeUsersRepository _usersRepository;
    private readonly FakeVerificationTokensRepository _tokensRepository;
    private readonly FakeEmailService _emailService;
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
    private readonly FakeDeathPositionsRepository _deathPositionsRepository;

    public FakeUsersRepository UsersRepository => _usersRepository;
    public FakeVerificationTokensRepository TokensRepository => _tokensRepository;
    public FakeEmailService EmailService => _emailService;
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
    public FakeDeathPositionsRepository DeathPositionsRepository => _deathPositionsRepository;

    public TestWebApplicationFactory(IDictionary<string, string?>? overrides = null)
    {
        _overrides = overrides ?? new Dictionary<string, string?>();
        _usersRepository = new FakeUsersRepository();
        _tokensRepository = new FakeVerificationTokensRepository();
        _emailService = new FakeEmailService();
        _riotAccountsRepository = new FakeRiotAccountsRepository();
        _userRiotAccountsRepository = new FakeUserRiotAccountsRepository(_riotAccountsRepository);
        _overviewStatsRepository = new FakeOverviewStatsRepository();
        _analyticsEventsRepository = new FakeAnalyticsEventsRepository();
        _gitHubService = new FakeGitHubService();
        _matchesRepository = new FakeMatchesRepository();
        _soloPerformanceRepository = new FakeSoloPerformanceRepository();
        _matchupRepository = new FakeMatchupRepository();
        _championSelectRepository = new FakeChampionSelectRepository();
        _trendRepository = new FakeTrendRepository();
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
            // Test encryption key (32 bytes base64-encoded) - only for testing
            // "test-encryption-key-32bytes!!!!!" (32 chars) -> base64
            const string testEmailEncryptionKey = "dGVzdC1lbmNyeXB0aW9uLWtleS0zMmJ5dGVzISEhISE=";

            var defaults = new Dictionary<string, string?>
            {
                ["Auth:EnableMvpLogin"] = "true",
                ["Auth:CookieName"] = "mongoose-auth",
                ["Auth:SessionTimeout"] = "30",
                ["Jobs:EnableMatchHistorySync"] = "false",
                ["RIOT_API_KEY"] = "test-key",
                ["Database_test"] = "Server=localhost;Port=3306;Database=test;User Id=test;Password=test;",
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
            // Replace UsersRepository with a fake to avoid real DB connections
            services.RemoveAll<UsersRepository>();
            services.AddSingleton<UsersRepository>(_usersRepository);

            // Replace VerificationTokensRepository with a fake
            services.RemoveAll<VerificationTokensRepository>();
            services.AddSingleton<VerificationTokensRepository>(_tokensRepository);

            // Replace IEmailService with a fake
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService>(_emailService);

            // Replace RiotAccountsRepository with a fake
            services.RemoveAll<RiotAccountsRepository>();
            services.AddSingleton<RiotAccountsRepository>(_riotAccountsRepository);

            // Replace UserRiotAccountsRepository with a fake
            services.RemoveAll<IUserRiotAccountsRepository>();
            services.AddSingleton<IUserRiotAccountsRepository>(_userRiotAccountsRepository);

            // Replace OverviewStatsRepository with a fake
            services.RemoveAll<OverviewStatsRepository>();
            services.AddSingleton<OverviewStatsRepository>(_overviewStatsRepository);

            // Replace AnalyticsEventsRepository with a fake
            services.RemoveAll<AnalyticsEventsRepository>();
            services.AddSingleton<AnalyticsEventsRepository>(_analyticsEventsRepository);

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
            }
            return Task.CompletedTask;
        }

        public void AddUnverifiedUser(string username, string email, string password)
        {
            var user = new User
            {
                UserId = _nextId++,
                Username = username,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
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

        public IReadOnlyList<SentEmail> SentEmails => _sentEmails;
        public IReadOnlyList<SentPasswordResetEmail> SentPasswordResetEmails => _sentPasswordResetEmails;

        public Task SendVerificationEmailAsync(string toEmail, string username, string verificationCode)
        {
            _sentEmails.Add(new SentEmail(toEmail, username, verificationCode));
            return Task.CompletedTask;
        }

        public Task SendPasswordResetEmailAsync(string toEmail, string username, string resetCode)
        {
            _sentPasswordResetEmails.Add(new SentPasswordResetEmail(toEmail, username, resetCode));
            return Task.CompletedTask;
        }

        public void Clear()
        {
            _sentEmails.Clear();
            _sentPasswordResetEmails.Clear();
        }

        public record SentEmail(string ToEmail, string Username, string VerificationCode);
        public record SentPasswordResetEmail(string ToEmail, string Username, string ResetCode);
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

        public override Task<List<MatchResultData>> GetLast20MatchesAsync(string puuid, int queueId)
        {
            if (_matchesByPuuid.TryGetValue(puuid, out var matches))
            {
                return Task.FromResult(matches.Take(20).ToList());
            }
            return Task.FromResult(new List<MatchResultData>());
        }

        public override Task<LastMatchData?> GetLastMatchAsync(string puuid)
        {
            _lastMatchByPuuid.TryGetValue(puuid, out var lastMatch);
            return Task.FromResult(lastMatch);
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

        public void SetDragonParticipationData(string puuid, DragonParticipationTrendPoint[] dataPoints, double averageParticipation, double overallAverage, string trend)
        {
            _dragonParticipationData[puuid] = (dataPoints, averageParticipation, overallAverage, trend);
        }

        public void SetVisionScoreData(string puuid, VisionScoreTrendPoint[] dataPoints, double averageVisionPerMinute, double overallAverage, double roleTarget, string trend)
        {
            _visionScoreData[puuid] = (dataPoints, averageVisionPerMinute, overallAverage, roleTarget, trend);
        }

        public void Clear()
        {
            _dragonParticipationData.Clear();
            _visionScoreData.Clear();
        }

        public Task<(DragonParticipationTrendPoint[] DataPoints, double AverageParticipation, double OverallAverage, string Trend)> GetDragonParticipationTrendAsync(string puuid, string? queueType = null, string? timeRange = null, int? limit = null)
        {
            if (_dragonParticipationData.TryGetValue(puuid, out var data))
                return Task.FromResult(data);

            // Return empty result if no data
            return Task.FromResult<(DragonParticipationTrendPoint[] DataPoints, double AverageParticipation, double OverallAverage, string Trend)>(
                (Array.Empty<DragonParticipationTrendPoint>(), 0, 0, "neutral"));
        }

        // Other trend methods return empty data for now since we're only testing dragon participation
        public Task<WinrateTrendPoint[]> GetWinrateTrendAsync(string puuid, string? queueType = null, string? timeRange = null, int? limit = null)
        {
            return Task.FromResult(Array.Empty<WinrateTrendPoint>());
        }

        public Task<GoldAt15TrendPoint[]> GetGoldAt15TrendAsync(string puuid, string? queueType = null, string? timeRange = null, int? limit = null)
        {
            return Task.FromResult(Array.Empty<GoldAt15TrendPoint>());
        }

        public Task<CsPerMinuteTrendPoint[]> GetCsPerMinuteTrendAsync(string puuid, string? queueType = null, string? timeRange = null, int? limit = null)
        {
            return Task.FromResult(Array.Empty<CsPerMinuteTrendPoint>());
        }

        public Task<(DeathsTrendPoint[] DataPoints, double AverageDeaths, double OverallAverage, string Trend)> GetDeathsTrendAsync(string puuid, string? queueType = null, string? timeRange = null, int? limit = null)
        {
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

        public Task<Dictionary<string, int>> GetDailyMatchCountsAsync(string puuid, int daysBack = 91)
        {
            return Task.FromResult(new Dictionary<string, int>());
        }
    }

    /// <summary>
    /// Fake implementation of IDeathPositionsRepository for testing.
    /// </summary>
    internal sealed class FakeDeathPositionsRepository : IDeathPositionsRepository
    {
        private readonly ConcurrentDictionary<string, DeathPositionsResponse> _deathPositionsData = new();

        public void SetDeathPositionsData(string puuid, DeathPositionsResponse data)
        {
            _deathPositionsData[puuid] = data;
        }

        public void Clear()
        {
            _deathPositionsData.Clear();
        }

        public Task<DeathPositionsResponse?> GetDeathPositionsAsync(
            string puuid, 
            string? queueType = null, 
            string? timeRange = null, 
            string? side = null)
        {
            _deathPositionsData.TryGetValue(puuid, out var data);
            return Task.FromResult(data);
        }
    }
}

