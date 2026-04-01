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
- [ ] Create `server/tests/Mongoose.Api.Tests/` (or `tests/Mongoose.Api.Tests/` at repo root)
- [ ] Move all test files from `server/Mongoose.Api.Tests/` to the new location
- [ ] Update `Mongoose.Api.Tests.csproj` `<ProjectReference>` path
- [ ] Update `mongoose.sln` project reference paths
- [ ] Remove `<Compile Remove="Mongoose.Api.Tests/**" />` and related remove lines from `Mongoose.Api.csproj`
- [ ] Verify `dotnet test` works from both solution root and test project directory
- [ ] Update CI scripts or `AGENTS.md` references to the old path

## Validation
- [ ] `dotnet build` succeeds
- [ ] `dotnet test` succeeds
- [ ] CI pipeline passes
