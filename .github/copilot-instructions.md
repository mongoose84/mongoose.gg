# Global Repository Instructions

## Project Overview
Mongoose.gg is a League of Legends performance analytics platform helping solo players, duos, and full teams understand their gameplay through rich match analytics, timeline-derived metrics, and AI-powered goal recommendations.

**Key Features**: Match history sync, solo/duo/team dashboards, LP/winrate trend charts, champion matchups, real-time champion select support, AI goal recommendations, match narratives.

## Technology Stack

- **Backend**: .NET 10 (C#), Minimal API, Clean Architecture (Core → Application → Infrastructure)
- **Database**: MySQL 8.0+ with MySqlConnector — raw SQL, no ORM, parameterized queries only
- **Frontend**: Vue 3 (Composition API) + Vite + Pinia + Tailwind CSS + Chart.js
- **Auth**: Cookie-based sessions (HttpOnly, Secure, SameSite=Strict)
- **External**: Riot Games API v5 with custom rate limiting
- **Testing**: xUnit (backend), Vitest + Vue Test Utils (frontend), Playwright (E2E)

## Detailed Standards (loaded automatically per file type)

- **C# files** → [backend.instructions.md](instructions/backend.instructions.md) — endpoint pattern, repos, DTOs, logging, DI
- **Vue/JS/CSS files** → [frontend.instructions.md](instructions/frontend.instructions.md) — components, stores, API layer, styling
- **Test files** → [testing.instructions.md](instructions/testing.instructions.md) — xUnit, Vitest, Playwright patterns
- **Build & run** → [server/AGENTS.md](../server/AGENTS.md), [client/AGENTS.md](../client/AGENTS.md)

## Reference Specs

- [Architecture & API spec](specs/architecture.spec.md) — all endpoints, DTOs, route map
- [Database schema](specs/database-schema.spec.md) — table structure and relationships
- [UI/UX spec](specs/ui-ux.spec.md) — design system, tokens, component inventory
- [Test strategy](specs/test-strategy.spec.md) — testing pyramid, coverage map, patterns
- [Feature template](specs/feature-template.spec.md) — template for new feature specs

## Universal Rules

### Security (Non-Negotiable)
- **PUUIDs are server-internal only** — all data endpoints resolve PUUID from User ID via `IUserRiotAccountsRepository`. Never expose PUUIDs to clients.
- **Log sanitization** — all user/external input must be sanitized via `LogSanitizer.Sanitize()` before logging. No exceptions.
- **PII encrypted at rest** — email and username use `IEncryptor` (AES-256). Riot API keys and DB credentials in env vars only.
- **Auth on every data endpoint** — verify `ClaimTypes.NameIdentifier` matches route `userId`. Use `AuthResults` helper for 401/403.
- **Parameterized SQL only** — never concatenate user input into queries.

### Architecture
- **Clean Architecture** — dependencies point inward: Infrastructure → Application → Core. Core has zero external dependencies.
- **Endpoint pattern** — every endpoint is a sealed class implementing `IEndpoint`, registered in `MongooseApiApplication.cs`.
- **UTC everywhere** — all `DateTime` values must be UTC. Use `DateTime.UtcNow` and `DateTimeKind.Utc`.
- **Records for DTOs** — all DTOs are C# records with `[JsonPropertyName("camelCase")]`.
- **Error responses** — JSON format: `{ "error": "message", "code": "ERROR_CODE" }`.

### API Conventions
- Base path: `/api/v2/`
- Queue filtering: `?queueType=ranked_solo|ranked_flex|normal|aram|all`
- Time range: `?timeRange=1w|1m|3m|6m|current_season|last_season`
- Standard HTTP verbs and status codes (200, 201, 400, 401, 403, 404, 500)

### Frontend Conventions
- Components: PascalCase `.vue` files with `<script setup>` and `data-testid` attributes
- Composables: `use` prefix (`useWinRateColor.js`). Stores: `Store` suffix (`authStore.js`). Services: `Api` suffix (`authApi.js`)
- All API calls centralized in `services/`. Use `apiRequest` from `apiClient.js`
- All components must handle loading, error, empty, and content states
- Accessibility: semantic HTML, `aria-label` on icon-only buttons, keyboard navigation, WCAG AA contrast

### Testing
- Backend: all endpoints must have integration tests (auth, forbidden, not-found, happy path)
- Frontend: all components with logic must have unit tests
- E2E: critical user flows covered via Playwright

### Performance
- `async`/`await` for all I/O. Lazy-load Vue routes. Debounce filters. Specify columns in SQL (no `SELECT *`).
- Match retention: 180 days (configurable via `Jobs:MatchRetentionDays`)

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

## Agent Specializations

<instructions>
<instruction>
<description>Backend development specialist with security focus</description>
<file>.github/agents/backend-engineer.agent.md</file>
<applyTo>**/*.cs</applyTo>
</instruction>
<instruction>
<description>Frontend development specialist with UI/UX focus</description>
<file>.github/agents/frontend-engineer.agent.md</file>
<applyTo>**/*.{vue,js,ts}</applyTo>
</instruction>
<instruction>
<description>System architect and planning specialist</description>
<file>.github/agents/architect.agent.md</file>
<applyTo>**/*.md</applyTo>
</instruction>
<instruction>
<description>Code review specialist focused on quality and best practices</description>
<file>.github/agents/code-reviewer.agent.md</file>
<applyTo>**/*</applyTo>
</instruction>
<instruction>
<description>DevOps and infrastructure specialist</description>
<file>.github/agents/devops-engineer.agent.md</file>
<applyTo>**/*.{yml,yaml,json,sh,dockerfile,Dockerfile}</applyTo>
</instruction>
<instruction>
<description>UX/UI design and research specialist</description>
<file>.github/agents/ux-ui-designer.agent.md</file>
<applyTo>**/*.{vue,css,scss,sass,less}</applyTo>
</instruction>
<instruction>
<description>Feature implementation orchestrator: architect → backend + frontend → code review → E2E</description>
<file>.github/agents/feature-implementation.agent.md</file>
<applyTo>**/*</applyTo>
</instruction>
<instruction>
<description>Test writing specialist for backend (xUnit) and frontend (Vitest)</description>
<file>.github/agents/test-writer.agent.md</file>
<applyTo>**/*.{spec.js,spec.cs,Tests.cs}</applyTo>
</instruction>
</instructions>