namespace Mongoose.Api.Infrastructure.Services.Analytics;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mongoose.Api.Core.Interfaces;

/// <summary>
/// Service for detecting and tracking user navigation journeys across events.
/// Identifies navigation flows and multi-step paths through the application.
/// </summary>
public class JourneyDetectionService
{
    private readonly IAnalyticsJourneyRepository _journeyRepository;
    private readonly IAnalyticsEventDimensionsRepository _dimensionRepository;
    private readonly ILogger<JourneyDetectionService> _logger;

    private const int MaxJourneyStepsPerSession = 100;
    private const string PagePathPropertyName = "page";

    public JourneyDetectionService(
        IAnalyticsJourneyRepository journeyRepository,
        IAnalyticsEventDimensionsRepository dimensionRepository,
        ILogger<JourneyDetectionService> logger)
    {
        _journeyRepository = journeyRepository;
        _dimensionRepository = dimensionRepository;
        _logger = logger;
    }

    /// <summary>
    /// Process a session's events to detect navigation journeys
    /// </summary>
    public async Task DetectJourneyAsync(string sessionId, List<AnalyticsEventDimension> events)
    {
        if (events.Count == 0)
            return;

        var orderedEvents = events.OrderBy(e => e.EventTimestampUtc).ToList();
        string? previousPage = null;
        DateTime? previousTimestamp = null;
        int stepNumber = 0;

        foreach (var @event in orderedEvents)
        {
            // Extract page path from this event
            var currentPage = @event.PagePath;
            if (string.IsNullOrEmpty(currentPage))
                continue;

            // Skip if same page (not a navigation)
            if (previousPage == currentPage)
                continue;

            stepNumber++;
            if (stepNumber > MaxJourneyStepsPerSession)
            {
                _logger.LogWarning($"Session {sessionId} exceeded max journey steps");
                break;
            }

            var timeOnPreviousPage = previousTimestamp.HasValue
                ? (int?)(@event.EventTimestampUtc - previousTimestamp.Value).TotalSeconds
                : null;

            var journeyStep = new AnalyticsJourneyStep
            {
                SessionId = sessionId,
                UserId = @event.UserId,
                StepNumber = stepNumber,
                SourcePage = previousPage,
                DestinationPage = currentPage,
                EventName = @event.EventName,
                TransitionTimestampUtc = @event.EventTimestampUtc,
                TimeOnPreviousPageSeconds = timeOnPreviousPage,
                DeviceType = @event.DeviceType,
                Tier = @event.Tier
            };

            try
            {
                await _journeyRepository.InsertJourneyStepAsync(journeyStep);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to record journey step for {sessionId}: {ex.Message}");
            }

            previousPage = currentPage;
            previousTimestamp = @event.EventTimestampUtc;
        }

        _logger.LogInformation($"Detected {stepNumber} journey steps for session {sessionId}");
    }

    /// <summary>
    /// Detect multi-step paths through the application
    /// </summary>
    public async Task<List<NavigationPath>> DetectPathsAsync(List<AnalyticsEventDimension> sessionEvents)
    {
        var paths = new List<NavigationPath>();

        // Group by session to detect paths
        var groupedBySession = sessionEvents.GroupBy(e => e.SessionId);

        foreach (var sessionGroup in groupedBySession)
        {
            var events = sessionGroup.OrderBy(e => e.EventTimestampUtc).ToList();
            var pageSequence = events
                .Select(e => e.PagePath)
                .Where(p => !string.IsNullOrEmpty(p))
                .Select(p => p!)
                .Distinct()
                .ToList();

            if (pageSequence.Count < 2)
                continue;

            var pathKey = string.Join(" → ", pageSequence);
            var existingPath = paths.FirstOrDefault(p => p.PathKey == pathKey);

            if (existingPath != null)
            {
                existingPath.Count++;
                if (sessionGroup.Key != null && !existingPath.SessionIds.Contains(sessionGroup.Key))
                    existingPath.SessionIds.Add(sessionGroup.Key);
            }
            else
            {
                paths.Add(new NavigationPath
                {
                    PathKey = pathKey,
                    Pages = pageSequence,
                    Count = 1,
                    SessionIds = new List<string> { sessionGroup.Key ?? string.Empty }
                });
            }
        }

        return paths.OrderByDescending(p => p.Count).Take(100).ToList();
    }

    /// <summary>
    /// Analyze navigation patterns for a specific time window
    /// </summary>
    public async Task<NavigationPatternAnalysis> AnalyzeNavigationPatternsAsync(DateTime startUtc, DateTime endUtc)
    {
        var analysis = new NavigationPatternAnalysis
        {
            StartTime = startUtc,
            EndTime = endUtc,
            AnalyzedAt = DateTime.UtcNow
        };

        try
        {
            // Get top flows
            var flows = await _journeyRepository.GetTopFlowsAsync(startUtc, endUtc, minTransitions: 5, limit: 50);
            analysis.TopFlows = flows.ToList();

            // Get common entry points
            analysis.EntryPoints = flows
                .GroupBy(f => f.SourcePage ?? "entry")
                .Select(g => new PageCount { Page = g.Key, Count = g.Sum(f => f.TransitionCount) })
                .OrderByDescending(p => p.Count)
                .Take(20)
                .ToList();

            // Get common exit points
            analysis.ExitPoints = flows
                .GroupBy(f => f.DestinationPage)
                .Select(g => new PageCount { Page = g.Key, Count = g.Sum(f => f.TransitionCount) })
                .OrderByDescending(p => p.Count)
                .Take(20)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to analyze navigation patterns: {ex.Message}");
        }

        return analysis;
    }
}

/// <summary>
/// Navigation path detected from session events
/// </summary>
public class NavigationPath
{
    public string PathKey { get; set; } = string.Empty;
    public List<string> Pages { get; set; } = new();
    public long Count { get; set; }
    public List<string> SessionIds { get; set; } = new();
}

/// <summary>
/// Navigation pattern analysis results
/// </summary>
public class NavigationPatternAnalysis
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public DateTime AnalyzedAt { get; set; }
    public List<NavigationFlow> TopFlows { get; set; } = new();
    public List<PageCount> EntryPoints { get; set; } = new();
    public List<PageCount> ExitPoints { get; set; } = new();
}

public record PageCount
{
    public string Page { get; set; } = string.Empty;
    public long Count { get; set; }
}
