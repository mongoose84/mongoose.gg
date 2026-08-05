# Phase 2 Quick Start Guide

This guide covers **integrating Phase 2 (async queueing)** into your Mongoose.gg instance and troubleshooting common issues.

---

## What Changed in Phase 2?

| Aspect | Phase 1 (Sync) | Phase 2 (Async) |
|--------|---|---|
| **Endpoint** | POST /api/v2/analytics (waits for DB) | POST /api/v2/analytics/async (returns immediately) |
| **Response** | 200 OK after persistence | 202 Accepted (processing in background) |
| **Client Experience** | Blocks 100-150ms | Returns in <5ms (fire-and-forget) |
| **Burst Handling** | Limited to DB connection pool | Buffers up to 10,000 events |
| **Monitoring** | Basic health endpoint | Queue metrics, latency percentiles, alerts |

---

## Integration Checklist

### Backend Setup

**1. Update Program.cs**
```csharp
var builder = WebApplicationBuilder.CreateBuilder(args);

// Phase 1: Versioned schema (already in place)
builder.Services.AddAnalyticsV2Services(builder.Configuration);

// Phase 2: Async queue + monitoring (NEW)
builder.Services.AddAnalyticsPhase2Services(
  queueOptions: new AnalyticsQueueOptions 
  { 
    WorkerCount = 4,              // CPU-bound, so 4 threads max
    MaxQueueDepth = 10000,        // Buffer up to 10k events
  },
  abuseOptions: new AnalyticsAbuseGuardsOptions
  {
    MaxEventsPerIpPerWindow = 1000,     // 1k events/min per IP
    MaxEventsPerUserPerWindow = 5000,   // 5k events/min per user
    MaxSuspiciousUaEvents = 100,        // Strict limit for bots
    WindowSizeSeconds = 60,              // Sliding window
  });

var app = builder.Build();
// ... endpoint mapping ...
app.Run();
```

**2. Verify IMemoryCache is Registered**
```csharp
// Should already be present, but check:
builder.Services.AddMemoryCache();
```

**3. Check Database Connection Pool**
```csharp
// In appsettings.json, ensure sufficient connections:
"ConnectionStrings": {
  "DefaultConnection": "Server=...;Max Pool Size=100;..."
}
```

### Frontend Setup

**1. Update main.js or App.vue**
```javascript
import { useAnalyticsQueue } from '@/composables/useAnalyticsQueue'
import router from '@/router'

// Initialize analytics queue on app start
const { track, flush } = useAnalyticsQueue()

// Register route-change flush
router.afterEach(async (to, from) => {
  flush() // Non-blocking
})

// Export for use in components
export { track, flush }
```

**2. Update Components to Use New API**
```vue
<script setup>
import { track } from '@/main'

const handleSearch = async (query) => {
  // Non-blocking; tracked in background
  track('feature:search', { query, timestamp: Date.now() })
  
  // Then do actual search
  const results = await searchApi.query(query)
}
</script>
```

**3. Test in Browser Console**
```javascript
// Open DevTools → Console
import { useAnalyticsQueue } from '@/composables/useAnalyticsQueue'
const { track, getMetrics } = useAnalyticsQueue()

track('test:event', { foo: 'bar' })
console.log(getMetrics()) // Should show 1 queued event
```

---

## Monitoring Setup

### Grafana Dashboard

**Create new dashboard with these panels:**

**Panel 1: Queue Depth (Gauge)**
```
SELECT queue_depth 
FROM analytics_queue_metrics
ORDER BY timestamp DESC LIMIT 1
```
- Min: 0
- Max: 10,000
- Threshold: 5,000 (yellow), 9,000 (red)

**Panel 2: Events/Sec (Graph)**
```
SELECT 
  TIMESTAMP(FLOOR(server_timestamp_utc * 1000 / 5000) * 5000 / 1000) as time,
  COUNT(*) / 5 as events_per_sec
FROM analytics_events_v2
WHERE server_timestamp_utc > NOW() - INTERVAL 1 HOUR
GROUP BY time
```

**Panel 3: Processing Latency (Histogram)**
```
SELECT latency_ms
FROM analytics_processing_latency
WHERE timestamp > NOW() - INTERVAL 1 HOUR
```
- Buckets: [0, 10, 50, 100, 500, 1000, 5000]ms

**Panel 4: Acceptance Rate**
```
SELECT 
  100 * COUNT(CASE WHEN rejection_reason IS NULL THEN 1 END) / COUNT(*) as pct
FROM analytics_events_v2
WHERE server_timestamp_utc > NOW() - INTERVAL 1 HOUR
```

### Alerts

**Create alert: Queue Depth High**
```
Condition: queue_depth > 5000
Duration: 5 minutes
Notification: Slack #monitoring
```

**Create alert: Low Acceptance Rate**
```
Condition: acceptance_rate < 95%
Duration: 10 minutes
Notification: Slack #monitoring + PagerDuty
```

**Create alert: High Latency**
```
Condition: latency_p95 > 500ms
Duration: 5 minutes
Notification: Slack #monitoring
```

---

## Load Testing

### Simulated Burst Traffic (100 events in 1 second)

**Client (JavaScript)**
```javascript
const { track, getMetrics } = useAnalyticsQueue()

async function simulateBurst() {
  console.time('burst')
  
  for (let i = 0; i < 100; i++) {
    track('test:burst', { index: i })
  }
  
  console.timeEnd('burst') // Should be <100ms
  
  // Wait for processing
  setTimeout(() => {
    const m = getMetrics()
    console.log('Metrics:', m)
  }, 5000)
}

await simulateBurst()
```

**Expected Results:**
- Burst time: <100ms (fire-and-forget)
- Queue depth: ~50-100 events (batched)
- Processing: Complete within 5 seconds
- Acceptance rate: 100%

### Sustained Load Test (1,000 events/min)

```javascript
setInterval(() => {
  for (let i = 0; i < 17; i++) {
    track('test:sustained', { 
      timestamp: Date.now(),
      index: Math.random() * 1000
    })
  }
}, 1000) // ~17 events/sec * 60 = 1,020 events/min

// Monitor for 5 minutes
setTimeout(() => {
  const m = getMetrics()
  console.log('Final metrics:', m)
  console.log('Acceptance rate:', 
    (m.totalQueued - m.totalRejected) / m.totalQueued)
}, 300000)
```

**Expected Results:**
- Queue depth: Stable (<1,000 events)
- Worker threads: All active
- Latency: <100ms p95
- Acceptance: ≥99%

---

## Troubleshooting

### Issue: Queue Not Processing (Events Stuck)

**Symptoms:**
- Queue depth increasing
- No events in analytics_events_v2 table

**Diagnosis:**
```bash
# Check if workers are running
dotnet logs "AnalyticsQueueProcessor" | grep "Worker"

# Check thread pool stats
dotnet diagnostics ps | grep Mongoose.Api

# Query queue depth
curl http://localhost:5000/api/v2/analytics/async/queue
```

**Fix:**
1. **Restart backend:**
   ```bash
   systemctl restart mongoose-api
   ```

2. **Check database connectivity:**
   ```bash
   # Test from API container
   sqlcmd -S server.db -d mongoose_db -Q "SELECT COUNT(*) FROM analytics_events_v2"
   ```

3. **Increase worker threads:**
   ```csharp
   new AnalyticsQueueOptions { WorkerCount = 8 } // Double it
   ```

### Issue: Rate Limiting Blocks Legitimate Traffic

**Symptoms:**
- 429 responses from /api/v2/analytics/async
- Users reporting lost events

**Diagnosis:**
```javascript
// Check what's being blocked
const response = await fetch('/api/v2/analytics/async', {
  method: 'POST',
  body: JSON.stringify({ events: [...] })
})

if (response.status === 429) {
  const data = await response.json()
  console.error('Rate limited:', data.reason)
}
```

**Fix:**
1. **Identify the IP/user:**
   ```sql
   SELECT client_ip, user_id, COUNT(*) as count
   FROM analytics_requests
   WHERE timestamp > NOW() - INTERVAL 1 HOUR
   GROUP BY client_ip, user_id
   ORDER BY count DESC LIMIT 10
   ```

2. **Adjust thresholds:**
   ```csharp
   new AnalyticsAbuseGuardsOptions
   {
     MaxEventsPerIpPerWindow = 2000,      // Increased from 1000
     WindowSizeSeconds = 120,              // Increased window
   }
   ```

3. **Whitelist if legitimate:**
   - Add to VIP list or bypass guards for pro users
   - Document in runbook

### Issue: High Memory Usage

**Symptoms:**
- Memory growing over time
- Queue buffer consuming GB

**Diagnosis:**
```csharp
// Check queue depth vs capacity
var metrics = await queueProcessor.GetMetricsAsync();
Console.WriteLine($"Depth: {metrics.QueueDepth} / {metrics.MaxCapacity}");

// Should be ratio, not absolute numbers
```

**Fix:**
1. **Reduce max queue depth:**
   ```csharp
   new AnalyticsQueueOptions 
   { 
     MaxQueueDepth = 5000 // Was 10,000
   }
   ```

2. **Increase worker count:**
   ```csharp
   WorkerCount = 8 // Process faster
   ```

3. **Check for slow database inserts:**
   ```sql
   -- Slow query log
   SHOW FULL PROCESSLIST WHERE TIME > 5 AND COMMAND != 'Sleep'
   ```

### Issue: Metrics Endpoint Not Responding

**Symptoms:**
- GET /api/v2/analytics/async/queue returns 500
- Or hangs

**Diagnosis:**
```javascript
// Check endpoint directly
fetch('/api/v2/analytics/async/queue')
  .then(r => r.json())
  .then(m => console.log(m))
  .catch(e => console.error(e))
```

**Fix:**
1. **Restart just the queue processor:**
   ```csharp
   // In diagnostic endpoint
   var processor = sp.GetRequiredService<IAnalyticsQueueProcessor>();
   await processor.StopAsync(CancellationToken.None);
   await processor.StartAsync(CancellationToken.None);
   ```

2. **Check for deadlocks:**
   ```bash
   dotnet diagnostics collect --process-id <PID> --output diagnostics.nettrace
   dotnet trace convert diagnostics.nettrace
   ```

---

## Performance Tuning

### For High Traffic (>100,000 events/day)

```csharp
// Increase workers for parallelism
new AnalyticsQueueOptions 
{ 
  WorkerCount = 8,           // More workers
  MaxQueueDepth = 20000,     // Larger buffer
}

// Increase rate limits
new AnalyticsAbuseGuardsOptions
{
  MaxEventsPerIpPerWindow = 2000,
  MaxEventsPerUserPerWindow = 10000,
  WindowSizeSeconds = 120,   // Wider window
}
```

### For Memory-Constrained Environments

```csharp
// Reduce queue size
new AnalyticsQueueOptions 
{ 
  WorkerCount = 2,           // Fewer workers
  MaxQueueDepth = 2000,      // Smaller buffer
  ShutdownTimeoutMs = 10000, // Faster shutdown
}
```

### For Latency-Critical Applications

```csharp
// Queue is already <5ms, but optimize monitoring
public class AnalyticsPipelineMonitor : IAnalyticsPipelineMonitor
{
  private const int MetricsBufferSize = 5000; // Smaller buffer
  
  // Record less frequently
  private int _recordsBeforeFlush = 100; // Batch updates
}
```

---

## Migration from Phase 1 to Phase 2

### Step 1: Deploy Phase 2 Backend (with Phase 1 kept)

Both endpoints work simultaneously:
- `POST /api/v2/analytics` — Phase 1 (sync)
- `POST /api/v2/analytics/async` — Phase 2 (async)

### Step 2: Update Client Gradually

```javascript
// Option A: Canary (10% of users via feature flag)
if (featureFlags.analyticsPhase2) {
  const { track } = useAnalyticsQueue() // Phase 2
} else {
  const { track } = useAnalyticsSync()  // Phase 1
}

// Option B: All users at once
import { useAnalyticsQueue } from '@/composables/useAnalyticsQueue'
const { track } = useAnalyticsQueue() // Phase 2 for all
```

### Step 3: Validate in Production

```javascript
// Compare v1 and v2 event counts
const v1Count = await query(`SELECT COUNT(*) FROM analytics_events`)
const v2Count = await query(`SELECT COUNT(*) FROM analytics_events_v2`)
console.log(`V1: ${v1Count}, V2: ${v2Count}, Ratio: ${v2Count / v1Count}`)
```

### Step 4: Phase 1 Deprecation (Phase 3)

After 2 weeks on Phase 2:
- Disable Phase 1 ingestion (`POST /api/v2/analytics`)
- Keep read-only for backcompat queries
- Archive old data to cold storage

---

## Useful Commands

**Check Queue Depth:**
```bash
curl -s http://localhost:5000/api/v2/analytics/async/queue | jq '.queue.depth'
```

**Monitor Queue in Real-Time:**
```bash
watch -n 1 'curl -s http://localhost:5000/api/v2/analytics/async/queue | jq "."'
```

**Get Acceptance Rate:**
```bash
curl -s http://localhost:5000/api/v2/analytics/health | jq '.acceptanceRate'
```

**Count Events by Status (Last Hour):**
```sql
SELECT 
  SUM(CASE WHEN rejection_reason IS NULL THEN 1 ELSE 0 END) as accepted,
  SUM(CASE WHEN rejection_reason IS NOT NULL THEN 1 ELSE 0 END) as rejected
FROM analytics_events_v2
WHERE server_timestamp_utc > NOW() - INTERVAL 1 HOUR;
```

**Find Slow Database Inserts:**
```sql
SELECT 
  EVENT_NAME,
  COUNT_STAR,
  AVG_TIMER_WAIT / 1000000000 / COUNT_STAR as avg_latency_ms
FROM performance_schema.events_statements_summary_by_event_name
WHERE EVENT_NAME LIKE '%INSERT%analytics%'
ORDER BY avg_latency_ms DESC;
```

---

## References

- **Implementation:** [telemetry-phase-2-implementation.md](./telemetry-phase-2-implementation.md)
- **Phase 1 Baseline:** [telemetry-phase-0-design.md](./telemetry-phase-0-design.md)
- **Migration Strategy:** [telemetry-phase-1-migration-strategy.md](./telemetry-phase-1-migration-strategy.md)
- **Queue Service:** [analyticsQueue.js](../client/src/services/analyticsQueue.js)
- **Vue Composable:** [useAnalyticsQueue.js](../client/src/composables/useAnalyticsQueue.js)

---

**Questions?** Contact the Analytics working group or see internal wiki.
