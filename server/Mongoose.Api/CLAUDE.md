# Server — Local Context

> C# .NET 10 Minimal API backend for Mongoose.gg.
> For repo-wide invariants see [CLAUDE.md](../../CLAUDE.md).

Load [architecture.spec.md](../../.github/specs/architecture.spec.md) only when changing routes, DTOs, auth flow, or endpoint contracts.
Load [database-schema.spec.md](../../.github/specs/database-schema.spec.md) only when changing SQL, repositories, or persistence shape.
Load [test-strategy.spec.md](../../.github/specs/test-strategy.spec.md) only when adding backend tests or changing test infrastructure.

## Build & Run

```bash
# From server/Mongoose.Api/ directory
dotnet build
dotnet run                          # Starts on http://localhost:5164
dotnet watch run                    # Hot reload
```

Requires a MySQL database. Connection string is resolved from `ConnectionStrings:Database_production` (production) or `ConnectionStrings:Database_test` (development/testing) via appsettings, env vars, or user secrets (see `Infrastructure/Configuration/Secrets.cs`). The Riot API key is required for match sync — set `RIOT_API_KEY` env var or `Riot:ApiKey` in configuration.

## Test

```bash
# From server/ directory
dotnet test Mongoose.Api.Tests/     # Run all tests
dotnet test Mongoose.Api.Tests/ --filter "FullyQualifiedName~LoginEndpoint"  # Single test class
```

Test project: `server/Mongoose.Api.Tests/Mongoose.Api.Tests.csproj` (xUnit). Uses `TestWebApplicationFactory` for integration tests with in-process server. Tests use `EnvironmentVariableScope` for isolated env var manipulation.

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

## Runtime Layout

- `Program.cs` — DI registrations, middleware pipeline, and WebSocket mapping.
- `Application/MongooseApiApplication.cs` — endpoint registration entry point.
- `Application/Endpoints/` — one endpoint class per file, grouped by subdomain.
- `Core/` — entities, interfaces, value objects, domain services, and query models.
- `Infrastructure/Database/` — connection factory, query filtering, and repositories.
- `Infrastructure/Jobs/` — background sync and cleanup jobs.
- `Infrastructure/WebSocket/` — sync progress broadcasting.

## Runtime Notes

- `IQueryFilterBuilder` standardizes queue and time-range filtering for data endpoints.
- `MatchHistorySyncJob` and `MatchCleanupJob` are controlled by `Jobs:*` flags in configuration.
- `SyncProgressHub` and `ISyncProgressBroadcaster` drive real-time sync progress updates.
- DI lifetimes follow nearby registrations in `Program.cs`; repository and request-scoped services are registered there.
- Namespaces mirror folder structure: `Mongoose.Api.{Layer}.{Subdomain}`.
- API routes are registered from `Application/MongooseApiApplication.cs` and live under `/api/v2/`.

## Configuration

Secrets resolved in order: `IConfiguration` → environment variables → `Secrets` static class (loaded from user secrets / env).

Key settings:
- `ConnectionStrings:Database_production` / `ConnectionStrings:Database_test` — MySQL connection string (also checked as env var `Database_production` / `Database_test`)
- `Riot:ApiKey` or env var `RIOT_API_KEY` — Riot Games API key
- `Security:EncryptionSecret` or env var `ENCRYPTION_SECRET` — AES encryption key for PII
- `Auth:CookieName` — auth cookie name (`mongoose-auth`)
- Auth sessions use a single 30-day persistent sliding cookie policy across login/register/verify.
- `Jobs:MatchRetentionDays` — match data retention (180 days)
- `Email:DevMode` — skip actual email sending in dev

## Validation

- For new or changed endpoints, add or update integration tests — see [server/Mongoose.Api.Tests/CLAUDE.md](../Mongoose.Api.Tests/CLAUDE.md).
- For new endpoints or vertical slices, use the [new-endpoint skill](../../.claude/skills/new-endpoint/SKILL.md).
- Reuse nearby implementation patterns before introducing new abstractions.
