namespace Mongoose.Api.Infrastructure.Services.Analytics;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mongoose.Api.Core.Interfaces;

/// <summary>
/// Service for detecting and tracking user progression through predefined conversion funnels.
/// Matches multi-step user flows (e.g., auth → dashboard → feature) and records completion status.
/// </summary>
public class FunnelDetectionService
{
    private readonly IAnalyticsFunnelRepository _funnelRepository;
    private readonly ILogger<FunnelDetectionService> _logger;

    public FunnelDetectionService(
        IAnalyticsFunnelRepository funnelRepository,
        ILogger<FunnelDetectionService> logger)
    {
        _funnelRepository = funnelRepository;
        _logger = logger;
    }

    /// <summary>
    /// Process a session against all enabled funnels
    /// </summary>
    public async Task DetectFunnelProgressionAsync(string sessionId, long? userId, List<AnalyticsEventDimension> events)
    {
        if (events.Count == 0)
            return;

        var funnelDefinitions = await _funnelRepository.GetAllFunnelDefinitionsAsync();

        foreach (var definition in funnelDefinitions)
        {
            if (!definition.Enabled)
                continue;

            try
            {
                await TrackFunnelAsync(sessionId, userId, events, definition);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to track funnel {definition.FunnelName} for session {sessionId}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Track a session through a specific funnel
    /// </summary>
    private async Task TrackFunnelAsync(string sessionId, long? userId, List<AnalyticsEventDimension> events, AnalyticsFunnelDefinition definition)
    {
        var steps = ParseFunnelSteps(definition.Steps);
        if (steps.Count == 0)
            return;

        var orderedEvents = events.OrderBy(e => e.EventTimestampUtc).ToList();
        var funnelStepRecords = new List<AnalyticsFunnelStep>();

        // Initialize all steps for this funnel
        for (int i = 0; i < steps.Count; i++)
        {
            var stepDef = steps[i];
            funnelStepRecords.Add(new AnalyticsFunnelStep
            {
                FunnelName = definition.FunnelName,
                SessionId = sessionId,
                UserId = userId,
                StepNumber = i + 1,
                StepName = stepDef.Name,
                EventName = stepDef.EventName,
                Completed = false,
                StepTimestampUtc = DateTime.UtcNow,
                Tier = orderedEvents.FirstOrDefault()?.Tier ?? "free",
                DeviceType = orderedEvents.FirstOrDefault()?.DeviceType
            });
        }

        // Match events to steps
        DateTime? previousStepTime = null;
        int currentStepIndex = 0;

        foreach (var @event in orderedEvents)
        {
            if (currentStepIndex >= steps.Count)
                break;

            var currentStep = steps[currentStepIndex];

            // Check if this event matches the current funnel step
            if (@event.EventName == currentStep.EventName)
            {
                var funnelStep = funnelStepRecords[currentStepIndex];
                funnelStep.Completed = true;
                funnelStep.CompletedAtUtc = @event.EventTimestampUtc;
                funnelStep.StepTimestampUtc = @event.EventTimestampUtc;

                if (previousStepTime.HasValue)
                {
                    funnelStep.TimeSincePreviousStepSeconds = (int)(@event.EventTimestampUtc - previousStepTime.Value).TotalSeconds;
                }

                previousStepTime = @event.EventTimestampUtc;
                currentStepIndex++;
            }
            // Check if max time between steps has been exceeded
            else if (previousStepTime.HasValue && 
                     (@event.EventTimestampUtc - previousStepTime.Value).TotalHours > definition.MaxTimeBetweenStepsHours)
            {
                _logger.LogInformation($"Funnel {definition.FunnelName}: Time exceeded between steps for session {sessionId}");
                break;
            }
        }

        // Persist funnel steps
        foreach (var step in funnelStepRecords)
        {
            try
            {
                await _funnelRepository.InsertFunnelStepAsync(step);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to insert funnel step: {ex.Message}");
            }
        }

        // Log completion status
        var completedSteps = funnelStepRecords.Count(s => s.Completed);
        if (completedSteps > 0)
        {
            _logger.LogInformation(
                $"Funnel {definition.FunnelName}: Session {sessionId} completed {completedSteps}/{steps.Count} steps");
        }
    }

    /// <summary>
    /// Parse funnel step definitions from JSON
    /// </summary>
    private List<FunnelStepDefinition> ParseFunnelSteps(string stepsJson)
    {
        try
        {
            using (var doc = JsonDocument.Parse(stepsJson))
            {
                var steps = new List<FunnelStepDefinition>();
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Array)
                {
                    foreach (var element in root.EnumerateArray())
                    {
                        var step = new FunnelStepDefinition();

                        if (element.TryGetProperty("step", out var stepNum))
                            step.StepNumber = stepNum.GetInt32();

                        if (element.TryGetProperty("name", out var name))
                            step.Name = name.GetString() ?? string.Empty;

                        if (element.TryGetProperty("eventName", out var eventName))
                            step.EventName = eventName.GetString() ?? string.Empty;

                        steps.Add(step);
                    }
                }

                return steps.OrderBy(s => s.StepNumber).ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to parse funnel steps: {ex.Message}");
            return new List<FunnelStepDefinition>();
        }
    }

    /// <summary>
    /// Calculate funnel conversion metrics
    /// </summary>
    public async Task<FunnelMetrics> CalculateFunnelMetricsAsync(string funnelName, DateTime startUtc, DateTime endUtc)
    {
        var metrics = new FunnelMetrics
        {
            FunnelName = funnelName,
            StartTime = startUtc,
            EndTime = endUtc
        };

        try
        {
            var analysis = await _funnelRepository.AnalyzeFunnelAsync(funnelName, startUtc, endUtc);
            
            metrics.TotalSessions = analysis.TotalSessions;
            metrics.CompletedSessions = analysis.CompletedSessions;
            metrics.OverallConversionRate = analysis.OverallConversionRate;
            
            metrics.StepMetrics = analysis.Steps.Select(s => new FunnelStepMetrics
            {
                StepNumber = s.StepNumber,
                StepName = s.StepName,
                Completions = s.CompletedCount,
                UniqueUsers = s.UniqueUsers,
                ConversionRate = s.ConversionRate,
                CumulativeConversionRate = s.CumulativeConversionRate,
                AvgTimeToCompleteSeconds = s.AvgTimeToCompleteSeconds
            }).ToList();

            metrics.SegmentBreakdown = analysis.ByTier.Select(s => new SegmentMetrics
            {
                Segment = s.Segment,
                Sessions = s.Sessions,
                ConversionRate = s.ConversionRate
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to calculate funnel metrics: {ex.Message}");
        }

        return metrics;
    }

    /// <summary>
    /// Identify where users drop off in a funnel
    /// </summary>
    public List<DropOffAnalysis> AnalyzeDropOffs(FunnelAnalysis analysis)
    {
        var dropOffs = new List<DropOffAnalysis>();

        for (int i = 1; i < analysis.Steps.Count; i++)
        {
            var previousStep = analysis.Steps[i - 1];
            var currentStep = analysis.Steps[i];

            var dropOffCount = previousStep.CompletedCount - currentStep.CompletedCount;
            var dropOffRate = previousStep.CompletedCount > 0
                ? (decimal)dropOffCount / previousStep.CompletedCount * 100
                : 0;

            dropOffs.Add(new DropOffAnalysis
            {
                FromStep = previousStep.StepNumber,
                ToStep = currentStep.StepNumber,
                FromStepName = previousStep.StepName,
                ToStepName = currentStep.StepName,
                DropOffCount = dropOffCount,
                DropOffRate = dropOffRate
            });
        }

        return dropOffs.OrderByDescending(d => d.DropOffCount).ToList();
    }
}

/// <summary>
/// Single step definition in a funnel
/// </summary>
public class FunnelStepDefinition
{
    public int StepNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
}

/// <summary>
/// Funnel conversion metrics
/// </summary>
public class FunnelMetrics
{
    public string FunnelName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public long TotalSessions { get; set; }
    public long CompletedSessions { get; set; }
    public decimal OverallConversionRate { get; set; }
    public List<FunnelStepMetrics> StepMetrics { get; set; } = new();
    public List<SegmentMetrics> SegmentBreakdown { get; set; } = new();
}

public record FunnelStepMetrics
{
    public int StepNumber { get; set; }
    public string StepName { get; set; } = string.Empty;
    public long Completions { get; set; }
    public int UniqueUsers { get; set; }
    public decimal ConversionRate { get; set; }
    public decimal CumulativeConversionRate { get; set; }
    public int AvgTimeToCompleteSeconds { get; set; }
}

public record SegmentMetrics
{
    public string Segment { get; set; } = string.Empty;
    public long Sessions { get; set; }
    public decimal ConversionRate { get; set; }
}

public record DropOffAnalysis
{
    public int FromStep { get; set; }
    public int ToStep { get; set; }
    public string FromStepName { get; set; } = string.Empty;
    public string ToStepName { get; set; } = string.Empty;
    public long DropOffCount { get; set; }
    public decimal DropOffRate { get; set; }
}
