---
description: Generate a feature specification from a description. Use for planning features, writing specs, or creating implementation plans.
argument-hint: describe the feature to spec out
model: sonnet
---

# Feature Specification Generator

Feature to spec: $ARGUMENTS

## Use When

- The user wants a new feature spec or implementation plan starting from a fresh description.
- The task is planning, not direct code delivery.
- If the feature was already designed in the current conversation, use `/to-spec` instead — it synthesizes without re-interviewing.

## Context To Load

1. `.github/specs/feature-template.spec.md`
2. `.github/specs/architecture.spec.md` when routes or contracts matter
3. `.github/specs/database-schema.spec.md` when data shape matters
4. `.github/specs/ui-ux.spec.md` when user-facing behavior matters
5. Similar existing implementations in the codebase

## Workflow

1. Translate the user request into problem statement, scope, and acceptance criteria.
2. Produce a complete spec following the feature template.
3. Cover backend changes, frontend changes, API contracts, UI/UX requirements, testing strategy, and risks.
4. Save the result to `.github/specs/features/{feature-name}.spec.md`.

## Output

- Complete feature spec in the template structure.
- Clear in-scope and out-of-scope boundaries.
- Risks, dependencies, and testing expectations.

For end-to-end implementation after the spec exists, delegate to the `fullstack-developer` subagent, or run the `feature-implementation` skill for the full multi-agent delivery workflow.
