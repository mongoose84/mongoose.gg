# Client Unit Tests — Local Context

> Vitest + Vue Test Utils unit tests under `client/test/unit/`.
> For repo-wide invariants see [CLAUDE.md](../../../CLAUDE.md); for build/run/test commands see [client/CLAUDE.md](../../CLAUDE.md).

Load [test-strategy.spec.md](../../../.github/specs/test-strategy.spec.md) only when changing frontend test scope or infrastructure.
Load [architecture.spec.md](../../../.github/specs/architecture.spec.md) only when the test depends on API or route contracts.
Load [ui-ux.spec.md](../../../.github/specs/ui-ux.spec.md) only when expected states or accessibility behavior are unclear.
Always read the source file under test and one nearby unit test first.

## Test Stack

- Use Vitest with Vue Test Utils.
- Reuse helpers in `client/test/helpers/testUtils.js` and nearby test utilities.
- Match local naming and mount-helper patterns before inventing new ones.

## Coverage Rules

- For components and views with logic, cover rendering with data, empty state, loading state, error state, and key interactions when applicable.
- For composables, stores, services, router logic, and utilities, test public behavior, derived state, and side effects.
- Add or update coverage when frontend logic changes.

## Test Quality

- Prefer `data-testid` selectors and explicit assertions over CSS selectors or snapshots.
- Mock external dependencies only as far as needed.
- Use local mount helpers and `setupPinia()` when store context is required.
- Await Vue updates after async state changes or prop changes.
- Test user-visible behavior and public outputs, not implementation details.
