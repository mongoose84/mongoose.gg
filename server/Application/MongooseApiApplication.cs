using Mongoose.Api.Application.Endpoints;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Application.Extensions;

namespace Mongoose.Api.Application;

/// <summary>
/// Automatically discovers and configures all endpoint implementations.
/// Eliminates manual registration - endpoints are discovered via reflection.
/// </summary>
public sealed class MongooseApiApplication
{
    private readonly WebApplication _app;
    private readonly ILogger<MongooseApiApplication> _logger;
    private readonly IList<IEndpoint> _endpoints;

    /// <summary>
    /// Initialize the application with automatic endpoint discovery.
    /// </summary>
    /// <param name="app">The WebApplication instance</param>
    /// <exception cref="InvalidOperationException">Thrown if endpoint discovery fails</exception>
    public MongooseApiApplication(WebApplication app)
    {
        _app = app;
        _logger = app.Services.GetRequiredService<ILogger<MongooseApiApplication>>();

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
        var shouldLogEndpointDiscovery = !_app.Environment.IsEnvironment("Testing");

        if (shouldLogEndpointDiscovery)
        {
            _logger.LogInformation("Configuring {EndpointCount} discovered endpoints", _endpoints.Count);
        }

        // Group endpoints by category for informative logging
        var groupedEndpoints = _endpoints
            .GroupBy(e => ExtractCategory(e.GetType().Name))
            .OrderBy(g => g.Key);

        foreach (var group in groupedEndpoints)
        {
            if (shouldLogEndpointDiscovery)
            {
                _logger.LogInformation("[{Category}]", LogSanitizer.Sanitize(group.Key));
            }

            foreach (var endpoint in group.OrderBy(e => e.Route))
            {
                endpoint.Configure(_app);
                if (shouldLogEndpointDiscovery)
                {
                    _logger.LogInformation("{Route}", LogSanitizer.Sanitize(endpoint.Route));
                }
            }
        }

        if (shouldLogEndpointDiscovery)
        {
            _logger.LogInformation("All {EndpointCount} endpoints configured successfully", _endpoints.Count);
        }
    }

    /// <summary>
    /// Extracts the endpoint category from the class name for logging organization.
    /// E.g., "SoloPerformanceEndpoint" -> "Solo"
    /// </summary>
    private static string ExtractCategory(string className)
    {
        // Remove "Endpoint" suffix
        var withoutEndpoint = className.Replace("Endpoint", string.Empty);

        // Match real endpoint naming patterns (e.g., SoloPerformance, MatchDetails, WinrateTrend)
        if (withoutEndpoint.EndsWith("Trend", StringComparison.Ordinal))
            return "Trends";

        if (withoutEndpoint.StartsWith("Solo", StringComparison.Ordinal)
            || withoutEndpoint is "RadarChart" or "DeathPositions" or "MatchActivity")
            return "Solo Dashboard";

        if (withoutEndpoint.StartsWith("Match", StringComparison.Ordinal))
            return "Matches";

        if (withoutEndpoint == "ChampionSelect")
            return "Real-time";

        if (withoutEndpoint == "Overview")
            return "Overview";

        if (withoutEndpoint == "Analytics")
            return "Analytics";

        if (withoutEndpoint == "Feedback")
            return "Feedback";

        if (withoutEndpoint == "Diagnostics")
            return "Diagnostics";

        if (withoutEndpoint == "Home")
            return "Status";

        if (withoutEndpoint == "PublicStats")
            return "Public";

        if (withoutEndpoint == "UsersMe")
            return "User Management";

        if (withoutEndpoint == "RiotAccounts")
            return "Account Linking";

        if (withoutEndpoint is "Register"
            or "Login"
            or "Logout"
            or "Verify"
            or "ResendVerification"
            or "ForgotPassword"
            or "ResetPassword"
            or "ChangePassword"
            or "DeleteAccount")
            return "Authentication";

        return withoutEndpoint;
    }
}