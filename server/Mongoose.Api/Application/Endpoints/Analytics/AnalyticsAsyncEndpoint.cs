using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mongoose.Api.Application.DTOs;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using static Mongoose.Api.Application.DTOs.AnalyticsV2Dto;

namespace Mongoose.Api.Application.Endpoints.Analytics;

/// <summary>
/// Async Analytics Ingestion Endpoint
///
/// Goals:
/// - Accept event batches and return 202 Accepted immediately
/// - Decouple request latency from persistence (async background processing)
/// - Fire-and-forget user experience; no blocking on network I/O
/// - Implement abuse guardrails to protect backend
/// - Track pipeline metrics for observability
///
/// Routes:
/// - POST /api/v2/analytics/async - Async batch ingestion (202 Accepted)
/// - GET  /api/v2/analytics/async/queue - Queue depth & processing metrics
/// </summary>
public class AnalyticsAsyncEndpoint : IEndpoint
{
  public string Route { get; }

  public AnalyticsAsyncEndpoint(string basePath)
  {
    Route = basePath + "/analytics/async";
  }

  public void Configure(WebApplication app)
  {
    app.MapPost(Route, HandleAsyncBatch);
    app.MapGet($"{Route}/queue", HandleQueueMetrics);
  }

  /// <summary>
  /// POST /api/v2/analytics/async
  /// Async batch ingestion endpoint
  ///
  /// Returns 202 Accepted immediately; processes in background
  /// Fire-and-forget semantics: client doesn't wait for persistence
  /// </summary>
  private async Task<IResult> HandleAsyncBatch(
    HttpContext context,
    [FromBody] TrackBatchV2Request request,
    [FromServices] IEventValidator validator,
    [FromServices] IEventSchemaRegistry schemaRegistry,
    [FromServices] IUsersRepository usersRepo,
    [FromServices] IAnalyticsQueueProcessor queueProcessor,
    [FromServices] IAnalyticsAbuseGuards abuseGuards,
    [FromServices] ILogger<AnalyticsAsyncEndpoint> logger)
  {
    try
    {
      // Null/empty check
      if (request?.Events == null || request.Events.Count == 0)
      {
        return Results.BadRequest(new { error = "Empty batch" });
      }

      // Get user context
      var (userId, tier) = await GetUserContext(context, usersRepo);

      // Check abuse guards
      var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
      var userAgent = context.Request.Headers["User-Agent"].ToString();
      var abuseCheckResult = await abuseGuards.CheckAsync(clientIp, userId?.ToString(), userAgent);

      if (!abuseCheckResult.IsAllowed)
      {
        logger.LogWarning(
          "Abuse guard rejected batch from IP={Ip}, UserId={UserId}, Reason={Reason}",
          LogSanitizer.Sanitize(clientIp), userId?.ToString() ?? "anonymous", LogSanitizer.Sanitize(abuseCheckResult.Reason));

        return Results.StatusCode(429); // Too Many Requests
      }

      // Validate batch size
      const int maxBatchSize = 50;
      if (request.Events.Count > maxBatchSize)
      {
        logger.LogWarning("Batch too large: {Count} events > {Max}", request.Events.Count, maxBatchSize);
        return Results.BadRequest(new { error = $"Batch too large ({request.Events.Count} > {maxBatchSize})" });
      }

      // Transform and validate events
      var sessionId = request.SessionId ?? Guid.NewGuid().ToString();
      var entities = new List<AnalyticsEventV2>();

      foreach (var evt in request.Events)
      {
        try
        {
          var entity = AnalyticsCompatibilityHelper.TransformV2RequestToEntity(
            evt, userId, tier, validator, schemaRegistry);

          entities.Add(entity);
        }
        catch (Exception ex)
        {
          logger.LogWarning("Error transforming event: {Error}", LogSanitizer.Sanitize(ex.Message));
          // Continue with other events in batch
        }
      }

      if (entities.Count == 0)
      {
        logger.LogWarning("All events in batch were invalid");
        return Results.BadRequest(new { error = "No valid events in batch" });
      }

      // Queue for async processing (non-blocking)
      var queuedCount = await queueProcessor.EnqueueBatchAsync(entities);

      logger.LogInformation(
        "Batch enqueued: {Queued}/{Total} events, IP={Ip}, UserId={UserId}",
        queuedCount, entities.Count, LogSanitizer.Sanitize(clientIp), userId?.ToString() ?? "anonymous");

      // Return 202 Accepted immediately (fire-and-forget)
      return Results.Accepted(
        $"/api/v2/analytics/async/queue",
        new
        {
          message = "Batch accepted for processing",
          enqueuedCount = queuedCount,
          totalCount = entities.Count,
          status = "processing",
        });
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error in async batch handler");
      return Results.StatusCode(500);
    }
  }

  /// <summary>
  /// GET /api/v2/analytics/async/queue
  /// Get queue processing metrics
  /// </summary>
  private async Task<IResult> HandleQueueMetrics(
    HttpContext context,
    [FromServices] IAnalyticsQueueProcessor queueProcessor,
    [FromServices] ILogger<AnalyticsAsyncEndpoint> logger)
  {
    try
    {
      var metrics = await queueProcessor.GetMetricsAsync();

      return Results.Ok(new
      {
        status = "operational",
        queue = new
        {
          depth = metrics.QueueDepth,
          maxCapacity = metrics.MaxCapacity,
          utilizationPercent = (metrics.QueueDepth * 100M) / metrics.MaxCapacity,
        },
        processing = new
        {
          activeWorkers = metrics.ActiveWorkers,
          processedCount = metrics.ProcessedCount,
          rejectedCount = metrics.RejectedCount,
          avgLatencyMs = metrics.AvgLatencyMs,
        },
        timeWindow = new
        {
          startTime = metrics.WindowStartTime,
          endTime = metrics.WindowEndTime,
          durationSeconds = (metrics.WindowEndTime - metrics.WindowStartTime).TotalSeconds,
        },
      });
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error getting queue metrics");
      return Results.StatusCode(500);
    }
  }

  /// <summary>
  /// Extract user context from request claims
  /// </summary>
  private async Task<(long? UserId, string Tier)> GetUserContext(HttpContext context, IUsersRepository usersRepo)
  {
    long? userId = null;
    string tier = "free";

    var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
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

/// <summary>
/// Queue processor interface
/// Manages background processing of enqueued events
/// </summary>
public interface IAnalyticsQueueProcessor
{
  /// <summary>
  /// Enqueue a batch of events for async processing
  /// Returns immediately; processing happens in background
  /// </summary>
  Task<int> EnqueueBatchAsync(List<AnalyticsEventV2> events);

  /// <summary>
  /// Get queue metrics snapshot
  /// </summary>
  Task<AnalyticsQueueMetrics> GetMetricsAsync();
}

/// <summary>
/// Queue metrics snapshot
/// </summary>
public class AnalyticsQueueMetrics
{
  public int QueueDepth { get; set; }
  public int MaxCapacity { get; set; }
  public int ActiveWorkers { get; set; }
  public long ProcessedCount { get; set; }
  public long RejectedCount { get; set; }
  public double AvgLatencyMs { get; set; }
  public DateTime WindowStartTime { get; set; }
  public DateTime WindowEndTime { get; set; }
}

/// <summary>
/// Abuse detection interface
/// Protects backend from burst traffic and malicious patterns
/// </summary>
public interface IAnalyticsAbuseGuards
{
  /// <summary>
  /// Check if request should be allowed
  /// Returns result with IsAllowed and Reason
  /// </summary>
  Task<AbuseCheckResult> CheckAsync(string clientIp, string? userId, string userAgent);
}

/// <summary>
/// Abuse check result
/// </summary>
public class AbuseCheckResult
{
  public bool IsAllowed { get; set; }
  public string Reason { get; set; } = "OK";
  public int? RetryAfterSeconds { get; set; }
}
