# Task 10: Extract useSoloDashboardData Composable

## Priority
Low

## Effort
Small

## Branch
`refactor/solo-dashboard-composable`

## Problem
`SoloStatsPage.vue` contains repetitive fetch and expand-handler logic for multiple charts and deep analysis panels.

## Changes
- [ ] Create `composables/useSoloDashboardData.js` to encapsulate dashboard data fetching and reactive state
- [ ] Move expand/collapse handlers and fetch orchestration into the composable
- [ ] Refactor `SoloStatsPage.vue` to consume the composable while keeping the template behavior unchanged

## Validation
- [ ] `SoloStatsPage.spec.js` passes
- [ ] Manual smoke test of filter changes and chart expand/collapse passes
