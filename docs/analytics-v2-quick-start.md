# Analytics V2 Phase 1 - Quick Start Guide

**For Developers:** This guide explains how Phase 1 infrastructure works and how to extend it.

---

## Architecture Overview

```
┌─────────────────────────────────────────┐
│ Frontend (Vue Router)                   │
│ - Router hook fires nav:page_view       │
│ - Feature code calls track()            │
└────────────┬────────────────────────────┘
             │ POST /api/v2/analytics (v1 or v2)
             │ POST /api/v2/analytics/v2 (strict v2)
             │ POST /api/v2/analytics/batch
             │
             ▼
┌─────────────────────────────────────────┐
│ AnalyticsEndpointV2 (New)               │
│ - Auto-detects v1 vs v2 request format  │
│ - Routes to appropriate handler         │
│ - Validates and transforms payload      │
└────────────┬────────────────────────────┘
             │
             ├─→ AnalyticsCompatibilityHelper
             │   - Transform v1 → v2
             │   - Sanitize payload
             │
             ├─→ EventValidator
             │   - Check schema registry
             │   - Validate field types
             │   - Detect PII (denylist)
             │   - Check payload size
             │
             └─→ AnalyticsEventsV2Repository
                 - Insert to analytics_events_v2 (authoritative)
                 - Dual-write to analytics_events (v1, fallback)
                 - Return response (success/rejection)
```

---

## File Structure

```
server/
├── Mongoose.Api/
│   ├── Application/
│   │   ├── Telemetry/
│   │   │   └── event-schema.yml          ← Event taxonomy
│   │   ├── DTOs/Analytics/
│   │   │   ├── AnalyticsDto.cs           ← V1 (legacy)
│   │   │   └── AnalyticsV2Dto.cs         ← V2 (new)
│   │   └── Endpoints/Analytics/
│   │       ├── AnalyticsEndpoint.cs      ← V1 (deprecated)
│   │       ├── AnalyticsEndpointV2.cs    ← V2 (new, hybrid)
│   │       └── AnalyticsCompatibilityHelper.cs
│   ├── Core/
│   │   ├── Entities/Analytics/
│   │   │   ├── AnalyticsEvent.cs         ← V1 entity
│   │   │   └── AnalyticsEventV2.cs       ← V2 entity
│   │   └── Interfaces/
│   │       └── Analytics/
│   │           ├── IAnalyticsEventsRepository.cs      ← V1
│   │           ├── IAnalyticsEventsV2Repository.cs    ← V2
│   │           └── IEventSchemaRegistry.cs
│   ├── Infrastructure/
│   │   ├── Database/
│   │   │   ├── Repositories/
│   │   │   │   ├── AnalyticsEventsRepository.cs      ← V1
│   │   │   │   └── AnalyticsEventsV2Repository.cs    ← V2
│   │   │   └── Migrations/
│   │   │       └── 001_AddAnalyticsEventsV2Schema.sql
│   │   └── Telemetry/
│   │       ├── EventSchemaRegistry.cs    ← YAML loader
│   │       └── EventValidator.cs         ← Validation logic
│   └── AnalyticsV2ServiceCollectionExtensions.cs
│
└── Mongoose.Api.Tests/
    ├── AnalyticsEndpointTests.cs          ← V1 tests
    └── AnalyticsV2EndpointTests.cs        ← V2 tests
```

---

## How to Add a New Event

### Step 1: Define Event in Schema Registry

**File:** `server/Mongoose.Api/Application/Telemetry/event-schema.yml`

```yaml
events:
  feature_new_feature:
    name: feature:new_feature
    category: feature
    version: 1
    retentionDays: 90
    piiSensitive: false
    allowedPayloadKeys: [param1, param2, count]
    requiredPayloadKeys: [param1]
    payloadKeyTypes:
      param1: string
      param2: string
      count: int
    description: "User triggered new feature"
```

### Step 2: Add Tracking to Frontend

**File:** `client/src/services/analyticsApi.js`

```javascript
export function trackNewFeature(param1, param2, count = 0) {
  track('feature:new_feature', { param1, param2, count })
}
```

**Or in Vue component:**
```vue
<script setup>
import { track } from '@/services/analyticsApi'

const handleClick = () => {
  track('feature:new_feature', { 
    param1: 'value1',
    param2: 'value2',
    count: 42
  })
}
</script>
```

### Step 3: Reload Schema Registry (Auto-Loaded on Deploy)

The schema is loaded at application startup. On next deployment, the new event is available.

**Or manually reload (if needed):**
```csharp
// In a diagnostic/admin endpoint
var schemaRegistry = app.Services.GetRequiredService<IEventSchemaRegistry>();
await schemaRegistry.ReloadAsync();
```

### Step 4: Test

```csharp
[Fact]
public async Task Analytics_v2_track_new_feature_event()
{
    using var factory = new TestWebApplicationFactory();
    using var client = factory.CreateClient();

    var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/analytics/v2")
    {
        Content = JsonContent.Create(new
        {
            eventName = "feature:new_feature",
            eventVersion = 1,
            payload = new { param1 = "value1", param2 = "value2", count = 42 }
        })
    };

    var response = await client.SendAsync(req);
    
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var result = await response.Content.ReadFromJsonAsync<TrackEventV2Response>();
    result!.Success.Should().BeTrue();
}
```

---

## How to Debug Rejections

### Check Health Endpoint

```bash
curl http://localhost:5000/api/v2/analytics/health
```

Response:
```json
{
  "status": "healthy",
  "acceptanceRate": 0.99,
  "totalEvents": 1000,
  "acceptedEvents": 990,
  "rejectedEvents": 10,
  "rejectionBreakdown": {
    "EventNotInRegistry": 5,
    "RequiredPayloadFieldMissing": 3,
    "PayloadTooLarge": 2
  }
}
```

### Query Rejection Details

```sql
SELECT event_name, rejection_reason, COUNT(*) as count
FROM analytics_events_v2
WHERE rejection_reason IS NOT NULL
  AND created_at > DATE_SUB(NOW(), INTERVAL 1 HOUR)
GROUP BY event_name, rejection_reason
ORDER BY count DESC;
```

### Common Rejection Reasons

| Reason | Cause | Fix |
|--------|-------|-----|
| `EventNotInRegistry` | Event name not in schema.yml | Add event to schema registry |
| `RequiredPayloadFieldMissing` | Missing required payload key | Add required field to event payload |
| `UnknownPayloadKey` | Key not in allowedPayloadKeys | Add key to schema or remove from payload |
| `PayloadFieldTypeMismatch` | Wrong type (e.g., string instead of int) | Coerce to correct type on client |
| `PayloadTooLarge` | Serialized JSON > 4KB | Reduce payload verbosity |
| `ProhibitedDataDetected` | PII detected (email, phone, etc.) | Remove sensitive data from payload |

---

## Integration Testing

### Test V2 Single Event

```csharp
[Fact]
public async Task Test_v2_event_acceptance()
{
    using var factory = new TestWebApplicationFactory();
    using var client = factory.CreateClient();

    var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/analytics/v2")
    {
        Content = JsonContent.Create(new
        {
            eventName = "nav:page_view",
            eventVersion = 1,
            payload = new { path = "/app/overview" }
        })
    };

    var response = await client.SendAsync(req);
    var result = await response.Content.ReadFromJsonAsync<TrackEventV2Response>();

    Assert.True(result.Success);
    Assert.Null(result.RejectionReason);
}
```

### Test V2 Batch

```csharp
[Fact]
public async Task Test_v2_batch_partial_acceptance()
{
    using var factory = new TestWebApplicationFactory();
    using var client = factory.CreateClient();

    var events = new[]
    {
        new { eventName = "nav:page_view", eventVersion = 1, payload = new { path = "/app" } },
        new { eventName = "invalid:event", eventVersion = 1, payload = new { field = "value" } }
    };

    var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/analytics/v2/batch")
    {
        Content = JsonContent.Create(new { events })
    };

    var response = await client.SendAsync(req);
    var result = await response.Content.ReadFromJsonAsync<TrackBatchV2Response>();

    Assert.True(result.Success); // At least one event accepted
    Assert.Equal(1, result.Accepted);
    Assert.Equal(1, result.Rejected);
}
```

---

## Performance Considerations

### Payload Optimization

**Good (small):**
```json
{
  "matchId": "EUW1_12345",
  "index": 2,
  "queueType": "ranked_solo"
}
```
Size: ~60 bytes

**Bad (large):**
```json
{
  "matchId": "EUW1_12345",
  "fullMatchData": { ... 3KB of match details ... },
  "userHistory": { ... 1KB of user data ... }
}
```
Size: >4KB (REJECTED)

### Batch Recommendations

- **Small batches (1–5 events):** Fire-and-forget, low overhead
- **Medium batches (5–20 events):** Use `trackBatch()` when batching navigation or filter changes
- **Large batches (20–50 events):** Queue offline events before sync

### Query Optimization

For dashboards, use the materialized view instead of raw table:

```sql
-- Fast: Hourly summary
SELECT event_name, count_total, unique_users
FROM analytics_event_summary
WHERE date_hour > DATE_SUB(NOW(), INTERVAL 24 HOUR);

-- Slow: Raw events
SELECT event_name, COUNT(*), COUNT(DISTINCT user_id)
FROM analytics_events_v2
WHERE created_at > DATE_SUB(NOW(), INTERVAL 24 HOUR)
GROUP BY event_name;
```

---

## Troubleshooting

### Issue: All Events Rejected

**Symptom:** `/api/v2/analytics/health` shows 0% acceptance rate

**Diagnosis:**
```sql
SELECT rejection_reason, COUNT(*) as count
FROM analytics_events_v2
WHERE created_at > DATE_SUB(NOW(), INTERVAL 1 HOUR)
GROUP BY rejection_reason;
```

**Common Causes & Fixes:**
- **EventNotInRegistry:** Schema registry not loaded or not reloaded after changes
  - Fix: Restart app or manually call `schemaRegistry.ReloadAsync()`
- **PayloadTooLarge:** Clients sending large payloads
  - Fix: Reduce payload size on client or increase limit in validator

### Issue: Dual-Write Latency High

**Symptom:** Latency p95 > 200ms in health endpoint

**Diagnosis:**
```sql
-- Check v1 table size
SELECT 
  TABLE_NAME,
  ROUND(((data_length + index_length) / 1024 / 1024), 2) as size_mb
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'mongoose_db'
  AND TABLE_NAME IN ('analytics_events', 'analytics_events_v2');
```

**Fix:**
- Purge old events from v1 table
- Add indexes if missing
- Or: Disable v1 dual-write in Phase 3

### Issue: PII Detected Too Aggressively

**Symptom:** Valid events rejected with `ProhibitedDataDetected`

**Diagnosis:**
```sql
SELECT event_name, payload_json
FROM analytics_events_v2
WHERE rejection_reason = 'ProhibitedDataDetected'
LIMIT 5;
```

**Fix:**
- Adjust PII regex patterns in `EventValidator.cs`
- Or: Set `piiSensitive: false` for non-sensitive events in schema

---

## References

- **Schema Registry:** [`event-schema.yml`](../server/Mongoose.Api/Application/Telemetry/event-schema.yml)
- **V2 DTOs:** [`AnalyticsV2Dto.cs`](../server/Mongoose.Api/Application/DTOs/Analytics/AnalyticsV2Dto.cs)
- **Endpoint:** [`AnalyticsEndpointV2.cs`](../server/Mongoose.Api/Application/Endpoints/Analytics/AnalyticsEndpointV2.cs)
- **Tests:** [`AnalyticsV2EndpointTests.cs`](../server/Mongoose.Api.Tests/AnalyticsV2EndpointTests.cs)
- **Migration:** [`telemetry-phase-1-migration-strategy.md`](./telemetry-phase-1-migration-strategy.md)

---

**Questions?** Refer to the detailed Phase 1 documentation or ask the Analytics working group.
