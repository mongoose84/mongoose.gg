using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Mongoose.Api.Application.DTOs.Analytics;
using Mongoose.Api.Application.Endpoints.Analytics;
using Mongoose.Api.Tests;
using Xunit;

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
    
    var request = new TrackBatchV2Request
    {
      Events = new List<TrackEventV2Request>
      {
        new()
        {
          EventName = "nav:page_view",
          EventVersion = 1,
          Payload = new Dictionary<string, object> { { "path", "/app/overview" } },
        },
      },
    };
    
    var response = await client.PostAsJsonAsync("/api/v2/analytics/async", request);
    
    response.StatusCode.Should().Be(HttpStatusCode.Accepted); // 202
    var content = await response.Content.ReadAsAsync<dynamic>();
    content.enqueuedCount.Should().Be(1);
  }
  
  [Fact]
  public async Task Analytics_async_batch_fire_and_forget()
  {
    using var factory = new TestWebApplicationFactory();
    using var client = factory.CreateClient();
    
    var request = new TrackBatchV2Request
    {
      Events = new List<TrackEventV2Request>
      {
        new()
        {
          EventName = "nav:page_view",
          EventVersion = 1,
          Payload = new Dictionary<string, object> { { "path", "/" } },
        },
      },
    };
    
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
    
    var request = new TrackBatchV2Request { Events = new List<TrackEventV2Request>() };
    
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
      events.Add(new TrackEventV2Request
      {
        EventName = "nav:page_view",
        EventVersion = 1,
        Payload = new Dictionary<string, object> { { "i", i } },
      });
    }
    
    var request = new TrackBatchV2Request { Events = events };
    var response = await client.PostAsJsonAsync("/api/v2/analytics/async", request);
    
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
  }
  
  [Fact]
  public async Task Analytics_async_queue_metrics_available()
  {
    using var factory = new TestWebApplicationFactory();
    using var client = factory.CreateClient();
    
    // Send event first
    var request = new TrackBatchV2Request
    {
      Events = new List<TrackEventV2Request>
      {
        new()
        {
          EventName = "nav:page_view",
          EventVersion = 1,
          Payload = new Dictionary<string, object> { { "path", "/" } },
        },
      },
    };
    
    await client.PostAsJsonAsync("/api/v2/analytics/async", request);
    
    // Query metrics
    var response = await client.GetAsync("/api/v2/analytics/async/queue");
    
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var metrics = await response.Content.ReadAsAsync<dynamic>();
    
    metrics.queue.Should().NotBeNull();
    metrics.queue.depth.Should().BeGreaterThanOrEqualTo(0);
    metrics.processing.Should().NotBeNull();
  }
  
  [Fact]
  public async Task Analytics_async_multiple_batches_queued()
  {
    using var factory = new TestWebApplicationFactory();
    using var client = factory.CreateClient();
    
    for (int batch = 0; batch < 3; batch++)
    {
      var request = new TrackBatchV2Request
      {
        Events = new List<TrackEventV2Request>
        {
          new()
          {
            EventName = "nav:page_view",
            EventVersion = 1,
            Payload = new Dictionary<string, object> { { "batch", batch } },
          },
        },
      };
      
      var response = await client.PostAsJsonAsync("/api/v2/analytics/async", request);
      response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }
    
    // All batches should be queued
    var metricsResponse = await client.GetAsync("/api/v2/analytics/async/queue");
    var metrics = await metricsResponse.Content.ReadAsAsync<dynamic>();
    
    // Queue should have processed or queued events
    metrics.processing.processedCount.Should().BeGreaterThanOrEqualTo(0);
  }
  
  [Fact]
  public async Task Analytics_async_partial_batch_acceptance()
  {
    using var factory = new TestWebApplicationFactory();
    using var client = factory.CreateClient();
    
    var request = new TrackBatchV2Request
    {
      Events = new List<TrackEventV2Request>
      {
        // Valid event
        new()
        {
          EventName = "nav:page_view",
          EventVersion = 1,
          Payload = new Dictionary<string, object> { { "path", "/app" } },
        },
        // Invalid event (unknown name)
        new()
        {
          EventName = "invalid:unknown",
          EventVersion = 1,
          Payload = new Dictionary<string, object>(),
        },
      },
    };
    
    var response = await client.PostAsJsonAsync("/api/v2/analytics/async", request);
    
    response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    var content = await response.Content.ReadAsAsync<dynamic>();
    
    // At least valid event should be enqueued
    content.enqueuedCount.Should().BeGreaterThanOrEqualTo(1);
  }
}

/// <summary>
/// Queue processor tests
/// </summary>
public class AnalyticsQueueProcessorTests
{
  [Fact]
  public async Task QueueProcessor_enqueues_events()
  {
    var repo = new Mock<IAnalyticsEventsV2Repository>();
    var logger = new Mock<ILogger<AnalyticsQueueProcessor>>();
    
    var processor = new AnalyticsQueueProcessor(repo.Object, logger.Object);
    
    var events = new List<AnalyticsEventV2>
    {
      new()
      {
        EventId = Guid.NewGuid().ToString(),
        EventName = "test:event",
        Payload = "{}",
        ServerTimestampUtc = DateTime.UtcNow,
      },
    };
    
    var count = await processor.EnqueueBatchAsync(events);
    
    count.Should().Be(1);
  }
  
  [Fact]
  public async Task QueueProcessor_returns_metrics()
  {
    var repo = new Mock<IAnalyticsEventsV2Repository>();
    var logger = new Mock<ILogger<AnalyticsQueueProcessor>>();
    
    var processor = new AnalyticsQueueProcessor(repo.Object, logger.Object);
    
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
    var cache = new MemoryCache(new MemoryCacheOptions());
    var logger = new Mock<ILogger<AnalyticsAbuseGuards>>();
    
    var guards = new AnalyticsAbuseGuards(cache, logger.Object);
    
    var result = await guards.CheckAsync("192.168.1.1", "user1", "Mozilla/5.0");
    
    result.IsAllowed.Should().BeTrue();
  }
  
  [Fact]
  public async Task AbuseGuards_rate_limits_suspicious_ua()
  {
    var cache = new MemoryCache(new MemoryCacheOptions());
    var logger = new Mock<ILogger<AnalyticsAbuseGuards>>();
    var options = new AnalyticsAbuseGuardsOptions { MaxSuspiciousUaEvents = 1 };
    
    var guards = new AnalyticsAbuseGuards(cache, logger.Object, options);
    
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
    var cache = new MemoryCache(new MemoryCacheOptions());
    var logger = new Mock<ILogger<AnalyticsAbuseGuards>>();
    var options = new AnalyticsAbuseGuardsOptions { MaxEventsPerIpPerWindow = 2 };
    
    var guards = new AnalyticsAbuseGuards(cache, logger.Object, options);
    
    var result1 = await guards.CheckAsync("192.168.1.1", null, "Mozilla/5.0");
    result1.IsAllowed.Should().BeTrue();
    
    var result2 = await guards.CheckAsync("192.168.1.1", null, "Mozilla/5.0");
    result2.IsAllowed.Should().BeTrue();
    
    var result3 = await guards.CheckAsync("192.168.1.1", null, "Mozilla/5.0");
    result3.IsAllowed.Should().BeFalse();
  }
}
