using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RiotProxy.Infrastructure.External.Database.Repositories;
using RiotProxy.Infrastructure.Security;
using RiotProxy.Infrastructure.Email;
using RiotProxy.External.Domain.Entities;
using RiotProxy.Application.DTOs.Overview;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;

namespace RiotProxy.Tests;

internal sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly IDictionary<string, string?> _overrides;
    private readonly FakeUsersRepository _usersRepository;
    private readonly FakeVerificationTokensRepository _tokensRepository;
    private readonly FakeEmailService _emailService;
    private readonly FakeRiotAccountsRepository _riotAccountsRepository;
    private readonly FakeOverviewStatsRepository _overviewStatsRepository;
    private readonly FakeLpSnapshotsRepository _lpSnapshotsRepository;
    private readonly FakeAnalyticsEventsRepository _analyticsEventsRepository;

    public FakeUsersRepository UsersRepository => _usersRepository;
    public FakeVerificationTokensRepository TokensRepository => _tokensRepository;
    public FakeEmailService EmailService => _emailService;
    public FakeRiotAccountsRepository RiotAccountsRepository => _riotAccountsRepository;
    public FakeOverviewStatsRepository OverviewStatsRepository => _overviewStatsRepository;
    public FakeLpSnapshotsRepository LpSnapshotsRepository => _lpSnapshotsRepository;
    public FakeAnalyticsEventsRepository AnalyticsEventsRepository => _analyticsEventsRepository;

    public TestWebApplicationFactory(IDictionary<string, string?>? overrides = null)
    {
        _overrides = overrides ?? new Dictionary<string, string?>();
        _usersRepository = new FakeUsersRepository();
        _tokensRepository = new FakeVerificationTokensRepository();
        _emailService = new FakeEmailService();
        _riotAccountsRepository = new FakeRiotAccountsRepository();
        _overviewStatsRepository = new FakeOverviewStatsRepository();
        _lpSnapshotsRepository = new FakeLpSnapshotsRepository();
        _analyticsEventsRepository = new FakeAnalyticsEventsRepository();
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

            // Replace OverviewStatsRepository with a fake
            services.RemoveAll<OverviewStatsRepository>();
            services.AddSingleton<OverviewStatsRepository>(_overviewStatsRepository);

            // Replace LpSnapshotsRepository with a fake
            services.RemoveAll<LpSnapshotsRepository>();
            services.AddSingleton<LpSnapshotsRepository>(_lpSnapshotsRepository);

            // Replace AnalyticsEventsRepository with a fake
            services.RemoveAll<AnalyticsEventsRepository>();
            services.AddSingleton<AnalyticsEventsRepository>(_analyticsEventsRepository);
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

        public IReadOnlyList<SentEmail> SentEmails => _sentEmails;

        public Task SendVerificationEmailAsync(string toEmail, string username, string verificationCode)
        {
            _sentEmails.Add(new SentEmail(toEmail, username, verificationCode));
            return Task.CompletedTask;
        }

        public void Clear() => _sentEmails.Clear();

        public record SentEmail(string ToEmail, string Username, string VerificationCode);
    }

    /// <summary>
    /// Fake Riot accounts repository for testing.
    /// </summary>
    internal sealed class FakeRiotAccountsRepository : RiotAccountsRepository
    {
        private readonly ConcurrentDictionary<string, RiotAccount> _accountsByPuuid = new();
        private readonly ConcurrentDictionary<long, List<RiotAccount>> _accountsByUserId = new();

        public FakeRiotAccountsRepository() : base(null!) { }

        public override Task<IList<RiotAccount>> GetByUserIdAsync(long userId)
        {
            if (_accountsByUserId.TryGetValue(userId, out var accounts))
            {
                return Task.FromResult<IList<RiotAccount>>(accounts.ToList());
            }
            return Task.FromResult<IList<RiotAccount>>(new List<RiotAccount>());
        }

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
            if (!_accountsByUserId.TryGetValue(account.UserId, out var userAccounts))
            {
                userAccounts = new List<RiotAccount>();
                _accountsByUserId[account.UserId] = userAccounts;
            }
            var existing = userAccounts.FindIndex(a => a.Puuid == account.Puuid);
            if (existing >= 0)
            {
                userAccounts[existing] = account;
            }
            else
            {
                userAccounts.Add(account);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Helper method to add a Riot account for testing.
        /// </summary>
        public void AddRiotAccount(long userId, string puuid, string gameName, string region, string summonerName, int summonerLevel, int profileIconId)
        {
            var account = new RiotAccount
            {
                Puuid = puuid,
                UserId = userId,
                GameName = gameName,
                TagLine = summonerName.Contains('#') ? summonerName.Split('#')[1] : "NA1",
                SummonerName = summonerName,
                Region = region,
                IsPrimary = true,
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
        /// </summary>
        public void AddRiotAccountWithRank(long userId, string puuid, string gameName, string region, string summonerName,
            int summonerLevel, int profileIconId, string? soloTier, string? soloRank, int? soloLp)
        {
            var account = new RiotAccount
            {
                Puuid = puuid,
                UserId = userId,
                GameName = gameName,
                TagLine = summonerName.Contains('#') ? summonerName.Split('#')[1] : "NA1",
                SummonerName = summonerName,
                Region = region,
                IsPrimary = true,
                SyncStatus = "synced",
                SummonerLevel = summonerLevel,
                ProfileIconId = profileIconId,
                SoloTier = soloTier,
                SoloRank = soloRank,
                SoloLp = soloLp,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            UpsertAsync(account).Wait();
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
    /// Fake LP snapshots repository for testing.
    /// </summary>
    internal sealed class FakeLpSnapshotsRepository : LpSnapshotsRepository
    {
        private readonly ConcurrentDictionary<string, List<LpSnapshot>> _snapshotsByPuuid = new();
        private long _nextId = 1;

        public FakeLpSnapshotsRepository() : base(null!) { }

        public override Task<long> InsertAsync(LpSnapshot snapshot)
        {
            var key = snapshot.Puuid;
            if (!_snapshotsByPuuid.TryGetValue(key, out var snapshots))
            {
                snapshots = new List<LpSnapshot>();
                _snapshotsByPuuid[key] = snapshots;
            }
            snapshot.Id = _nextId++;
            snapshots.Add(snapshot);
            return Task.FromResult(snapshot.Id);
        }

        public override Task<IList<LpSnapshot>> GetByPuuidAndQueueAsync(string puuid, string queueType, int limit = 100)
        {
            if (_snapshotsByPuuid.TryGetValue(puuid, out var snapshots))
            {
                var filtered = snapshots
                    .Where(s => s.QueueType == queueType)
                    .OrderByDescending(s => s.RecordedAt)
                    .Take(limit)
                    .ToList();
                return Task.FromResult<IList<LpSnapshot>>(filtered);
            }
            return Task.FromResult<IList<LpSnapshot>>(new List<LpSnapshot>());
        }

        public override Task<LpSnapshot?> GetLatestByPuuidAndQueueAsync(string puuid, string queueType)
        {
            if (_snapshotsByPuuid.TryGetValue(puuid, out var snapshots))
            {
                var latest = snapshots
                    .Where(s => s.QueueType == queueType)
                    .OrderByDescending(s => s.RecordedAt)
                    .FirstOrDefault();
                return Task.FromResult(latest);
            }
            return Task.FromResult<LpSnapshot?>(null);
        }

        public override Task<IList<LpSnapshot>> GetByPuuidAsync(string puuid, int limit = 100)
        {
            if (_snapshotsByPuuid.TryGetValue(puuid, out var snapshots))
            {
                var result = snapshots
                    .OrderByDescending(s => s.RecordedAt)
                    .Take(limit)
                    .ToList();
                return Task.FromResult<IList<LpSnapshot>>(result);
            }
            return Task.FromResult<IList<LpSnapshot>>(new List<LpSnapshot>());
        }

        public override Task<LpSnapshot?> GetSnapshotAtOrBeforeAsync(string puuid, string queueType, DateTime timestamp)
        {
            if (_snapshotsByPuuid.TryGetValue(puuid, out var snapshots))
            {
                var snapshot = snapshots
                    .Where(s => s.QueueType == queueType && s.RecordedAt <= timestamp)
                    .OrderByDescending(s => s.RecordedAt)
                    .FirstOrDefault();
                return Task.FromResult(snapshot);
            }
            return Task.FromResult<LpSnapshot?>(null);
        }

        /// <summary>
        /// Adds an LP snapshot for testing.
        /// </summary>
        public void AddSnapshot(string puuid, string queueType, string tier, string division, int lp, DateTime recordedAt)
        {
            var snapshot = new LpSnapshot
            {
                Id = _nextId++,
                Puuid = puuid,
                QueueType = queueType,
                Tier = tier,
                Division = division,
                Lp = lp,
                RecordedAt = recordedAt,
                CreatedAt = DateTime.UtcNow
            };

            if (!_snapshotsByPuuid.TryGetValue(puuid, out var snapshots))
            {
                snapshots = new List<LpSnapshot>();
                _snapshotsByPuuid[puuid] = snapshots;
            }
            snapshots.Add(snapshot);
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
}
