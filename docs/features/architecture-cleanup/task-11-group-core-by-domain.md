# Task 11: Group Core Interfaces and Entities by Domain

## Priority
Low

## Effort
Medium

## Branch
`refactor/core-domain-grouping`

## Problem
`Core/Interfaces/` and `Core/Entities/` are currently flat, which reduces navigability as the codebase grows.

## Changes
- [ ] Group `Core/Interfaces/` into domain subfolders (`Identity`, `Matches`, `Analytics`, `Teams`, `Shared`)
- [ ] Apply the same grouping approach to `Core/Entities/`
- [ ] Update all project-wide `using` statements and namespace paths
- [ ] Perform this task last to minimize churn and merge conflicts

## Validation
- [ ] `dotnet build` passes
- [ ] `dotnet test` passes
- [ ] No behavior changes
