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
- [Testing Instructions](../instructions/testing.instructions.md) — mandatory patterns
- [Architecture Spec](../specs/architecture.spec.md) — all endpoints that need tests

## Step 2: Backend Audit

### Endpoint Coverage
List every endpoint registered in `server/Mongoose.Api/Application/MongooseApiApplication.cs`. For each, check if a matching `*Tests.cs` file exists in `server/Mongoose.Api.Tests/`.

For existing test files, verify the mandatory four cases:
- [ ] Happy path (200 OK with authenticated user)
- [ ] 401 Unauthorized (unauthenticated request)
- [ ] 403 Forbidden (accessing another user's data)
- [ ] 404 Not Found (no linked Riot account)

### Service/Mapper Coverage
Check `server/Infrastructure/` and `server/Application/Services/` for classes with logic that lack corresponding test files. Priority targets from the test strategy:
- LoginSyncService
- RiotTimelineMapper
- SeasonHelper

### Test Quality
- [ ] All tests use `FluentAssertions` (not raw `Assert`)
- [ ] Integration tests use `TestWebApplicationFactory`
- [ ] No `Thread.Sleep` or timing-dependent assertions
- [ ] `[Theory]` + `[InlineData]` for parameterized edge cases where appropriate

## Step 3: Frontend Audit

### Component Coverage
List all `.vue` files in `client/src/components/` and `client/src/views/`. For each component with logic (props, emits, computed, methods), check if a matching `.spec.js` file exists in `client/test/unit/`.

For existing test files, verify state coverage:
- [ ] Renders with data
- [ ] Empty state
- [ ] Loading state
- [ ] Error state
- [ ] Key user interactions (clicks, inputs)

### Composable/Store/Utility Coverage
Check for missing test files:
- `client/src/composables/` → `client/test/unit/`
- `client/src/stores/` → `client/test/unit/`
- `client/src/utils/` → `client/test/unit/`

### Test Quality
- [ ] Uses `data-testid` selectors (not CSS classes or tag names)
- [ ] External deps mocked (`vi.mock` for Chart.js, HeadlessUI, API services)
- [ ] Store tests use `setupPinia()` from `test/helpers/testUtils.js`
- [ ] No snapshot tests (prefer explicit assertions)

## Step 4: E2E Audit

List critical user flows from the test strategy. Check which have Playwright specs in `client/e2e/`:
- [ ] Login → Overview dashboard
- [ ] Solo dashboard navigation and filtering
- [ ] Match history browsing
- [ ] Riot account linking

## Output Format

```markdown
## Test Audit Results

**Date**: [date]
**Backend endpoints**: [covered/total]
**Frontend components**: [covered/total]
**E2E flows**: [covered/total]

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
