# E2E Test Rules

> Scope: `client/e2e/**/*.js`

Load [test-strategy.spec.md](../specs/test-strategy.spec.md) only when changing E2E scope or deciding whether a workflow belongs in Playwright.
Load [architecture.spec.md](../specs/architecture.spec.md) only when auth flow, protected routes, or backend contract details affect the UI path.
Always read the target workflow in `client/src/` and one nearby Playwright spec first.

## Scope

- Cover critical user journeys, not component internals.
- Prefer broad workflow confidence over unit-level detail.
- Add negative-path coverage when auth, redirects, loading, sync, or visible errors matter to the user.

## Playwright Rules

- Use robust user-facing selectors where possible.
- Keep helpers in `client/e2e/helpers/` focused and reusable.
- Keep `global-setup.js` and `global-teardown.js` deterministic.
- Avoid brittle timing assumptions; wait on intended UI or network state.
- Cover the success path first, then add the highest-value failure paths.

## Priority Flows

- Authentication and session handling
- Dashboard access and filtering
- Match history and details flows
- Riot account linking and sync flows
- User-visible error or redirect behavior