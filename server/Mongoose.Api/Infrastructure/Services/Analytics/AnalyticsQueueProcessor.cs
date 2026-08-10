using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Mongoose.Api.Application.Endpoints.Analytics;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mongoose.Api.Infrastructure.Services.Analytics;

/// <summary>
/// Background queue processor for async event ingestion
/// 
/// Goals:
/// - Decouple request handling from database I/O
/// - Process events in background workers
/// - Track processing metrics
/// - Graceful shutdown on application stop
/// - Bounded queue to protect memory
/// </summary>
public class AnalyticsQueueProcessor : BackgroundService, IAnalyticsQueueProcessor
{
  private readonly IServiceScopeFactory _scopeFactory;
  private readonly ILogger<AnalyticsQueueProcessor> _logger;
  private readonly AnalyticsQueueOptions _options;

  // Processing state
  private readonly Channel<List<AnalyticsEventV2>> _queue;
  private readonly List<Task> _workers = new();

  // Metrics
  private readonly object _metricsLock = new();
  private long _totalProcessed = 0;
  private long _totalRejected = 0;
  private long _totalLatencyMs = 0;
  private int _processingCount = 0;
  private DateTime _metricsWindowStart = DateTime.UtcNow;

  public AnalyticsQueueProcessor(
    IServiceScopeFactory scopeFactory,
    ILogger<AnalyticsQueueProcessor> logger,
    AnalyticsQueueOptions? options = null)
  {
    _scopeFactory = scopeFactory;
    _logger = logger;
    _options = options ?? new AnalyticsQueueOptions();
    
    // Create bounded channel for queue
    _queue = Channel.CreateBounded<List<AnalyticsEventV2>>(
      new BoundedChannelOptions(_options.MaxQueueDepth)
      {
        FullMode = BoundedChannelFullMode.DropWrite, // Drop if full (protect memory)
      });
  }
  
  /// <summary>
  /// Enqueue batch for async processing
  /// </summary>
  public async Task<int> EnqueueBatchAsync(List<AnalyticsEventV2> events)
  {
    if (events == null || events.Count == 0)
      return 0;
    
    // Try to write to queue (non-blocking)
    // If queue is full, batch is dropped (abuse protection)
    var written = _queue.Writer.TryWrite(events);
    
    if (!written)
    {
      _logger.LogWarning("Queue full, dropping batch of {Count} events", events.Count);
      return 0;
    }
    
    _logger.LogDebug("Enqueued batch of {Count} events", events.Count);
    return events.Count;
  }
  
  /// <summary>
  /// Get current metrics snapshot
  /// </summary>
  public Task<AnalyticsQueueMetrics> GetMetricsAsync()
  {
    lock (_metricsLock)
    {
      var avgLatency = _totalProcessed > 0
        ? (double)_totalLatencyMs / _totalProcessed
        : 0;
      
      return Task.FromResult(new AnalyticsQueueMetrics
      {
        QueueDepth = _queue.Reader.Count,
        MaxCapacity = _options.MaxQueueDepth,
        ActiveWorkers = _processingCount,
        ProcessedCount = _totalProcessed,
        RejectedCount = _totalRejected,
        AvgLatencyMs = avgLatency,
        WindowStartTime = _metricsWindowStart,
        WindowEndTime = DateTime.UtcNow,
      });
    }
  }
  
  /// <summary>
  /// Start background processing workers
  /// </summary>
  public override Task StartAsync(CancellationToken cancellationToken)
  {
    _logger.LogInformation(
      "Starting AnalyticsQueueProcessor with {Workers} worker threads",
      _options.WorkerCount);
    
    // Start worker threads
    for (int i = 0; i < _options.WorkerCount; i++)
    {
      _workers.Add(ProcessWorkerAsync(i, cancellationToken));
    }
    
    return base.StartAsync(cancellationToken);
  }
  
  /// <summary>
  /// Stop background processing workers
  /// </summary>
  public override async Task StopAsync(CancellationToken cancellationToken)
  {
    _logger.LogInformation("Stopping AnalyticsQueueProcessor");
    
    // Signal no more data will be enqueued
    _queue.Writer.TryComplete();
    
    // Wait for workers to finish (with timeout)
    var completionTimeout = Task.Delay(_options.ShutdownTimeoutMs, cancellationToken);
    var allWorkers = Task.WhenAll(_workers);
    
    var completed = await Task.WhenAny(allWorkers, completionTimeout);
    
    if (completed == completionTimeout)
    {
      _logger.LogWarning("Queue processor shutdown timeout after {Timeout}ms", _options.ShutdownTimeoutMs);
    }
    else
    {
      _logger.LogInformation("Queue processor shutdown complete");
    }
    
    await base.StopAsync(cancellationToken);
  }
  
  /// <summary>
  /// Background worker: process batches from queue
  /// </summary>
  private async Task ProcessWorkerAsync(int workerId, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Worker {Id} started", workerId);
    
    try
    {
      await foreach (var batch in _queue.Reader.ReadAllAsync(cancellationToken))
      {
        try
        {
          Interlocked.Increment(ref _processingCount);
          
          var stopwatch = Stopwatch.StartNew();
          
          // Insert batch to database (scoped repository resolved per batch,
          // since this processor is a singleton and the repository is scoped)
          var insertCount = 0;
          using (var scope = _scopeFactory.CreateScope())
          {
            var repository = scope.ServiceProvider.GetRequiredService<IAnalyticsEventsV2Repository>();
            foreach (var evt in batch)
            {
              try
              {
                await repository.InsertAsync(evt);
                insertCount++;
              }
              catch (Exception ex)
              {
                _logger.LogWarning(
                  "Error inserting event {EventName}: {Error}",
                  evt.EventName, ex.Message);

                Interlocked.Increment(ref _totalRejected);
              }
            }
          }
          
          stopwatch.Stop();
          
          // Update metrics
          lock (_metricsLock)
          {
            _totalProcessed += insertCount;
            _totalLatencyMs += stopwatch.ElapsedMilliseconds;
          }
          
          _logger.LogDebug(
            "Worker {Id} processed batch: {Inserted}/{Total} events in {Latency}ms",
            workerId, insertCount, batch.Count, stopwatch.ElapsedMilliseconds);
          
          Interlocked.Decrement(ref _processingCount);
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Worker {Id} error processing batch", workerId);
          Interlocked.Decrement(ref _processingCount);
        }
      }
    }
    catch (OperationCanceledException)
    {
      _logger.LogDebug("Worker {Id} cancelled", workerId);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Worker {Id} fatal error", workerId);
    }
    finally
    {
      _logger.LogInformation("Worker {Id} stopped", workerId);
    }
  }
  
  /// <summary>
  /// Background service execute (monitors metrics)
  /// </summary>
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        // Log metrics every 30 seconds
        await Task.Delay(30000, stoppingToken);
        
        lock (_metricsLock)
        {
          var avgLatency = _totalProcessed > 0
            ? (double)_totalLatencyMs / _totalProcessed
            : 0;
          
          _logger.LogInformation(
            "Queue metrics: Depth={Depth}, Processed={Processed}, Rejected={Rejected}, AvgLatency={Latency}ms, ActiveWorkers={Workers}",
            _queue.Reader.Count,
            _totalProcessed,
            _totalRejected,
            avgLatency,
            _processingCount);
        }
      }
      catch (OperationCanceledException)
      {
        break;
      }
    }
  }
}

/// <summary>
/// Queue processor configuration
/// </summary>
public class AnalyticsQueueOptions
{
  /// <summary>
  /// Number of background worker threads
  /// </summary>
  public int WorkerCount { get; set; } = Environment.ProcessorCount > 4 ? 4 : 2;
  
  /// <summary>
  /// Maximum queue depth (bounded to protect memory)
  /// </summary>
  public int MaxQueueDepth { get; set; } = 10000;
  
  /// <summary>
  /// Graceful shutdown timeout in milliseconds
  /// </summary>
  public int ShutdownTimeoutMs { get; set; } = 30000;
}
