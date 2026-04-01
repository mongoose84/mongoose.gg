# Task 2: Fix Layer Violation for IRiotApiClient

## Priority
High

## Effort
Small

## Branch
`refactor/move-riot-api-interface`

## Problem
`IRiotApiClient` lives in `Infrastructure/Riot/`, but Application services (for example `LoginSyncService`) depend on it. This violates the inward dependency rule.

## Changes
- [ ] Move `IRiotApiClient` from `Infrastructure/Riot/` to `Core/Interfaces/`
- [ ] Update all `using` statements across the codebase
- [ ] Verify `LoginSyncService` and `MatchHistorySyncJob` still compile and tests pass

## Validation
- [ ] All existing tests pass
- [ ] No behavior changes
