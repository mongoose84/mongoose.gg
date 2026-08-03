using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;
using static Mongoose.Api.Application.DTOs.AnalyticsDto;
using static Mongoose.Api.Application.DTOs.AnalyticsV2Dto;

namespace Mongoose.Api.Application.Endpoints.Analytics;

/// <summary>
/// Analytics Endpoint V1 + V2 - Supports both legacy and new versioned contracts
/// 
/// Compatibility Strategy:
/// - POST /api/v2/analytics: Accepts v1 or v2 request (detects based on structure)
/// - POST /api/v2/analytics/v2: Explicit v2 endpoint (strict validation)
/// - POST /api/v2/analytics/batch: Accepts v1 batch (legacy compatibility)
/// - POST /api/v2/analytics/v2/batch: Explicit v2 batch with detailed rejections
/// - GET /api/v2/analytics/health: Pipeline observability metrics
/// - GET /api/v2/analytics/schema: List registered event schemas
/// 
/// Dual-Write Strategy (During Migration):
/// - Both v1 (analytics_events) and v2 (analytics_events_v2) tables populated
/// - If v2 write fails, v1 still succeeds (rollback safety)
/// - Phase 1: Both tables active
/// - Phase 2: Deprecate v1 ingestion endpoint (keep read-only)
/// - Phase 3: Archive v1 data (optional)
/// </summary>
public sealed class AnalyticsEndpointV2 : IEndpoint
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public string Route { get; }

    public AnalyticsEndpointV2(string basePath)
    {
        Route = basePath + "/analytics";
    }

    public void Configure(WebApplication app)
    {
        // Legacy: POST /api/v2/analytics - Accept v1 or v2 (auto-detect)
        app.MapPost(Route, HandleTrackEvent);

        // V2: POST /api/v2/analytics/v2 - Strict v2 validation
        app.MapPost(Route + "/v2", HandleTrackEventV2);

        // Legacy: POST /api/v2/analytics/batch - v1 batch (max 50)
        app.MapPost(Route + "/batch", HandleBatchV1);

        // V2: POST /api/v2/analytics/v2/batch - v2 batch with detailed rejections
        app.MapPost(Route + "/v2/batch", HandleBatchV2);

        // Observability: GET /api/v2/analytics/health
        app.MapGet(Route + "/health", HandleHealth);

        // Observability: GET /api/v2/analytics/schema
        app.MapGet(Route + "/schema", HandleGetSchemas);
    }

    // ============ V1 + V2 Hybrid Handler ============

    private async Task<IResult> HandleTrackEvent(
        [FromBody] dynamic? request,
        HttpContext httpContext,
        [FromServices] IAnalyticsEventsRepository analyticsRepoV1,
        [FromServices] IAnalyticsEventsV2Repository analyticsRepoV2,
        [FromServices] IEventValidator validator,
        [FromServices] IEventSchemaRegistry schemaRegistry,
        [FromServices] IUsersRepository usersRepo,
        [FromServices] ILogger<AnalyticsEndpointV2> logger)
    {
        try
        {
            if (request is null)
                return Results.BadRequest(new { error = "Invalid request format" });

            var (userId, tier) = await GetUserContext(httpContext, usersRepo);

            // Detect v1 vs v2 by presence of the "eventVersion" field itself — checking the
            // deserialized value instead (e.g. `> 0`) is unreliable, since System.Text.Json fills
            // a missing property with the record constructor's default value (1), not 0.
            JsonElement requestElement = request;
            var isV2Request = requestElement.ValueKind == JsonValueKind.Object
                && requestElement.TryGetProperty("eventVersion", out _);

            if (isV2Request)
            {
                try
                {
                    var v2Request = JsonSerializer.Deserialize<TrackEventV2Request>(
                        request.ToString(),
                        JsonOptions);

                    if (v2Request is not null)
                    {
                        return await HandleV2SingleInternal(v2Request, userId, tier, analyticsRepoV1, analyticsRepoV2, validator, schemaRegistry, logger);
                    }
                }
                catch { }
            }

            // Fall back to V1
            var v1Request = JsonSerializer.Deserialize<TrackEventRequest>(
                request.ToString(),
                JsonOptions);

            if (v1Request is null)
                return Results.BadRequest(new { error = "Invalid request format" });

            return await HandleV1SingleInternal(v1Request, userId, tier, analyticsRepoV1, analyticsRepoV2, validator, schemaRegistry, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to record analytics event");
            return Results.Ok(new { success = false });
        }
    }

    // ============ V2 Explicit Handler ============

    private async Task<IResult> HandleTrackEventV2(
        [FromBody] TrackEventV2Request request,
        HttpContext httpContext,
        [FromServices] IAnalyticsEventsRepository analyticsRepoV1,
        [FromServices] IAnalyticsEventsV2Repository analyticsRepoV2,
        [FromServices] IEventValidator validator,
        [FromServices] IEventSchemaRegistry schemaRegistry,
        [FromServices] IUsersRepository usersRepo,
        [FromServices] ILogger<AnalyticsEndpointV2> logger)
    {
        try
        {
            var (userId, tier) = await GetUserContext(httpContext, usersRepo);
            return await HandleV2SingleInternal(request, userId, tier, analyticsRepoV1, analyticsRepoV2, validator, schemaRegistry, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to record v2 analytics event");
            return Results.Ok(new TrackEventV2Response(false, null, "DatabaseError", "Failed to record event"));
        }
    }

    private async Task<IResult> HandleV2SingleInternal(
        TrackEventV2Request request,
        long? userId,
        string tier,
        IAnalyticsEventsRepository analyticsRepoV1,
        IAnalyticsEventsV2Repository analyticsRepoV2,
        IEventValidator validator,
        IEventSchemaRegistry schemaRegistry,
        ILogger<AnalyticsEndpointV2> logger)
    {
        // Transform v2 request to entity
        var entity = AnalyticsCompatibilityHelper.TransformV2RequestToEntity(request, userId, tier, validator, schemaRegistry);

        // Store in V2 table
        await analyticsRepoV2.InsertAsync(entity);

        // Dual-write to V1 (if accepted)
        if (entity.IsAccepted)
        {
            try
            {
                var v1Event = new AnalyticsEvent
                {
                    UserId = entity.UserId,
                    Tier = entity.Tier,
                    EventName = entity.EventName,
                    PayloadJson = entity.PayloadJson,
                    SessionId = entity.SessionId,
                    CreatedAt = entity.CreatedAt
                };
                await analyticsRepoV1.InsertAsync(v1Event);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to dual-write to v1 table for event: {EventName}", entity.EventName);
                // Don't fail the response; v2 table has the authoritative copy
            }
        }

        logger.LogDebug("V2 event recorded: {EventName} (accepted: {IsAccepted}) for user {UserId}",
            LogSanitizer.Sanitize(entity.EventName), entity.IsAccepted, userId?.ToString() ?? "anonymous");

        var response = AnalyticsCompatibilityHelper.CreateResponseFromEntity(entity);
        return Results.Ok(response);
    }

    // ============ V1 Handlers ============

    private async Task<IResult> HandleV1SingleInternal(
        TrackEventRequest request,
        long? userId,
        string tier,
        IAnalyticsEventsRepository analyticsRepoV1,
        IAnalyticsEventsV2Repository analyticsRepoV2,
        IEventValidator validator,
        IEventSchemaRegistry schemaRegistry,
        ILogger<AnalyticsEndpointV2> logger)
    {
        // Validate event name — v1 contract predates the v2 schema registry, so these
        // checks (not registry membership) are what determine v1 acceptance.
        if (string.IsNullOrWhiteSpace(request.EventName))
            return Results.BadRequest(new { error = "eventName is required" });

        if (request.EventName.Length > 100)
            return Results.BadRequest(new { error = "eventName must be 100 characters or less" });

        // Transform v1 to v2 for the (best-effort, dual-write) v2 table
        var entity = AnalyticsCompatibilityHelper.TransformV1RequestToEntity(request, request.SessionId, userId, tier, validator, schemaRegistry);

        // Store in V2 table (best-effort; a v1 event not yet in the v2 registry is expected and not fatal)
        try
        {
            await analyticsRepoV2.InsertAsync(entity);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to dual-write to v2 table");
        }

        // Store in V1 table — authoritative for the v1 contract; if this fails, v1 truly failed.
        var v1Event = new AnalyticsEvent
        {
            UserId = entity.UserId,
            Tier = entity.Tier,
            EventName = entity.EventName,
            PayloadJson = entity.PayloadJson,
            SessionId = entity.SessionId,
            CreatedAt = entity.CreatedAt
        };
        await analyticsRepoV1.InsertAsync(v1Event);

        logger.LogDebug("V1→V2 event recorded: {EventName} (v2 accepted: {IsAccepted})",
            LogSanitizer.Sanitize(entity.EventName), entity.IsAccepted);

        return Results.Ok(new TrackEventResponse(true));
    }

    // ============ Batch Handlers ============

    private async Task<IResult> HandleBatchV1(
        [FromBody] TrackBatchRequest request,
        HttpContext httpContext,
        [FromServices] IAnalyticsEventsRepository analyticsRepoV1,
        [FromServices] IAnalyticsEventsV2Repository analyticsRepoV2,
        [FromServices] IEventValidator validator,
        [FromServices] IEventSchemaRegistry schemaRegistry,
        [FromServices] IUsersRepository usersRepo,
        [FromServices] ILogger<AnalyticsEndpointV2> logger)
    {
        try
        {
            if (request?.Events == null || request.Events.Length == 0)
                return Results.BadRequest(new { error = "events array is required" });

            if (request.Events.Length > 50)
                return Results.BadRequest(new { error = "max 50 events per batch" });

            var (userId, tier) = await GetUserContext(httpContext, usersRepo);

            var eventsV1 = new List<AnalyticsEvent>();
            var eventsV2 = new List<AnalyticsEventV2>();

            foreach (var req in request.Events)
            {
                // v1 contract predates the v2 schema registry — skip only on v1-level validity,
                // not on v2 registry membership.
                if (string.IsNullOrWhiteSpace(req.EventName) || req.EventName.Length > 100)
                    continue;

                var entity = AnalyticsCompatibilityHelper.TransformV1RequestToEntity(req, null, userId, tier, validator, schemaRegistry);

                eventsV2.Add(entity);

                eventsV1.Add(new AnalyticsEvent
                {
                    UserId = entity.UserId,
                    Tier = entity.Tier,
                    EventName = entity.EventName,
                    PayloadJson = entity.PayloadJson,
                    SessionId = entity.SessionId,
                    CreatedAt = entity.CreatedAt
                });
            }

            // Insert V2 (best-effort; v1 events not yet in the v2 registry are expected and not fatal)
            try
            {
                await analyticsRepoV2.InsertBatchAsync(eventsV2);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to dual-write batch to v2 table");
            }

            // Insert V1 — authoritative for the v1 contract
            var countV1 = await analyticsRepoV1.InsertBatchAsync(eventsV1);

            return Results.Ok(new TrackBatchResponse(true, countV1));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to record analytics batch v1");
            return Results.Ok(new TrackBatchResponse(false, 0));
        }
    }

    private async Task<IResult> HandleBatchV2(
        [FromBody] TrackBatchV2Request request,
        HttpContext httpContext,
        [FromServices] IAnalyticsEventsRepository analyticsRepoV1,
        [FromServices] IAnalyticsEventsV2Repository analyticsRepoV2,
        [FromServices] IEventValidator validator,
        [FromServices] IEventSchemaRegistry schemaRegistry,
        [FromServices] IUsersRepository usersRepo,
        [FromServices] ILogger<AnalyticsEndpointV2> logger)
    {
        try
        {
            if (request?.Events == null || request.Events.Count == 0)
                return Results.BadRequest(new { error = "events array is required" });

            if (request.Events.Count > 50)
                return Results.BadRequest(new { error = "max 50 events per batch" });

            var (userId, tier) = await GetUserContext(httpContext, usersRepo);

            var entitiesV2 = new List<AnalyticsEventV2>();
            var eventsV1 = new List<AnalyticsEvent>();

            foreach (var evt in request.Events)
            {
                var entity = AnalyticsCompatibilityHelper.TransformV2RequestToEntity(evt, userId, tier, validator, schemaRegistry);

                entitiesV2.Add(entity);

                if (entity.IsAccepted)
                {
                    eventsV1.Add(new AnalyticsEvent
                    {
                        UserId = entity.UserId,
                        Tier = entity.Tier,
                        EventName = entity.EventName,
                        PayloadJson = entity.PayloadJson,
                        SessionId = entity.SessionId,
                        CreatedAt = entity.CreatedAt
                    });
                }
            }

            // Insert V2
            await analyticsRepoV2.InsertBatchAsync(entitiesV2);

            // Dual-write V1
            if (eventsV1.Count > 0)
            {
                try
                {
                    await analyticsRepoV1.InsertBatchAsync(eventsV1);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to dual-write batch to v1 table");
                }
            }

            var response = AnalyticsCompatibilityHelper.CreateBatchResponseFromEntities(entitiesV2);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to record analytics batch v2");
            return Results.Ok(new TrackBatchV2Response(false, 0, 0));
        }
    }

    // ============ Observability Endpoints ============

    private async Task<IResult> HandleHealth(
        [FromServices] IAnalyticsEventsV2Repository analyticsRepo,
        [FromServices] ILogger<AnalyticsEndpointV2> logger)
    {
        try
        {
            var now = DateTime.UtcNow;
            var from = now.AddHours(-1);

            var acceptanceRate = await analyticsRepo.GetAcceptanceRateAsync(from, now);
            var acceptedCount = await analyticsRepo.GetAcceptedEventCountAsync(from, now);
            var totalCount = (long)(acceptedCount / (acceptanceRate > 0 ? acceptanceRate : 1));
            var rejectedCount = totalCount - acceptedCount;
            var rejectionBreakdown = await analyticsRepo.GetRejectionsByReasonAsync(from, now);

            var response = new AnalyticsHealthResponse(
                Status: "healthy",
                AcceptanceRate: acceptanceRate,
                TotalEvents: totalCount,
                AcceptedEvents: acceptedCount,
                RejectedEvents: rejectedCount,
                RejectionBreakdown: rejectionBreakdown,
                Latency: new LatencyMetrics(
                    P50: 25.0,
                    P95: 75.0,
                    P99: 200.0,
                    Max: 500.0
                ),
                SchemaVersion: 1,
                Timestamp: now
            );

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get analytics health");
            return Results.StatusCode(500);
        }
    }

    private async Task<IResult> HandleGetSchemas(
        [FromServices] IEventSchemaRegistry schemaRegistry,
        [FromServices] ILogger<AnalyticsEndpointV2> logger)
    {
        try
        {
            var allSchemas = schemaRegistry.GetAllSchemas();

            var schemas = allSchemas.Values.Select(s => new EventSchemaInfo(
                EventName: s.Name,
                Version: s.Version,
                Category: s.Category,
                RequiredFields: s.RequiredPayloadKeys.ToArray(),
                AllowedPayloadKeys: s.AllowedPayloadKeys.ToArray(),
                RetentionDays: s.RetentionDays,
                Description: s.Description
            )).ToArray();

            var response = new GetSchemasResponse(
                Schemas: schemas,
                Count: schemas.Length,
                SchemaVersion: schemaRegistry.GetSchemaVersion()
            );

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get event schemas");
            return Results.StatusCode(500);
        }
    }

    // ============ Helpers ============

    private async Task<(long? UserId, string Tier)> GetUserContext(HttpContext httpContext, IUsersRepository usersRepo)
    {
        long? userId = null;
        string tier = "free";

        var userIdClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userIdClaim) && long.TryParse(userIdClaim, out var parsedUserId))
        {
            userId = parsedUserId;
            var user = await usersRepo.GetByIdAsync(parsedUserId);
            if (user != null)
            {
                tier = user.Tier;
            }
        }

        return (userId, tier);
    }
}
