# Feature: Architecture Cleanup & DDD Alignment

This feature has been split into one file per task for independent execution.

## Task Files

1. [Task 1: Fix Layer Violation for IDeathPositionsRepository](docs/features/architecture-cleanup/task-01-move-death-positions-interface.md)
2. [Task 2: Fix Layer Violation for IRiotApiClient](docs/features/architecture-cleanup/task-02-move-riot-api-interface.md)
3. [Task 3: Fix Concrete Injection in Application Services](docs/features/architecture-cleanup/task-03-inject-interfaces-not-concretes.md)
4. [Task 4: Move Test Project to Sibling Directory](docs/features/architecture-cleanup/task-04-move-test-project.md)
5. [Task 5: Extract Shared Trend Downsample Pattern](docs/features/architecture-cleanup/task-05-dedup-trend-repository.md)
6. [Task 6: Split MatchesRepository](docs/features/architecture-cleanup/task-06-split-matches-repository.md)
7. [Task 7: Extract MatchDataPersistenceService from MatchHistorySyncJob](docs/features/architecture-cleanup/task-07-extract-match-data-persistence-service.md)
8. [Task 8: Split authApi.js into Domain-Specific API Modules](docs/features/architecture-cleanup/task-08-split-auth-api-modules.md)
9. [Task 9: Organize Frontend Unit Tests into Subfolders](docs/features/architecture-cleanup/task-09-organize-frontend-unit-tests.md)
10. [Task 10: Extract useSoloDashboardData Composable](docs/features/architecture-cleanup/task-10-extract-solo-dashboard-composable.md)
11. [Task 11: Group Core Interfaces and Entities by Domain](docs/features/architecture-cleanup/task-11-group-core-by-domain.md)

## Suggested Execution Order

1. Tasks 1, 2, 3
2. Task 4
3. Tasks 5, 6, 7
4. Tasks 8, 9, 10
5. Task 11
