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
- [x] Create `services/soloApi.js` with `getSoloDashboard`, `getRadarChart`, and `getDeathPositions`
- [x] Create `services/matchesApi.js` with `getMatchList` and `getMatchDetails`
- [x] Create `services/trendsApi.js` with all trend calls
- [x] Keep `authApi.js` for auth-only calls (login, register, verify, riot accounts, users/me)
- [x] Update imports in views and components
- [x] Update related test files

## Validation
- [x] Vitest unit tests pass
- [x] Manual smoke test for solo dashboard and matches page passes
