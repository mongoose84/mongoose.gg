# Test Implementation Backlog

**Project**: Mongoose.gg  
**Created**: 2026-04-06  
**Source**: repo audit of current backend, frontend, and Playwright coverage

> This backlog reflects the **current codebase state**, not the older coverage counts in the spec files. A few spec documents are slightly stale, so this file should be used as the practical implementation list.

---

## Goal

Add the missing tests with the highest product and regression risk first:

1. **Protect critical backend behavior** around auth, sync, and Riot ingestion
2. **Cover high-value frontend flows** for matches, settings, and dashboard states
3. **Expand E2E coverage** for real user journeys and negative paths

---

## Priority Legend

- **Critical** — high regression or security risk; should be implemented first
- **Important** — meaningful gaps in existing coverage
- **Low** — useful confidence boosters, lower immediate risk

---

## 1) Critical Tests to Implement First

### Backend — new test suites

#### `server/Mongoose.Api.Tests/LoginSyncServiceTests.cs`
**Why**: `LoginSyncService` runs on login and coordinates Riot profile refresh + sync triggering.

**Add tests for:**
- [x] No linked Riot accounts → exits without error
- [x] Profile icon / summoner level changes → repository update is called
- [x] Rank data changes → rank update is persisted correctly
- [x] Recent `LastSyncAt` within cooldown → match sync is skipped
- [x] Account already `pending` or `syncing` → no duplicate sync trigger
- [x] New matches found from Riot API → account moves to `pending` and progress broadcast starts
- [x] Riot API/profile update failure → service logs warning and does not break login
- [x] One account fails but remaining linked accounts still continue processing

#### `server/Mongoose.Api.Tests/RiotTimelineMapperTests.cs`
**Why**: timeline-derived analytics feed multiple solo dashboard features.

**Add tests for:**
- [x] Checkpoint mapping from timeline frames
- [x] Death timing extraction
- [x] Objective participation extraction
- [x] Death position extraction
- [x] Team gold metric extraction
- [x] Partial / malformed Riot timeline payloads handled safely
- [x] Empty frames/events return stable defaults instead of exceptions

#### `server/Mongoose.Api.Tests/RiotApiClientTests.cs`
**Why**: external dependency behavior is currently under-tested.

**Add tests for:**
- [x] Correct URL composition per Riot endpoint
- [x] Non-200 responses are surfaced or handled correctly
- [ ] Rate-limit / retry behavior
- [x] Timeout / cancellation handling
- [x] JSON parsing for expected response shapes

#### `server/Mongoose.Api.Tests/SeasonHelperTests.cs`
**Why**: season resolution affects ingestion and persistence correctness.

**Add tests for:**
- [x] Reuses an existing season when present
- [x] Inserts a new season when missing
- [x] Handles current season boundaries correctly
- [x] Works with UTC date assumptions

---

### Backend — missing endpoint suites

For all protected data endpoints, follow the standard endpoint pattern:
- [ ] **200 OK** happy path
- [ ] **401 Unauthorized** unauthenticated request
- [ ] **403 Forbidden** accessing another user’s data
- [ ] **404 Not Found** no linked Riot account

#### Create these suites:
- [x] `RegisterEndpointTests.cs`
- [x] `LogoutEndpointTests.cs`
- [x] `PublicStatsEndpointTests.cs`
- [x] `MatchActivityEndpointTests.cs`

**Extra checks to include where relevant:**
- [ ] Bad request validation for malformed input
- [ ] Rate-limiting behavior for public endpoints
- [ ] Response contract shape / JSON field names

---

### Frontend — highest-value missing unit coverage

#### Match history UI (`client/src/components/matches/`)
**Why**: important user-facing flow with many currently untested components.

Create specs for:
- [x] `MatchList.vue`
- [x] `MatchDetails.vue`
- [x] `MatchNarrative.vue`
- [x] `MatchHeader.vue`
- [x] `MatchActions.vue`
- [x] `ImpactStats.vue`
- [x] `LaneMatchupDetails.vue`
- [x] `StatSnapshot.vue`
- [x] `TeamComparison.vue`
- [x] `TrendBadge.vue`

**Each spec should cover:**
- [ ] render with data
- [ ] empty state
- [ ] loading state
- [ ] error state (if component supports it)
- [ ] key interactions / emitted events

#### High-value views
- [ ] `client/src/views/UserSettingsPage.vue`
- [ ] `client/src/views/ChampionSelectPage.vue`
- [ ] `client/src/views/FeedbackPage.vue`

**View-level scenarios:**
- [ ] initial load state
- [ ] service success state
- [ ] service failure state
- [ ] user interaction flows (save, submit, retry, navigate)

---

## 2) Important Coverage Expansions

### Backend — strengthen existing endpoint suites

#### `OverviewEndpointTests.cs`
Add:
- [ ] `403 Forbidden` for authenticated user requesting another user’s overview

#### `SoloPerformanceEndpointTests.cs`
Add:
- [ ] `403 Forbidden`
- [ ] `404 Not Found` when no Riot account is linked

#### Trend endpoint suites
Current trend coverage is uneven. Expand:
- [ ] `WinrateTrendEndpointTests.cs`
- [ ] `CsPerMinuteTrendEndpointTests.cs`
- [ ] `GoldAt15TrendEndpointTests.cs`
- [ ] `DeathsTrendEndpointTests.cs`

**Add:**
- [ ] 401 unauthenticated
- [ ] 403 forbidden
- [ ] 404 missing Riot account
- [ ] validation / filter edge cases where appropriate

#### `RiotAccountsEndpointTests.cs`
Add explicit coverage for:
- [ ] `/users/me/riot-accounts/{puuid}/sync`
- [ ] `/users/me/riot-accounts/{puuid}/sync-status`
- [ ] wrong-user / non-owned account cases
- [ ] already-linked / invalid-region / Riot lookup failure paths

#### Background jobs
Expand:
- [ ] `MatchHistorySyncJobTests.cs` with duplicate IDs, partial Riot failures, retry/rate-limit scenarios
- [ ] `MatchCleanupJobTests.cs` with retention boundary and idempotency scenarios

---

### Frontend — module tests with strong ROI

#### Composables
- [ ] `client/test/unit/composables/useWinRateColor.spec.js`

#### Stores
- [ ] `client/test/unit/stores/uiStore.spec.js`

#### Services
- [ ] `client/test/unit/services/feedbackApi.spec.js`
- [ ] `client/test/unit/services/matchesApi.spec.js`
- [ ] `client/test/unit/services/soloApi.spec.js`
- [ ] `client/test/unit/services/trendsApi.spec.js`
- [ ] `client/test/unit/services/publicApi.spec.js`
- [ ] `client/test/unit/services/accountContext.spec.js`

#### Utils
- [ ] `client/test/unit/utils/leagueAssets.spec.js`
- [ ] `client/test/unit/utils/chartConfigs.spec.js`

#### Remaining views/layouts
- [ ] `client/src/layouts/AppLayout.vue`
- [ ] `client/src/views/GoalsPage.vue`
- [ ] `client/src/views/TeamAnalytics.vue`

---

## 3) E2E Flows to Add

### Critical journeys

#### Authentication / onboarding
- [ ] Registration flow end-to-end
- [ ] Email verification flow end-to-end
- [ ] Invalid / expired verification code path

#### Riot account linking
- [ ] Link Riot account from UI
- [ ] Duplicate account / invalid Riot ID negative path
- [ ] Sync status feedback shown to the user

#### Match history journey
- [ ] Navigate from overview to matches
- [ ] Select a match row
- [ ] Open and validate match details
- [ ] Narrative/error fallback handling

#### Session and auth resilience
- [ ] Session expiry redirect behavior
- [ ] Unauthenticated access to protected pages
- [ ] API failure / dashboard error-state handling

#### Feedback flow
- [ ] Submit feedback successfully
- [ ] Invalid submission / rate-limited submission path

---

## 4) Test Quality Rules to Follow

### Backend
- [ ] Prefer `FluentAssertions` over raw `Assert.*`
- [ ] Use `TestWebApplicationFactory` for endpoint integration tests
- [ ] No `Thread.Sleep` or brittle time-based waits
- [ ] Use `[Theory]` + `[InlineData]` for parameterized edge cases

### Frontend
- [ ] Prefer `data-testid` selectors over CSS-class selectors where practical
- [ ] Use `vi.mock()` for external dependencies like Chart.js and API services
- [ ] Use `setupPinia()` from `client/test/helpers/testUtils.js` for store tests
- [ ] Avoid snapshot tests; use explicit assertions instead

---

## 5) Suggested Implementation Order

### Phase 1 — immediate
1. `LoginSyncServiceTests.cs`
2. `RiotTimelineMapperTests.cs`
3. `RegisterEndpointTests.cs`
4. `LogoutEndpointTests.cs`
5. `MatchActivityEndpointTests.cs`

### Phase 2 — high-value app coverage
6. Trend endpoint missing 401/403/404 cases
7. Match history Vue component specs
8. `uiStore.spec.js`
9. `feedbackApi.spec.js`
10. Match history Playwright flow

### Phase 3 — cleanup and confidence
11. `SeasonHelperTests.cs`
12. `RiotApiClientTests.cs`
13. Remaining service/util specs
14. Registration + verification E2E
15. Session-expiry and negative-path E2E

---

## Definition of Done

A backlog item is complete when:
- [ ] The new tests are added in the correct folder
- [ ] The tests follow repo conventions and helpers
- [ ] Relevant success + failure states are covered
- [ ] The test suite passes locally
- [ ] The new tests improve confidence in real behavior, not just mocks

---

## Recommended First PR

If we want the **highest impact** from the next test PR, it should include:

- `LoginSyncServiceTests.cs`
- `RiotTimelineMapperTests.cs`
- `RegisterEndpointTests.cs`
- `LogoutEndpointTests.cs`
- `MatchActivityEndpointTests.cs`

This gives the best mix of **security**, **data-ingestion confidence**, and **coverage of currently untested backend behavior**.
