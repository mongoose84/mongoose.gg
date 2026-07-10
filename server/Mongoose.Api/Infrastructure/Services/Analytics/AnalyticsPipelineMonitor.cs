using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Mongoose.Api.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mongoose.Api.Infrastructure.Services.Analytics;

/// <summary>
/// Analytics Pipeline Monitoring Service
/// 
/// Tracks:
/// - Event acceptance/rejection rates
/// - Processing latency (p50, p95, p99)
/// - Queue depth trends
/// - PII detection violations
/// - Abuse pattern triggers
/// </summary>
public interface IAnalyticsPipelineMonitor
{
  /// <summary>
  /// Record event acceptance
  /// </summary>
  void RecordAcceptance(string eventName, long latencyMs);
  
  /// <summary>
  /// Record event rejection
  /// </summary>
  void RecordRejection(string eventName, string reason);
  
  /// <summary>
  /// Record PII violation
  /// </summary>
  void RecordPiiViolation(string eventName, string pattern);
  
  /// <summary>
  /// Record abuse trigger
  /// </summary>
  void RecordAbuseTrigger(string reason, string clientIp);
  
  /// <summary>
  /// Get health dashboard data
  /// </summary>
  Task<AnalyticsPipelineHealth> GetHealthAsync();
  
  /// <summary>
  /// Get metrics by event name
  /// </summary>
  Task<Dictionary<string, EventMetrics>> GetEventMetricsAsync();
}

/// <summary>
/// Pipeline health snapshot
/// </summary>
public class AnalyticsPipelineHealth
{
  public string Status { get; set; } = "healthy";
  public DateTime Timestamp { get; set; }
  public double AcceptanceRate { get; set; }
  public long TotalEvents { get; set; }
  public long AcceptedEvents { get; set; }
  public long RejectedEvents { get; set; }
  
  public LatencyMetrics Latency { get; set; } = new();
  public Dictionary<string, long> RejectionBreakdown { get; set; } = new();
  public Dictionary<string, long> PiiViolations { get; set; } = new();
  public long AbuseTriggersLastHour { get; set; }
  
  public AlertsSnapshot Alerts { get; set; } = new();
}

/// <summary>
/// Latency metrics (p50, p95, p99)
/// </summary>
public class LatencyMetrics
{
  public double P50Ms { get; set; }
  public double P95Ms { get; set; }
  public double P99Ms { get; set; }
  public double AvgMs { get; set; }
}

/// <summary>
/// Per-event metrics
/// </summary>
public class EventMetrics
{
  public string EventName { get; set; } = "";
  public long AcceptedCount { get; set; }
  public long RejectedCount { get; set; }
  public double AcceptanceRate { get; set; }
  public LatencyMetrics Latency { get; set; } = new();
}

/// <summary>
/// Alerts snapshot
/// </summary>
public class AlertsSnapshot
{
  public List<PipelineAlert> Active { get; set; } = new();
  
  /// <summary>
  /// Example alerts:
  /// - Acceptance rate < 95%
  /// - Latency p95 > 500ms
  /// - Queue depth > 80% capacity
  /// - PII violations detected
  /// - Abuse patterns detected
  /// </summary>
}

/// <summary>
/// Pipeline alert
/// </summary>
public class PipelineAlert
{
  public string Name { get; set; } = "";
  public string Severity { get; set; } = "warning"; // info, warning, critical
  public string Message { get; set; } = "";
  public DateTime Timestamp { get; set; }
  public Dictionary<string, object> Context { get; set; } = new();
}

/// <summary>
/// Monitoring implementation
/// </summary>
public class AnalyticsPipelineMonitor : IAnalyticsPipelineMonitor
{
  private readonly ILogger<AnalyticsPipelineMonitor> _logger;
  
  // Metrics buffers (rolling windows)
  private readonly object _metricsLock = new();
  private readonly Queue<(long LatencyMs, DateTime Timestamp)> _latencyBuffer = new();
  private readonly Dictionary<string, (long Accepted, long Rejected)> _eventCounts = new();
  private readonly Dictionary<string, long> _rejectionReasons = new();
  private readonly Dictionary<string, long> _piiPatterns = new();
  private readonly Queue<(string Reason, string ClientIp, DateTime Timestamp)> _abuseBuffer = new();
  
  private long _totalAccepted = 0;
  private long _totalRejected = 0;
  
  public AnalyticsPipelineMonitor(ILogger<AnalyticsPipelineMonitor> logger)
  {
    _logger = logger;
  }
  
  public void RecordAcceptance(string eventName, long latencyMs)
  {
    lock (_metricsLock)
    {
      _totalAccepted++;
      
      // Update event counts
      if (!_eventCounts.ContainsKey(eventName))
      {
        _eventCounts[eventName] = (0, 0);
      }
      var (accepted, rejected) = _eventCounts[eventName];
      _eventCounts[eventName] = (accepted + 1, rejected);
      
      // Add to latency buffer (keep last 10,000 samples)
      _latencyBuffer.Enqueue((latencyMs, DateTime.UtcNow));
      if (_latencyBuffer.Count > 10000)
      {
        _latencyBuffer.Dequeue();
      }
    }
  }
  
  public void RecordRejection(string eventName, string reason)
  {
    lock (_metricsLock)
    {
      _totalRejected++;
      
      // Update event counts
      if (!_eventCounts.ContainsKey(eventName))
      {
        _eventCounts[eventName] = (0, 0);
      }
      var (accepted, rejected) = _eventCounts[eventName];
      _eventCounts[eventName] = (accepted, rejected + 1);
      
      // Track reason breakdown
      if (!_rejectionReasons.ContainsKey(reason))
      {
        _rejectionReasons[reason] = 0;
      }
      _rejectionReasons[reason]++;
    }
  }
  
  public void RecordPiiViolation(string eventName, string pattern)
  {
    lock (_metricsLock)
    {
      if (!_piiPatterns.ContainsKey(pattern))
      {
        _piiPatterns[pattern] = 0;
      }
      _piiPatterns[pattern]++;
    }
    
    _logger.LogWarning("PII violation detected: Event={Event}, Pattern={Pattern}", eventName, pattern);
  }
  
  public void RecordAbuseTrigger(string reason, string clientIp)
  {
    lock (_metricsLock)
    {
      _abuseBuffer.Enqueue((reason, clientIp, DateTime.UtcNow));
      if (_abuseBuffer.Count > 10000)
      {
        _abuseBuffer.Dequeue();
      }
    }
    
    _logger.LogWarning("Abuse trigger: Reason={Reason}, IP={Ip}", reason, clientIp);
  }
  
  public Task<AnalyticsPipelineHealth> GetHealthAsync()
  {
    lock (_metricsLock)
    {
      var total = _totalAccepted + _totalRejected;
      var acceptanceRate = total > 0 ? (double)_totalAccepted / total : 1.0;
      
      // Calculate latency percentiles
      var latencies = new List<long>();
      foreach (var (latency, _) in _latencyBuffer)
      {
        latencies.Add(latency);
      }
      
      var latencyMetrics = CalculateLatencyMetrics(latencies);
      
      // Check for alerts
      var alerts = EvaluateAlerts(acceptanceRate, latencyMetrics);
      
      // Count abuse triggers in last hour
      var abuseLastHour = 0;
      var oneHourAgo = DateTime.UtcNow.AddHours(-1);
      foreach (var (_, _, ts) in _abuseBuffer)
      {
        if (ts > oneHourAgo)
          abuseLastHour++;
      }
      
      // Determine status
      var status = "healthy";
      if (acceptanceRate < 0.95 || latencyMetrics.P95Ms > 500 || abuseLastHour > 100)
      {
        status = "degraded";
      }
      if (acceptanceRate < 0.80 || latencyMetrics.P95Ms > 1000 || abuseLastHour > 1000)
      {
        status = "critical";
      }
      
      return Task.FromResult(new AnalyticsPipelineHealth
      {
        Status = status,
        Timestamp = DateTime.UtcNow,
        AcceptanceRate = acceptanceRate,
        TotalEvents = total,
        AcceptedEvents = _totalAccepted,
        RejectedEvents = _totalRejected,
        Latency = latencyMetrics,
        RejectionBreakdown = new Dictionary<string, long>(_rejectionReasons),
        PiiViolations = new Dictionary<string, long>(_piiPatterns),
        AbuseTriggersLastHour = abuseLastHour,
        Alerts = alerts,
      });
    }
  }
  
  public Task<Dictionary<string, EventMetrics>> GetEventMetricsAsync()
  {
    lock (_metricsLock)
    {
      var result = new Dictionary<string, EventMetrics>();
      
      foreach (var (eventName, (accepted, rejected)) in _eventCounts)
      {
        var total = accepted + rejected;
        var acceptanceRate = total > 0 ? (double)accepted / total : 1.0;
        
        result[eventName] = new EventMetrics
        {
          EventName = eventName,
          AcceptedCount = accepted,
          RejectedCount = rejected,
          AcceptanceRate = acceptanceRate,
        };
      }
      
      return Task.FromResult(result);
    }
  }
  
  /// <summary>
  /// Calculate latency percentiles
  /// </summary>
  private LatencyMetrics CalculateLatencyMetrics(List<long> latencies)
  {
    if (latencies.Count == 0)
    {
      return new LatencyMetrics { AvgMs = 0, P50Ms = 0, P95Ms = 0, P99Ms = 0 };
    }
    
    latencies.Sort();
    
    var avg = 0L;
    foreach (var l in latencies)
    {
      avg += l;
    }
    avg /= latencies.Count;
    
    var p50Index = (int)(latencies.Count * 0.50);
    var p95Index = (int)(latencies.Count * 0.95);
    var p99Index = (int)(latencies.Count * 0.99);
    
    return new LatencyMetrics
    {
      AvgMs = avg,
      P50Ms = latencies[p50Index],
      P95Ms = latencies[Math.Min(p95Index, latencies.Count - 1)],
      P99Ms = latencies[Math.Min(p99Index, latencies.Count - 1)],
    };
  }
  
  /// <summary>
  /// Evaluate and generate alerts
  /// </summary>
  private AlertsSnapshot EvaluateAlerts(double acceptanceRate, LatencyMetrics latency)
  {
    var alerts = new AlertsSnapshot();
    
    if (acceptanceRate < 0.95)
    {
      alerts.Active.Add(new PipelineAlert
      {
        Name = "LowAcceptanceRate",
        Severity = acceptanceRate < 0.80 ? "critical" : "warning",
        Message = $"Acceptance rate is {acceptanceRate:P}",
        Timestamp = DateTime.UtcNow,
      });
    }
    
    if (latency.P95Ms > 500)
    {
      alerts.Active.Add(new PipelineAlert
      {
        Name = "HighLatency",
        Severity = latency.P95Ms > 1000 ? "critical" : "warning",
        Message = $"P95 latency is {latency.P95Ms}ms",
        Timestamp = DateTime.UtcNow,
      });
    }
    
    return alerts;
  }
}
