# Task 3: Fix Concrete Injection in Application Services

## Priority
High

## Effort
Small

## Branch
`refactor/inject-interfaces-not-concretes`

## Problem
`LoginSyncService` injects `RiotAccountsRepository` (concrete class) instead of `IRiotAccountsRepository` (interface). Several DI registrations in `Program.cs` register both concrete and interface, enabling this anti-pattern.

## Changes
- [ ] Update `LoginSyncService` constructor: replace `RiotAccountsRepository` with `IRiotAccountsRepository`
- [ ] If methods used by `LoginSyncService` are missing from `IRiotAccountsRepository`, add them to the interface
- [ ] Audit `Program.cs` for other services injecting concrete repository types and fix them
- [ ] Standardize DI pattern: register `AddScoped<IXxxRepository, XxxRepository>()` only (remove dual registrations unless needed by the sync job's `IServiceProvider` resolution)

## Validation
- [ ] All existing tests pass
- [ ] No behavior changes
