# Task 6: Split MatchesRepository

## Priority
Medium

## Effort
Medium

## Branch
`refactor/split-matches-repository`

## Problem
`MatchesRepository.cs` handles different query responsibilities (match list summaries, full match details, and match CRUD) and also includes `ComputeTrendBadge` business logic.

## Changes
- [x] Extract `ComputeTrendBadge` and `ComputeTrendBadgeSummary` logic into a shared Application service or helper (business logic should not live in data access)
- [x] Consider splitting implementation into focused repository files, or at minimum extract private raw-data mapper methods into a `MatchDataMapper` helper class
- [x] Keep `IMatchesRepository` as a single interface

## Validation
- [x] Match endpoint tests pass
- [x] Match list tests pass
- [x] No behavior changes
