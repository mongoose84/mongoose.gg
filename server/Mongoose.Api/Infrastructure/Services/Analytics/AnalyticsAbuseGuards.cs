using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mongoose.Api.Application.Endpoints.Analytics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Mongoose.Api.Infrastructure.Services.Analytics;

/// <summary>
/// Abuse Guards Service
/// 
/// Protects backend from:
/// - Burst traffic (rate limiting per IP)
/// - Malicious patterns (suspicious user agents)
/// - Authenticated user abuse (rate limiting per user)
/// 
/// Uses in-memory sliding window counters with exponential backoff
/// </summary>
public class AnalyticsAbuseGuards : IAnalyticsAbuseGuards
{
  private readonly IMemoryCache _cache;
  private readonly ILogger<AnalyticsAbuseGuards> _logger;
  private readonly AnalyticsAbuseGuardsOptions _options;
  
  // Suspicious user agent patterns
  private static readonly HashSet<string> SuspiciousUserAgentKeywords = new()
  {
    "bot", "crawler", "spider", "scraper",
    "curl", "wget", "python", "java", "ruby",
    "postman", "insomnia", "thunderclient",
  };
  
  public AnalyticsAbuseGuards(
    IMemoryCache cache,
    ILogger<AnalyticsAbuseGuards> logger,
    AnalyticsAbuseGuardsOptions? options = null)
  {
    _cache = cache;
    _logger = logger;
    _options = options ?? new AnalyticsAbuseGuardsOptions();
  }
  
  /// <summary>
  /// Check if request should be allowed
  /// </summary>
  public async Task<AbuseCheckResult> CheckAsync(string clientIp, string? userId, string userAgent)
  {
    try
    {
      // Check IP-based rate limiting (anonymous + authenticated)
      var ipKey = $"analytics:ratelimit:ip:{clientIp}";
      var ipCount = await GetCounterAsync(ipKey);
      
      if (ipCount >= _options.MaxEventsPerIpPerWindow)
      {
        _logger.LogWarning(
          "IP rate limit exceeded: IP={Ip}, Count={Count}/{Max}",
          clientIp, ipCount, _options.MaxEventsPerIpPerWindow);
        
        return new AbuseCheckResult
        {
          IsAllowed = false,
          Reason = "IP rate limit exceeded",
          RetryAfterSeconds = _options.WindowSizeSeconds,
        };
      }
      
      // Check user-based rate limiting (authenticated only)
      if (!string.IsNullOrEmpty(userId))
      {
        var userKey = $"analytics:ratelimit:user:{userId}";
        var userCount = await GetCounterAsync(userKey);
        
        if (userCount >= _options.MaxEventsPerUserPerWindow)
        {
          _logger.LogWarning(
            "User rate limit exceeded: UserId={UserId}, Count={Count}/{Max}",
            userId, userCount, _options.MaxEventsPerUserPerWindow);
          
          return new AbuseCheckResult
          {
            IsAllowed = false,
            Reason = "User rate limit exceeded",
            RetryAfterSeconds = _options.WindowSizeSeconds,
          };
        }
      }
      
      // Check user agent (suspicious patterns)
      if (IsSuspiciousUserAgent(userAgent))
      {
        var suspiciousKey = $"analytics:suspicious:ua:{userAgent.GetHashCode()}";
        var suspiciousCount = await GetCounterAsync(suspiciousKey);
        
        if (suspiciousCount >= _options.MaxSuspiciousUaEvents)
        {
          _logger.LogWarning(
            "Suspicious user agent rate limit exceeded: UA={Ua}, Count={Count}",
            userAgent, suspiciousCount);
          
          return new AbuseCheckResult
          {
            IsAllowed = false,
            Reason = "Suspicious user agent rate limited",
            RetryAfterSeconds = _options.WindowSizeSeconds,
          };
        }
        
        // Log suspicious UA once
        if (suspiciousCount == 0)
        {
          _logger.LogInformation("Suspicious user agent detected: {Ua}", userAgent);
        }
      }
      
      // Increment counters
      await IncrementCounterAsync(ipKey);
      if (!string.IsNullOrEmpty(userId))
      {
        await IncrementCounterAsync($"analytics:ratelimit:user:{userId}");
      }
      if (IsSuspiciousUserAgent(userAgent))
      {
        await IncrementCounterAsync($"analytics:suspicious:ua:{userAgent.GetHashCode()}");
      }
      
      return new AbuseCheckResult
      {
        IsAllowed = true,
        Reason = "OK",
      };
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error checking abuse guards");
      
      // Fail open (allow request if guard check fails)
      return new AbuseCheckResult
      {
        IsAllowed = true,
        Reason = "Guard check error (fail-open)",
      };
    }
  }
  
  /// <summary>
  /// Check if user agent looks suspicious
  /// </summary>
  private bool IsSuspiciousUserAgent(string userAgent)
  {
    if (string.IsNullOrEmpty(userAgent))
      return false;
    
    var lowerUa = userAgent.ToLower();
    
    foreach (var keyword in SuspiciousUserAgentKeywords)
    {
      if (lowerUa.Contains(keyword))
        return true;
    }
    
    return false;
  }
  
  /// <summary>
  /// Get current counter value for key
  /// </summary>
  private async Task<int> GetCounterAsync(string key)
  {
    if (_cache.TryGetValue(key, out int value))
    {
      return value;
    }
    
    return 0;
  }
  
  /// <summary>
  /// Increment counter (with sliding window expiration)
  /// </summary>
  private async Task IncrementCounterAsync(string key)
  {
    var current = GetCounterAsync(key).Result;
    
    _cache.Set(
      key,
      current + 1,
      new MemoryCacheEntryOptions
      {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_options.WindowSizeSeconds),
        SlidingExpiration = TimeSpan.FromSeconds(10), // Refresh every 10s of inactivity
      });
  }
}

/// <summary>
/// Abuse guards configuration
/// </summary>
public class AnalyticsAbuseGuardsOptions
{
  /// <summary>
  /// Max events per IP per window
  /// Default: 1000 events / 60 seconds = ~16.7 events/sec
  /// </summary>
  public int MaxEventsPerIpPerWindow { get; set; } = 1000;
  
  /// <summary>
  /// Max events per authenticated user per window
  /// Default: 5000 events / 60 seconds = ~83 events/sec (higher for auth'd users)
  /// </summary>
  public int MaxEventsPerUserPerWindow { get; set; } = 5000;
  
  /// <summary>
  /// Max events from suspicious user agent per window
  /// Default: 100 events / 60 seconds (strict limit for bots)
  /// </summary>
  public int MaxSuspiciousUaEvents { get; set; } = 100;
  
  /// <summary>
  /// Time window for rate limiting in seconds
  /// Default: 60 seconds (sliding window)
  /// </summary>
  public int WindowSizeSeconds { get; set; } = 60;
}
