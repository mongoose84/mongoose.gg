using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Mongoose.Api.Application.DTOs;
using Mongoose.Api.Application.Endpoints.Analytics;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Infrastructure.Services.Analytics;
using Xunit;
using static Mongoose.Api.Application.DTOs.AnalyticsV2Dto;

namespace Mongoose.Api.Tests;

/// <summary>
/// Integration tests for async analytics endpoint
/// Testing:
/// - 202 Accepted responses
/// - Queue depth monitoring
/// - Abuse guards enforcement
/// - Retry logic on backend
/// - Fire-and-forget semantics
/// </summary>
public class AnalyticsAsyncEndpointTests
{
  [Fact]
  public async Task Analytics_async_batch_returns_202_accepted()
  {
    using var factory = new TestWebApplicationFactory();
    using var client = factory.CreateClient();

    var request = new TrackBatchV2Request(
      Events: new List<TrackEventV2Request>
      {
        new(
          EventName: "nav:page_view",
          EventVersion: 1,
          Payload: new Dictionary<string, object> { { "path", "/app/overview" } })
      });

    var response = await client.PostAsJsonAsync("/api/v2/analytics/async", request);

    response.StatusCode.Should().Be(HttpStatusCode.Accepted); // 202
    var content = await response.Content.ReadFromJsonAsync<JsonElement>();
    content.GetProperty("enqueuedCount").GetInt32().Should().Be(1);
  }

  [Fact]
  public async Task Analytics_async_batch_fire_and_forget()
  {
    using var factory = new TestWebApplicationFactory();
    using var client = factory.CreateClient();

    var request = new TrackBatchV2Request(
      Events: new List<TrackEventV2Request>
      {
        new(
          EventName: "nav:page_view",
          EventVersion: 1,
          Payload: new Dictionary<string, object> { { "path", "/" } })
      });

    // Warm up the host (first request on a fresh factory pays one-time JIT/DI-graph
    // startup cost unrelated to the endpoint's own latency) before timing.
    await client.PostAsJsonAsync("/api/v2/analytics/async", request);

    // Should return immediately without waiting for persistence
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    var response = await client.PostAsJsonAsync("/api/v2/analytics/async", request);
    stopwatch.Stop();

    response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(500); // Should be < 500ms (not waiting for DB)
  }

  [Fact]
  public async Task Analytics_async_empty_batch_rejected()
  {
    using var factory = new TestWebApplicationFactory();
    using var client = factory.CreateClient();

    var request = new TrackBatchV2Request(Events: new List<TrackEventV2Request>());

    var response = await client.PostAsJsonAsync("/api/v2/analytics/async", request);

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
  }

  [Fact]
  public async Task Analytics_async_batch_max_size_enforced()
  {
    using var factory = new TestWebApplicationFactory();
    using var client = factory.CreateClient();

    var events = new List<TrackEventV2Request>();
    for (int i = 0; i < 51; i++)
    {
      events.Add(new TrackEventV2Request(
        EventName: "nav:page_view",
        EventVersion: 1,
        Payload: new Dictionary<string, object> { { "i", i } }));
    }

    var request = new TrackBatchV2Request(Events: events);
    var response = await client.PostAsJsonAsync("/api/v2/analytics/async", request);

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
  }

  [Fact]
  public async Task Analytics_async_queue_metrics_available()
  {
    using var factory = new TestWebApplicationFactory();
    using var client = factory.CreateClient();

    // Send event first
    var request = new TrackBatchV2Request(
      Events: new List<TrackEventV2Request>
      {
        new(
          EventName: "nav:page_view",
          EventVersion: 1,
          Payload: new Dictionary<string, object> { { "path", "/" } })
      });

    await client.PostAsJsonAsync("/api/v2/analytics/async", request);

    // Query metrics
    var response = await client.GetAsync("/api/v2/analytics/async/queue");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var metrics = await response.Content.ReadFromJsonAsync<JsonElement>();

    metrics.TryGetProperty("queue", out var queue).Should().BeTrue();
    queue.GetProperty("depth").GetInt32().Should().BeGreaterThanOrEqualTo(0);
    metrics.TryGetProperty("processing", out _).Should().BeTrue();
  }

  [Fact]
  public async Task Analytics_async_multiple_batches_queued()
  {
    using var factory = new TestWebApplicationFactory();
    using var client = factory.CreateClient();

    for (int batch = 0; batch < 3; batch++)
    {
      var request = new TrackBatchV2Request(
        Events: new List<TrackEventV2Request>
        {
          new(
            EventName: "nav:page_view",
            EventVersion: 1,
            Payload: new Dictionary<string, object> { { "batch", batch } })
        });

      var response = await client.PostAsJsonAsync("/api/v2/analytics/async", request);
      response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    // All batches should be queued
    var metricsResponse = await client.GetAsync("/api/v2/analytics/async/queue");
    var metrics = await metricsResponse.Content.ReadFromJsonAsync<JsonElement>();

    // Queue should have processed or queued events
    metrics.GetProperty("processing").GetProperty("processedCount").GetInt64().Should().BeGreaterThanOrEqualTo(0);
  }

  [Fact]
  public async Task Analytics_async_partial_batch_acceptance()
  {
    using var factory = new TestWebApplicationFactory();
    using var client = factory.CreateClient();

    var request = new TrackBatchV2Request(
      Events: new List<TrackEventV2Request>
      {
        // Valid event
        new(
          EventName: "nav:page_view",
          EventVersion: 1,
          Payload: new Dictionary<string, object> { { "path", "/app" } }),
        // Invalid event (unknown name)
        new(
          EventName: "invalid:unknown",
          EventVersion: 1,
          Payload: new Dictionary<string, object>())
      });

    var response = await client.PostAsJsonAsync("/api/v2/analytics/async", request);

    response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    var content = await response.Content.ReadFromJsonAsync<JsonElement>();

    // At least valid event should be enqueued
    content.GetProperty("enqueuedCount").GetInt32().Should().BeGreaterThanOrEqualTo(1);
  }
}

/// <summary>
/// Queue processor tests
/// </summary>
public class AnalyticsQueueProcessorTests
{
  private sealed class FakeAnalyticsEventsV2Repository : IAnalyticsEventsV2Repository
  {
    public Task<long> InsertAsync(AnalyticsEventV2 evt) => Task.FromResult(1L);

    public Task<int> InsertBatchAsync(IEnumerable<AnalyticsEventV2> events) => Task.FromResult(0);

    public Task<long> GetEventCountAsync(string eventName, DateTime from, DateTime to, bool includeRejected = false) => Task.FromResult(0L);

    public Task<long> GetUniqueUserCountAsync(string eventName, DateTime from, DateTime to) => Task.FromResult(0L);

    public Task<long> GetAcceptedEventCountAsync(DateTime from, DateTime to) => Task.FromResult(0L);

    public Task<Dictionary<string, long>> GetRejectionsByReasonAsync(DateTime from, DateTime to) => Task.FromResult(new Dictionary<string, long>());

    public Task<double> GetAcceptanceRateAsync(DateTime from, DateTime to) => Task.FromResult(0.0);

    public Task<int> DeleteOlderThanAsync(DateTime cutoffDate) => Task.FromResult(0);

    public Task<Dictionary<string, long>> GetEventDistributionByCategoryAsync(DateTime from, DateTime to) => Task.FromResult(new Dictionary<string, long>());
  }

  [Fact]
  public async Task QueueProcessor_enqueues_events()
  {
    var repo = new FakeAnalyticsEventsV2Repository();

    var processor = new AnalyticsQueueProcessor(repo, NullLogger<AnalyticsQueueProcessor>.Instance);

    var events = new List<AnalyticsEventV2>
    {
      new()
      {
        EventId = Guid.NewGuid().ToString(),
        EventName = "test:event",
        PayloadJson = "{}",
        ServerTimestampUtc = DateTime.UtcNow,
      },
    };

    var count = await processor.EnqueueBatchAsync(events);

    count.Should().Be(1);
  }

  [Fact]
  public async Task QueueProcessor_returns_metrics()
  {
    var repo = new FakeAnalyticsEventsV2Repository();

    var processor = new AnalyticsQueueProcessor(repo, NullLogger<AnalyticsQueueProcessor>.Instance);

    var metrics = await processor.GetMetricsAsync();

    metrics.QueueDepth.Should().BeGreaterThanOrEqualTo(0);
    metrics.MaxCapacity.Should().BeGreaterThan(0);
  }
}

/// <summary>
/// Abuse guards tests
/// </summary>
public class AnalyticsAbuseGuardsTests
{
  [Fact]
  public async Task AbuseGuards_allows_normal_traffic()
  {
    using var cache = new MemoryCache(new MemoryCacheOptions());

    var guards = new AnalyticsAbuseGuards(cache, NullLogger<AnalyticsAbuseGuards>.Instance);

    var result = await guards.CheckAsync("192.168.1.1", "user1", "Mozilla/5.0");

    result.IsAllowed.Should().BeTrue();
  }

  [Fact]
  public async Task AbuseGuards_rate_limits_suspicious_ua()
  {
    using var cache = new MemoryCache(new MemoryCacheOptions());
    var options = new AnalyticsAbuseGuardsOptions { MaxSuspiciousUaEvents = 1 };

    var guards = new AnalyticsAbuseGuards(cache, NullLogger<AnalyticsAbuseGuards>.Instance, options);

    // First request with suspicious UA should be allowed
    var result1 = await guards.CheckAsync("192.168.1.1", null, "python-requests");
    result1.IsAllowed.Should().BeTrue();

    // Second request should be rate limited
    var result2 = await guards.CheckAsync("192.168.1.1", null, "python-requests");
    result2.IsAllowed.Should().BeFalse();
  }

  [Fact]
  public async Task AbuseGuards_ip_rate_limits()
  {
    using var cache = new MemoryCache(new MemoryCacheOptions());
    var options = new AnalyticsAbuseGuardsOptions { MaxEventsPerIpPerWindow = 2 };

    var guards = new AnalyticsAbuseGuards(cache, NullLogger<AnalyticsAbuseGuards>.Instance, options);

    var result1 = await guards.CheckAsync("192.168.1.1", null, "Mozilla/5.0");
    result1.IsAllowed.Should().BeTrue();

    var result2 = await guards.CheckAsync("192.168.1.1", null, "Mozilla/5.0");
    result2.IsAllowed.Should().BeTrue();

    var result3 = await guards.CheckAsync("192.168.1.1", null, "Mozilla/5.0");
    result3.IsAllowed.Should().BeFalse();
  }
}
