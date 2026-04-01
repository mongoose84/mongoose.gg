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
- [ ] Extract `ComputeTrendBadge` and `ComputeTrendBadgeSummary` logic into a shared Application service or helper (business logic should not live in data access)
- [ ] Consider splitting implementation into focused repository files, or at minimum extract private raw-data mapper methods into a `MatchDataMapper` helper class
- [ ] Keep `IMatchesRepository` as a single interface

## Validation
- [ ] Match endpoint tests pass
- [ ] Match list tests pass
- [ ] No behavior changes
