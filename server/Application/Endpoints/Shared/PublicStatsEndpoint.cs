using Microsoft.AspNetCore.Mvc;
using RiotProxy.Application.Endpoints.Shared;
using RiotProxy.Core.Interfaces;

namespace RiotProxy.Application.Endpoints
{
    /// <summary>
    /// Public stats endpoint for exposing non-sensitive aggregate metrics
    /// (e.g. total matches analyzed) used on the marketing/landing page.
    /// Rate limited to 60 requests per minute per IP to prevent abuse.
    /// </summary>
    public sealed class PublicStatsEndpoint : IEndpoint
    {
        public string Route { get; }

        // Rate limiting configuration: 60 requests per minute per IP
        private const int RateLimitRequests = 60;
        private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Extracts the client IP address from the HTTP context.
        /// Checks X-Forwarded-For header first (for proxies/load balancers),
        /// then falls back to the direct connection IP.
        /// </summary>
        private static string? GetClientIpAddress(HttpContext context)
        {
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }
            return context.Connection.RemoteIpAddress?.ToString();
        }

        public PublicStatsEndpoint(string basePath)
        {
            Route = basePath + "/public/stats";
        }

        public void Configure(WebApplication app)
        {
            app.MapGet(Route, async (
                HttpContext httpContext,
                [FromServices] IMatchesRepository matchesRepository,
                [FromServices] IUsersRepository usersRepository,
                [FromServices] IRateLimiter rateLimiter,
                [FromServices] ILogger<PublicStatsEndpoint> logger) =>
            {
                // Check rate limit before processing (IP-based only)
                var clientIp = GetClientIpAddress(httpContext);
                var rateLimitResult = await rateLimiter.CheckEndpointAsync(
                    "public-stats",
                    clientIp,
                    null, // No user ID for public endpoint
                    RateLimitRequests,
                    RateLimitWindow);

                if (!rateLimitResult.IsAllowed)
                {
                    logger.LogWarning(
                        "Rate limit exceeded for public-stats endpoint. IP: {IP}",
                        LogSanitizer.Sanitize(clientIp) ?? "unknown");

                    httpContext.Response.Headers["X-RateLimit-Remaining"] = "0";
                    if (rateLimitResult.RetryAfter.HasValue)
                    {
                        httpContext.Response.Headers["Retry-After"] =
                            ((int)rateLimitResult.RetryAfter.Value.TotalSeconds).ToString();
                    }

                    return Results.Json(
                        new { error = "Too many requests. Please try again later." },
                        statusCode: 429);
                }

                var totalMatches = await matchesRepository.GetTotalMatchCountAsync();
                var activePlayers = await usersRepository.GetActiveUserCountAsync();

                return Results.Ok(new
                {
                    totalMatches,
                    activePlayers
                });
            });
        }
    }
}
