namespace RiotProxy.Application.Endpoints;

/// <summary>
/// Represents a generic endpoint interface for handling API requests
/// </summary>
public interface IEndpoint
{
    /// <summary>
    /// Gets the route pattern for the endpoint
    /// </summary>
    string Route { get; }

    /// <summary>
    /// Maps the endpoint to the application's request pipeline
    /// </summary>
    /// <param name="app">The web application builder</param>
    void Configure(WebApplication app);
}