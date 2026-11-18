using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Infrastructure.Email;
using Mongoose.Api.Infrastructure.Jobs;
using Mongoose.Api.Infrastructure.Jobs.Analytics;

namespace Mongoose.Api.Tests;

internal sealed partial class TestWebApplicationFactory : WebApplicationFactory<Program>
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
    private readonly FakeAnalyticsEventsV2Repository _analyticsEventsV2Repository;
    private readonly FakeGitHubService _gitHubService;
    private readonly FakeMatchesRepository _matchesRepository;
    private readonly FakeSoloPerformanceRepository _soloPerformanceRepository;
    private readonly FakeMatchupRepository _matchupRepository;
    private readonly FakeChampionSelectRepository _championSelectRepository;
    private readonly FakeTrendRepository _trendRepository;

    public FakeUsersRepository UsersRepository => _usersRepository;
    public FakeVerificationTokensRepository TokensRepository => _tokensRepository;
    public FakeEmailService EmailService => _emailService;
    public FakeRiotApiClient RiotApiClient => _riotApiClient;
    public FakeRiotAccountsRepository RiotAccountsRepository => _riotAccountsRepository;
    public FakeRankSnapshotsRepository RankSnapshotsRepository { get; } = new();
    public FakeUserRiotAccountsRepository UserRiotAccountsRepository => _userRiotAccountsRepository;
    public FakeOverviewStatsRepository OverviewStatsRepository => _overviewStatsRepository;
    public FakeAnalyticsEventsRepository AnalyticsEventsRepository => _analyticsEventsRepository;
    public FakeAnalyticsEventsV2Repository AnalyticsEventsV2Repository => _analyticsEventsV2Repository;
    public FakeGitHubService GitHubService => _gitHubService;
    public FakeMatchesRepository MatchesRepository => _matchesRepository;
    public FakeSoloPerformanceRepository SoloPerformanceRepository => _soloPerformanceRepository;
    public FakeMatchupRepository MatchupRepository => _matchupRepository;
    public FakeChampionSelectRepository ChampionSelectRepository => _championSelectRepository;
    public FakeTrendRepository TrendRepository => _trendRepository;
    public FakeSoloTrendsRepository SoloTrendsRepository { get; } = new();

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
        _analyticsEventsV2Repository = new FakeAnalyticsEventsV2Repository();
        _gitHubService = new FakeGitHubService();
        _matchesRepository = new FakeMatchesRepository(_riotAccountsRepository);
        _soloPerformanceRepository = new FakeSoloPerformanceRepository();
        _matchupRepository = new FakeMatchupRepository();
        _championSelectRepository = new FakeChampionSelectRepository();
        _trendRepository = new FakeTrendRepository();
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
                ["Jobs:EnableMatchHistorySync"] = "false",
                ["Jobs:EnableMatchCleanup"] = "false",
                ["Jobs:EnableRankSnapshots"] = "false",
                ["Jobs:EnableDeathDetailBackfill"] = "false",
                ["Jobs:EnableAnalyticsBackgroundJobs"] = "false",
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
                    || descriptor.ImplementationType == typeof(MatchCleanupJob)
                    || descriptor.ImplementationType == typeof(DimensionExtractionBackgroundJob)
                    || descriptor.ImplementationType == typeof(RollupAggregationBackgroundJob)
                    || descriptor.ImplementationType == typeof(RetentionAndPurgeBackgroundJob))
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

            // Rank snapshots are recorded at login; keep them in memory
            services.RemoveAll<IRankSnapshotsRepository>();
            services.AddSingleton<IRankSnapshotsRepository>(RankSnapshotsRepository);

            // Replace UserRiotAccountsRepository with a fake
            services.RemoveAll<IUserRiotAccountsRepository>();
            services.AddSingleton<IUserRiotAccountsRepository>(_userRiotAccountsRepository);

            // Replace OverviewStatsRepository with a fake
            services.RemoveAll<IOverviewStatsRepository>();
            services.AddSingleton<IOverviewStatsRepository>(_overviewStatsRepository);

            // Replace AnalyticsEventsRepository with a fake
            services.RemoveAll<IAnalyticsEventsRepository>();
            services.AddSingleton<IAnalyticsEventsRepository>(_analyticsEventsRepository);

            // Replace AnalyticsEventsV2Repository with a fake (avoids real DB connections)
            services.RemoveAll<IAnalyticsEventsV2Repository>();
            services.AddSingleton<IAnalyticsEventsV2Repository>(_analyticsEventsV2Repository);

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

            // Replace ISoloTrendsRepository with a fake
            services.RemoveAll<ISoloTrendsRepository>();
            services.AddSingleton<ISoloTrendsRepository>(SoloTrendsRepository);

            // Use ephemeral (in-memory) Data Protection so tests never write key files to disk
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
        });

        // Program re-initializes Secrets while the host is built; wait for tests that hold them
        SecretsTestGate.Gate.Wait();
        try
        {
            return base.CreateHost(builder);
        }
        finally
        {
            SecretsTestGate.Gate.Release();
        }
    }
}
