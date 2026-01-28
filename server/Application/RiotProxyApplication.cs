using RiotProxy.Application.Endpoints;
using RiotProxy.Application.Endpoints.Analytics;
using RiotProxy.Application.Endpoints.Auth;
using RiotProxy.Application.Endpoints.ChampionSelect;
using RiotProxy.Application.Endpoints.Diagnostics;
using RiotProxy.Application.Endpoints.Matches;
using RiotProxy.Application.Endpoints.Overview;
using RiotProxy.Application.Endpoints.Solo;
using RiotProxy.Application.Endpoints.Trends;

namespace RiotProxy.Application
{
    public class RiotProxyApplication
    {
        private readonly WebApplication _app;
        private readonly IList<IEndpoint> _endpoints = [];
        public RiotProxyApplication(WebApplication app)
        {
            _app = app;
            var apiVersion = "v2";
            var basePath = "/api/" + apiVersion;
            var homeEndPoint = new HomeEndpoint(apiVersion, basePath);
            _endpoints.Add(homeEndPoint);

            // Diagnostics endpoint (public, no auth required)
            var diagnosticsEndpoint = new DiagnosticsEndpoint(basePath);
            _endpoints.Add(diagnosticsEndpoint);

            // Public stats endpoint (no auth required)
            var publicStatsEndpoint = new PublicStatsEndpoint(basePath);
            _endpoints.Add(publicStatsEndpoint);

            // Auth endpoints (no auth required)
            var registerEndpoint = new RegisterEndpoint(basePath);
            _endpoints.Add(registerEndpoint);

            var loginEndpoint = new LoginEndpoint(basePath);
            _endpoints.Add(loginEndpoint);

            var logoutEndpoint = new LogoutEndpoint(basePath);
            _endpoints.Add(logoutEndpoint);

            var verifyEndpoint = new VerifyEndpoint(basePath);
            _endpoints.Add(verifyEndpoint);

            var resendVerificationEndpoint = new ResendVerificationEndpoint(basePath);
            _endpoints.Add(resendVerificationEndpoint);

            // Users endpoint - auth required
            var usersMeEndpoint = new UsersMeEndpoint(basePath);
            _endpoints.Add(usersMeEndpoint);

            // Riot account linking endpoints - auth required
            var riotAccountsEndpoint = new RiotAccountsEndpoint(basePath);
            _endpoints.Add(riotAccountsEndpoint);
            
            // Solo Dashboard (auth required)
            var soloDashboardEndpoint = new SoloDashboardEndpoint(basePath);
            _endpoints.Add(soloDashboardEndpoint);

            // Champion Select (auth required)
            var championSelectEndpoint = new ChampionSelectEndpoint(basePath);
            _endpoints.Add(championSelectEndpoint);

            // Solo Matchups (auth required)
            var soloMatchupsEndpoint = new SoloMatchupsEndpoint(basePath);
            _endpoints.Add(soloMatchupsEndpoint);

            // Match Activity Heatmap (auth required)
            var matchActivityEndpoint = new MatchActivityEndpoint(basePath);
            _endpoints.Add(matchActivityEndpoint);

            // Match List (auth required)
            var matchListEndpoint = new MatchListEndpoint(basePath);
            _endpoints.Add(matchListEndpoint);

            // Match Narrative (auth required)
            var matchNarrativeEndpoint = new MatchNarrativeEndpoint(basePath);
            _endpoints.Add(matchNarrativeEndpoint);

            // Trends endpoints (shared, auth required)
            var winrateTrendEndpoint = new WinrateTrendEndpoint(basePath);
            _endpoints.Add(winrateTrendEndpoint);

            // Overview endpoint (auth required)
            var overviewEndpoint = new OverviewEndpoint(basePath);
            _endpoints.Add(overviewEndpoint);

            // Analytics endpoint (public, no auth required - captures anonymous + authenticated events)
            var analyticsEndpoint = new AnalyticsEndpoint(basePath);
            _endpoints.Add(analyticsEndpoint);
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