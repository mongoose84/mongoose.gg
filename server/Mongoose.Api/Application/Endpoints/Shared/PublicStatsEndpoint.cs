using Microsoft.AspNetCore.Mvc;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Core.Interfaces;

namespace Mongoose.Api.Application.Endpoints
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
                var clientIp = ClientIpAddressResolver.GetClientIpAddress(httpContext);
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
