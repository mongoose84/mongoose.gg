namespace Mongoose.Api.Application.Endpoints.Shared;

/// <summary>
/// Resolves the client IP address from the current HTTP connection.
/// Relies on ASP.NET Core forwarded headers middleware to normalize
/// <see cref="HttpContext.Connection"/> values when behind reverse proxies.
/// </summary>
public static class ClientIpAddressResolver
{
    /// <summary>
    /// Gets the best-known client IP address for rate limiting and diagnostics.
    /// </summary>
    /// <param name="context">Current HTTP context.</param>
    /// <returns>The client IP address as a string, or null when unavailable.</returns>
    public static string? GetClientIpAddress(HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString();
    }
}