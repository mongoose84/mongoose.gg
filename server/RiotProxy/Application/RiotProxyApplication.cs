using RiotProxy.Application.Endpoints;

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

            var metricsEndpoint = new MetricsEndpoint(_basePath);
            _endpoints.Add(metricsEndpoint);

            var userEndpoint = new UserEndpoint(_basePath);
            _endpoints.Add(userEndpoint);

            var usersEndpoint = new UsersEndpoint(_basePath);
            _endpoints.Add(usersEndpoint);

            var gamersEndpoint = new GamersEndpoint(_basePath);
            _endpoints.Add(gamersEndpoint);

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

            var duoVsSoloPerformanceEndpoint = new DuoVsSoloPerformanceEndpoint(_basePath);
            _endpoints.Add(duoVsSoloPerformanceEndpoint);

            var championSynergyEndpoint = new ChampionSynergyEndpoint(_basePath);
            _endpoints.Add(championSynergyEndpoint);

            var duoVsEnemyEndpoint = new DuoVsEnemyEndpoint(_basePath);
            _endpoints.Add(duoVsEnemyEndpoint);

            var duoRoleConsistencyEndpoint = new DuoRoleConsistencyEndpoint(_basePath);
            _endpoints.Add(duoRoleConsistencyEndpoint);

            var duoLaneMatchupEndpoint = new DuoLaneMatchupEndpoint(_basePath);
            _endpoints.Add(duoLaneMatchupEndpoint);

            var duoKillEfficiencyEndpoint = new DuoKillEfficiencyEndpoint(_basePath);
            _endpoints.Add(duoKillEfficiencyEndpoint);

            var duoMatchDurationEndpoint = new DuoMatchDurationEndpoint(_basePath);
            _endpoints.Add(duoMatchDurationEndpoint);

            var duoImprovementSummaryEndpoint = new DuoImprovementSummaryEndpoint(_basePath);
            _endpoints.Add(duoImprovementSummaryEndpoint);

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