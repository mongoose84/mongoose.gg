# Analytics Phase 1: Contract & Storage Redesign - Migration Strategy
**Version:** 1.0  
**Date:** 2026-05-17  
**Status:** Implementation Complete - Ready for Rollout  

---

## 1. Overview

This document outlines the three-phase migration strategy for rolling out the v2 analytics contract and normalized storage schema. The strategy prioritizes **rollout safety** through dual-write, compatibility transformation, and observability checkpoints.

**Key Principles:**
- **Zero User Impact** — Transparent migration; old clients continue working
- **Safe Rollback** — At any phase, revert to v1 table (analytics_events) in <5 minutes
- **Observability First** — Health checks and metrics gate each phase transition
- **Minimal Coordination** — No database migrations required; additive schema only

---

## 2. Pre-Cutover Checklist

**Before deploying Phase 1:**

- [ ] Database migration applied (creates analytics_events_v2 table + procedures)
- [ ] Event schema registry deployed and validated (event-schema.yml)
- [ ] V2 DTOs compiled and integrated
- [ ] V2 Repository and Validator services registered in DI
- [ ] V2 Endpoint configured and tested locally
- [ ] Integration tests passing (AnalyticsV2EndpointTests.cs)
- [ ] Staging environment tested with v1 + v2 endpoints active
- [ ] Rollback procedure documented and tested
- [ ] On-call engineer briefed and available for monitoring
- [ ] Dashboard configured to track acceptance rate, rejection breakdown

---

## 3. Phase 1: Deployment (Endpoint Compatibility Transform First)

**Duration:** 3–7 days (observability window)  
**Goal:** Deploy v2 infrastructure alongside v1; dual-write both tables  
**Rollout:** Canary (10% of instances) → Full Deployment  
**Exit Criteria:** ≥99% acceptance rate for 48 hours

### 3.1 Activities

#### A. Database Schema (Day 0)

```bash
# Apply migration in production
mysql -u admin -p < 001_AddAnalyticsEventsV2Schema.sql
```

**Verifies:**
- `analytics_events_v2` table created ✓
- Indexes present ✓
- Retention policy table seeded ✓
- Views and stored procedures available ✓

#### B. Endpoint Deployment (Day 1)

**Deploy code changes:**
1. AnalyticsEndpointV2.cs (new file; registers alongside v1)
2. V2 DTOs, entities, repositories
3. Event schema registry (YAML loader)
4. Validation service (EventValidator)
5. Compatibility helper (AnalyticsCompatibilityHelper)

**Endpoint Routes Added:**
- `POST /api/v2/analytics` — Hybrid (auto-detect v1 or v2)
- `POST /api/v2/analytics/v2` — Explicit v2
- `POST /api/v2/analytics/batch` — v1 batch (legacy)
- `POST /api/v2/analytics/v2/batch` — v2 batch (detailed rejections)
- `GET /api/v2/analytics/health` — Pipeline observability
- `GET /api/v2/analytics/schema` — Event schema introspection

**Behavior:**
- All ingestion routes dual-write to both v1 and v2 tables
- If v2 insert fails, v1 still succeeds (resilience)
- Clients continue using old endpoints (auto-upgraded to v2 schema)

#### C. Validation Period (Days 2–7)

**Monitor metrics hourly:**
```sql
-- Check acceptance rate
SELECT COUNT(*) as total, 
       SUM(CASE WHEN rejection_reason IS NULL THEN 1 ELSE 0 END) as accepted,
       (SUM(CASE WHEN rejection_reason IS NULL THEN 1 ELSE 0 END) / COUNT(*) * 100) as acceptance_pct
FROM analytics_events_v2
WHERE created_at > DATE_SUB(NOW(), INTERVAL 1 HOUR);

-- Check rejection breakdown
SELECT rejection_reason, COUNT(*) as count
FROM analytics_events_v2
WHERE rejection_reason IS NOT NULL
  AND created_at > DATE_SUB(NOW(), INTERVAL 1 HOUR)
GROUP BY rejection_reason;

-- Compare row counts (v1 vs v2)
SELECT 
  (SELECT COUNT(*) FROM analytics_events WHERE created_at > DATE_SUB(NOW(), INTERVAL 1 HOUR)) as v1_count,
  (SELECT COUNT(*) FROM analytics_events_v2 WHERE created_at > DATE_SUB(NOW(), INTERVAL 1 HOUR)) as v2_count;
```

**Exit Criteria:**
- V2 acceptance rate ≥99% (target <1% rejections)
- All common event names in registry (no unknown events)
- No PII detected in payloads
- Dual-write latency <50ms p95
- Database size stable (no runaway growth)

**If Issues Found:**
1. Investigate rejection reasons (use `/api/v2/analytics/health` endpoint)
2. Update event schema registry if new events discovered
3. If systematic failure: flip `EnableV2` feature flag OFF
4. Rollback (see Section 6)

### 3.2 Monitoring Dashboard

**Create Grafana dashboard showing:**

| Metric | Target | Check Frequency |
|--------|--------|-----------------|
| Events per minute (v2) | >100 | Continuous |
| Acceptance rate | ≥99% | Every 5 min |
| Rejections by reason | <1% each | Every 15 min |
| Dual-write latency (p50/p95) | <50ms/<100ms | Every 15 min |
| Database size (analytics_events_v2) | <5GB | Daily |
| Unique events per hour | ≥5 distinct | Daily |

**Alert Thresholds:**
- Acceptance rate < 98% → Warn (investigate)
- Acceptance rate < 95% → Alert (page on-call; consider rollback)
- Dual-write latency p95 > 200ms → Warn
- Database growth >100MB/hour → Alert

---

## 4. Phase 2: Schema Validation Enforcement (If Parity Issues Exist)

**Duration:** Optional; only if Phase 1 reveals systematic issues  
**Goal:** Add dual-write only if needed; prefer endpoint compatibility transform  
**Exit Criteria:** Confirmed parity; ready for Phase 3

### 4.1 Fallback Strategy: Dual-Write (If Needed)

**If observability reveals:**
- Many rejected events due to missing fields
- Client version mismatch (old SDK sends wrong payload format)
- Network errors during dual-write

**Action:**
1. Keep both endpoints running (no change to Phase 1)
2. **Do NOT** implement dual-write logic yet
3. Instead: Update event schema registry to handle variants
4. Add compatibility transform for payloads (if needed)
5. Re-validate acceptance rate
6. If still <99%: Engage with frontend team to update client

### 4.2 Parity Validation

**Compare dataset from both tables:**
```sql
-- Match event counts by name
SELECT 
  v1.event_name,
  v1.count as v1_count,
  v2.count as v2_count,
  ROUND(ABS(v1.count - v2.count) / v1.count * 100, 2) as divergence_pct
FROM
  (SELECT event_name, COUNT(*) as count FROM analytics_events 
   WHERE created_at > DATE_SUB(NOW(), INTERVAL 24 HOUR) GROUP BY event_name) v1
INNER JOIN
  (SELECT event_name, COUNT(*) as count FROM analytics_events_v2 
   WHERE created_at > DATE_SUB(NOW(), INTERVAL 24 HOUR) AND rejection_reason IS NULL GROUP BY event_name) v2
ON v1.event_name = v2.event_name;
```

**Exit Criteria:**
- Divergence <2% for all events
- No missing event names in v2
- Row counts match within acceptable margin

---

## 5. Phase 3: Cutover to V2 (Primary Source of Truth)

**Duration:** 1–2 weeks  
**Goal:** Transition v2 table to authoritative data source; v1 read-only  
**Rollout:** Gradual traffic shift (10% → 50% → 100%)  
**Exit Criteria:** All queries using v2 table; v1 archived

### 5.1 Deprecation Path

**Week 1: Announce Deprecation**
- Blog post: v1 analytics endpoint deprecated in 30 days
- Notify API partners (if any)
- Update SDKs to prefer v2 endpoint

**Week 2–3: Traffic Shift**

| Day | v2 Traffic | v1 Traffic | Action |
|-----|-----------|-----------|--------|
| D1–5 | 10% | 90% | Route 10% of new clients to v2; monitor errors |
| D6–10 | 50% | 50% | Shift 50% traffic; verify parity |
| D11–14 | 100% | 0% | Full cutover; disable v1 ingestion endpoint |

**Week 4: Archive (Optional)**

```sql
-- Export v1 data to archive
INSERT INTO archive_analytics_events_v1_backup 
SELECT * FROM analytics_events
WHERE created_at < DATE_SUB(NOW(), INTERVAL 90 DAY);

-- Truncate v1 table (keep structure for backward compat views)
DELETE FROM analytics_events
WHERE created_at < DATE_SUB(NOW(), INTERVAL 90 DAY);
```

### 5.2 Cutover Activities

**Day 1: Remove Dual-Write**
- Disable v1 inserts in AnalyticsEndpointV2.cs
- Keep v1 table for read-only queries (backward compatibility)
- V2 table becomes authoritative

**Day 2–3: Update Analytics Pipelines**
- Grafana dashboards read from analytics_events_v2
- Archive jobs target v2 table
- Retention purge jobs use v2 table

**Day 4: Monitor & Validate**
- Verify all queries switched to v2
- Check v1 table for stale data
- Confirm schema version in /api/v2/analytics/schema endpoint

---

## 6. Rollback Procedures

### 6.1 Phase 1 Rollback (If Acceptance Rate <95%)

**Trigger:** Systematic failures detected; acceptance rate drops below 95%

**Steps (Estimated 5 minutes):**

1. **Disable V2 Ingestion:**
   ```csharp
   // In AnalyticsEndpointV2.cs
   if (!config.GetValue<bool>("EnableV2Analytics")) 
       return Results.StatusCode(503); // Service Unavailable
   ```

2. **Revert to Legacy Endpoint:**
   ```csharp
   // Re-enable original AnalyticsEndpoint.cs
   app.MapPost(Route, HandleTrackEvent_LegacyV1Only);
   ```

3. **Verify:**
   - Old `/api/v2/analytics` endpoint responds
   - Events still writing to v1 table (analytics_events)
   - Client SDKs continue working (no changes needed)

4. **Notify:**
   - Page on-call; write incident report
   - Update status page: "Analytics v2 temporarily disabled; reverting to stable v1"
   - No user impact; events continue flowing

5. **Investigate:**
   - Check rejection breakdown in analytics_events_v2
   - Review error logs for exceptions
   - Engage with platform team to fix issues
   - Re-validate schema registry completeness

6. **Re-Attempt:**
   - Fix underlying issue (e.g., schema registry missing events)
   - Re-deploy with fix
   - Repeat Phase 1

### 6.2 Phase 3 Rollback (Traffic Shift)

**If issues occur during Phase 3 traffic shift:**

**Steps (<2 minutes):**

1. **Pause Traffic Shift:**
   ```csharp
   trafficShiftPercentage = 0; // Route 0% to v2
   ```

2. **Revert to V1:**
   - All traffic rerouted to v1 table
   - V2 table remains for investigation
   - No data loss

3. **Debug & Fix:**
   - Analyze errors in v2 logs
   - Check query performance (v2 schema may have indexing issues)
   - Verify data consistency

4. **Resume Slowly:**
   - Fix deployed; resume at 10% traffic shift
   - Monitor for recurrence

---

## 7. Data Retention & Purge Strategy

### 7.1 Automated Retention Job

**Run nightly at 2 AM UTC:**

```sql
-- Delete events older than retention period
CALL sp_purge_old_events();
```

**Retention by Category:**
| Category | Retention | Purge After | Archive |
|----------|-----------|-------------|---------|
| system | 7 days | 7d + 1h | None |
| navigation | 90 days | 90d + 1h | S3 (optional) |
| auth | 365 days | 365d + 1h | Indefinite (compliance) |
| feature | 90 days | 90d + 1h | S3 (optional) |
| engagement | 180 days | 180d + 1h | S3 (optional) |
| premium | 365 days | 365d + 1h | S3 (optional) |

**Monitoring:**
```sql
-- Check purge job results
SELECT * FROM analytics_event_rejections
WHERE event_name = 'system:purge'
ORDER BY created_at DESC
LIMIT 1;
```

### 7.2 Storage Projections

**Assumptions:**
- 100 active users
- 50 events per user per day = 5,000 events/day
- ~1KB per event

**Storage by Retention:**
- 7-day (system): ~35MB
- 90-day (navigation/feature): ~450MB
- 180-day (engagement): ~900MB
- 365-day (auth/premium): ~1.8GB
- **Total:** ~3.2GB hot storage + archival (cold)

**Growth Rate:**
- ~5MB/day = ~150MB/month
- At 3.2GB capacity, purge jobs prevent runaway growth

---

## 8. Success Metrics (Post-Deployment)

**Phase 1 Completion Targets:**

| Metric | Target | Current (v1) |
|--------|--------|-------------|
| Event Acceptance Rate | ≥99% | 100% (no rejection) |
| Ingestion Latency (p95) | <100ms | ~50ms (fast) |
| Schema Coverage | 100% | ~100% (14 events) |
| PII Violations | 0 | Unknown |
| Dual-Write Success Rate | ≥99% | N/A (new) |

**Phase 3 Completion Targets:**

| Metric | Target | Measurement |
|--------|--------|-------------|
| v2 Query Performance | ≤v1 latency | SELECT COUNT(*) by event_name |
| Archive Size | <10GB | S3 cold storage |
| Retention Compliance | 100% | Verify purge job results |
| Dashboard Uptime | ≥99.9% | Grafana availability |

---

## 9. Runbooks & Troubleshooting

### 9.1 High Rejection Rate (>1%)

**Symptoms:**
- `/api/v2/analytics/health` shows `AcceptanceRate < 0.99`
- Rejection breakdown shows common reason (e.g., "EventNotInRegistry")

**Diagnosis:**
```sql
-- Find rejected events
SELECT event_name, rejection_reason, COUNT(*) as count
FROM analytics_events_v2
WHERE rejection_reason IS NOT NULL
  AND created_at > DATE_SUB(NOW(), INTERVAL 1 HOUR)
GROUP BY event_name, rejection_reason
ORDER BY count DESC;
```

**Common Fixes:**

| Rejection Reason | Fix |
|-----------------|-----|
| EventNotInRegistry | Update event-schema.yml with missing events; reload registry |
| RequiredPayloadFieldMissing | Check client SDK; ensure payloads match schema |
| PayloadTooLarge | Reduce payload verbosity on client |
| ProhibitedDataDetected | Update PII regex or event piiSensitive flag |
| EventNameTooLong | Truncate event name (<100 chars) |

### 9.2 Dual-Write Failures

**Symptoms:**
- V2 events inserted, v1 events missing
- Errors in logs: "Failed to dual-write to v1 table"

**Diagnosis:**
```sql
-- Compare row counts
SELECT 
  COUNT(*) as v2_events,
  (SELECT COUNT(*) FROM analytics_events WHERE created_at > DATE_SUB(NOW(), INTERVAL 1 HOUR)) as v1_events
FROM analytics_events_v2
WHERE created_at > DATE_SUB(NOW(), INTERVAL 1 HOUR);
```

**Resolution:**
- Check v1 table permissions and disk space
- Verify MySQL connection pool size
- If v1 table is full, purge old data
- **Action:** Acceptable to accept v2 failures if v1 succeeds (fallback safety)

### 9.3 Schema Registry Stale

**Symptoms:**
- New events added by frontend, but analyzer says "EventNotInRegistry"

**Fix:**
```csharp
// In AnalyticsEndpointV2.cs
await schemaRegistry.ReloadAsync(); // Manual reload
```

**Automated:**
- Add refresh endpoint: `POST /api/v2/analytics/admin/reload-schema`
- Call on deployment
- Or: Hot-reload on file change (implement FileSystemWatcher)

---

## 10. Communication Plan

### 10.1 Stakeholder Notifications

**Team Leads:**
- Kickoff: "Rolling out v2 analytics infrastructure (zero user impact)"
- Phase 1 complete: "V2 schema live; monitoring for 48–72 hours"
- Phase 3 start: "Gradual cutover to v2 primary; 10% → 100% traffic shift"

**On-Call Engineer:**
- Alert thresholds: Acceptance rate <98%, latency p95 >200ms
- Runbook location: This document (Section 9)
- Rollback: <5 minutes to v1; page lead if issues persist

**Product/Analytics Team:**
- Provide new event schema URL: `GET /api/v2/analytics/schema`
- Available endpoints: Realtime feed, health metrics, rejection breakdown
- Phase 3 milestone: "v2 dashboards ready for GA"

### 10.2 Timeline

| Date | Activity | Audience |
|------|----------|----------|
| 2026-05-17 | Phase 1 deployment (canary) | Eng, On-call |
| 2026-05-18–20 | Observation window | Eng, Analytics |
| 2026-05-23 | Phase 1 full rollout | All |
| 2026-06-06 | Phase 3 traffic shift starts (10%) | Eng, On-call |
| 2026-06-13 | Phase 3 complete (100% v2) | All |

---

## 11. Risk Mitigation

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| V2 schema incomplete | Medium | High | Inventory all event names; validate in Phase 1 |
| PII data leakage | Low | Critical | Regex denylist + audit sampling |
| Dual-write latency bloat | Low | Medium | Monitor p95 latency; set timeout <100ms |
| Database growth runaway | Low | High | Automated retention purge; monitor daily |
| Client SDK incompatibility | Medium | Medium | Endpoint compatibility transform (v1 → v2) |
| Rollback delays | Low | High | Test rollback procedure; keep AnalyticsEndpoint.cs available |

---

## 12. Post-Deployment Retrospective

**Scheduled:** 2026-06-20 (1 week after Phase 3 complete)

**Review Topics:**
- Acceptance rate stability (≥99%?)
- Rejection breakdown (any surprises?)
- Observability (sufficient metrics?)
- Performance vs. v1 (latency, throughput)
- Pain points in migration (what went wrong?)
- Lessons learned for next iteration

**Output:** Updated runbook + practices for Phase 2 feature releases

---

## Appendix: Key Files & Locations

| File | Location | Purpose |
|------|----------|---------|
| Event Schema Registry | `server/Mongoose.Api/Application/Telemetry/event-schema.yml` | Authoritative event catalog |
| V2 DTOs | `server/Mongoose.Api/Application/DTOs/Analytics/AnalyticsV2Dto.cs` | Request/response contracts |
| V2 Endpoint | `server/Mongoose.Api/Application/Endpoints/Analytics/AnalyticsEndpointV2.cs` | Ingestion & observability routes |
| V2 Entity | `server/Mongoose.Api/Core/Entities/Analytics/AnalyticsEventV2.cs` | Storage model |
| V2 Repository | `server/Mongoose.Api/Infrastructure/Database/Repositories/AnalyticsEventsV2Repository.cs` | Data access layer |
| Validator Service | `server/Mongoose.Api/Infrastructure/Telemetry/EventValidator.cs` | Validation logic |
| Compatibility Helper | `server/Mongoose.Api/Application/Endpoints/Analytics/AnalyticsCompatibilityHelper.cs` | V1 → V2 transform |
| Tests | `server/Mongoose.Api.Tests/AnalyticsV2EndpointTests.cs` | Integration tests |
| DB Migration | `server/Mongoose.Api/Infrastructure/Database/Migrations/001_AddAnalyticsEventsV2Schema.sql` | Schema deployment |

---

**Document Approval:**
- [ ] Tech Lead (Jeppe Kronborg)
- [ ] On-Call Lead
- [ ] Database Admin
- [ ] Analytics Lead

**Version History:**
- v1.0 (2026-05-17): Initial migration and cutover strategy
