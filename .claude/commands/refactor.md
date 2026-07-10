---
description: Code refactoring workflow with safety checks. Use when restructuring code for clarity or maintainability without changing behavior.
argument-hint: describe the code to refactor and the goal (readability, dedup, etc.)
model: sonnet
---

# Code Refactoring Workflow

Refactor target: $ARGUMENTS

## Use When

- The user asks to restructure code without changing behavior.
- The goal is readability, maintainability, or duplication reduction.

## Context To Load

1. The target code.
2. All usages of the target surface.
3. Related test files.
4. `.github/specs/architecture.spec.md` only when conventions or boundaries matter.

## Workflow

1. Map dependencies and risks before editing.
2. Establish a baseline with existing tests where possible.
3. Apply small, incremental refactors.
4. Re-run focused validation after each meaningful step.
5. Keep behavior unchanged unless the user explicitly requests otherwise.

## Output

1. Refactoring goal and approach.
2. Files changed.
3. Validation performed.
4. Any behavior or compatibility risks.
