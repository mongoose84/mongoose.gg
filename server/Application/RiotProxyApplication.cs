using RiotProxy.Application.Endpoints;
using RiotProxy.Application.Endpoints.Auth;
using RiotProxy.Application.Endpoints.Diagnostics;
using RiotProxy.Application.Endpoints.Solo;

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

            

            // Auth endpoints (no auth required)
            var registerEndpoint = new RegisterEndpoint(basePath);
            _endpoints.Add(registerEndpoint);

            var loginEndpoint = new LoginEndpoint(basePath);
            _endpoints.Add(loginEndpoint);

            var logoutEndpoint = new LogoutEndpoint(basePath);
            _endpoints.Add(logoutEndpoint);

            var verifyEndpoint = new VerifyEndpoint(basePath);
            _endpoints.Add(verifyEndpoint);

            // Users (v2) - auth required
            var usersMeEndpoint = new UsersMeEndpoint(basePath);
            _endpoints.Add(usersMeEndpoint);

            // Riot account linking endpoints (v2) - auth required
            var riotAccountsEndpoint = new RiotAccountsEndpoint(basePath);
            _endpoints.Add(riotAccountsEndpoint);
            
            // Solo Dashboard V2 (auth required)
            var soloDashboardV2Endpoint = new SoloDashboardV2Endpoint(basePath);
            _endpoints.Add(soloDashboardV2Endpoint);           
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