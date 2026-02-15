# Server — Agent Instructions

> C# .NET 9 Minimal API backend for Mongoose.gg.
> For architecture details, endpoint specs, DTOs, and entity models see [architecture.spec.md](../.github/specs/architecture.spec.md).
> For database schema see [database-schema.spec.md](../.github/specs/database-schema.spec.md).

## Build & Run

```bash
# From server/ directory
dotnet build
dotnet run                          # Starts on http://localhost:5164
dotnet watch run                    # Hot reload
```

Requires a MySQL database. Connection string is resolved from environment variable `CONNECTION_STRING` or user secrets (see `Infrastructure/Configuration/Secrets.cs`). The Riot API key is required for match sync — set `API_KEY` env var or user secret.

## Test

```bash
# From server/ directory
dotnet test Mongoose.Api.Tests/     # Run all tests
dotnet test Mongoose.Api.Tests/ --filter "FullyQualifiedName~LoginEndpoint"  # Single test class
```

Test project: `Mongoose.Api.Tests/Mongoose.Api.Tests.csproj` (xUnit). Uses `TestWebApplicationFactory` for integration tests with in-process server. Tests use `EnvironmentVariableScope` for isolated env var manipulation.

## Architecture

Clean Architecture with three layers. Dependencies point inward: Infrastructure → Application → Core.

```
server/
├── Core/                    # Domain: entities, interfaces, enums, value objects
│   ├── Entities/            # POCOs: User, Match, Participant, RiotAccount, LpSnapshot, etc.
│   ├── Interfaces/          # Repository + service contracts (29 interfaces)
│   ├── Enums/
│   ├── ValueObjects/
│   └── QueryModels/         # Shared query result types
├── Application/             # Use cases: endpoints, DTOs, services
│   ├── Endpoints/           # Minimal API endpoint classes (one per endpoint)
│   │   ├── Auth/            # Register, Login, Logout, Delete, Verify, Resend, RiotAccounts, UsersMe
│   │   ├── Solo/            # SoloPerformance, SoloMatchups, MatchActivity
│   │   ├── Matches/         # MatchList, MatchDetails, MatchNarrative
│   │   ├── ChampionSelect/
│   │   ├── Overview/
│   │   ├── Trends/          # WinrateTrend, LpTrend
│   │   ├── Analytics/
│   │   ├── Feedback/
│   │   ├── Diagnostics/
│   │   └── Shared/          # AuthResults helper, IEndpoint interface
│   ├── DTOs/                # Response records organized by domain (mirrors Endpoints/)
│   ├── Services/            # LoginSyncService, LpCalculationService, MainChampionRecommender
│   └── QueryModels/
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

### Endpoint Pattern

Every endpoint implements `IEndpoint` with `Route` property and `Configure(WebApplication app)` method. Endpoints are registered in `MongooseApiApplication.cs`. Each endpoint is a sealed class in its own file.

```csharp
public sealed class MyEndpoint : IEndpoint
{
    public string Route { get; }

    public MyEndpoint(string basePath)
    {
        Route = basePath + "/my-resource/{userId}";
    }

    public void Configure(WebApplication app)
    {
        app.MapGet(Route, async (
            HttpContext httpContext,
            [FromRoute] string userId,
            [FromQuery] string? filter,
            [FromServices] IMyRepository repo,
            [FromServices] ILogger<MyEndpoint> logger
        ) =>
        {
            // 1. Auth check
            if (httpContext.User?.Identity?.IsAuthenticated != true)
                return AuthResults.NotAuthenticated();

            // 2. Parse + validate route params
            if (!int.TryParse(userId, out var userIdInt))
                return Results.BadRequest(new { error = "Invalid userId format" });

            // 3. Authorization — user can only access own data
            var authenticatedUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (authenticatedUserId != userIdInt.ToString())
                return Results.Forbid();

            // 4. Resolve PUUID from user → riot account link
            var linkedAccounts = await userRiotAccountsRepo.GetByUserIdAsync(userIdInt);
            var primaryPuuid = linkedAccounts.FirstOrDefault(la => la.Link.IsPrimary)?.Account?.Puuid;

            // 5. Query + return
            var result = await repo.GetDataAsync(primaryPuuid, filter);
            return Results.Ok(result);
        }).RequireAuthorization();
    }
}
```

**Critical**: All data endpoints follow the User → PUUID resolution pattern. Users don't query by PUUID directly — the endpoint resolves it from `IUserRiotAccountsRepository`.

### Repository Pattern

All repositories extend `RepositoryBase` which provides:
- `ExecuteScalarAsync<T>` — single value
- `ExecuteSingleAsync<T>` — single row with mapper
- `ExecuteListAsync<T>` — multiple rows with mapper
- `ExecuteNonQueryAsync` — INSERT/UPDATE/DELETE
- `ExecuteWithConnectionAsync<T>` — raw connection access
- `ExecuteTransactionAsync` — transactional operations

Repositories use raw SQL with `MySqlConnector` (no ORM). Parameters use named tuples: `("@param", value)`.

**PII handling**: `UsersRepository` encrypts email/username via `IEncryptor` before storage. Always use `_encryptor.Encrypt()` / `_encryptor.Decrypt()` for PII fields.

### DI Registration

Singletons: `IRiotApiClient`, `IDbConnectionFactory`, `IEncryptor`, `IEmailService`, `ILpCalculationService`, `IRateLimiter`, `SyncProgressHub`

Scoped (per-request): All repositories, `LoginSyncService`, `IQueryFilterBuilder`

### Query Filtering

`IQueryFilterBuilder` standardizes queue type and time range filtering across all data endpoints:
- `ValidateQueueType(string?)` → normalized queue string
- `BuildQueueFilter(string)` → SQL WHERE clause fragment
- `ResolveTimeRangeAsync(string?)` → `TimeRangeFilter` record
- `BuildTimeRangeFilter(TimeRangeFilter)` → SQL WHERE clause fragment

### Error Responses

All errors return JSON: `{ "error": "message", "code": "ERROR_CODE" }`. Use `AuthResults` helper for auth errors. Common codes: `NOT_AUTHENTICATED`, `SESSION_EXPIRED`, `FORBIDDEN`, `INVALID_PASSWORD`, `RIOT_ACCOUNT_NOT_FOUND`, `ACCOUNT_ALREADY_LINKED`.

### Background Jobs

`MatchHistorySyncJob` and `MatchCleanupJob` are `BackgroundService` implementations. Controlled via `appsettings.json` flags (`Jobs:EnableMatchHistorySync`, `Jobs:EnableMatchCleanup`). Sync job broadcasts progress via `ISyncProgressBroadcaster` → WebSocket.

## Configuration

Secrets resolved in order: `IConfiguration` → environment variables → `Secrets` static class (loaded from user secrets / env).

Key settings:
- `CONNECTION_STRING` — MySQL connection string
- `API_KEY` — Riot Games API key
- `ENCRYPTION_SECRET` — AES encryption key for PII
- `Auth:SessionTimeout` — session timeout in minutes (default: 30, dev: 5)
- `Auth:CookieName` — auth cookie name (`mongoose-auth`)
- `Jobs:MatchRetentionDays` — match data retention (180 days)
- `Email:DevMode` — skip actual email sending in dev

## Conventions

- **Namespaces** mirror folder structure: `Mongoose.Api.{Layer}.{Subdomain}`
- **One class per file**, filename matches class name
- **Records for DTOs** — immutable response types
- **Interfaces in Core**, implementations in Infrastructure
- **No ORM** — raw SQL via MySqlConnector for performance and control
- **UTC everywhere** — all `DateTime` values are UTC (`DateTime.SpecifyKind(..., DateTimeKind.Utc)`)
- **Nullable enabled** — `<Nullable>enable</Nullable>` project-wide
- **API versioning** — all endpoints under `/api/v2/`
