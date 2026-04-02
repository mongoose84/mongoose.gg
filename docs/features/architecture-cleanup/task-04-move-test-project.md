# Task 4: Move Test Project to Sibling Directory

## Priority
High

## Effort
Medium

## Branch
`refactor/move-test-project`

## Problem
`Mongoose.Api.Tests/` is a subfolder of the main project `server/`. The `.csproj` needs `<Compile Remove="Mongoose.Api.Tests/**" />` to exclude it. This is non-standard and confusing.

## Changes
- [x] Create `tests/Mongoose.Api.Tests/` at repo root
- [x] Move all test files from `server/Mongoose.Api.Tests/` to the new location
- [x] Update `Mongoose.Api.Tests.csproj` `<ProjectReference>` path
- [x] Update `mongoose.sln` project reference paths
- [x] Remove `<Compile Remove="Mongoose.Api.Tests/**" />` and related remove lines from `Mongoose.Api.csproj`
- [x] Verify `dotnet test` works from both solution root and test project directory
- [x] Update CI scripts or `AGENTS.md` references to the old path

## Validation
- [x] `dotnet build` succeeds
- [x] `dotnet test` succeeds (pre-existing DB-dependent failures unrelated to move)
- [ ] CI pipeline passes
