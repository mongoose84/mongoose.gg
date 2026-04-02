# Task 1: Fix Layer Violation for IDeathPositionsRepository

## Priority
High

## Effort
Small

## Branch
`refactor/move-death-positions-interface`

## Problem
`IDeathPositionsRepository` lives in `Application/Interfaces/` instead of `Core/Interfaces/`. Its return type references `Application.DTOs.Solo.DeathPositionsDto`, coupling the contract to the Application layer.

## Changes
- [ ] Create a Core query model (e.g., `Core/QueryModels/DeathPositionQueryModels.cs`) with the response shape
- [ ] Move `IDeathPositionsRepository` from `Application/Interfaces/` to `Core/Interfaces/`
- [ ] Update the interface to return the Core query model instead of the DTO
- [ ] Update `DeathPositionsRepository` (Infrastructure) to return the Core query model
- [ ] Update `DeathPositionsEndpoint` (Application) to map the Core query model to the DTO
- [ ] Update namespaces in existing tests

## Validation
- [ ] All existing tests pass
- [ ] No endpoint behavior changes
