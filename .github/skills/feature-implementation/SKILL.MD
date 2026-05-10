---
name: feature-implementation
description: "Use when implementing a feature from a completed spec in this .NET 10 Minimal API + Vue 3 project. Orchestrates architect, backend-developer, frontend-developer, test-writer, and code-reviewer with quality gates and fix loops."
---

# Feature Implementation Skill

## Purpose
Use this skill after a feature has been documented and approved.
It coordinates subagents in a deterministic sequence so work is planned, implemented, verified, and reviewed before completion.

This skill is the canonical home for long-form feature workflow guidance, delivery gates, and fix-loop policy. Keep always-on and file-scoped instructions limited to routing and constraints.

## Project Context
- Backend: .NET 10 Minimal API (C#), Clean Architecture + DDD (Core → Application → Infrastructure)
- Frontend: Vue 3 (Composition API) + Vite + Pinia + Tailwind CSS
- Data: MySQL 8.0+ with raw parameterized SQL (no ORM)
- Backend tests: `server/Mongoose.Api.Tests/` (xUnit, `TestWebApplicationFactory`)
- Frontend tests: `client/test/unit/` (Vitest + Vue Test Utils)
- E2E tests: `client/e2e/` (Playwright)

## Key References
- [Architecture spec](../../specs/architecture.spec.md) — endpoints, DTOs, route map
- [Database schema](../../specs/database-schema.spec.md) — table structure and relationships
- [UI/UX spec](../../specs/ui-ux.spec.md) — design system, tokens, component inventory
- [Test strategy](../../specs/test-strategy.spec.md) — testing pyramid, coverage map
- [Feature template](../../specs/feature-template.spec.md) — template for new feature specs
- [new-endpoint skill](../new-endpoint/SKILL.md) — endpoint scaffolding pattern
- [run-all-tests skill](../run-all-tests/SKILL.md) — full test suite execution

## Inputs Required
- Feature spec path under `.github/specs/features/`
- Definition of done (acceptance criteria)
- Scope constraints (in scope / out of scope)

## Output
- Implemented feature aligned with spec
- Updated tests covering changed behavior
- Review findings addressed or explicitly tracked
- Final implementation summary with changed files

## Recommended Orchestration Model
You do not need a separate orchestrator agent to start.
This skill itself should orchestrate the workflow and call specialized subagents.

Create a dedicated orchestrator agent only if one or more of these are true:
- You want strict tool restrictions per phase.
- You want reusable delivery governance across many repositories.
- You need compliance gates beyond normal review and testing.

## Agent Sequence

### Step 1: Architecture Breakdown
Agent: architect

Goals:
- Break feature into backend, frontend, data, and test work items.
- Identify risks, dependencies, migration needs, and non-functional constraints.
- Produce a phased implementation plan with acceptance checks.
- Ensure DDD alignment: bounded contexts, ubiquitous language, domain model placement.

Required deliverable:
- Task breakdown with ordered milestones and risk notes.

Contract freeze output (required before implementation):
- Interface contracts between streams (endpoint routes, request/response DTOs, shared data shapes).
- Validation and error-state contract.
- Work stream definitions: which agent handles each stream, and which files it owns.

### Step 2: Implementation Streams

The architect defines one or more **independent work streams** — each assigned to the most appropriate agent. Streams run sequentially (subagent calls are blocking), but are designed to be independent so they do not conflict.

Common stream configurations:
- **Backend + Frontend** — one `backend-developer` stream, one `frontend-developer` stream.
- **Multiple frontend streams** — e.g., two `frontend-developer` invocations building different dashboard cards.
- **Backend-only** — a single `backend-developer` stream when no UI changes are needed.
- **Any mix** — the architect decides based on the feature shape.

Stream preconditions (enforced for every stream):
- Architect contract freeze is complete.
- File ownership map has no overlap between streams.
- Any shared contracts (API shapes, store interfaces) are documented before streams begin.

#### Backend stream rules (when applicable)
Agent: backend-developer

- Follow [backend.instructions.md](../../instructions/backend.instructions.md), [backend-test.instructions.md](../../instructions/backend-test.instructions.md), and [new-endpoint/SKILL.md](../new-endpoint/SKILL.md) when applicable.

Required deliverable: code changes plus test updates and implementation notes.

#### Frontend stream rules (when applicable)
Agent: frontend-developer

- Follow [frontend.instructions.md](../../instructions/frontend.instructions.md) and [frontend-unit-test.instructions.md](../../instructions/frontend-unit-test.instructions.md).

Required deliverable: code changes plus Vitest unit tests and UX notes.

### Step 3: Sync Gate (Post-Implementation)
Owner: orchestrating skill execution

Goals:
- Reconcile integration points across all streams.
- Confirm contracts still match implementation.
- Resolve any cross-stream conflicts before verification.

Required checks:
- Endpoint routes and DTO shapes match contract.
- Payload and validation behavior match contract.
- Error and empty states are consistent across all streams.

### Step 4: Verification
Agent: test-writer

Goals:
- Verify acceptance criteria against implemented behavior.
- Validate regression risk in touched areas.
- Confirm tests are deterministic and meaningful.
- Use the [run-all-tests skill](../run-all-tests/SKILL.md) to execute the full suite (backend xUnit → frontend Vitest → Playwright E2E).

Required deliverable:
- Verification report with pass/fail criteria and defects.

### Step 5: Code Review
Agent: code-reviewer

Goals:
- Review for correctness, maintainability, security, and test adequacy.
- Use `copilot-instructions.md` and the relevant targeted instruction files as the review standard.
- Prioritize issues by severity and identify required fixes.

Required deliverable:
- Findings list ordered by severity, with file references.

## Fix Loop Policy
If verification or review finds issues:
1. Route each issue to the agent that owns the affected stream (per the file ownership map).
2. Route cross-cutting design issues to architect.
3. If multiple streams are impacted, run fixes sequentially with a mini contract freeze to prevent conflicts.
4. Re-run sync gate checks.
5. Re-run test-writer after fixes.
6. Re-run code-reviewer for final sign-off.

Repeat until:
- Acceptance criteria are met.
- Tests pass (xUnit, Vitest, and Playwright).
- No critical review findings remain.

## Guardrails
- Do not start implementation without a feature spec.
- Keep changes scoped to approved feature boundaries.
- Avoid unrelated refactors unless required for correctness.
- Keep commits/review units small and traceable.
- Follow repo-wide invariants from `copilot-instructions.md` and file-scoped rules from the targeted instruction files.

## Suggested Invocation Prompt
Use this skill when you say:
- Implement feature from spec at [path]
- Run full feature delivery workflow
- Plan, build, verify, and review this feature

## Assumptions To Confirm At Start
- Spec is approved and stable enough to implement.
- Database migration strategy is agreed (if schema changes are needed).
- Expected rollout strategy is known (feature flag, direct release, etc.).