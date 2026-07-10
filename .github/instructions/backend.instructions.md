# Backend Rules

> Scope: `server/Mongoose.Api/**/*.cs`

Load [architecture.spec.md](../specs/architecture.spec.md) only when changing routes, DTOs, auth flow, or endpoint contracts.
Load [database-schema.spec.md](../specs/database-schema.spec.md) only when changing SQL, repositories, or persistence shape.
Load [test-strategy.spec.md](../specs/test-strategy.spec.md) only when adding backend tests or changing test infrastructure.
Use [server/Mongoose.Api/AGENTS.md](../../server/Mongoose.Api/AGENTS.md) for backend build and run context.

## Architecture

- Preserve Clean Architecture dependency direction: Infrastructure → Application → Core.
- Keep domain rules in Core and orchestration in Application.
- Apply SOLID inside those boundaries; avoid abstractions that weaken the domain model.

## Implementation Rules

- Endpoints must implement `IEndpoint`, live in their own sealed class, and be registered in `MongooseApiApplication.cs`.
- Protected data endpoints must authenticate, validate route input, enforce ownership, and resolve Riot account identity server-side.
- Own-account management sub-routes may use PUUID only when already scoped to the authenticated user.
- DTOs must be records with `[JsonPropertyName("camelCase")]`; keep them organized by domain.
- Repositories must extend `RepositoryBase` and use raw parameterized SQL with MySqlConnector.
- Use `IQueryFilterBuilder` for shared queue and time-range filtering instead of duplicating filter logic.
- Use `AuthResults` and the shared error patterns already present in the codebase.
- Encrypt PII with `IEncryptor`.
- Use singleton and scoped DI lifetimes consistently with nearby registrations.

## Logging And Data Safety

- Sanitize every user or external value passed to logger templates with `LogSanitizer.Sanitize()`.
- For numeric, enum, or boolean values derived from user or external input, convert to string before sanitizing.
- Never concatenate user input into SQL.
- Keep timestamps UTC and set `DateTimeKind.Utc` where needed.

## Validation

- For new or changed endpoints, add or update integration tests in [backend-test.instructions.md](backend-test.instructions.md).
- For new endpoints or vertical slices, use [new-endpoint/SKILL.md](../skills/new-endpoint/SKILL.md).
- Reuse nearby implementation patterns before introducing new abstractions.
