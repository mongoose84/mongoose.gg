# DPM Trend Feature - Architectural Review

Reviewed on: 2026-05-10
Scope: Architecture fit, API contracts, security boundaries, and implementation risk
Feature Scope: Overall DPM trend only (no phase splits)

## Review Summary

The feature intent is strong and user-value is clear, but the current spec has several architectural mismatches with established platform patterns. The most important issues are route/versioning drift, raw PUUID input in client query parameters, and a data-model gap for phase-level DPM calculation.
The feature intent is strong and user-value is clear. Current spec is well-aligned with established /api/v2 trend patterns after contract corrections. Primary findings are filter contract alignment and optional account entitlement specification.
The feature intent is strong and user-value is clear. Current spec is well-aligned with established /api/v2 trend patterns after contract corrections. Primary findings are filter contract alignment and optional account entitlement specification.
The feature intent is strong and user-value is clear. Current spec is well-aligned with established /api/v2 trend patterns after contract corrections. Primary findings are filter contract alignment and optional account entitlement specification.

## Findings (Ordered by Severity)

## 1) Critical - Route and versioning mismatch

Current spec proposes:
- GET /api/v2/trends/damage-per-minute/{userId}
- Query params: queueType, timeRange, accountId

Architecture baseline uses:
Alignment:
- Versioned under /api/v2
- User-scoped route segment {userId}
- Kebab-case metric name matches cs-per-minute, vision-score pattern
- Query filter contract matches existing trend endpoints

Risk:
Status: RESOLVED - spec is architecture-aligned.

Required correction:

## 2) Critical - Raw PUUID client input conflicts with security pattern

Current spec accepts client-provided PUUIDs through `accounts=puuid1,puuid2`.
Current spec uses:
- Query parameter: accountId (not raw PUUIDs)
- Values: omitted (primary), all, or opaque acc_*
- Server-side account resolution via account resolution service

Existing architecture already uses safe account selection:

Risk:
Alignment:
- Matches existing trend endpoint security pattern
- Avoids raw identity keys in client requests
- Preserves ownership abstraction

Required correction:

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
## 3) High - Filter contract alignment

Current spec proposes:
- queueType (ranked_solo | ranked_flex | normal | aram | all)
- timeRange (1w | 1m | 3m | 6m | current_season | last_season | all)

Alignment:
- Uses existing IQueryFilterBuilder normalization
- Matches other trend endpoint filter contracts
- Reuses centralized query filter logic

Risk:
- Creates a one-off filter contract and duplicates normalization logic already centralized in query filtering.
Status: RESOLVED - filter contract is aligned.

Required correction:
- Use the existing query filter format to stay consistent with all analytics/trend endpoints.

## 5) Medium - Multi-account overlay behavior is underspecified vs tier gating
## 4) Medium - Account overlay entitlement is unspecified

Spec requires multi-account overlay behavior by default.
Current spec allows accountId parameter (omitted, all, or acc_*) without tier gating.

Current account resolution behavior limits free-tier users to primary account visibility in some flows.
Existing account resolution behavior:
Status: RESOLVED - filter contract is aligned.

## 4) Medium - Account overlay entitlement is unspecified

Current spec allows accountId parameter (omitted, all, or acc_*) without tier gating.

Existing account resolution behavior:
- Free-tier users see primary account only in some flows
- Pro-tier users can overlay multiple linked accounts

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
- Free tier: primary only (accountId omitted or limited to primary)

Risk:
- Product behavior may differ by tier unless explicitly specified.

Required correction:
- Add entitlement rules to spec:
	- Free tier: primary only (accountId omitted or limited to primary)
	- Pro tier: full account selection (accountId = all or any acc_*)
	- Make this explicit in endpoint documentation and service tests

	Risk:
	- Product behavior may differ by tier unless explicitly specified.

	Required correction:
	- Add entitlement rules to spec:
		- Free tier: primary only (accountId omitted or limited to primary)
	- Pro tier: full account selection (accountId = all or any acc_*)
	- Make this explicit in endpoint documentation and service tests

## Recommended Contract (Proposed)

## Endpoint

`GET /api/v2/trends/damage-per-minute/{userId}`

## Query parameters

- queueType (optional): ranked_solo | ranked_flex | normal | aram | all
- timeRange (optional): 1w | 1m | 3m | 6m | current_season | last_season | all
- `accountId` (optional): omitted = primary, `all`, or opaque `acc_*`
- `limit` (optional): integer, max 500, default per trend standard

## Response shape (architecture-aligned)

- dpmTrend: array of points
- averageDamagePerMinute: double
- overallAverage: double
- trend: string (up, down, stable)
- Point includes matchId, gameIndex, timestamp, totalDamageDealt, damagePerMinute, gameDurationMinutes, championName, role, accountGameName

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
- DTO names and JSON camelCase are aligned with VisionScoreTrendResponse, CsPerMinuteTrendResponse
Note:
- DTO names and JSON camelCase are aligned with VisionScoreTrendResponse, CsPerMinuteTrendResponse
DTO names and JSON camelCase are aligned with VisionScoreTrendResponse, CsPerMinuteTrendResponse

## Data Model Decision Deferred

This feature ships overall DPM only. Phase-level DPM (early/mid/late split) is deferred to a future iteration.

If phase DPM is required later:
- Option A: Extend checkpoints with damage snapshots (10min, 20min, end-game marks)
- Option B: Add dedicated participant damage phase metrics table
- Recommend storing at sync time to avoid expensive computation at read time

## Test Strategy Adjustments

- Replace PUUID query tests with `accountId`-based tests
- Add entitlement tests for free vs pro account resolution behavior
- Add tests for unsupported phase data path (until model is added)
- Keep auth/ownership/validation tests aligned to existing endpoint patterns
- Auth/ownership/validation tests aligned to existing endpoint patterns
- Auth/ownership/validation tests aligned to existing endpoint patterns
- accountId parameter validation (omitted, all, acc_*)
- Account resolution via account service (not raw PUUID)
- Time-range and queue-type normalization via IQueryFilterBuilder
- Response shape matches trend endpoint convention (aggregates + point array)
- If tiering applies: add entitlement tests for free vs pro account resolution

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
1. Should multi-account overlay (accountId = all) be restricted to Pro tier?
1. Should multi-account overlay (accountId = all) be restricted to Pro tier?
1. Should multi-account overlay (accountId = all) be restricted to Pro tier?
2. Should overall DPM be the default metric, with account-by-account overlay as optional detail?

## Definition of Architecture-Ready

The spec is architecture-ready when:

- Route and query contract are aligned with `/api/v2` trend conventions
- Raw PUUID parameters are removed from client-facing contract
- Phase DPM data source is explicitly defined and feasible
- Entitlement behavior is explicit for account overlays
- Canonical docs references are corrected
- Route and query contract are aligned with /api/v2 trend conventions
- Route and query contract are aligned with /api/v2 trend conventions
- Raw PUUID parameters are removed from client-facing contract
- Response naming aligns with existing trend DTO patterns
- Tier-gating for account overlay is confirmed or explicit (free tier primary only, pro tier all allowed)

## Status

- Architecture-Ready - Current spec aligns with platform patterns. Proceed with implementation pending decisions on (1) tier-gating rules and (2) confirmation that overall DPM without phase splits meets product requirements.
GET /api/v2/trends/damage-per-minute/{userId}
- Architecture-Ready - Current spec aligns with platform patterns. Proceed with implementation pending decisions on (1) tier-gating rules and (2) confirmation that overall DPM without phase splits meets product requirements.
