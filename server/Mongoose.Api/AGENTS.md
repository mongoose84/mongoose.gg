# Server — Agent Instructions

> C# .NET 10 Minimal API backend for Mongoose.gg.
> For architecture details, endpoint specs, DTOs, and entity models see [architecture.spec.md](../../.github/specs/architecture.spec.md).
> For database schema see [database-schema.spec.md](../../.github/specs/database-schema.spec.md).
> For coding patterns (endpoints, repositories, logging, DTOs) see [backend.instructions.md](../../.github/instructions/backend.instructions.md).

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

Clean Architecture with three layers. Dependencies point inward: Infrastructure → Application → Core.

```
server/Mongoose.Api/
├── Core/                    # Domain: entities, interfaces, enums, value objects
│   ├── Entities/            # POCOs: User, Match, Participant, RiotAccount, etc.
│   ├── Interfaces/          # Repository + service contracts (29 interfaces)
│   ├── Enums/
│   ├── ValueObjects/
│   ├── Services/            # Domain services: TrendBadgeCalculator, MainChampionRecommender
│   └── QueryModels/         # Shared query result types used across all layers
├── Application/             # Use cases: endpoints, DTOs, services
│   ├── Endpoints/           # Minimal API endpoint classes (one per endpoint)
│   │   ├── Auth/            # Register, Login, Logout, Delete, Verify, Resend, RiotAccounts, UsersMe
│   │   ├── Solo/            # SoloPerformance, SoloMatchups, MatchActivity
│   │   ├── Matches/         # MatchList, MatchDetails, MatchNarrative
│   │   ├── ChampionSelect/
│   │   ├── Overview/
│   │   ├── Trends/          # WinrateTrend
│   │   ├── Analytics/
│   │   ├── Feedback/
│   │   ├── Diagnostics/
│   │   └── Shared/          # AuthResults helper, IEndpoint interface
│   ├── DTOs/                # Response record stubs (types live in Core.QueryModels)
│   └── Services/            # Application orchestration: LoginSyncService, PuuidResolutionService
├── Infrastructure/          # External concerns: DB, Riot API, email, jobs
│   ├── Database/            # DbConnectionFactory, QueryFilterBuilder, Repositories/
│   ├── Riot/                # RiotApiClient, RiotUrlBuilder, rate limiting, mappers
│   ├── Jobs/                # MatchHistorySyncJob, MatchCleanupJob (BackgroundService)
│   ├── Security/            # AesEncryptor, Secrets, VerificationCodeGenerator
│   ├── Email/               # SmtpEmailService
│   ├── WebSocket/           # SyncProgressHub (SignalR-like raw WebSocket)
│   ├── Middleware/          # JsonExceptionMiddleware
│   ├── RateLimiting/        # EndpointRateLimiter (in-memory)
│   ├── GitHub/              # GitHubService (issue creation for feedback)
│   ├── Serialization/       # UTC DateTime JSON converters
│   ├── Telemetry/
│   └── Configuration/       # Secrets loader
├── Program.cs               # Composition root: DI, middleware pipeline, WebSocket mapping
└── MongooseApiApplication.cs # Endpoint registration
```

## Key Patterns

> Full endpoint, repository, logging, and DTO patterns are in [backend.instructions.md](../../.github/instructions/backend.instructions.md). This section covers server-specific runtime details only.

### DI Registration

Singletons: `IRiotApiClient`, `IDbConnectionFactory`, `IEncryptor`, `IEmailService`, `IRateLimiter`, `SyncProgressHub`

Scoped (per-request): All repositories, `LoginSyncService`, `IQueryFilterBuilder`

### Query Filtering

`IQueryFilterBuilder` standardizes queue type and time range filtering across all data endpoints:
- `ValidateQueueType(string?)` → normalized queue string
- `BuildQueueFilter(string)` → SQL WHERE clause fragment
- `ResolveTimeRangeAsync(string?)` → `TimeRangeFilter` record
- `BuildTimeRangeFilter(TimeRangeFilter)` → SQL WHERE clause fragment

### Background Jobs

`MatchHistorySyncJob` and `MatchCleanupJob` are `BackgroundService` implementations. Controlled via `appsettings.json` flags (`Jobs:EnableMatchHistorySync`, `Jobs:EnableMatchCleanup`). Sync job broadcasts progress via `ISyncProgressBroadcaster` → WebSocket.

## Configuration

Secrets resolved in order: `IConfiguration` → environment variables → `Secrets` static class (loaded from user secrets / env).

Key settings:
- `ConnectionStrings:Database_production` / `ConnectionStrings:Database_test` — MySQL connection string (also checked as env var `Database_production` / `Database_test`)
- `Riot:ApiKey` or env var `RIOT_API_KEY` — Riot Games API key
- `Security:EncryptionSecret` or env var `ENCRYPTION_SECRET` — AES encryption key for PII
- `Auth:SessionTimeout` — session timeout in minutes (default: 30, dev: 5)
- `Auth:CookieName` — auth cookie name (`mongoose-auth`)
- `Jobs:MatchRetentionDays` — match data retention (180 days)
- `Email:DevMode` — skip actual email sending in dev

## Conventions

See [backend.instructions.md](../.github/instructions/backend.instructions.md) for all coding conventions. Key reminders:
- **Namespaces** mirror folder structure: `Mongoose.Api.{Layer}.{Subdomain}`
- **No ORM** — raw SQL via MySqlConnector
- **UTC everywhere** — all `DateTime` values are UTC
- **API versioning** — all endpoints under `/api/v2/`
