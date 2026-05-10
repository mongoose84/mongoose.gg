---
agent: agent
model: Claude Sonnet 4.6
tools: ['read', 'search']
description: 'Generate a feature specification from a description. Use for planning features, writing specs, or creating implementation plans.'
---
# Feature Specification Generator

## Use When

- The user wants a new feature spec or implementation plan.
- The task is planning, not direct code delivery.

## Context To Load

1. [feature-template.spec.md](../specs/feature-template.spec.md)
2. [architecture.spec.md](../specs/architecture.spec.md) when routes or contracts matter
3. [database-schema.spec.md](../specs/database-schema.spec.md) when data shape matters
4. [ui-ux.spec.md](../specs/ui-ux.spec.md) when user-facing behavior matters
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

For end-to-end implementation after the spec exists, use `@feature-implementation`.
