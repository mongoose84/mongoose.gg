---
agent: agent
model: Claude Sonnet 4.6
tools: ['file-search', 'semantic-search', 'codebase', 'problems', 'testFailure', 'editFiles', 'runTests']
description: 'Systematic bug investigation and fix workflow. Use when debugging a regression, reproducing a failing behavior, or repairing a concrete bug.'
---
# Bug Fix Workflow

## Use When

- The user reports a concrete bug, failing behavior, or regression.
- A failing test or reproducible issue needs investigation and repair.

## Context To Load

1. The bug report or failing behavior.
2. The owning implementation and one nearby example.
3. Relevant test failures or recent changes when available.

## Workflow

1. Reproduce the bug or identify the failing check.
2. Trace the owning code path and isolate the root cause.
3. Add or update the narrowest test that proves the defect when practical.
4. Implement the minimal fix.
5. Run the narrowest relevant validation, then widen only if needed.

## Output

1. Root cause.
2. Fix approach.
3. Files changed.
4. Validation performed.
5. Remaining risks or follow-ups.
