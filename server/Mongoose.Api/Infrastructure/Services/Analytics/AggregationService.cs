namespace Mongoose.Api.Infrastructure.Services.Analytics;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mongoose.Api.Core.Interfaces;

/// <summary>
/// Service for aggregating raw event data into hourly rollups.
/// Computes trends, segment breakdowns, and geographic distributions.
/// </summary>
public class AggregationService
{
    private readonly IAnalyticsRollupRepository _rollupRepository;
    private readonly IAnalyticsEventDimensionsRepository _dimensionRepository;
    private readonly ILogger<AggregationService> _logger;

    public AggregationService(
        IAnalyticsRollupRepository rollupRepository,
        IAnalyticsEventDimensionsRepository dimensionRepository,
        ILogger<AggregationService> logger)
    {
        _rollupRepository = rollupRepository;
        _dimensionRepository = dimensionRepository;
        _logger = logger;
    }

    /// <summary>
    /// Aggregate events from the last hour into rollup table
    /// </summary>
    public async Task AggregateLastHourAsync()
    {
        var now = DateTime.UtcNow;
        var hourStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);
        
        await AggregateHourAsync(hourStart);
    }

    /// <summary>
    /// Aggregate events for a specific hour
    /// </summary>
    public async Task AggregateHourAsync(DateTime hourUtc)
    {
        var hourStart = new DateTime(hourUtc.Year, hourUtc.Month, hourUtc.Day, hourUtc.Hour, 0, 0, DateTimeKind.Utc);
        var hourEnd = hourStart.AddHours(1);

        _logger.LogInformation($"Starting aggregation for hour: {hourStart:yyyy-MM-dd HH:00:00}");

        try
        {
            // Get all events for this hour (grouped by event_name, event_category)
            var hourlyStats = new Dictionary<string, List<AnalyticsEventDimension>>();

            // In real implementation, query from analytics_event_dimensions
            // For now, this would be done via repository

            // Build and insert rollup records
            var rollupRecords = ComputeRollups(hourStart, hourEnd);

            foreach (var record in rollupRecords)
            {
                try
                {
                    await _rollupRepository.UpsertHourlyRollupAsync(record);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Failed to upsert rollup for {record.EventName}: {ex.Message}");
                }
            }

            _logger.LogInformation($"Completed aggregation for hour: {hourStart:yyyy-MM-dd HH:00:00}, created {rollupRecords.Count} rollup records");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Aggregation failed for hour {hourStart}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Compute rollup records from dimension data
    /// </summary>
    private List<AnalyticsRollupHourly> ComputeRollups(DateTime hourStart, DateTime hourEnd)
    {
        var rollups = new List<AnalyticsRollupHourly>();

        // This would be implemented with actual repository queries
        // For now, return empty list (would be implemented with IAnalyticsEventDimensionsRepository)

        return rollups;
    }

    /// <summary>
    /// Calculate event trends over time period
    /// </summary>
    public async Task<EventTrend> CalculateEventTrendAsync(string eventName, int hoursBack = 168)
    {
        var endTime = DateTime.UtcNow;
        var startTime = endTime.AddHours(-hoursBack);

        var trend = new EventTrend
        {
            EventName = eventName,
            StartTime = startTime,
            EndTime = endTime,
            HourlyData = new List<TrendDataPoint>()
        };

        try
        {
            var rollups = await _rollupRepository.GetEventTrendAsync(eventName, hoursBack);

            foreach (var rollup in rollups)
            {
                trend.HourlyData.Add(new TrendDataPoint
                {
                    Timestamp = rollup.DateHour,
                    EventCount = rollup.EventCount,
                    UniqueUsers = rollup.UniqueUsers,
                    UniqueSessions = rollup.UniqueSessions,
                    ProTierCount = rollup.ProTierCount
                });
            }

            // Calculate summary metrics
            if (trend.HourlyData.Count > 0)
            {
                trend.TotalEvents = trend.HourlyData.Sum(d => d.EventCount);
                trend.TotalUniqueUsers = trend.HourlyData.Sum(d => d.UniqueUsers);
                trend.AverageEventsPerHour = trend.HourlyData.Average(d => d.EventCount);
                trend.PeakEventCount = trend.HourlyData.Max(d => d.EventCount);
                trend.PeakTimestamp = trend.HourlyData.First(d => d.EventCount == trend.PeakEventCount).Timestamp;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to calculate trend for {eventName}: {ex.Message}");
        }

        return trend;
    }

    /// <summary>
    /// Calculate segment distribution (tier, device, geography)
    /// </summary>
    public async Task<SegmentDistribution> CalculateSegmentDistributionAsync(
        DateTime startUtc, 
        DateTime endUtc,
        string? eventNameFilter = null)
    {
        var distribution = new SegmentDistribution
        {
            StartTime = startUtc,
            EndTime = endUtc,
            EventNameFilter = eventNameFilter
        };

        try
        {
            // Get rollups for the time period
            var rollups = await _rollupRepository.GetLatestRollupsAsync(hoursBack: (int)(DateTime.UtcNow - startUtc).TotalHours, limit: 1000);

            // Aggregate by tier
            distribution.ByTier = rollups
                .GroupBy(r => new { r.EventName, Tier = "all" })
                .Select(g => new TierDistribution
                {
                    Tier = g.Key.Tier,
                    EventCount = g.Sum(r => r.EventCount),
                    UniqueUsers = g.Sum(r => r.UniqueUsers),
                    ProTierEvents = g.Sum(r => r.CountProTier)
                })
                .ToList();

            // Aggregate by device
            distribution.ByDevice = new List<DeviceDistribution>
            {
                new() { Device = "desktop", EventCount = rollups.Sum(r => r.CountDesktop) },
                new() { Device = "mobile", EventCount = rollups.Sum(r => r.CountMobile) },
                new() { Device = "tablet", EventCount = rollups.Sum(r => r.CountTablet) }
            };

            // Aggregate by geography
            distribution.TopCountries = ExtractTopCountries(rollups);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to calculate segment distribution: {ex.Message}");
        }

        return distribution;
    }

    /// <summary>
    /// Extract and aggregate top countries from rollup data
    /// </summary>
    private List<CountryDistribution> ExtractTopCountries(IEnumerable<AnalyticsRollupHourly> rollups)
    {
        var countryCounts = new Dictionary<string, long>();

        foreach (var rollup in rollups)
        {
            if (string.IsNullOrEmpty(rollup.TopCountries))
                continue;

            try
            {
                using (var doc = JsonDocument.Parse(rollup.TopCountries))
                {
                    foreach (var element in doc.RootElement.EnumerateArray())
                    {
                        var country = element.GetProperty("country").GetString();
                        var count = element.GetProperty("count").GetInt64();

                        if (country != null)
                        {
                            if (countryCounts.ContainsKey(country))
                                countryCounts[country] += count;
                            else
                                countryCounts[country] = count;
                        }
                    }
                }
            }
            catch
            {
                // Skip malformed JSON
            }
        }

        return countryCounts
            .OrderByDescending(x => x.Value)
            .Take(20)
            .Select(x => new CountryDistribution
            {
                Country = x.Key,
                EventCount = x.Value
            })
            .ToList();
    }

    /// <summary>
    /// Calculate growth rates between two time periods
    /// </summary>
    public async Task<GrowthAnalysis> AnalyzeGrowthAsync(DateTime period1Start, DateTime period1End, DateTime period2Start, DateTime period2End)
    {
        var analysis = new GrowthAnalysis
        {
            Period1Start = period1Start,
            Period1End = period1End,
            Period2Start = period2Start,
            Period2End = period2End
        };

        try
        {
            var rollups1 = await _rollupRepository.GetEventRollupsAsync("*", period1Start, period1End);
            var rollups2 = await _rollupRepository.GetEventRollupsAsync("*", period2Start, period2End);

            var period1Total = rollups1.Sum(r => r.EventCount);
            var period2Total = rollups2.Sum(r => r.EventCount);

            if (period1Total > 0)
            {
                analysis.EventCountGrowth = ((decimal)(period2Total - period1Total) / period1Total) * 100;
            }

            var period1Users = rollups1.Sum(r => r.UniqueUsers);
            var period2Users = rollups2.Sum(r => r.UniqueUsers);

            if (period1Users > 0)
            {
                analysis.UniqueUserGrowth = ((decimal)(period2Users - period1Users) / period1Users) * 100;
            }

            analysis.Period1EventCount = period1Total;
            analysis.Period2EventCount = period2Total;
            analysis.Period1UniqueUsers = period1Users;
            analysis.Period2UniqueUsers = period2Users;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to analyze growth: {ex.Message}");
        }

        return analysis;
    }
}

// ============================================================================
// QUERY RESULT TYPES
// ============================================================================

public class EventTrend
{
    public string EventName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public long TotalEvents { get; set; }
    public int TotalUniqueUsers { get; set; }
    public double AverageEventsPerHour { get; set; }
    public long PeakEventCount { get; set; }
    public DateTime PeakTimestamp { get; set; }
    public List<TrendDataPoint> HourlyData { get; set; } = new();
}

public record TrendDataPoint
{
    public DateTime Timestamp { get; set; }
    public long EventCount { get; set; }
    public int UniqueUsers { get; set; }
    public int UniqueSessions { get; set; }
    public long ProTierCount { get; set; }
}

public class SegmentDistribution
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? EventNameFilter { get; set; }
    public List<TierDistribution> ByTier { get; set; } = new();
    public List<DeviceDistribution> ByDevice { get; set; } = new();
    public List<CountryDistribution> TopCountries { get; set; } = new();
}

public record TierDistribution
{
    public string Tier { get; set; } = string.Empty;
    public long EventCount { get; set; }
    public int UniqueUsers { get; set; }
    public long ProTierEvents { get; set; }
}

public record DeviceDistribution
{
    public string Device { get; set; } = string.Empty;
    public long EventCount { get; set; }
}

public record CountryDistribution
{
    public string Country { get; set; } = string.Empty;
    public long EventCount { get; set; }
}

public class GrowthAnalysis
{
    public DateTime Period1Start { get; set; }
    public DateTime Period1End { get; set; }
    public DateTime Period2Start { get; set; }
    public DateTime Period2End { get; set; }
    public long Period1EventCount { get; set; }
    public long Period2EventCount { get; set; }
    public int Period1UniqueUsers { get; set; }
    public int Period2UniqueUsers { get; set; }
    public decimal EventCountGrowth { get; set; }
    public decimal UniqueUserGrowth { get; set; }
}
