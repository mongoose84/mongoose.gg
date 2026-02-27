using Mongoose.Api.Application.Endpoints;

namespace Mongoose.Api.Application.Extensions;

/// <summary>
/// Extension for automatically discovering and registering all IEndpoint implementations.
/// Uses reflection to find all endpoint classes without manual registration.
/// </summary>
public static class EndpointDiscoveryExtension
{
    /// <summary>
    /// Discovers all IEndpoint implementations in the application assembly and creates instances.
    /// </summary>
    /// <param name="basePath">The API base path (e.g., "/api/v2")</param>
    /// <returns>A list of all discovered endpoint instances</returns>
    public static IList<IEndpoint> DiscoverEndpoints(string basePath)
    {
        var endpoints = new List<IEndpoint>();

        // Get all types in the current assembly
        var assembly = typeof(IEndpoint).Assembly;
        var endpointTypes = assembly.GetTypes()
            .Where(t =>
                // Type must implement IEndpoint
                typeof(IEndpoint).IsAssignableFrom(t) &&
                // Must be a concrete class (not abstract or interface)
                !t.IsAbstract &&
                !t.IsInterface &&
                // Must not be generic
                !t.IsGenericTypeDefinition)
            .OrderBy(t => t.Name) // Sort for consistent ordering
            .ToList();

        if (endpointTypes.Count == 0)
        {
            throw new InvalidOperationException(
                "No IEndpoint implementations found. " +
                "Ensure at least one endpoint class implements IEndpoint.");
        }

        // Instantiate each endpoint with the basePath parameter
        foreach (var endpointType in endpointTypes)
        {
            try
            {
                // Find constructor that takes basePath string parameter
                var constructor = endpointType.GetConstructor(new[] { typeof(string) });
                if (constructor == null)
                {
                    throw new InvalidOperationException(
                        $"Endpoint {endpointType.FullName} must have a constructor that accepts a string (basePath) parameter.");
                }

                // Create instance
                var endpoint = (IEndpoint?)Activator.CreateInstance(endpointType, basePath);
                if (endpoint == null)
                {
                    throw new InvalidOperationException(
                        $"Failed to instantiate endpoint {endpointType.FullName}.");
                }

                endpoints.Add(endpoint);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Error discovering endpoint {endpointType.FullName}: {ex.Message}",
                    ex);
            }
        }

        return endpoints;
    }
}
