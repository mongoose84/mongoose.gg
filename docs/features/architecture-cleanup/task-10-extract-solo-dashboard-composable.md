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
- [x] Create `composables/useSoloDashboardData.js` to encapsulate dashboard data fetching and reactive state
- [x] Move expand/collapse handlers and fetch orchestration into the composable
- [x] Refactor `SoloStatsPage.vue` to consume the composable while keeping the template behavior unchanged

## Validation
- [x] `SoloStatsPage.spec.js` passes
- [x] Manual smoke test of filter changes and chart expand/collapse passes
