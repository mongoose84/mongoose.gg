# Mongoose.gg Telemetry Phase 0 Design
**Version:** 0.1  
**Date:** 2026-05-16  
**Owner:** Mongoose.gg Team  
**Status:** Phase 0 - Baseline and Decisions

---

## 1. Executive Summary

This document establishes the foundation for a privacy-first, product-analytics-focused telemetry system inspired by Betterlytics patterns. Phase 0 defines the event taxonomy, privacy governance, retention strategy, and measurable success criteria to enable realtime event visibility, journey analysis, and funnel tracking while maintaining strong privacy and rollout safety.

**Primary Outcomes (Planned):**
- Realtime event ingestion with 99%+ acceptance rate
- Navigation journey reconstruction and funnel analysis
- Product-centric dashboards (event feed, conversion funnels, user journeys)
- Privacy-compliant data handling with clear retention policy

---

## 2. Telemetry Inventory

### 2.1 Current Producers (Frontend)

| Producer | Location | Events | Frequency | Auth Required |
|----------|----------|--------|-----------|----------------|
| Router Navigation Guard | `client/src/router/index.js` | `page:view` | On route change | No (attached post-login) |
| Match Details | `analyticsApi.js` convenience methods | `match:select`, `match:details_view`, `match:section_toggle`, `match:lane_expand` | On user action | Implicit (session-tracked) |
| Auth Service | `analyticsApi.js` convenience methods | `auth:login`, `auth:logout`, `auth:register` | On auth action | No (pre/post-login) |
| Filter Changes | `analyticsApi.js` convenience methods | `filter:change` | On filter update | No |

**Total Active Events (Current):** ~10 distinct event names  
**Session ID Tracking:** Enabled (UUID or timestamp-based fallback)  
**Batch Support:** Yes (50 events max per batch)

### 2.2 Current Consumers (Backend)

| Consumer | Location | Usage | Access Pattern |
|----------|----------|-------|-----------------|
| Analytics Events Repository | `server/Mongoose.Api/Infrastructure/Database/Repositories/AnalyticsEventsRepository.cs` | Event storage and basic aggregation | INSERT (single/batch), SELECT COUNT, SELECT DISTINCT COUNT |
| Grafana Dashboards (Planned) | External (read-only) | Usage trends, event frequency, user segments | Direct SQL or materialized views |
| Logging/Debugging | `AnalyticsEndpoint.cs` | Validation errors, acceptance/rejection reasons | Structured logs with sanitized event names |

**Current Query Patterns:** Event count by name and time range, unique user count  
**Pipeline Observability:** Minimal (only logs errors/rejections; no queue depth or latency metrics)

### 2.3 Current Data Contracts

#### Analytics Event Entity (Backend)
```csharp
public class AnalyticsEvent : EntityBase
{
    public long Id { get; set; }
    public long? UserId { get; set; }          // Nullable for anonymous events
    public string Tier { get; set; }            // "free", "pro" (captures user segment)
    public string EventName { get; set; }       // Max 100 chars, required, colon-separated naming
    public string? PayloadJson { get; set; }    // Max 4KB, optional
    public string? SessionId { get; set; }      // Max 64 chars, optional
    public DateTime CreatedAt { get; set; }     // Server-side UTC
}
```

#### Ingestion Request Contract (Frontend)
```typescript
interface TrackEventRequest {
  eventName: string;                 // Required, max 100 chars
  payload?: Record<string, unknown>; // Optional, max 4KB serialized JSON
  sessionId?: string;                // Optional, max 64 chars, client-generated
}

interface TrackBatchRequest {
  events: TrackEventRequest[];       // Max 50 events per batch
}
```

#### HTTP Endpoints
- `POST /api/v2/analytics` — Single event ingestion
- `POST /api/v2/analytics/batch` — Batch event ingestion (max 50)
- Authentication: Optional (attaches userId if present; fires successfully as anonymous otherwise)

---

## 3. Event Taxonomy & Naming Convention

### 3.1 Naming Standard

**Format:** `<category>:<action>` or `<category>:<subcategory>:<action>`

**Rules:**
- Lowercase letters, digits, and colons only
- Maximum 100 characters
- Unique, immutable, and versioned (append `_v2` if schema changes)
- Human-readable for debugging and dashboards
- Hierarchical for grouping and filtering

### 3.2 Event Classification

#### System Events (Informational)
*For infrastructure and debugging; excluded from product analytics dashboards.*

| Event | Payload | Tier | Purpose |
|-------|---------|------|---------|
| `system:session_start` | `{ duration?: ms }` | N/A | Session initialization; fire-and-forget |
| `system:error:network` | `{ status, endpoint }` | N/A | Network failures (5xx, timeout) |
| `system:error:validation` | `{ field, reason }` | N/A | Input validation failures |

#### Navigation Events (User Journey)
*Foundation for funnel and journey analysis.*

| Event | Payload | Tier | Purpose |
|-------|---------|------|---------|
| `nav:page_view` | `{ path, referrer?, title? }` | All | Page/view entry point; fired on route change |
| `nav:section_enter` | `{ section, entryPoint }` | All | Entry into major app section (e.g., `/app/matches`) |
| `nav:section_exit` | `{ section, exitReason? }` | All | Exit from section; helps detect drop-off points |

#### Authentication Events
*Account lifecycle and security tracking.*

| Event | Payload | Tier | Purpose |
|-------|---------|------|---------|
| `auth:login_attempt` | `{ method, success, errorCode? }` | N/A | Login attempt; method = 'email' or 'oauth' |
| `auth:register_attempt` | `{ method, success, errorCode? }` | N/A | Registration attempt |
| `auth:logout` | `{ reason? }` | N/A | User-initiated logout |
| `auth:session_expired` | `{}` | N/A | Session timeout (passive) |
| `auth:password_reset_attempt` | `{ success }` | N/A | Password reset flow |

#### Feature Events (Product Usage)
*User interaction with core product features.*

| Event | Payload | Tier | Purpose |
|-------|---------|------|---------|
| `feature:match_select` | `{ matchId, index, queueType }` | All | Match selected from list; funnel: matches viewed → selected |
| `feature:match_details_view` | `{ matchId, role, win? }` | All | Match details panel opened; funnel: selected → details viewed |
| `feature:champion_select_opened` | `{ context? }` | All | Champion select tool opened |
| `feature:goal_created` | `{ goalType, public? }` | Pro | Goal creation; engagement indicator |
| `feature:filter_applied` | `{ filterType, value }` | All | Queue/time range filter change |
| `feature:section_expanded` | `{ section, from? }` | All | Accordion/expandable content toggled |

#### Engagement Events (Conversion & Retention)
*High-level product engagement and value signals.*

| Event | Payload | Tier | Purpose |
|-------|---------|------|---------|
| `engagement:session_duration` | `{ seconds, viewCount, eventCount }` | All | Session summary; fired on exit (batch or passive close) |
| `engagement:feature_adoption` | `{ feature, adoptionStage }` | All | Feature first-use or regular adoption milestone |
| `engagement:upgrade_initiated` | `{ plan, source? }` | Free | User starts upgrade flow |
| `engagement:feedback_submitted` | `{ sentiment?, category? }` | All | User submits feedback or bug report |

#### Premium/Tier Events
*Feature usage tracking by user tier.*

| Event | Payload | Tier | Purpose |
|-------|---------|------|---------|
| `premium:advanced_filter_used` | `{ filterName }` | Pro Only | Usage of pro-tier features |
| `premium:export_requested` | `{ format, dataType }` | Pro Only | Data export initiation |

### 3.3 Payload Allowlist & Validation

**General Rules:**
- Payloads must be flat JSON objects (no nested objects, arrays only for primitives).
- Max 4KB serialized JSON.
- Keys must use camelCase.
- No PII allowed (see Section 5).

**Payload Allowlist (Whitelist Approach):**
- Only predefined keys are accepted for each event type.
- Unknown keys are silently dropped to prevent payload bloat.
- Example: `feature:match_select` accepts only `{ matchId, index, queueType }`.

**Validation Logic (Backend):**
```csharp
// Pseudo-code for validation
var allowedKeys = eventRegistry[eventName]?.AllowedPayloadKeys ?? [];
var filtered = payload
    .Where(kv => allowedKeys.Contains(kv.Key))
    .ToDictionary(kv => kv.Key, kv => kv.Value);
```

---

## 4. Privacy & Data Governance

### 4.1 Privacy-First Principles

1. **Minimize PII Collection:** Collect only userId (opaque identifier) and tier; no names, emails, or Riot accounts at event level.
2. **Sanitize Sensitive Payloads:** Log only event name, never full payload, to prevent accidental PII leakage.
3. **Transparent Retention:** Clear, documented retention policies per event class.
4. **Opt-Out Ready:** Design for future opt-out: flag events with `telemetryOptOut` and handle gracefully.

### 4.2 Prohibited Data (DenyList)

**Absolute Prohibitions (Validation & Rejection):**
- Email addresses, phone numbers
- Plaintext usernames or Riot account names
- IP addresses (logged by CDN, not in event)
- Device fingerprints
- Full URLs or paths containing PII (e.g., `/user/jdoe@example.com/settings`)
- Plaintext credentials or API keys

**Validation Strategy:**
- Regex patterns to detect email-like strings in payload
- Reject event with `400 Bad Request` if prohibited data detected
- Log rejection reason sanitized (no actual data)

### 4.3 User Consent & Opt-Out

**Current State:** Consent handled via landing page banner; telemetry fires for all users (anon or auth).

**Future (Post-Phase 0):** 
- Add `telemetryOptOut` flag to user preferences.
- Backend: Skip ingestion if flag set; return `200 OK` silently.
- Frontend: Check flag before firing non-essential events.

### 4.4 Data Retention Policy

| Event Class | Retention Period | Rationale | Archive/Purge |
|-------------|------------------|-----------|----------------|
| System Events | 7 days | Debugging logs; short-lived utility | Auto-delete after 7 days |
| Navigation Events | 90 days | Journey analysis; monthly/quarterly trends | Archive to cold storage after 90 days |
| Authentication Events | 365 days | Security audit trail; compliance | Archive after 365 days; retain indefinitely for compliance |
| Feature Events | 90 days | Product usage trends; weekly dashboards | Archive after 90 days |
| Engagement Events | 180 days | Retention/churn analysis; historical funnels | Archive after 180 days |
| Premium Events | 365 days | Revenue attribution; pro-user behavior | Archive after 365 days |

**Database Size Estimate (Conservative):**
- Assumption: 100 active users, 50 events per user per day = 5,000 events/day
- Storage: ~1KB per event = 5MB/day = ~150MB/month
- At 90-day retention: ~450MB hot storage

**Purge Strategy:**
1. Automated nightly job: Delete events older than retention period
2. Monthly batch archive to S3 for cold analysis
3. Compliance records (auth events) retained indefinitely in secure archive

---

## 5. Pipeline Observability & Quality Metrics

### 5.1 Acceptance Criteria

**Target Acceptance Rate:** ≥99% of valid events persisted  
**Valid Event Definition:** Proper event name (1–100 chars), optional valid payload (<4KB), optional valid sessionId (<64 chars)

**Rejection Reasons (Track Separately):**
- `MISSING_EVENT_NAME` — No event name provided
- `EVENT_NAME_TOO_LONG` — Event name >100 chars
- `PAYLOAD_TOO_LARGE` — Serialized JSON >4KB
- `PROHIBITED_DATA_DETECTED` — PII regex match
- `INVALID_SESSION_ID` — SessionId >64 chars
- `INVALID_PAYLOAD_JSON` — JSON parsing error
- `DATABASE_ERROR` — Persistence failure (retry candidate)

**Observability Endpoints (Future):**
- `GET /api/v2/analytics/health` — Event ingestion health: acceptance rate, rejections by reason, ingestion latency p50/p95/p99
- `GET /api/v2/analytics/schema` — Current event schema and allowed payloads (for frontend validation)

### 5.2 Latency & Performance Targets

| Metric | Target | Measurement |
|--------|--------|-------------|
| Ingestion latency (p50) | <50ms | Client fetch time to `/api/v2/analytics` |
| Ingestion latency (p95) | <200ms | 95th percentile |
| Batch ingestion throughput | >1,000 events/sec | Max concurrent batch requests |
| Database insert latency (p50) | <10ms | SQL INSERT completion |

---

## 6. KPI Targets & Success Criteria

### 6.1 Phase 0 Success Metrics

| KPI | Target | Purpose | Measurement |
|-----|--------|---------|-------------|
| Event Schema Completeness | 100% of active features mapped to events | Ensure complete visibility | Checklist: Feature → Event mapping |
| Privacy Policy Enforcement | 0 PII leaks in sampling | Prevent data loss | Weekly sample review |
| Retention Policy Adherence | 100% auto-deletion on schedule | Data governance | Verify purge jobs run successfully |
| Event Ingestion Uptime | ≥99.9% | Reliable pipeline | Monitor endpoint availability |

### 6.2 Phase 1+ Product Analytics Targets

| KPI | Target | Purpose |
|-----|--------|---------|
| Journey Completion Rate | Track % of users completing key journeys (e.g., Login → Overview) | Identify drop-off points |
| Match Details Funnel Conversion | ≥70% of match selections → details view | Feature engagement baseline |
| Session Duration (Auth Users) | Baseline establish; target +10% per quarter | Retention improvement |
| Premium Feature Adoption | ≥30% pro-tier users use advanced filters within 7 days | Feature value realization |

---

## 7. System Architecture

### 7.1 Event Flow

```
┌─────────────────────────────────────────────────────────────┐
│ FRONTEND (Vue + Router)                                      │
│  - Router hook fires page:view on navigation                │
│  - Feature code calls track() or trackBatch()               │
│  - Session ID maintained in-memory                          │
└────────────────────┬────────────────────────────────────────┘
                     │ fetch POST /api/v2/analytics
                     │ fetch POST /api/v2/analytics/batch
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ BACKEND (AnalyticsEndpoint)                                 │
│  1. Validate event name, payload size, sessionId length     │
│  2. Check for prohibited data (PII regex)                   │
│  3. Attach userId (if authenticated) and tier               │
│  4. Serialize payload to JSON                               │
│  5. Insert to analytics_events table                        │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ DATABASE (MySQL)                                             │
│  analytics_events table:                                    │
│    - id (auto-increment)                                    │
│    - user_id (nullable for anon events)                     │
│    - tier ('free' or 'pro')                                 │
│    - event_name (varchar 100, indexed)                      │
│    - payload_json (longtext, optional)                      │
│    - session_id (varchar 64, optional, indexed)             │
│    - created_at (timestamp UTC)                             │
│  Indexes: (event_name, created_at), (user_id, created_at) │
└─────────────────────────────────────────────────────────────┘
```

### 7.2 Batch Processing & Reliability

**Single Event:**
- Client: Fire-and-forget; failures silently logged to console.
- Server: Return `200 OK` even if DB fails; log error for debugging.
- Reliability: Best-effort; analytics should never break user experience.

**Batch Events:**
- Client: Optional explicit batching via `trackBatch([events])`.
- Server: Validate all events; insert individually (future: bulk insert).
- Partial Success: If 3/5 events valid, insert 3; return count and success=true.

**Retry Strategy (Frontend):**
- No automatic retry (fire-and-forget model).
- Future: Optional queueing for offline/slow network.

---

## 8. Implementation Roadmap

### Phase 0: Baseline & Decisions (Current)

**Deliverables:**
- ✅ Telemetry inventory (producers, consumers, event registry)
- ✅ Event taxonomy & naming conventions
- ✅ Privacy policy & data governance
- ✅ Retention strategy
- ✅ KPI targets & success criteria
- ✅ System architecture documented
- ⬜ (Future) Event schema registry implementation

**Exit Criteria:**
- Stakeholder sign-off on event taxonomy and privacy policy
- All producers/consumers mapped to event types
- Retention purge jobs scheduled (not yet deployed)

### Phase 1: Event Schema Formalization (Planned)

**Goals:**
- Implement event registry (YAML or JSON file).
- Add server-side schema validation and rejection tracking.
- Deploy health endpoints for observability.
- Add PII detection regex patterns.

**Estimated Effort:** 1–2 weeks

**Key Tasks:**
1. Define event registry schema (name, category, allowedPayloadKeys, retentionDays, piiSensitive flag).
2. Implement `EventSchemaValidator` in backend.
3. Add rejection reason tracking and `GET /api/v2/analytics/health` endpoint.
4. Add frontend-side schema sync for pre-validation.

### Phase 2: Privacy & Retention Automation (Planned)

**Goals:**
- Implement PII denylist validation.
- Deploy automated retention purge jobs.
- Add opt-out support (database flag + backend logic).

**Estimated Effort:** 1–2 weeks

**Key Tasks:**
1. Define PII regex patterns (email, phone, common sensitive fields).
2. Implement `PiiValidator` in backend validation pipeline.
3. Implement nightly purge job using task scheduler or Hangfire.
4. Add telemetry opt-out to user preferences and event filtering.

### Phase 3: Product Analytics Dashboards (Planned)

**Goals:**
- Realtime event feed (Grafana dashboard).
- Journey/funnel analysis views (custom SQL queries or dedicated service).
- Event distribution by name, user tier, time range.

**Estimated Effort:** 2–3 weeks

**Key Tasks:**
1. Create Grafana dashboard for event overview (count by name, user segments).
2. Add journey queries (e.g., "% of users who viewed matches → selected match → viewed details").
3. Add funnel visualization (multi-step conversion rates).
4. Add retention cohort queries by user tier.

---

## 9. Data Model & Schema

### 9.1 Database Table (Existing)

```sql
CREATE TABLE analytics_events (
  id BIGINT AUTO_INCREMENT PRIMARY KEY,
  user_id BIGINT NULL,
  tier VARCHAR(20) NOT NULL DEFAULT 'free',
  event_name VARCHAR(100) NOT NULL,
  payload_json LONGTEXT NULL,
  session_id VARCHAR(64) NULL,
  created_at DATETIME NOT NULL DEFAULT UTC_TIMESTAMP,
  
  INDEX idx_event_name_created (event_name, created_at),
  INDEX idx_user_id_created (user_id, created_at),
  INDEX idx_session_id (session_id),
  INDEX idx_created_at (created_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

### 9.2 Event Registry (Phase 1+)

**Planned format (YAML):**
```yaml
events:
  nav_page_view:
    name: nav:page_view
    category: navigation
    allowedPayloadKeys: [path, referrer, title]
    retentionDays: 90
    piiSensitive: false
    description: "User navigated to a page/view"
  
  feature_match_select:
    name: feature:match_select
    category: feature
    allowedPayloadKeys: [matchId, index, queueType]
    retentionDays: 90
    piiSensitive: false
    description: "User selected a match from the list"
```

---

## 10. Rollout & Safety

### 10.1 Rollout Strategy

**Phase 0 (Baseline):** No changes to existing infrastructure.  
**Phase 1 (Formalization):** Backward-compatible schema validation (no rejections in initial phase).  
**Phase 2 (Enforcement):** Gradual rollout of PII validation (alert-only → soft-reject → hard-reject).

### 10.2 Feature Flags

**Planned (Phase 1+):**
- `EnableEventSchemaValidation` — Toggle strict schema enforcement.
- `EnablePiiDetection` — Toggle PII denylist validation (alert vs. reject).
- `TelemetryOptOutEnabled` — Allow users to opt-out of non-essential events.

---

## 11. Open Questions & Future Considerations

| Question | Status | Notes |
|----------|--------|-------|
| Should system events be captured separately from product events? | Decided: YES | Separate table or flag field for filtering |
| Will we support custom event ingestion from 3rd-party integrations? | TBD (Phase 2+) | Likely API key auth; defined schema only |
| How do we handle mobile app telemetry (future)? | TBD | Likely same contract; native client TBD |
| Should we archive cold data to S3 for long-term analysis? | TBD (Phase 3+) | Depends on analytics tool (Grafana limitations) |
| How do we correlate backend errors with user events? | TBD (Phase 2+) | Link error logs to session_id or user_id? |

---

## 12. Appendix: Event Taxonomy Reference

### Full Event Catalog (v1.0)

```
SYSTEM EVENTS (non-product)
  system:session_start
  system:error:network
  system:error:validation

NAVIGATION EVENTS (user journey)
  nav:page_view
  nav:section_enter
  nav:section_exit

AUTHENTICATION EVENTS (account lifecycle)
  auth:login_attempt
  auth:register_attempt
  auth:logout
  auth:session_expired
  auth:password_reset_attempt

FEATURE EVENTS (core product usage)
  feature:match_select
  feature:match_details_view
  feature:champion_select_opened
  feature:goal_created
  feature:filter_applied
  feature:section_expanded

ENGAGEMENT EVENTS (conversion & retention)
  engagement:session_duration
  engagement:feature_adoption
  engagement:upgrade_initiated
  engagement:feedback_submitted

PREMIUM/TIER EVENTS (pro-tier usage)
  premium:advanced_filter_used
  premium:export_requested
```

---

## 13. Approval & Sign-Off

| Role | Name | Date | Signature |
|------|------|------|-----------|
| Product Manager | — | — | — |
| Tech Lead | Jeppe Kronborg | 2026-05-16 | (pending) |
| Security/Privacy | — | — | — |
| Engineering Lead (Backend) | — | — | — |

---

**Document History:**
- 2026-05-16: Initial Phase 0 baseline document created
- (Future updates to be logged here)
