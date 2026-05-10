# DPM Trend Feature - Architectural Review

Reviewed on: 2026-05-10
Scope: Architecture fit, API contracts, security boundaries, data model feasibility, and implementation risk

## Review Summary

The feature intent is strong and user-value is clear, but the current spec has several architectural mismatches with established platform patterns. The most important issues are route/versioning drift, raw PUUID input in client query parameters, and a data-model gap for phase-level DPM calculation.

## Findings (Ordered by Severity)

## 1) Critical - Route and versioning mismatch

Current spec proposes:
- `GET /api/trends/dpm?queueId=420&timeRange=90&accounts=puuid1,puuid2`

Architecture baseline uses:
- Versioned routes under `/api/v2/*`
- Trend endpoints with `{userId}` route segment, for example `/api/v2/trends/winrate/{userId}`

Risk:
- Inconsistent endpoint style increases maintenance cost and complicates client integration.

Required correction:
- Use a versioned, user-scoped trend route: `GET /api/v2/trends/dpm/{userId}`

## 2) Critical - Raw PUUID client input conflicts with security pattern

Current spec accepts client-provided PUUIDs through `accounts=puuid1,puuid2`.

Existing architecture already uses safe account selection:
- Query parameter: `accountId`
- Values: omitted (primary), `all`, or opaque `acc_*`
- Server resolves account IDs to linked PUUIDs via account resolution service

Risk:
- Raw identity keys in client requests increase exposure risk and break current ownership abstraction.

Required correction:
- Replace `accounts` parameter with `accountId`
- Resolve PUUIDs server-side only

## 3) High - Phase DPM cannot be derived from documented stored fields

Spec requires phase-level DPM for Early/Mid/Late and states existing columns are sufficient.

Documented schema currently provides:
- `participant_checkpoints`: gold/cs/xp snapshots
- `participant_metrics`: damage share, damage taken, mitigated, death timing
- No documented per-phase or minute-level damage accumulation for participants

Risk:
- Early/Mid/Late DPM values cannot be computed accurately from currently documented persisted fields.

Required correction:
- Add a storage strategy before implementation (see Data Model Options below)

## 4) High - Filter contract diverges from shared query filtering system

Current spec proposes:
- `queueId` numeric
- `timeRange` numeric days (30/90/365/0)

Existing shared filter model uses:
- `queueType` (ranked_solo, ranked_flex, normal, aram, all)
- `timeRange` normalized string values (7d, 14d, 30d, 60d, 90d, season, all)

Risk:
- Creates a one-off filter contract and duplicates normalization logic already centralized in query filtering.

Required correction:
- Use the existing query filter format to stay consistent with all analytics/trend endpoints.

## 5) Medium - Multi-account overlay behavior is underspecified vs tier gating

Spec requires multi-account overlay behavior by default.

Current account resolution behavior limits free-tier users to primary account visibility in some flows.

Risk:
- Product behavior may differ by tier unless explicitly specified in this feature.

Required correction:
- Add entitlement rules to the spec:
- Free tier: primary only
- Pro tier: single selected account or `all` overlay (if this is desired behavior)

## 6) Medium - Documentation path references are inconsistent

Current spec references architecture and UX docs using local relative paths outside canonical location.

Risk:
- Broken/ambiguous references and reduced maintainability.

Required correction:
- Update references to canonical `.github/specs/*` files.

## Recommended Contract (Proposed)

## Endpoint

`GET /api/v2/trends/dpm/{userId}`

## Query parameters

- `queueType` (optional): `ranked_solo | ranked_flex | normal | aram | all`
- `timeRange` (optional): `7d | 14d | 30d | 60d | 90d | season | all`
- `accountId` (optional): omitted = primary, `all`, or opaque `acc_*`
- `limit` (optional): integer, max 500, default per trend standard

## Response shape (architecture-aligned)

- `trendData`: array of points
- `summary`: aggregate metrics
- `gamesAnalyzed`: integer
- Optional account context metadata for overlays:
- `seriesByAccount` or `accountLabel` on points

Note:
- Keep DTO names and JSON camelCase aligned with existing trend DTO conventions.

## Data Model Options for Phase DPM (Decision Needed)

## Option A - Extend checkpoints with damage snapshots

Store cumulative damage at key minute marks (10, 20, end). Compute phase deltas in service layer.

Pros:
- Fits existing checkpoint pattern
- Low query complexity at read time

Cons:
- Schema + sync pipeline change required

## Option B - Add dedicated participant damage phase metrics table

Store early/mid/late damage per participant per match during sync.

Pros:
- Fast reads, direct API query shape
- Clear ownership of derived metric

Cons:
- New table and write-time derivation complexity

## Option C - Phase 1 with overall DPM only

Ship overall DPM now (available from total damage and game duration), defer phase split until data model is expanded.

Pros:
- Fastest path to delivery
- Minimal backend risk

Cons:
- Delivers partial user story

## Recommended path

- If timeline/sync updates are in scope: Option B
- If near-term delivery is priority: Option C now, Option B in follow-up

## Test Strategy Adjustments

- Replace PUUID query tests with `accountId`-based tests
- Add entitlement tests for free vs pro account resolution behavior
- Add tests for unsupported phase data path (until model is added)
- Keep auth/ownership/validation tests aligned to existing endpoint patterns

## Implementation Guardrails

- Keep endpoint in `Application/Endpoints/Trends/`
- Keep orchestration in Application layer; domain calculations in Core where reusable
- Use parameterized SQL only
- Sanitize logged values
- Use UTC for all timestamps
- Preserve dependency direction: Infrastructure -> Application -> Core

## Open Decisions

1. Is multi-account overlay a Pro-only feature for this endpoint?
2. Should this feature ship as overall DPM first, then phase DPM?
3. Which phase-DPM persistence option is preferred (A, B, or C)?

## Definition of Architecture-Ready

The spec is architecture-ready when:

- Route and query contract are aligned with `/api/v2` trend conventions
- Raw PUUID parameters are removed from client-facing contract
- Phase DPM data source is explicitly defined and feasible
- Entitlement behavior is explicit for account overlays
- Canonical docs references are corrected
