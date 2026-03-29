---
agent: agent
model: Claude Sonnet 4.6
tools: ['read', 'search']
description: 'Generate a feature specification from a description. Use for planning features, writing specs, or creating implementation plans.'
---
# Feature Specification Generator

Generate a complete feature specification for the described feature.

## Context Loading
1. Review the [feature template](../specs/feature-template.spec.md) for the required structure
2. Review the [architecture spec](../specs/architecture.spec.md) for existing endpoints and patterns
3. Review the [database schema](../specs/database-schema.spec.md) for available tables
4. Review the [UI/UX spec](../specs/ui-ux.spec.md) for design system and component inventory
5. Search the codebase for similar existing implementations

## Output
Produce a complete spec following the feature template structure, covering:
- Problem statement and user stories
- Backend changes (endpoints, DTOs, repositories, SQL)
- Frontend changes (components, stores, API services)
- API contracts (request/response JSON shapes)
- UI/UX requirements with layout descriptions
- Testing strategy
- Risks and dependencies

Save the spec to `.github/specs/features/{feature-name}.spec.md`.

**Tip**: For full end-to-end implementation (spec → code → review → test), use the `@feature-implementation` agent instead.
- [ ] Add usage examples

## Structured Output Requirements
Generate implementation with:
1. Feature code in appropriate module
2. Comprehensive unit tests
3. Integration tests for API endpoints
4. Documentation updates

## Human Validation Gate
🚨 **STOP**: Review implementation plan before proceeding to code generation.
Confirm: Architecture alignment, test strategy, and breaking change impact.
