# Server — Local Context

> C# .NET 10 Minimal API backend for Mongoose.gg.
> For repo-wide invariants see [CLAUDE.md](../../CLAUDE.md).
> For endpoint, repository, logging, and validation rules see [backend.instructions.md](../../.github/instructions/backend.instructions.md).
> For contracts and schema see [architecture.spec.md](../../.github/specs/architecture.spec.md) and [database-schema.spec.md](../../.github/specs/database-schema.spec.md).

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
