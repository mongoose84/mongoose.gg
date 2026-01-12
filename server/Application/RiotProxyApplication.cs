using RiotProxy.Application.Endpoints;
using RiotProxy.Application.Endpoints.Auth;
using RiotProxy.Application.Endpoints.Diagnostics;
using RiotProxy.Application.Endpoints.Solo;

namespace RiotProxy.Application
{
    public class RiotProxyApplication
    {
        private readonly WebApplication _app;
        private readonly string _apiVersion = "v1.0";
        private readonly string _basePath;
        private readonly IList<IEndpoint> _endpoints = [];
        public RiotProxyApplication(WebApplication app)
        {
            _app = app;
            _basePath = "/api/" + _apiVersion;

            var homeEndPoint = new HomeEndpoint(_apiVersion, _basePath);
            _endpoints.Add(homeEndPoint);

            // Diagnostics endpoint (public, no auth required)
            var diagnosticsEndpoint = new DiagnosticsEndpoint(_basePath);
            _endpoints.Add(diagnosticsEndpoint);

            var userEndpoint = new UserEndpoint(_basePath);
            _endpoints.Add(userEndpoint);

            var usersEndpoint = new UsersEndpoint(_basePath);
            _endpoints.Add(usersEndpoint);

            var gamersEndpoint = new GamersEndpoint(_basePath);
            _endpoints.Add(gamersEndpoint);

            // ========== V2 API Endpoints ==========
            var basePath_v2 = "/api/v2";

            // Auth endpoints (no auth required)
            var registerEndpoint = new RegisterEndpoint(basePath_v2);
            _endpoints.Add(registerEndpoint);

            var loginEndpoint = new LoginEndpoint(basePath_v2);
            _endpoints.Add(loginEndpoint);

            var logoutEndpoint = new LogoutEndpoint(basePath_v2);
            _endpoints.Add(logoutEndpoint);

            var verifyEndpoint = new VerifyEndpoint(basePath_v2);
            _endpoints.Add(verifyEndpoint);

            // Users (v2) - auth required
            var usersMeEndpoint = new UsersMeEndpoint(basePath_v2);
            _endpoints.Add(usersMeEndpoint);

            var usersV2Endpoint = new UsersV2Endpoint(basePath_v2);
            _endpoints.Add(usersV2Endpoint);

            // Riot account linking endpoints (v2) - auth required
            var riotAccountsEndpoint = new RiotAccountsEndpoint(basePath_v2);
            _endpoints.Add(riotAccountsEndpoint);
            
            // Solo Dashboard V2 (auth required)
            var soloDashboardV2Endpoint = new SoloDashboardV2Endpoint(basePath_v2);
            _endpoints.Add(soloDashboardV2Endpoint);

            // ========== V1 API Endpoints ==========
            var overallStatsEndpoint = new ComparisonEndpoint(_basePath);
            _endpoints.Add(overallStatsEndpoint);

            var performanceTimelineEndpoint = new PerformanceTimelineEndpoint(_basePath);
            _endpoints.Add(performanceTimelineEndpoint);

            var championPerformanceEndpoint = new ChampionPerformanceEndpoint(_basePath);
            _endpoints.Add(championPerformanceEndpoint);

            var roleDistributionEndpoint = new RoleDistributionEndpoint(_basePath);
            _endpoints.Add(roleDistributionEndpoint);

            var matchDurationEndpoint = new MatchDurationEndpoint(_basePath);
            _endpoints.Add(matchDurationEndpoint);

            var championMatchupsEndpoint = new ChampionMatchupsEndpoint(_basePath);
            _endpoints.Add(championMatchupsEndpoint);

            var duoStatsEndpoint = new DuoStatsEndpoint(_basePath);
            _endpoints.Add(duoStatsEndpoint);

            var championSynergyEndpoint = new ChampionSynergyEndpoint(_basePath);
            _endpoints.Add(championSynergyEndpoint);

            var duoVsEnemyEndpoint = new DuoVsEnemyEndpoint(_basePath);
            _endpoints.Add(duoVsEnemyEndpoint);

            var duoMatchDurationEndpoint = new DuoMatchDurationEndpoint(_basePath);
            _endpoints.Add(duoMatchDurationEndpoint);

            var duoImprovementSummaryEndpoint = new DuoImprovementSummaryEndpoint(_basePath);
            _endpoints.Add(duoImprovementSummaryEndpoint);

            // Duo Kill Analysis endpoints
            var duoMultiKillsEndpoint = new DuoMultiKillsEndpoint(_basePath);
            _endpoints.Add(duoMultiKillsEndpoint);

            var duoKillsByPhaseEndpoint = new DuoKillsByPhaseEndpoint(_basePath);
            _endpoints.Add(duoKillsByPhaseEndpoint);

            var duoKillParticipationEndpoint = new DuoKillParticipationEndpoint(_basePath);
            _endpoints.Add(duoKillParticipationEndpoint);

            var duoKillsTrendEndpoint = new DuoKillsTrendEndpoint(_basePath);
            _endpoints.Add(duoKillsTrendEndpoint);

            // Duo Death Analysis endpoints
            var duoDeathTimerImpactEndpoint = new DuoDeathTimerImpactEndpoint(_basePath);
            _endpoints.Add(duoDeathTimerImpactEndpoint);

            var duoDeathsByDurationEndpoint = new DuoDeathsByDurationEndpoint(_basePath);
            _endpoints.Add(duoDeathsByDurationEndpoint);

            var duoDeathShareEndpoint = new DuoDeathShareEndpoint(_basePath);
            _endpoints.Add(duoDeathShareEndpoint);

            var duoDeathsTrendEndpoint = new DuoDeathsTrendEndpoint(_basePath);
            _endpoints.Add(duoDeathsTrendEndpoint);

            // Duo Trend Analysis endpoints
            var duoWinRateTrendEndpoint = new DuoWinRateTrendEndpoint(_basePath);
            _endpoints.Add(duoWinRateTrendEndpoint);

            var duoPerformanceRadarEndpoint = new DuoPerformanceRadarEndpoint(_basePath);
            _endpoints.Add(duoPerformanceRadarEndpoint);

            var duoStreakEndpoint = new DuoStreakEndpoint(_basePath);
            _endpoints.Add(duoStreakEndpoint);

            var duoLatestGameEndpoint = new DuoLatestGameEndpoint(_basePath);
            _endpoints.Add(duoLatestGameEndpoint);

            // Team endpoints (3+ players)
            var teamStatsEndpoint = new TeamStatsEndpoint(_basePath);
            _endpoints.Add(teamStatsEndpoint);

            var teamSynergyEndpoint = new TeamSynergyEndpoint(_basePath);
            _endpoints.Add(teamSynergyEndpoint);

            var teamWinRateTrendEndpoint = new TeamWinRateTrendEndpoint(_basePath);
            _endpoints.Add(teamWinRateTrendEndpoint);

            var teamDurationAnalysisEndpoint = new TeamDurationAnalysisEndpoint(_basePath);
            _endpoints.Add(teamDurationAnalysisEndpoint);

            var teamChampionCombosEndpoint = new TeamChampionCombosEndpoint(_basePath);
            _endpoints.Add(teamChampionCombosEndpoint);

            var teamRolePairEffectivenessEndpoint = new TeamRolePairEffectivenessEndpoint(_basePath);
            _endpoints.Add(teamRolePairEffectivenessEndpoint);

            // Team Kill Analysis endpoints
            var teamKillParticipationEndpoint = new TeamKillParticipationEndpoint(_basePath);
            _endpoints.Add(teamKillParticipationEndpoint);

            var teamKillsByPhaseEndpoint = new TeamKillsByPhaseEndpoint(_basePath);
            _endpoints.Add(teamKillsByPhaseEndpoint);

            var teamKillsTrendEndpoint = new TeamKillsTrendEndpoint(_basePath);
            _endpoints.Add(teamKillsTrendEndpoint);

            var teamMultiKillsEndpoint = new TeamMultiKillsEndpoint(_basePath);
            _endpoints.Add(teamMultiKillsEndpoint);

            // Team Death Analysis endpoints
            var teamDeathTimerImpactEndpoint = new TeamDeathTimerImpactEndpoint(_basePath);
            _endpoints.Add(teamDeathTimerImpactEndpoint);

            var teamDeathsByDurationEndpoint = new TeamDeathsByDurationEndpoint(_basePath);
            _endpoints.Add(teamDeathsByDurationEndpoint);

            var teamDeathShareEndpoint = new TeamDeathShareEndpoint(_basePath);
            _endpoints.Add(teamDeathShareEndpoint);

            var teamDeathsTrendEndpoint = new TeamDeathsTrendEndpoint(_basePath);
            _endpoints.Add(teamDeathsTrendEndpoint);

            var teamLatestGameEndpoint = new TeamLatestGameEndpoint(_basePath);
            _endpoints.Add(teamLatestGameEndpoint);

            // Side stats endpoint (blue/red win rates)
            var sideStatsEndpoint = new SideStatsEndpoint(_basePath);
            _endpoints.Add(sideStatsEndpoint);

            // Dev endpoint for refreshing games
            var refreshGamesEndpoint = new RefreshGamesEndpoint(_basePath);
            _endpoints.Add(refreshGamesEndpoint);

            // Admin endpoint for backfilling data
            var backfillDataEndpoint = new BackfillDataEndpoint(_basePath);
            _endpoints.Add(backfillDataEndpoint);
        }

        public void ConfigureEndpoints()
        {
            Console.WriteLine("Available endpoints:");
            foreach (var endpoint in _endpoints)
            {
                endpoint.Configure(_app);
                Console.WriteLine(endpoint.Route);
            }
        }
    }
}