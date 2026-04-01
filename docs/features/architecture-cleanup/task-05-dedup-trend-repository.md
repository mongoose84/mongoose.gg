# Task 5: Extract Shared Trend Downsample Pattern

## Priority
Medium

## Effort
Medium

## Branch
`refactor/trend-repository-dedup`

## Problem
`TrendRepository.cs` has six trend methods that duplicate limit and downsample logic.

## Changes
- [ ] Extract a generic `DownsampleTrendData<T>` helper method that encapsulates: limit check -> take last N, or downsample if > 100 points, always include last point
- [ ] Refactor each trend method to use the shared helper
- [ ] Consider extracting shared query preamble logic (validate queue, resolve time range, build filters)

## Validation
- [ ] All trend endpoint tests pass
- [ ] Response data remains identical to current behavior
