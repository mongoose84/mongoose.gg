# Global Repository Instructions

Mongoose.gg is a League of Legends analytics platform built with .NET 10, MySQL, Vue 3, Pinia, Tailwind, and Playwright.

Keep this file short and always-on. Use it for repo-wide invariants, routing, and deep-reference pointers. Put detailed templates, checklists, and long examples in targeted instructions, skills, prompts, or specs.

## Targeted Guidance

- C# app code → [backend.instructions.md](instructions/backend.instructions.md)
- Vue/JS/CSS app code → [frontend.instructions.md](instructions/frontend.instructions.md)
- Backend tests → [backend-test.instructions.md](instructions/backend-test.instructions.md)
- Frontend unit tests → [frontend-unit-test.instructions.md](instructions/frontend-unit-test.instructions.md)
- E2E tests → [e2e-test.instructions.md](instructions/e2e-test.instructions.md)
- Backend local context → [server/Mongoose.Api/AGENTS.md](../server/Mongoose.Api/AGENTS.md)
- Frontend local context → [client/AGENTS.md](../client/AGENTS.md)

## Deep References

- API routes and contracts → [architecture.spec.md](specs/architecture.spec.md)
- Database structure and SQL shape → [database-schema.spec.md](specs/database-schema.spec.md)
- UI behavior and design tokens → [ui-ux.spec.md](specs/ui-ux.spec.md)
- Test scope and coverage strategy → [test-strategy.spec.md](specs/test-strategy.spec.md)
- Feature spec template → [feature-template.spec.md](specs/feature-template.spec.md)

Load those specs only when the change actually touches contracts, schema, UX behavior, or test strategy.

## Repo-Wide Invariants

- Do not accept raw PUUID as client input for analytics or protected data endpoints; resolve Riot account identity server-side.
- Sanitize all user or external values before logging with `LogSanitizer.Sanitize()`.
- Encrypt PII at rest with `IEncryptor`; keep secrets in environment variables only.
- Protect data endpoints with authenticated ownership checks.
- Use parameterized SQL only.
- Preserve Clean Architecture dependency direction: Infrastructure → Application → Core.
- Keep domain rules in Core and orchestration in Application; apply SOLID inside those boundaries.
- Use UTC for all `DateTime` values.
- Keep changes minimal and aligned with existing patterns.
- Update tests and docs when behavior or contracts change.

## Agent Routing

- Backend implementation → `backend-developer`
- Frontend implementation → `frontend-developer`
- Markdown, specs, and planning → `architect`
- Tests → `test-writer`
- Code review → `code-reviewer`
- DevOps, YAML, and CI → `devops-engineer`
- UX/UI design → `ux-ui-designer`
- End-to-end feature delivery → `feature-implementation`

Pick the narrowest specialist that fits the task.
