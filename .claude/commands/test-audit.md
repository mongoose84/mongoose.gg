---
description: Audit test coverage gaps against the test strategy spec and project conventions.
model: sonnet
---

# Test Audit

Audit the repo for missing tests, coverage gaps, and deviations from the current testing conventions.

## Use When

- The user asks where coverage is missing.
- A test planning or quality pass is needed before broader work.

## Context To Load

- `.github/specs/test-strategy.spec.md`
- `server/Mongoose.Api.Tests/CLAUDE.md`
- `client/test/unit/CLAUDE.md`
- `client/e2e/CLAUDE.md`
- `.github/specs/architecture.spec.md` when endpoint inventory matters

Use the current workspace as the source of truth and call out spec drift explicitly.

## Workflow

1. Audit backend endpoint, service, and job coverage.
2. Audit frontend component, composable, store, service, router, utility, and bootstrap coverage.
3. Audit Playwright coverage for critical user journeys and missing negative paths.
4. Note both missing tests and quality issues in existing tests.

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
