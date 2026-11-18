using System.Collections.Concurrent;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Infrastructure.Database.Repositories;

namespace Mongoose.Api.Tests;

internal sealed partial class TestWebApplicationFactory
{
    /// <summary>
    /// Fake analytics events repository for testing.
    /// </summary>
    internal sealed class FakeAnalyticsEventsRepository : AnalyticsEventsRepository
    {
        private readonly ConcurrentDictionary<long, AnalyticsEvent> _events = new();
        private long _nextId = 1;

        public FakeAnalyticsEventsRepository() : base(null!) { }

        public override Task<int> InsertAsync(AnalyticsEvent evt)
        {
            evt.Id = _nextId++;
            _events[evt.Id] = evt;
            return Task.FromResult(1);
        }

        public override Task<int> InsertBatchAsync(IEnumerable<AnalyticsEvent> events)
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

        public override Task<long> GetEventCountAsync(string eventName, DateTime from, DateTime to)
        {
            var count = _events.Values.Count(e =>
                e.EventName == eventName &&
                e.CreatedAt >= from &&
                e.CreatedAt <= to);
            return Task.FromResult((long)count);
        }

        public override Task<long> GetUniqueUserCountAsync(string eventName, DateTime from, DateTime to)
        {
            var count = _events.Values
                .Where(e => e.EventName == eventName &&
                           e.CreatedAt >= from &&
                           e.CreatedAt <= to &&
                           e.UserId != null)
                .Select(e => e.UserId)
                .Distinct()
                .Count();
            return Task.FromResult((long)count);
        }

        public IReadOnlyCollection<AnalyticsEvent> GetAllEvents() => _events.Values.ToList();

        public void Clear() => _events.Clear();
    }
}
