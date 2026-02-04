using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using RiotProxy.Application.Endpoints.Shared;
using RiotProxy.Core.Interfaces;
using System.Text.Json;

namespace RiotProxy.Infrastructure.RateLimiting;

/// <summary>
/// Rate limiter implementation using distributed cache.
/// Uses a sliding window counter algorithm for rate limiting.
/// </summary>
public sealed class EndpointRateLimiter : IRateLimiter
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<EndpointRateLimiter> _logger;

    public EndpointRateLimiter(IDistributedCache cache, ILogger<EndpointRateLimiter> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<RateLimitResult> CheckAsync(string key, int limit, TimeSpan window)
    {
        var cacheKey = $"ratelimit:{key}";
        var now = DateTimeOffset.UtcNow;
        
        try
        {
            // Get current state from cache
            var stateJson = await _cache.GetStringAsync(cacheKey);
            RateLimitState state;
            
            if (string.IsNullOrEmpty(stateJson))
            {
                // First request - create new state
                state = new RateLimitState
                {
                    Count = 1,
                    WindowStart = now
                };
            }
            else
            {
                state = JsonSerializer.Deserialize<RateLimitState>(stateJson)!;
                
                // Check if window has expired
                if (now - state.WindowStart >= window)
                {
                    // Reset window
                    state = new RateLimitState
                    {
                        Count = 1,
                        WindowStart = now
                    };
                }
                else
                {
                    // Within window - check limit
                    if (state.Count >= limit)
                    {
                        var retryAfter = window - (now - state.WindowStart);
                        _logger.LogWarning(
                            "Rate limit exceeded for key {Key}. Count: {Count}, Limit: {Limit}, RetryAfter: {RetryAfter}s",
                            LogSanitizer.Sanitize(key), state.Count, limit, retryAfter.TotalSeconds);

                        return new RateLimitResult(
                            IsAllowed: false,
                            RemainingRequests: 0,
                            RetryAfter: retryAfter
                        );
                    }
                    
                    state.Count++;
                }
            }
            
            // Save updated state
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = window
            };
            
            await _cache.SetStringAsync(
                cacheKey, 
                JsonSerializer.Serialize(state), 
                options);
            
            return new RateLimitResult(
                IsAllowed: true,
                RemainingRequests: limit - state.Count,
                RetryAfter: null
            );
        }
        catch (Exception ex)
        {
            // On cache failure, allow the request but log the error
            _logger.LogError(ex, "Rate limiter cache error for key {Key}. Allowing request.", LogSanitizer.Sanitize(key));
            return new RateLimitResult(
                IsAllowed: true,
                RemainingRequests: limit,
                RetryAfter: null
            );
        }
    }

    public async Task<RateLimitResult> CheckEndpointAsync(
        string endpointName,
        string? ipAddress,
        long? userId,
        int limit,
        TimeSpan window)
    {
        // Prefer user ID for authenticated users, fall back to IP
        string key;
        if (userId.HasValue)
        {
            key = $"{endpointName}:user:{userId.Value}";
        }
        else if (!string.IsNullOrEmpty(ipAddress))
        {
            key = $"{endpointName}:ip:{ipAddress}";
        }
        else
        {
            // No identifier available - use a generic key (very permissive)
            key = $"{endpointName}:unknown";
            _logger.LogWarning("Rate limiting with no identifier for endpoint {Endpoint}", LogSanitizer.Sanitize(endpointName));
        }
        
        return await CheckAsync(key, limit, window);
    }

    private class RateLimitState
    {
        public int Count { get; set; }
        public DateTimeOffset WindowStart { get; set; }
    }
}

