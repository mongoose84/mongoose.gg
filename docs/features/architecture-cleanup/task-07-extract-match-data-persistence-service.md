# Task 7: Extract MatchDataPersistenceService from MatchHistorySyncJob

## Priority
Medium

## Effort
Medium

## Branch
`refactor/extract-match-persistence`

## Problem
`MatchHistorySyncJob.cs` combines job orchestration with a large `PersistMatchDataAsync` method that handles entity mapping, participant insertion, metrics enrichment, and season management.

## Changes
- [x] Extract `PersistMatchDataAsync` into a new `MatchDataPersistenceService` in `Application/Services/` or `Infrastructure/Services/`
- [x] Inject the new service into `MatchHistorySyncJob`
- [x] Ensure the new service is independently testable

## Validation
- [x] `MatchHistorySyncJobTests` pass
- [x] `MatchesRepositoryIntegrationTests` pass
- [x] No behavior changes
