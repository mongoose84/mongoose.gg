# Global Repository Instructions

## Project Overview
Mongoose.gg is a League of Legends performance analytics platform helping solo players, duos, and full teams understand their gameplay through rich match analytics, timeline-derived metrics, and AI-powered goal recommendations.

**Key Features**: Match history sync, solo/duo/team dashboards, LP/winrate trend charts, champion matchups, real-time champion select support, AI goal recommendations, match narratives.

## Technology Stack

- **Backend**: .NET 10 (C#), Minimal API, Domain-Driven Design (DDD) aligned Clean Architecture (Core → Application → Infrastructure)
- **Database**: MySQL 8.0+ with MySqlConnector — raw SQL, no ORM, parameterized queries only
- **Frontend**: Vue 3 (Composition API) + Vite + Pinia + Tailwind CSS + Chart.js
- **Auth**: Cookie-based sessions (HttpOnly, Secure, SameSite=Strict)
- **External**: Riot Games API v5 with custom rate limiting
- **Testing**: xUnit (backend), Vitest + Vue Test Utils (frontend), Playwright (E2E)

## Purpose Of This File

Keep this file short and always-on. It should define repo-wide invariants, point to deeper guidance, and help the executing agent choose the right specialist when needed.

Do not duplicate large API contracts, DTO catalogs, schema inventories, or detailed framework rules here. Those belong in specs and targeted instruction files.

## Targeted Guidance

- **C# files** → [backend.instructions.md](instructions/backend.instructions.md) — endpoint pattern, repositories, DTOs, logging, DI
- **Vue/JS/CSS files** → [frontend.instructions.md](instructions/frontend.instructions.md) — components, stores, API layer, styling
- **Backend test files** → [backend-test.instructions.md](instructions/backend-test.instructions.md) — xUnit and integration test patterns for `server/Mongoose.Api.Tests/`
- **Frontend unit test files** → [frontend-unit-test.instructions.md](instructions/frontend-unit-test.instructions.md) — Vitest and Vue Test Utils patterns for `client/test/unit/`
- **E2E test files** → [e2e-test.instructions.md](instructions/e2e-test.instructions.md) — Playwright patterns for `client/e2e/`
- **Server local context** → [server/Mongoose.Api/AGENTS.md](../server/Mongoose.Api/AGENTS.md) — build, run, backend workflow notes
- **Client local context** → [client/AGENTS.md](../client/AGENTS.md) — build, run, frontend workflow notes

## Reference Specs

- [Architecture & API spec](specs/architecture.spec.md) — all endpoints, DTOs, route map
- [Database schema](specs/database-schema.spec.md) — table structure and relationships
- [UI/UX spec](specs/ui-ux.spec.md) — design system, tokens, component inventory
- [Test strategy](specs/test-strategy.spec.md) — testing pyramid, coverage map, patterns
- [Feature template](specs/feature-template.spec.md) — template for new feature specs

## Repo-Wide Rules

### Security (Non-Negotiable)
- **PUUID exposure is bounded** — analytics and data endpoints must not accept raw PUUID as client input. Resolve Riot account identity server-side. Own-account routes scoped to the authenticated user may use PUUID as a sub-resource key. Prefer the opaque account identifier used by the application for multi-account selection.
- **Log sanitization** — all user/external input must be sanitized via `LogSanitizer.Sanitize()` before logging. No exceptions.
- **PII encrypted at rest** — email and username use `IEncryptor` (AES-256). Riot API keys and DB credentials in env vars only.
- **Auth on every data endpoint** — protected data routes must enforce authenticated user ownership and follow the shared auth/error patterns defined in backend instructions and the architecture spec.
- **Parameterized SQL only** — never concatenate user input into queries.

### Architecture
- **Clean Architecture** — dependencies point inward: Infrastructure → Application → Core. Core has zero external dependencies.
- **Domain-Driven Design (DDD)** — the primary design approach for backend modeling. Model business logic in domain entities/value objects, keep ubiquitous language consistent across Core/Application, and respect bounded contexts when introducing new features.
- **SOLID** — apply as a secondary implementation heuristic inside the chosen DDD boundaries and Clean Architecture layers. Use it to improve cohesion, testability, and dependency direction, but do not introduce abstractions that weaken the domain model or optimize for reuse ahead of domain clarity.
- **UTC everywhere** — all `DateTime` values must be UTC. Use `DateTime.UtcNow` and `DateTimeKind.Utc`.

### Delivery Expectations
- Follow the targeted instruction file for the language or test type you are editing.
- Treat the spec files as the source of truth for contracts, routes, endpoint patterns, DTO details, schema details, and UX structure.
- Keep changes minimal and aligned with existing patterns before introducing new abstractions.
- Update tests and documentation when behavior or contracts change.

## Agent Routing Hints

- **Backend implementation** → use `backend-developer`
- **Frontend implementation** → use `frontend-developer`
- **Markdown, specs, and planning** → use `architect`
- **Tests** → use `test-writer`
- **Code review** → use `code-reviewer`
- **DevOps, YAML, CI, infra** → use `devops-engineer`
- **UX/UI design work** → use `ux-ui-designer`
- **End-to-end feature delivery across backend, frontend, review, and validation** → use `feature-implementation`

Pick the narrowest specialist that fits the change. Use the broader orchestrator only when the task spans multiple disciplines.

## Development Workflow

### Branching
- `main` — production. Feature branches: `feature/description`. Bug fixes: `fix/description`.

### Commits
Conventional commits: `feat:`, `fix:`, `refactor:`, `test:`, `docs:`

### Code Review Checklist
- [ ] Tests added/updated
- [ ] Error handling covers edge cases
- [ ] Logging uses `LogSanitizer.Sanitize()` for all user input
- [ ] No hardcoded secrets or PII
- [ ] Follows existing patterns
- [ ] Accessibility met (frontend)
