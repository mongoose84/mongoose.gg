---
applyTo: "client/e2e/**/*.js"
description: "Playwright end-to-end test guidance for flows under client/e2e/. Use when writing or editing browser-flow tests, Playwright helpers, global setup, or teardown for the client E2E suite."
---
# E2E Test Guidelines

## Context Loading
Review these BEFORE writing end-to-end tests:
- [Test Strategy Spec](../specs/test-strategy.spec.md) — critical user journeys and E2E scope
- [Architecture Spec](../specs/architecture.spec.md) — protected routes, auth flows, and endpoint expectations that affect the UI
- The target view or workflow in `client/src/` — understand the intended user path and failure states
- Existing Playwright specs in `client/e2e/` — match structure, fixtures, and selector strategy

## E2E Test Scope

- Cover critical user journeys, not every UI detail
- Prefer broad workflow confidence over duplicating unit-test-level assertions
- Add negative-path coverage when auth, loading, sync, or error handling is user-visible

## Primary Flows

Prioritize coverage for these flows when applicable:

1. Authentication and session handling
2. Overview dashboard access
3. Solo dashboard navigation and filtering
4. Match history and details flows
5. Riot account linking and sync flows
6. User-visible error or redirect behavior

## Playwright Rules

- Use robust, user-facing selectors where possible
- Keep helpers in `client/e2e/helpers/` focused and reusable
- Keep `global-setup.js` and `global-teardown.js` deterministic
- Avoid brittle timing assumptions; wait on UI state or network effects intentionally
- Cover the success path first, then add high-value failure-path scenarios

## Quality Checklist

- [ ] The test exercises a real user journey with meaningful assertions
- [ ] Assertions focus on visible state, navigation, and guarded behavior
- [ ] The spec avoids unnecessary duplication of component-level assertions
- [ ] Helpers and setup code remain reusable and deterministic
- [ ] New critical workflows or regressions are covered by Playwright where appropriate