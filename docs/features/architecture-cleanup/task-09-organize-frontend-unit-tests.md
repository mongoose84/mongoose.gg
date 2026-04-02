# Task 9: Organize Frontend Unit Tests into Subfolders

## Priority
Low

## Effort
Small

## Branch
`refactor/organize-frontend-tests`

## Problem
There are 60+ spec files in a flat `client/test/unit/` directory, which makes test discovery and maintenance harder.

## Changes
- [ ] Create subdirectories mirroring `client/src/` (`components`, `composables`, `stores`, `services`, `utils`, `views`)
- [ ] Move each spec file to the matching subfolder
- [ ] Update relative import paths in moved spec files
- [ ] Verify Vitest glob patterns still include all tests

## Validation
- [ ] `npm run test:unit` discovers all tests
- [ ] `npm run test:unit` passes
