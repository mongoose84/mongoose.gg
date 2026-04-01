# Task 8: Split authApi.js into Domain-Specific API Modules

## Priority
Medium

## Effort
Medium

## Branch
`refactor/split-frontend-api-modules`

## Problem
`authApi.js` contains API calls for auth, solo dashboard, matches, trends, radar chart, and death positions, violating single responsibility.

## Changes
- [ ] Create `services/soloApi.js` with `getSoloDashboard`, `getRadarChart`, and `getDeathPositions`
- [ ] Create `services/matchesApi.js` with `getMatchList` and `getMatchDetails`
- [ ] Create `services/trendsApi.js` with all trend calls
- [ ] Keep `authApi.js` for auth-only calls (login, register, verify, riot accounts, users/me)
- [ ] Update imports in views and components
- [ ] Update related test files

## Validation
- [ ] Vitest unit tests pass
- [ ] Manual smoke test for solo dashboard and matches page passes
