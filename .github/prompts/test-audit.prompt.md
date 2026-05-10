---
agent: agent
model: Claude Sonnet 4.6
description: 'Audit test coverage gaps against the test strategy spec and project conventions'
---
# Test Audit

Audit the codebase for missing tests, coverage gaps, and deviations from the test strategy.

## Step 1: Load References

Read these before auditing:
- [Test Strategy Spec](../specs/test-strategy.spec.md) — coverage map, gaps, pyramid
- [Backend Test Instructions](../instructions/backend-test.instructions.md) — backend test patterns and helpers
- [Frontend Unit Test Instructions](../instructions/frontend-unit-test.instructions.md) — Vitest and Vue Test Utils patterns
- [E2E Test Instructions](../instructions/e2e-test.instructions.md) — Playwright scope and quality rules
- [Architecture Spec](../specs/architecture.spec.md) — all endpoints that need tests

Use the current workspace as the source of truth. If the spec is stale or out of sync with the repo, call that out explicitly instead of repeating outdated counts.

## Step 2: Backend Audit

### Endpoint Coverage
List every endpoint registered in `server/Mongoose.Api/Application/MongooseApiApplication.cs`. For each, check if a matching `*Tests.cs` file exists in `server/Mongoose.Api.Tests/`.

For existing test files, verify the mandatory four cases:
- [ ] Happy path (200 OK with authenticated user)
- [ ] 401 Unauthorized (unauthenticated request)
- [ ] 403 Forbidden (accessing another user's data)
- [ ] 404 Not Found (no linked Riot account)

### Service/Mapper/Job/External Integration Coverage
Check `server/Mongoose.Api/Infrastructure/`, `server/Mongoose.Api/Application/Services/`, and any background job / Riot integration code for classes with logic that lack corresponding test files or have only shallow coverage. Priority targets from the test strategy:
- LoginSyncService
- RiotTimelineMapper
- SeasonHelper
- MatchHistorySyncJob / MatchCleanupJob depth
- RiotApiClient mocking / integration coverage

### Test Quality
- [ ] All tests use `FluentAssertions` (not raw `Assert`)
- [ ] Integration tests use `TestWebApplicationFactory`
- [ ] No `Thread.Sleep` or timing-dependent assertions
- [ ] `[Theory]` + `[InlineData]` for parameterized edge cases where appropriate

## Step 3: Frontend Audit

### Component Coverage
List all `.vue` files in `client/src/components/`, `client/src/layouts/`, `client/src/views/`, and the root `client/src/App.vue`. For each component/view/layout with logic (props, emits, computed, methods, watchers, or async lifecycle hooks), check if a matching `.spec.js` or `.test.js` file exists in the appropriate folder under `client/test/unit/`.

For existing test files, verify state coverage:
- [ ] Renders with data
- [ ] Empty state
- [ ] Loading state
- [ ] Error state
- [ ] Key user interactions (clicks, inputs)

### Composable/Store/Utility/Service/Router/Bootstrap Coverage
Check for missing test files:
- `client/src/composables/` → `client/test/unit/composables/`
- `client/src/stores/` → `client/test/unit/stores/`
- `client/src/utils/` → `client/test/unit/utils/`
- `client/src/services/` → `client/test/unit/services/`
- `client/src/router/` → `client/test/unit/router/`
- `client/src/plugins/` and `client/src/main.js` → relevant unit/bootstrap coverage in `client/test/unit/`

### Test Quality
- [ ] Uses `data-testid` selectors (not CSS classes or tag names)
- [ ] External deps mocked (`vi.mock` for Chart.js, HeadlessUI, API services)
- [ ] Store tests use `setupPinia()` from `test/helpers/testUtils.js`
- [ ] No snapshot tests (prefer explicit assertions)

## Step 4: E2E Audit

List critical user flows from the test strategy. Check which have Playwright specs in `client/e2e/` and note missing negative-path coverage:
- [ ] Login / registration / verification flow
- [ ] Overview dashboard access
- [ ] Solo dashboard navigation and filtering
- [ ] Match history browsing and details
- [ ] Riot account linking
- [ ] Unauthenticated redirect / session-expiry handling
- [ ] Error-state / negative-path coverage

## Output Format

```markdown
## Test Audit Results

**Date**: [date]
**Backend endpoints**: [covered/total]
**Frontend views/components/layouts**: [covered/total]
**Frontend unit modules** (composables + stores + utils + services + router): [covered/total]
**E2E flows**: [covered/total]
**Spec drift noted**: [yes/no — short note if outdated]

### Missing Tests (by priority)

#### Critical — No tests at all
| File | Type | Reason |
|------|------|--------|

#### Important — Tests exist but incomplete
| Test File | Missing Cases |
|-----------|---------------|

#### Low — Nice to have
| File | Type |
|------|------|

### Quality Issues
- [ ] [File] Description of pattern violation

### Recommendations
1. Highest-impact test to add next
2. Second priority
3. Third priority
```

Prioritize findings by the risk levels defined in the test strategy spec (Critical > High > Standard).
