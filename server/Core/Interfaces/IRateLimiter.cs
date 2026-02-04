namespace RiotProxy.Core.Interfaces;

/// <summary>
/// Result of a rate limit check.
/// </summary>
public record RateLimitResult(
    bool IsAllowed,
    int RemainingRequests,
    TimeSpan? RetryAfter
);

/// <summary>
/// Service for rate limiting API endpoints.
/// Supports both IP-based and user-based rate limiting.
/// </summary>
public interface IRateLimiter
{
    /// <summary>
    /// Check if a request is allowed and consume a token if so.
    /// </summary>
    /// <param name="key">Unique key for rate limiting (e.g., "feedback:ip:192.168.1.1" or "feedback:user:123")</param>
    /// <param name="limit">Maximum number of requests allowed in the window</param>
    /// <param name="window">Time window for the rate limit</param>
    /// <returns>Result indicating if the request is allowed and remaining quota</returns>
    Task<RateLimitResult> CheckAsync(string key, int limit, TimeSpan window);
    
    /// <summary>
    /// Check if a request is allowed for an endpoint, using IP or user ID.
    /// This is a convenience method that builds the key automatically.
    /// </summary>
    /// <param name="endpointName">Name of the endpoint (e.g., "feedback")</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="userId">User ID if authenticated, null otherwise</param>
    /// <param name="limit">Maximum number of requests allowed in the window</param>
    /// <param name="window">Time window for the rate limit</param>
    /// <returns>Result indicating if the request is allowed and remaining quota</returns>
    Task<RateLimitResult> CheckEndpointAsync(
        string endpointName, 
        string? ipAddress, 
        long? userId, 
        int limit, 
        TimeSpan window);
}

