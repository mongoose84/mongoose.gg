using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RiotProxy.Core.Entities;
using RiotProxy.Infrastructure.Database.Repositories;
using static RiotProxy.Application.DTOs.AnalyticsDto;

namespace RiotProxy.Application.Endpoints.Analytics;

/// <summary>
/// Analytics endpoint for recording user behavior events.
/// Public endpoint (no auth required) to capture anonymous events.
/// Attaches userId when authenticated.
/// </summary>
public sealed class AnalyticsEndpoint : IEndpoint
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public string Route { get; }

    public AnalyticsEndpoint(string basePath)
    {
        Route = basePath + "/analytics";
    }

    public void Configure(WebApplication app)
    {
        // POST /api/v2/analytics - Record a single event
        app.MapPost(Route, async (
            [FromBody] TrackEventRequest request,
            HttpContext httpContext,
            [FromServices] AnalyticsEventsRepository analyticsRepo,
            [FromServices] UsersRepository usersRepo,
            [FromServices] ILogger<AnalyticsEndpoint> logger
        ) =>
        {
            try
            {
                // Validate event name
                if (string.IsNullOrWhiteSpace(request.EventName))
                {
                    return Results.BadRequest(new { error = "eventName is required" });
                }

                if (request.EventName.Length > 100)
                {
                    return Results.BadRequest(new { error = "eventName must be 100 characters or less" });
                }

                // Get user info if authenticated
                long? userId = null;
                string tier = "free";

                var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && long.TryParse(userIdClaim, out var parsedUserId))
                {
                    userId = parsedUserId;
                    // Fetch user tier
                    var user = await usersRepo.GetByIdAsync(parsedUserId);
                    if (user != null)
                    {
                        tier = user.Tier;
                    }
                }

                // Validate sessionId length (VARCHAR(64) in database)
                var sessionId = request.SessionId;
                if (sessionId != null && sessionId.Length > 64)
                {
                    sessionId = sessionId[..64]; // Truncate to fit column
                }

                // Serialize payload to JSON
                string? payloadJson = null;
                if (request.Payload != null && request.Payload.Count > 0)
                {
                    payloadJson = JsonSerializer.Serialize(request.Payload, JsonOptions);
                    // Limit payload size to prevent abuse
                    if (payloadJson.Length > 4096)
                    {
                        return Results.BadRequest(new { error = "payload too large (max 4KB)" });
                    }
                }

                var evt = new AnalyticsEvent
                {
                    UserId = userId,
                    Tier = tier,
                    EventName = request.EventName,
                    PayloadJson = payloadJson,
                    SessionId = sessionId,
                    CreatedAt = DateTime.UtcNow
                };

                await analyticsRepo.InsertAsync(evt);

                logger.LogDebug("Analytics event recorded: {EventName} for user {UserId}", 
                    request.EventName, userId?.ToString() ?? "anonymous");

                return Results.Ok(new TrackEventResponse(true));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to record analytics event: {EventName}", request.EventName);
                // Don't fail the user experience for analytics errors
                return Results.Ok(new TrackEventResponse(false));
            }
        });

        // POST /api/v2/analytics/batch - Record multiple events
        app.MapPost(Route + "/batch", async (
            [FromBody] TrackBatchRequest request,
            HttpContext httpContext,
            [FromServices] AnalyticsEventsRepository analyticsRepo,
            [FromServices] UsersRepository usersRepo,
            [FromServices] ILogger<AnalyticsEndpoint> logger
        ) =>
        {
            try
            {
                if (request.Events == null || request.Events.Length == 0)
                {
                    return Results.BadRequest(new { error = "events array is required" });
                }

                if (request.Events.Length > 50)
                {
                    return Results.BadRequest(new { error = "max 50 events per batch" });
                }

                // Get user info if authenticated
                long? userId = null;
                string tier = "free";

                var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && long.TryParse(userIdClaim, out var parsedUserId))
                {
                    userId = parsedUserId;
                    var user = await usersRepo.GetByIdAsync(parsedUserId);
                    if (user != null)
                    {
                        tier = user.Tier;
                    }
                }

                var events = new List<AnalyticsEvent>();
                foreach (var req in request.Events)
                {
                    if (string.IsNullOrWhiteSpace(req.EventName) || req.EventName.Length > 100)
                        continue;

                    // Validate sessionId length (VARCHAR(64) in database)
                    var sessionId = req.SessionId;
                    if (sessionId != null && sessionId.Length > 64)
                    {
                        sessionId = sessionId[..64]; // Truncate to fit column
                    }

                    string? payloadJson = null;
                    if (req.Payload != null && req.Payload.Count > 0)
                    {
                        payloadJson = JsonSerializer.Serialize(req.Payload, JsonOptions);
                        if (payloadJson.Length > 4096) continue; // Skip oversized
                    }

                    events.Add(new AnalyticsEvent
                    {
                        UserId = userId,
                        Tier = tier,
                        EventName = req.EventName,
                        PayloadJson = payloadJson,
                        SessionId = sessionId,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                var count = await analyticsRepo.InsertBatchAsync(events);

                logger.LogDebug("Analytics batch recorded: {Count} events for user {UserId}",
                    count, userId?.ToString() ?? "anonymous");

                return Results.Ok(new TrackBatchResponse(true, count));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to record analytics batch");
                return Results.Ok(new TrackBatchResponse(false, 0));
            }
        });
    }
}

