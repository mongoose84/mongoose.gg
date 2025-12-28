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