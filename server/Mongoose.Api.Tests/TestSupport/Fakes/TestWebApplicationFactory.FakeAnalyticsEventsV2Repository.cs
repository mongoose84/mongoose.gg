using System.Collections.Concurrent;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Infrastructure.Database.Repositories;

namespace Mongoose.Api.Tests;

internal sealed partial class TestWebApplicationFactory
{
    /// <summary>
    /// Fake analytics events v2 repository for testing (avoids real DB connections).
    /// </summary>
    internal sealed class FakeAnalyticsEventsV2Repository : AnalyticsEventsV2Repository
    {
        private readonly ConcurrentDictionary<long, AnalyticsEventV2> _events = new();
        private long _nextId = 1;

        public FakeAnalyticsEventsV2Repository() : base(null!) { }

        public override Task<long> InsertAsync(AnalyticsEventV2 evt)
        {
            evt.Id = _nextId++;
            _events[evt.Id] = evt;
            return Task.FromResult(evt.Id);
        }

        public override Task<int> InsertBatchAsync(IEnumerable<AnalyticsEventV2> events)
        {
            var count = 0;
            foreach (var evt in events)
            {
                evt.Id = _nextId++;
                _events[evt.Id] = evt;
                count++;
            }
            return Task.FromResult(count);
        }

        public override Task<long> GetEventCountAsync(string eventName, DateTime from, DateTime to, bool includeRejected = false)
        {
            var count = _events.Values.Count(e =>
                e.EventName == eventName &&
                e.CreatedAt >= from &&
                e.CreatedAt <= to &&
                (includeRejected || e.IsAccepted));
            return Task.FromResult((long)count);
        }

        public override Task<long> GetUniqueUserCountAsync(string eventName, DateTime from, DateTime to)
        {
            var count = _events.Values
                .Where(e => e.EventName == eventName && e.CreatedAt >= from && e.CreatedAt <= to && e.IsAccepted && e.UserId != null)
                .Select(e => e.UserId)
                .Distinct()
                .Count();
            return Task.FromResult((long)count);
        }

        public override Task<long> GetAcceptedEventCountAsync(DateTime from, DateTime to)
        {
            var count = _events.Values.Count(e => e.IsAccepted && e.CreatedAt >= from && e.CreatedAt <= to);
            return Task.FromResult((long)count);
        }

        public override Task<Dictionary<string, long>> GetRejectionsByReasonAsync(DateTime from, DateTime to)
        {
            var result = _events.Values
                .Where(e => !e.IsAccepted && e.CreatedAt >= from && e.CreatedAt <= to)
                .GroupBy(e => e.RejectionReason!)
                .ToDictionary(g => g.Key, g => (long)g.Count());
            return Task.FromResult(result);
        }

        public override Task<double> GetAcceptanceRateAsync(DateTime from, DateTime to)
        {
            var inWindow = _events.Values.Where(e => e.CreatedAt >= from && e.CreatedAt <= to).ToList();
            if (inWindow.Count == 0)
                return Task.FromResult(0.0);

            var accepted = inWindow.Count(e => e.IsAccepted);
            return Task.FromResult((double)accepted / inWindow.Count);
        }

        public override Task<int> DeleteOlderThanAsync(DateTime cutoffDate)
        {
            var toRemove = _events.Values.Where(e => e.ServerTimestampUtc < cutoffDate).Select(e => e.Id).ToList();
            foreach (var id in toRemove)
            {
                _events.TryRemove(id, out _);
            }
            return Task.FromResult(toRemove.Count);
        }

        public override Task<Dictionary<string, long>> GetEventDistributionByCategoryAsync(DateTime from, DateTime to)
        {
            var result = _events.Values
                .Where(e => e.IsAccepted && e.CreatedAt >= from && e.CreatedAt <= to)
                .GroupBy(e => e.EventCategory)
                .ToDictionary(g => g.Key, g => (long)g.Count());
            return Task.FromResult(result);
        }

        public IReadOnlyCollection<AnalyticsEventV2> GetAllEvents() => _events.Values.ToList();

        public void Clear() => _events.Clear();
    }
}
