using Mongoose.Api.Application.Endpoints;
using Mongoose.Api.Application.Endpoints.Analytics;
using Mongoose.Api.Application.Endpoints.Auth;
using Mongoose.Api.Application.Endpoints.ChampionSelect;
using Mongoose.Api.Application.Endpoints.Diagnostics;
using Mongoose.Api.Application.Endpoints.Feedback;
using Mongoose.Api.Application.Endpoints.Matches;
using Mongoose.Api.Application.Endpoints.Overview;
using Mongoose.Api.Application.Endpoints.Solo;
using Mongoose.Api.Application.Endpoints.Trends;

namespace Mongoose.Api.Application
{
    public class MongooseApiApplication
    {
        private readonly WebApplication _app;
        private readonly IList<IEndpoint> _endpoints = [];
        public MongooseApiApplication(WebApplication app)
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

            var deleteAccountEndpoint = new DeleteAccountEndpoint(basePath);
            _endpoints.Add(deleteAccountEndpoint);

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
            
            // Solo Performance (auth required)
            var soloPerformanceEndpoint = new SoloPerformanceEndpoint(basePath);
            _endpoints.Add(soloPerformanceEndpoint);

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

            // Match Details (auth required) - on-demand full match data
            var matchDetailsEndpoint = new MatchDetailsEndpoint(basePath);
            _endpoints.Add(matchDetailsEndpoint);

            // Match Narrative (auth required)
            var matchNarrativeEndpoint = new MatchNarrativeEndpoint(basePath);
            _endpoints.Add(matchNarrativeEndpoint);

            // Trends endpoints (shared, auth required)
            var winrateTrendEndpoint = new WinrateTrendEndpoint(basePath);
            _endpoints.Add(winrateTrendEndpoint);

            var goldAt15TrendEndpoint = new GoldAt15TrendEndpoint(basePath);
            _endpoints.Add(goldAt15TrendEndpoint);

            var csPerMinuteTrendEndpoint = new CsPerMinuteTrendEndpoint(basePath);
            _endpoints.Add(csPerMinuteTrendEndpoint);

            var deathsTrendEndpoint = new DeathsTrendEndpoint(basePath);
            _endpoints.Add(deathsTrendEndpoint);

            var dragonParticipationTrendEndpoint = new DragonParticipationTrendEndpoint(basePath);
            _endpoints.Add(dragonParticipationTrendEndpoint);

            // Overview endpoint (auth required)
            var overviewEndpoint = new OverviewEndpoint(basePath);
            _endpoints.Add(overviewEndpoint);

            // Analytics endpoint (public, no auth required - captures anonymous + authenticated events)
            var analyticsEndpoint = new AnalyticsEndpoint(basePath);
            _endpoints.Add(analyticsEndpoint);

            // Feedback endpoint (public, no auth required - captures user context if authenticated)
            var feedbackEndpoint = new FeedbackEndpoint(basePath);
            _endpoints.Add(feedbackEndpoint);
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