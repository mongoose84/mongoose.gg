using Mongoose.Api.Application.Endpoints;
using Mongoose.Api.Application.Extensions;

namespace Mongoose.Api.Application;

/// <summary>
/// Automatically discovers and configures all endpoint implementations.
/// Eliminates manual registration - endpoints are discovered via reflection.
/// </summary>
public sealed class MongooseApiApplication
{
    private readonly WebApplication _app;
    private readonly IList<IEndpoint> _endpoints;

    /// <summary>
    /// Initialize the application with automatic endpoint discovery.
    /// </summary>
    /// <param name="app">The WebApplication instance</param>
    /// <exception cref="InvalidOperationException">Thrown if endpoint discovery fails</exception>
    public MongooseApiApplication(WebApplication app)
    {
        _app = app;

        const string apiVersion = "v2";
        const string basePath = "/api/" + apiVersion;

        // Automatically discover all IEndpoint implementations
        try
        {
            _endpoints = EndpointDiscoveryExtension.DiscoverEndpoints(basePath);

            if (_endpoints.Count == 0)
            {
                throw new InvalidOperationException(
                    "No endpoints were discovered. Check that endpoint classes implement IEndpoint.");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to initialize MongooseApiApplication - endpoint discovery failed: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Configures all discovered endpoints with the WebApplication.
    /// Call this after Application/Infrastructure services are registered.
    /// </summary>
    public void ConfigureEndpoints()
    {
        Console.WriteLine($"Configuring {_endpoints.Count} discovered endpoints:");
        Console.WriteLine();

        // Group endpoints by category for informative logging
        var groupedEndpoints = _endpoints
            .GroupBy(e => ExtractCategory(e.GetType().Name))
            .OrderBy(g => g.Key);

        foreach (var group in groupedEndpoints)
        {
            Console.WriteLine($"  [{group.Key}]");
            foreach (var endpoint in group.OrderBy(e => e.Route))
            {
                endpoint.Configure(_app);
                Console.WriteLine($"    {endpoint.Route}");
            }
            Console.WriteLine();
        }

        Console.WriteLine($"✓ All {_endpoints.Count} endpoints configured successfully");
    }

    /// <summary>
    /// Extracts the endpoint category from the class name for logging organization.
    /// E.g., "SoloPerformanceEndpoint" -> "Solo"
    /// </summary>
    private static string ExtractCategory(string className)
    {
        // Remove "Endpoint" suffix
        var withoutEndpoint = className.Replace("Endpoint", string.Empty);

        // Special cases for clarity
        return withoutEndpoint switch
        {
            "Auth" => "Authentication",
            "Users" => "User Management",
            "RiotAccounts" => "Account Linking",
            "Solo" => "Solo Dashboard",
            "ChampionSelect" => "Real-time",
            "Match" => "Matches",
            "Trend" or "Winrate" or "GoldAt15" or "CsPerMinute" or "Deaths" or "DragonParticipation" or "VisionScore" => "Trends",
            "Overview" => "Overview",
            "Analytics" => "Analytics",
            "Feedback" => "Feedback",
            "Diagnostics" => "Diagnostics",
            "Home" => "Status",
            "PublicStats" => "Public",
            _ => withoutEndpoint
        };
    }
}