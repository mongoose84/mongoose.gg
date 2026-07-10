# Mongoose.gg

League of Legends analytics platform — .NET 10 Minimal API backend, MySQL, Vue 3 SPA, Pinia, Tailwind CSS, Playwright E2E.

## Targeted Guidance

Read these files when the task touches the relevant area — do not load them all upfront:

| Area | File |
|------|------|
| C# app code | `.github/instructions/backend.instructions.md` |
| Vue / JS / CSS app code | `.github/instructions/frontend.instructions.md` |
| Backend tests | `.github/instructions/backend-test.instructions.md` |
| Frontend unit tests | `.github/instructions/frontend-unit-test.instructions.md` |
| E2E tests | `.github/instructions/e2e-test.instructions.md` |
| Backend build, run, runtime layout | `server/Mongoose.Api/AGENTS.md` |
| Frontend build, run, runtime layout | `client/AGENTS.md` |

## Deep References

Load only when the change directly touches contracts, schema, UX behavior, or test strategy:

| Topic | File |
|-------|------|
| API routes and endpoint contracts | `.github/specs/architecture.spec.md` |
| Database structure and SQL shape | `.github/specs/database-schema.spec.md` |
| UI behavior and design tokens | `.github/specs/ui-ux.spec.md` |
| Test scope and coverage strategy | `.github/specs/test-strategy.spec.md` |
| Component template | `.github/specs/component.spec.md` |

## Repo-Wide Invariants

These apply to every change regardless of area:

- Do not accept raw PUUID as client input for analytics or protected data endpoints — resolve Riot account identity server-side.
- Sanitize all user or external values before logging with `LogSanitizer.Sanitize()`.
- Encrypt PII at rest with `IEncryptor`; keep secrets in environment variables only.
- Protect data endpoints with authenticated ownership checks.
- Use parameterized SQL only — never concatenate user input into SQL.
- Preserve Clean Architecture dependency direction: Infrastructure → Application → Core.
- Keep domain rules in Core and orchestration in Application; apply SOLID inside those boundaries.
- Use UTC for all `DateTime` values.
- Keep changes minimal and aligned with existing patterns.
- Update tests and docs when behavior or contracts change.
