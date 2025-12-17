using RiotProxy.Application.Endpoints;
using RiotProxy.Infrastructure.External.Riot;

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