# Phase 3 Quick Start Guide

**Time to Deploy:** 2-4 hours  
**Complexity:** Moderate

---

## What's New in Phase 3?

| Feature | Before | After | Value |
|---------|--------|-------|-------|
| **Event Exploration** | None | 6+ dimensions | Product can self-serve analytics |
| **User Journeys** | Sessions only | Flow diagrams | Understand navigation patterns |
| **Funnels** | Manual SQL | Automated tracking | See conversion at each step |
| **Real-time** | Delayed | <5 sec | Monitor health live |
| **Data Management** | Manual | Automated purge | No data bloat |

---

## 30-Second Setup

### 1. Database (5 min)

```bash
# Apply migration
mysql -h localhost mongoose.api < server/Mongoose.Api/Infrastructure/Database/Migrations/002_AddAnalyticsPhase3Schema.sql

# Verify
mysql -h localhost mongoose.api -e "SHOW TABLES LIKE 'analytics_%';"
# Should see: event_dimensions, journey_steps, funnel_steps, rollup_hourly, funnel_definitions
```

### 2. Code Registration (5 min)

Edit `server/Mongoose.Api/Program.cs`:

```csharp
// Add after Phase 2 services
using Mongoose.Api.Infrastructure.Services.Analytics;
using Mongoose.Api.Infrastructure.Database.Repositories;
using Mongoose.Api.Infrastructure.Jobs.Analytics;

// Services
builder.Services.AddScoped<DimensionExtractionService>();
builder.Services.AddScoped<JourneyDetectionService>();
builder.Services.AddScoped<FunnelDetectionService>();
builder.Services.AddScoped<AggregationService>();

// Repositories
builder.Services.AddScoped<IAnalyticsEventDimensionsRepository, AnalyticsEventDimensionsRepository>();
builder.Services.AddScoped<IAnalyticsJourneyRepository, AnalyticsJourneyRepository>();
builder.Services.AddScoped<IAnalyticsFunnelRepository, AnalyticsFunnelRepository>();
builder.Services.AddScoped<IAnalyticsRollupRepository, AnalyticsRollupRepository>();

// Background jobs (automatically start on app launch)
builder.Services.AddHostedService<DimensionExtractionBackgroundJob>();
builder.Services.AddHostedService<RollupAggregationBackgroundJob>();
builder.Services.AddHostedService<RetentionAndPurgeBackgroundJob>();

// Endpoints automatically wired via MongooseApiApplication
```

### 3. Test Endpoints (5 min)

```bash
# List events
curl http://localhost:5000/api/v2/analytics/explore/events?timeRange=last_7d

# Get dimensions
curl http://localhost:5000/api/v2/analytics/explore/dimensions?dimension=deviceType

# View flows
curl http://localhost:5000/api/v2/analytics/journey/flows?timeRange=last_7d

# List funnels
curl http://localhost:5000/api/v2/analytics/funnels

# Check realtime
curl http://localhost:5000/api/v2/analytics/realtime/stats
```

---

## Integration Checklist

### Backend Setup

- [ ] Run database migration (002_AddAnalyticsPhase3Schema.sql)
- [ ] Add DI registrations to Program.cs
- [ ] Build solution: `dotnet build`
- [ ] Run tests: `dotnet test`
- [ ] Start local dev server
- [ ] Verify background jobs started (check logs)

### Frontend Setup

- [ ] Create `client/src/views/ProductAnalyticsView.vue` (dashboard container)
- [ ] Create `client/src/views/ExploreView.vue` (event exploration)
- [ ] Create `client/src/views/JourneyFlowView.vue` (navigation flows)
- [ ] Create `client/src/views/FunnelAnalysisView.vue` (funnel metrics)
- [ ] Create `client/src/views/RealtimeEventsView.vue` (live feed)
- [ ] Add routes to `client/src/router/index.ts`
- [ ] Add navigation link in main menu
- [ ] Test all views connect to backend

### Validation

- [ ] Generate 100+ test events using Phase 2 client queue
- [ ] Verify dimensions extracted (wait 5 min)
- [ ] Check exploration queries return results
- [ ] Validate journey flows detected
- [ ] Confirm funnel steps recorded
- [ ] Check hourly rollups created (wait 1 hour)
- [ ] Verify real-time feed populated

---

## Background Job Verification

### Check DimensionExtractionJob

```sql
-- Should grow over time (new records added every 5 min)
SELECT COUNT(*) as dimension_count FROM analytics_event_dimensions;

-- Check extraction progress
CALL sp_get_dimension_extraction_status();
```

### Check RollupAggregationJob

```sql
-- Should have one record per hour
SELECT 
  DATE_FORMAT(date_hour, '%Y-%m-%d %H:00:00') as hour,
  COUNT(DISTINCT event_name) as events,
  SUM(event_count) as total
FROM analytics_rollup_hourly
GROUP BY hour
ORDER BY hour DESC
LIMIT 24;
```

### Check RetentionAndPurgeJob

```sql
-- Runs daily at 02:00 UTC, logs to analytics_event_rejections
SELECT 
  created_at, 
  rejection_reason, 
  payload_preview
FROM analytics_event_rejections
WHERE event_name = 'system:purge'
ORDER BY created_at DESC
LIMIT 5;
```

---

## API Examples

### Explore Events

```bash
curl -s 'http://localhost:5000/api/v2/analytics/explore/events?timeRange=last_7d&tier=pro' | jq .
```

**Response:**
```json
{
  "events": [
    {
      "eventName": "feature:button_clicked",
      "eventCategory": "feature",
      "count": 1250,
      "uniqueUsers": 342,
      "uniqueSessions": 456,
      "lastOccurred": "2026-05-18T14:32:15Z",
      "topPaths": ["/dashboard", "/profile"],
      "topReferrers": ["google.com"]
    }
  ],
  "pageInfo": { "page": 1, "pageSize": 50, "total": 127 }
}
```

### Get Event Detail

```bash
curl -s 'http://localhost:5000/api/v2/analytics/explore/events/feature:button_clicked?timeRange=last_7d' | jq .
```

### Get Dimension Values

```bash
curl -s 'http://localhost:5000/api/v2/analytics/explore/dimensions?dimension=deviceType&limit=10' | jq .
```

### View Navigation Flows

```bash
curl -s 'http://localhost:5000/api/v2/analytics/journey/flows?timeRange=last_7d' | jq '.flows[0:5]'
```

### Analyze Funnel

```bash
curl -s 'http://localhost:5000/api/v2/analytics/funnels/auth_to_feature?timeRange=last_7d' | jq .
```

**Response:**
```json
{
  "funnelId": "auth_to_feature",
  "funnelName": "Authentication to Feature Activation",
  "steps": [
    {
      "stepNumber": 1,
      "stepName": "auth",
      "completedCount": 1000,
      "conversionRate": 100,
      "cumulativeConversionRate": 100
    },
    {
      "stepNumber": 2,
      "stepName": "dashboard",
      "completedCount": 850,
      "conversionRate": 85,
      "cumulativeConversionRate": 85
    },
    {
      "stepNumber": 3,
      "stepName": "feature",
      "completedCount": 542,
      "conversionRate": 63.76,
      "cumulativeConversionRate": 54.2
    }
  ],
  "summary": {
    "totalSessions": 1000,
    "completedSessions": 542,
    "overallConversionRate": 54.2
  }
}
```

### Get Real-time Metrics

```bash
curl -s 'http://localhost:5000/api/v2/analytics/realtime/stats' | jq .
```

---

## Monitoring

### Important Metrics to Track

#### 1. Dimension Extraction Lag

```sql
-- Should be <5 min
SELECT 
  TIMESTAMPDIFF(MINUTE, last_processed_at, NOW()) as lag_minutes
FROM analytics_dimension_extraction_status
LIMIT 1;
```

If lag >5 min: Check job logs, may need more workers

#### 2. Rollup Freshness

```sql
-- Should have last hour within 15 min
SELECT 
  MAX(date_hour) as latest_rollup,
  TIMESTAMPDIFF(MINUTE, MAX(date_hour), NOW()) as age_minutes
FROM analytics_rollup_hourly;
```

If age >30 min: Job may have failed, check logs

#### 3. Query Performance

```bash
# Check endpoint response times in application logs
# Target: <500ms for exploration, <1s for analysis
```

#### 4. Data Volumes

```sql
-- Monthly event growth
SELECT 
  DATE_FORMAT(event_timestamp_utc, '%Y-%m') as month,
  COUNT(*) as event_count,
  COUNT(DISTINCT user_id) as unique_users
FROM analytics_event_dimensions
GROUP BY DATE_FORMAT(event_timestamp_utc, '%Y-%m')
ORDER BY month DESC;
```

---

## Common Issues & Fixes

### Issue: "No data in /api/v2/analytics/explore/events"

**Cause:** Dimension extraction hasn't run yet  
**Fix:** Wait 5 minutes for DimensionExtractionBackgroundJob, then retry

**Cause:** No events being ingested  
**Fix:** Check Phase 2 is working: `SELECT COUNT(*) FROM analytics_events_v2;`

### Issue: Funnel steps not appearing in analysis

**Cause:** Event names don't match funnel definition  
**Fix:** Verify funnel_definitions.steps JSON matches actual event_names

**Cause:** Time between steps exceeds max_time_between_steps_hours  
**Fix:** Check analytics_funnel_steps for "completed = 0" records

### Issue: Real-time feed shows "No events"

**Cause:** No events in last 60 seconds  
**Fix:** Generate test events, or increase "seconds" query param

**Cause:** Realtime endpoint query is slow  
**Fix:** Check analytics_event_dimensions.idx_created_at index exists

### Issue: Background job errors in logs

**Cause:** Permission errors  
**Fix:** Verify database user has SELECT/INSERT on all analytics_* tables

**Cause:** Connection pool exhausted  
**Fix:** Increase MaxPoolSize in connection config

---

## Performance Tuning

### If exploration queries are slow (>1 second):

```sql
-- Verify indexes exist
SHOW INDEX FROM analytics_event_dimensions;

-- Should have indexes on:
-- - (event_name, event_timestamp_utc)
-- - (device_type, event_timestamp_utc)
-- - (tier, event_timestamp_utc)

-- If missing, rebuild:
ALTER TABLE analytics_event_dimensions ADD INDEX idx_event_name_ts (event_name, event_timestamp_utc);
```

### If dimension extraction is lagging:

Edit Program.cs DimensionExtractionBackgroundJob config:
```csharp
// Increase batch size or run frequency
const int RunIntervalMinutes = 3;  // Was 5
const int BatchSize = 2000;        // Was 1000
```

### If rollup job times out:

```sql
-- Add index if missing
ALTER TABLE analytics_event_dimensions 
  ADD INDEX idx_timestamp_category (event_timestamp_utc, event_category);

-- Or increase timeout
public async Task ExecuteJobAsync(CancellationToken cancellationToken)
{
    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    cts.CancelAfter(TimeSpan.FromMinutes(30));  // Was 15
    // ...
}
```

---

## Deployment Checklist

**Pre-Deployment:**
- [ ] All tests passing
- [ ] Database migration tested locally
- [ ] Background jobs tested (verified in logs)
- [ ] Staging environment ready

**Deployment:**
- [ ] Run migration on staging
- [ ] Deploy code to staging
- [ ] Restart API
- [ ] Monitor job execution for 1 hour
- [ ] Verify dimensions extracted
- [ ] Test all endpoints
- [ ] Get product sign-off

**Post-Deployment:**
- [ ] Monitor for 24 hours
- [ ] Check background job logs daily
- [ ] Set up alerts for anomalies
- [ ] Document any production issues
- [ ] Plan for Phase 3.1 enhancements

---

## Support

**Issues or questions?**
- Check implementation guide: [telemetry-phase-3-implementation.md](telemetry-phase-3-implementation.md)
- Review endpoint specs in [architecture.spec.md](../.github/specs/architecture.spec.md)
- Check database schema in [database-schema.spec.md](../.github/specs/database-schema.spec.md)

**File Locations:**
```
Backend:
  - Interfaces: server/Mongoose.Api/Core/Interfaces/IAnalyticsPhase3Repositories.cs
  - Services: server/Mongoose.Api/Infrastructure/Services/Analytics/
  - Jobs: server/Mongoose.Api/Infrastructure/Jobs/Analytics/
  - Endpoints: server/Mongoose.Api/Application/Endpoints/Analytics/
  - Repos: server/Mongoose.Api/Infrastructure/Database/Repositories/

Frontend:
  - Views: client/src/views/ (to be created)

Database:
  - Migration: server/Mongoose.Api/Infrastructure/Database/Migrations/002_AddAnalyticsPhase3Schema.sql
```
