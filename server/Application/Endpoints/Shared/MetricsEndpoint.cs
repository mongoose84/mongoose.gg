using RiotProxy.Utilities;

namespace RiotProxy.Application.Endpoints
{
    public class MetricsEndpoint : IEndpoint
    {
        public string Route { get; }
        public MetricsEndpoint(string basePath)
        {
            Route = $"{basePath}/metrics";
        }

        public void Configure(WebApplication app)
        {
            app.MapGet(Route, () =>
            {
                Metrics.IncrementMetrics();
                var metrics = Metrics.GetMetricsJson();
                return Results.Content(metrics, "application/json");
            });
        }

    }
}