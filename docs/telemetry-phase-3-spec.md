# Phase 3: Product Analytics Views
## Build Exploration Query Dimensions & First Analytics Surfaces

**Status:** Planning  
**Delivery Target:** 2026-06-15  
**Scope:** Product-usable analytics surfaces with exploration dimensions, journey flows, and funnel analysis

---

## Overview

Phase 3 transforms raw event data into **actionable product analytics** for engineering and product teams to:
- Explore events across multiple dimensions (event name, path, device, geography, tier)
- Understand user navigation journeys (paths through the app)
- Analyze critical funnels (auth → dashboard → core feature engagement)
- Visualize real-time event streams
- Manage long-term data lifecycle with retention policies and rollups

---

## Phase 3 Deliverables

### 1. Database Schema Extensions

#### Exploration Dimension Tables

**`analytics_event_dimensions`** — Pre-computed dimensions for fast querying
```sql
CREATE TABLE analytics_event_dimensions (
  id BIGINT AUTO_INCREMENT PRIMARY KEY,
  event_id BIGINT UNIQUE,
  
  -- Extracted dimensions
  event_name VARCHAR(100),
  event_category VARCHAR(50),
  
  -- Navigation
  page_path VARCHAR(500),          -- From payload.page
  referrer_domain VARCHAR(255),    -- From payload.referrer
  referrer_path VARCHAR(500),
  
  -- Device/Browser
  device_type VARCHAR(20),         -- mobile|tablet|desktop (from UA)
  browser_name VARCHAR(50),        -- chrome|safari|firefox|edge (from UA)
  browser_version VARCHAR(20),
  os_name VARCHAR(50),
  os_version VARCHAR(20),
  
  -- Geography (if IP geo available)
  country_code CHAR(2),
  region_code VARCHAR(10),
  city VARCHAR(100),
  
  -- User segment
  tier VARCHAR(20),
  is_authenticated BOOLEAN,
  
  -- Custom properties (from payload JSON)
  custom_properties JSON,
  
  -- Link back to event
  user_id BIGINT,
  session_id VARCHAR(64),
  
  -- Timestamps
  event_timestamp_utc DATETIME,
  created_at DATETIME DEFAULT UTC_TIMESTAMP,
  
  INDEX idx_event_name_timestamp (event_name, event_timestamp_utc),
  INDEX idx_page_path_timestamp (page_path, event_timestamp_utc),
  INDEX idx_device_type_timestamp (device_type, event_timestamp_utc),
  INDEX idx_country_code_timestamp (country_code, event_timestamp_utc),
  INDEX idx_tier_timestamp (tier, event_timestamp_utc),
  INDEX idx_session_id (session_id),
  INDEX idx_user_id (user_id),
  
  CONSTRAINT fk_event_id FOREIGN KEY (event_id) REFERENCES analytics_events_v2(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
COMMENT='Pre-computed dimensions for exploration queries';
```

**`analytics_journey_steps`** — User navigation flow tracking
```sql
CREATE TABLE analytics_journey_steps (
  id BIGINT AUTO_INCREMENT PRIMARY KEY,
  session_id VARCHAR(64) NOT NULL,
  user_id BIGINT,
  
  -- Journey sequence
  step_number INT NOT NULL,
  source_page VARCHAR(500),
  destination_page VARCHAR(500),
  event_name VARCHAR(100),
  
  -- Timing
  transition_timestamp_utc DATETIME,
  time_on_page_seconds INT,
  
  -- Context
  device_type VARCHAR(20),
  tier VARCHAR(20),
  
  created_at DATETIME DEFAULT UTC_TIMESTAMP,
  
  INDEX idx_session_id (session_id),
  INDEX idx_user_id (user_id),
  INDEX idx_timestamp (transition_timestamp_utc),
  CONSTRAINT fk_user_id FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
COMMENT='User navigation journey flow';
```

**`analytics_funnel_steps`** — Funnel conversion tracking
```sql
CREATE TABLE analytics_funnel_steps (
  id BIGINT AUTO_INCREMENT PRIMARY KEY,
  funnel_name VARCHAR(100) NOT NULL,         -- auth_to_feature
  session_id VARCHAR(64) NOT NULL,
  user_id BIGINT,
  
  -- Step tracking
  step_number INT NOT NULL,
  step_name VARCHAR(100) NOT NULL,           -- auth|dashboard|feature_intro|feature_action
  event_name VARCHAR(100),
  completed BOOLEAN DEFAULT 0,
  
  -- Timing
  completed_at_utc DATETIME,
  step_timestamp_utc DATETIME,
  time_since_previous_step_seconds INT,
  
  -- Context
  tier VARCHAR(20),
  device_type VARCHAR(20),
  
  created_at DATETIME DEFAULT UTC_TIMESTAMP,
  
  INDEX idx_session_id (session_id),
  INDEX idx_user_id (user_id),
  INDEX idx_funnel_name (funnel_name),
  INDEX idx_completed (completed),
  INDEX idx_timestamp (completed_at_utc),
  
  CONSTRAINT fk_user_id FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
COMMENT='Funnel step tracking for conversion analysis';
```

#### Rollup Tables

**`analytics_rollup_hourly`** — Hourly aggregations for trends
```sql
CREATE TABLE analytics_rollup_hourly (
  id BIGINT AUTO_INCREMENT PRIMARY KEY,
  
  date_hour DATETIME NOT NULL,
  event_name VARCHAR(100) NOT NULL,
  event_category VARCHAR(50),
  
  -- Aggregates
  event_count BIGINT,
  unique_users INT,
  unique_sessions INT,
  avg_payload_size_bytes FLOAT,
  
  -- Segment breakdowns
  count_authenticated BIGINT,
  count_authenticated_unique_users INT,
  count_free_tier BIGINT,
  count_pro_tier BIGINT,
  
  -- Device breakdown
  count_desktop BIGINT,
  count_mobile BIGINT,
  count_tablet BIGINT,
  
  -- Geography (if available)
  top_countries JSON,  -- [{country: 'US', count: 100}, ...]
  
  created_at DATETIME DEFAULT UTC_TIMESTAMP,
  
  UNIQUE KEY uniq_hour_event (date_hour, event_name),
  INDEX idx_date_hour (date_hour),
  INDEX idx_event_name (event_name),
  INDEX idx_event_category (event_category)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
COMMENT='Hourly event aggregates for trend analysis';
```

---

### 2. API Endpoints

#### Exploration Endpoints

**`GET /api/v2/analytics/explore/events`** — List unique events with filters
```typescript
Query Parameters:
  - timeRange: 'last_7d' | 'last_30d' | 'last_90d'
  - eventName?: string (prefix match)
  - eventCategory?: string
  - tier?: 'free' | 'pro'
  - deviceType?: 'mobile' | 'tablet' | 'desktop'
  - countryCode?: string

Response: {
  events: [
    {
      eventName: string,
      eventCategory: string,
      count: number,
      uniqueUsers: number,
      uniqueSessions: number,
      lastOccurred: ISO8601,
      topPaths: string[],
      topReferrers: string[]
    }
  ],
  pageInfo: { page: number, pageSize: number, total: number }
}
```

**`GET /api/v2/analytics/explore/dimensions`** — List dimension values for exploration
```typescript
Query Parameters:
  - dimension: 'pagePath' | 'referrer' | 'deviceType' | 'browser' | 'os' | 'country' | 'tier'
  - eventName?: string (filter to events)
  - timeRange: 'last_7d' | 'last_30d' | 'last_90d'
  - limit?: number (default 50)

Response: {
  dimension: string,
  values: [
    {
      value: string,
      count: number,
      percentOfTotal: number,
      uniqueUsers: number
    }
  ]
}
```

**`GET /api/v2/analytics/explore/events/:eventName`** — Deep-dive into single event
```typescript
Query Parameters:
  - timeRange: 'last_7d' | 'last_30d' | 'last_90d'

Response: {
  eventName: string,
  eventCategory: string,
  totalCount: number,
  uniqueUsers: number,
  
  dimensionBreakdown: {
    byPath: [{ path: string, count: number, pct: number }],
    byDevice: [{ device: string, count: number, pct: number }],
    byBrowser: [{ browser: string, count: number, pct: number }],
    byTier: [{ tier: string, count: number, pct: number }],
    byCountry: [{ country: string, count: number, pct: number }]
  },
  
  customProperties: {
    [propertyName: string]: {
      type: 'string' | 'number' | 'boolean',
      sampleValues: string[],
      cardinality: number
    }
  },
  
  timeSeriesHourly: [
    {
      hour: ISO8601,
      count: number,
      uniqueUsers: number
    }
  ]
}
```

#### Journey Endpoints

**`GET /api/v2/analytics/journey/flows`** — Top navigation flows
```typescript
Query Parameters:
  - timeRange: 'last_7d' | 'last_30d' | 'last_90d'
  - minTransitions?: number (default 5)
  - tier?: string

Response: {
  flows: [
    {
      sourcePages: string[],
      destinationPage: string,
      transitionCount: number,
      uniqueUsers: number,
      avgTimeOnSourcePageSeconds: number,
      conversionRate?: number (if leads to goal event)
    }
  ]
}
```

**`GET /api/v2/analytics/journey/user/:userId`** — Single user's journey
```typescript
Query Parameters:
  - sessionId?: string (filter to session)
  - timeRange: 'last_7d' | 'last_30d' | 'last_90d'

Response: {
  userId: number,
  sessions: [
    {
      sessionId: string,
      startTime: ISO8601,
      endTime: ISO8601,
      steps: [
        {
          stepNumber: number,
          page: string,
          eventName: string,
          timestamp: ISO8601,
          timeOnPageSeconds: number,
          deviceType: string
        }
      ]
    }
  ]
}
```

**`GET /api/v2/analytics/journey/paths`** — Common multi-step paths
```typescript
Query Parameters:
  - startEvent: string (required)
  - maxSteps?: number (default 5)
  - timeRange: 'last_7d' | 'last_30d' | 'last_90d'

Response: {
  paths: [
    {
      steps: [string],           // [event1, event2, event3]
      count: number,
      uniqueUsers: number,
      conversionRate?: number
    }
  ]
}
```

#### Funnel Endpoints

**`GET /api/v2/analytics/funnels`** — List configured funnels
```typescript
Response: {
  funnels: [
    {
      funnelId: string,
      funnelName: string,
      description: string,
      steps: [
        { stepNumber: number, stepName: string, eventName: string }
      ],
      enabled: boolean
    }
  ]
}
```

**`GET /api/v2/analytics/funnels/:funnelId`** — Funnel analysis
```typescript
Query Parameters:
  - timeRange: 'last_7d' | 'last_30d' | 'last_90d'
  - tier?: string
  - deviceType?: string

Response: {
  funnelId: string,
  funnelName: string,
  
  steps: [
    {
      stepNumber: number,
      stepName: string,
      completedCount: number,
      uniqueUsers: number,
      conversionRate: number,          // % of previous step
      cumulativeConversionRate: number,// % of first step
      avgTimeToCompleteSeconds: number
    }
  ],
  
  summary: {
    totalSessions: number,
    completedSessions: number,
    overallConversionRate: number,
    dropOffByStep: [{ step: number, dropOffs: number, dropOffRate: number }]
  },
  
  segmentBreakdown: {
    byTier: [{ tier: string, conversionRate: number, sessions: number }],
    byDeviceType: [{ device: string, conversionRate: number }],
    byCountry: [{ country: string, conversionRate: number }]
  }
}
```

#### Real-time Endpoints

**`GET /api/v2/analytics/realtime/events`** — Live event feed
```typescript
Query Parameters:
  - eventName?: string (filter)
  - limit?: number (default 50)
  - seconds?: number (events from last N seconds, default 60)

Response: {
  events: [
    {
      eventId: string,
      eventName: string,
      userId?: number,
      sessionId: string,
      pagePath: string,
      timestamp: ISO8601,
      deviceType: string,
      tier: string
    }
  ],
  generatedAt: ISO8601
}
```

**`GET /api/v2/analytics/realtime/stats`** — Real-time metrics
```typescript
Response: {
  lastMinute: {
    eventCount: number,
    uniqueUsers: number,
    uniqueSessions: number,
    eventsPerSecond: number
  },
  lastHour: {
    eventCount: number,
    uniqueUsers: number
  },
  topEvents: [{ eventName: string, count: number }],
  topPages: [{ path: string, count: number }]
}
```

---

### 3. Frontend Components

#### Vue Components

**`ProductAnalyticsView.vue`** — Main analytics dashboard
- Tab navigation: Explore | Journeys | Funnels | Real-time
- Time range selector
- Filter sidebar (tier, device, country)

**`ExploreView.vue`** — Event exploration
- Event list with search/filter
- Dimension breakdown (path, device, browser, geo, tier)
- Time series chart
- Custom property inspector

**`JourneyFlowView.vue`** — User journey visualization
- Flow diagram (Sankey or similar)
- Top navigation paths
- Step-by-step breakdown
- User session history

**`FunnelAnalysisView.vue`** — Funnel visualization
- Funnel chart (waterfall style)
- Step-by-step metrics
- Segment breakdown
- Drop-off analysis

**`RealtimeEventsView.vue`** — Live event stream
- Real-time event feed (auto-scroll)
- Live metrics (current rate)
- Event filter
- 60-second rolling window

---

### 4. Background Jobs

#### RetentionAndPurgeJob

Runs: Daily at 02:00 UTC

**Responsibilities:**
1. Purge events older than retention window (from `analytics_retention_policies`)
2. Archive old events to `analytics_events_archive` if enabled
3. Delete from dimensions/journey/funnel tables (cascading)
4. Vacuum table fragmentation
5. Update retention policy statistics

#### RollupAggregationJob

Runs: Hourly at :05 past each hour

**Responsibilities:**
1. Read raw events from last hour
2. Aggregate into `analytics_rollup_hourly`
3. Update dimension breakdowns
4. Calculate tier/device/geography breakdowns
5. Optionally prune raw events older than 30 days (if rollup enabled)

#### DimensionExtractionJob

Runs: Every 5 minutes (or trigger on event ingest)

**Responsibilities:**
1. Read non-dimension-extracted events from `analytics_events_v2`
2. Parse user agent → device_type, browser_name, os_name
3. Extract page_path, referrer from payload JSON
4. Parse geography from IP (if available)
5. Insert into `analytics_event_dimensions`
6. Mark processed in base event table (optional processed_at field)

---

### 5. Data Pipelines

#### Event Enrichment Pipeline

```
Raw Event (analytics_events_v2)
    ↓ [DimensionExtractionJob]
Parsed Dimensions (analytics_event_dimensions)
    ↓ [Journey Detector]
Navigation Step (analytics_journey_steps)
    ↓ [Funnel Detector]
Funnel Step (analytics_funnel_steps)
    ↓ [Rollup Aggregation]
Hourly Rollup (analytics_rollup_hourly)
```

#### Funnel Detection Logic

**Initial Funnel:** Auth → Dashboard → Feature Action

1. Listen for `auth:login_success` event → Mark step 1 complete
2. Within 24 hours, listen for `navigation:dashboard_viewed` → Mark step 2 complete
3. Within 24 hours, listen for configured feature action → Mark step 3 complete
4. Record timing and conversion metrics
5. Extend with additional funnels via configuration

---

## Implementation Plan

### Phase 3a: Database Schema (Days 1-2)
- [ ] Create `analytics_event_dimensions` table
- [ ] Create `analytics_journey_steps` table
- [ ] Create `analytics_funnel_steps` table
- [ ] Create `analytics_rollup_hourly` table
- [ ] Create indexes for query optimization
- [ ] Create migration script

### Phase 3b: Backend Services (Days 3-4)
- [ ] DimensionExtractionService (UA parsing, geo extraction)
- [ ] JourneyDetectionService (multi-step flow tracking)
- [ ] FunnelDetectionService (predefined funnel tracking)
- [ ] AggregationService (rollup calculations)

### Phase 3c: Background Jobs (Days 5-6)
- [ ] RetentionAndPurgeJob
- [ ] RollupAggregationJob
- [ ] DimensionExtractionJob

### Phase 3d: API Endpoints (Days 7-8)
- [ ] Exploration endpoints (events, dimensions, event detail)
- [ ] Journey endpoints (flows, user history, paths)
- [ ] Funnel endpoints (list, analysis)
- [ ] Real-time endpoints (live feed, stats)

### Phase 3e: Frontend (Days 9-10)
- [ ] ProductAnalyticsView layout
- [ ] ExploreView
- [ ] JourneyFlowView
- [ ] FunnelAnalysisView
- [ ] RealtimeEventsView

### Phase 3f: Testing & Documentation (Days 11-12)
- [ ] Integration tests for background jobs
- [ ] API endpoint tests
- [ ] Frontend component tests
- [ ] Implementation guide
- [ ] User documentation

---

## Key Decisions

| Decision | Rationale |
|----------|-----------|
| **Pre-computed dimensions vs on-demand parsing** | Pre-computed avoids parsing user agents on every query; extraction job handles async |
| **Hourly rollups vs real-time aggregates** | Hourly is practical for trending; real-time endpoints query raw events |
| **Funnel detection as async job** | Allows complex multi-step logic without blocking event ingestion |
| **Separate journey_steps table** | Faster navigation queries without joining 1M+ events; supports journey replay |
| **JSON for top_countries in rollup** | Compact storage; avoids explosion of columns for geographic breakdown |
| **Purge by retention_policies** | Flexible per-category retention (auth 365d, navigation 90d, system 7d) |

---

## Success Criteria

✅ Product team can explore events across 6+ dimensions  
✅ Journey flows visible with >99% data accuracy  
✅ Auth → Dashboard → Feature funnel shows conversion at each step  
✅ Real-time feed updates within 5 seconds of event  
✅ Query response times <1 second for exploration  
✅ Retention policies purge on schedule without errors  
✅ Hourly rollups complete within 10 minutes  
✅ Zero data loss in purge process  

---

## Performance Targets

| Metric | Target |
|--------|--------|
| Exploration query latency | <500ms |
| Funnel analysis latency | <1s |
| Journey flow query latency | <500ms |
| Real-time feed latency | <5s from event |
| Rollup completion time | <10 minutes |
| Purge job duration | <30 minutes |
| Storage efficiency | <5% overhead for dimensions/rollups |

---

## Monitoring & Alerts

**Key Metrics:**
- Dimension extraction lag (events waiting for dimension parsing)
- Funnel step detection rate (% of sessions matching each step)
- Rollup update frequency (all tables within SLA?)
- Purge success rate (0 failures)
- Query latency percentiles (p50, p95, p99)

**Alerts:**
- Dimension extraction lag >1 hour
- Funnel detection rate <80%
- Rollup job failure
- Purge job failure
- Query p95 latency >2s

---

## Phase 3 vs Phase 1 & 2

| Aspect | Phase 1 | Phase 2 | Phase 3 |
|--------|---------|---------|---------|
| **Focus** | Schema & collection | Throughput & reliability | Insights & analysis |
| **Client Impact** | Fire-and-forget ingestion | Faster responses | New analytics UI |
| **Backend** | Raw event storage | Async queue | Query & aggregation |
| **Database** | analytics_events_v2 | Queue processor | Dimensions, journeys, funnels, rollups |
| **Query Pattern** | SELECT COUNT by category | None (async only) | Complex aggregation & filtering |

---

## Next Steps

1. Review and approve Phase 3 spec
2. Schedule database schema review
3. Begin Phase 3a implementation (database schema)
4. Plan parallel backend/frontend work
5. Coordinate with Product & Engineering leads for funnel configuration
