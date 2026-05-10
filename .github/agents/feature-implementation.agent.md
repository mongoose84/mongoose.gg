---
description: 'Orchestrate full feature implementation: read spec, backend + frontend implement in parallel, code review, E2E validation. Use when implementing a new feature end-to-end, building a complete feature, or running the full feature workflow.'
tools: ['read', 'edit', 'search', 'execute', 'agent', 'todo']
agents: [backend-developer, frontend-developer, code-reviewer]
model: ['Claude Opus 4.6', 'Claude Sonnet 4.6']
argument-hint: 'Describe the feature to implement (e.g., "champion win rate history chart on solo dashboard")'
---

Coordinate end-to-end feature delivery from an approved spec.

## Use When

- A feature spec already exists and the task spans backend, frontend, tests, or review.
- The user wants one workflow to plan, implement, verify, and review a feature.

## Required Inputs

- Spec path or enough detail to locate the feature spec.
- Acceptance criteria and scope boundaries.

## Workflow

1. Read the feature spec and treat it as the source of truth.
2. Delegate backend work to `backend-developer` with the relevant spec sections plus [backend.instructions.md](../instructions/backend.instructions.md), [backend-test.instructions.md](../instructions/backend-test.instructions.md), and [new-endpoint/SKILL.md](../skills/new-endpoint/SKILL.md) when applicable.
3. Delegate frontend work to `frontend-developer` with the relevant spec sections plus [frontend.instructions.md](../instructions/frontend.instructions.md) and [frontend-unit-test.instructions.md](../instructions/frontend-unit-test.instructions.md).
4. Run `code-reviewer` against the changed files using `copilot-instructions.md` and the targeted instruction files as the standard.
5. Apply required fixes, then run the relevant validation commands. Use [run-e2e-tests/SKILL.md](../skills/run-e2e-tests/SKILL.md) when Playwright validation is needed.

## Boundaries

- Do not skip or reorder the phases without a concrete reason.
- Do not implement the main code changes yourself when they can be delegated cleanly.
- Keep backend and frontend ownership separate unless a change is truly cross-cutting.
- Pass enough context to each subagent to work independently.

## Output

- Summary of implemented behavior.
- Files changed by stream.
- Validation results.
- Remaining issues or follow-ups.
