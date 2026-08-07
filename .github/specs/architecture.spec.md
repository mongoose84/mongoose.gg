# Mongoose.gg — Architecture & API Specification

> **Purpose**: Single-source-of-truth for AI agents and developers working on the Mongoose.gg codebase. Contains architecture decisions, every implemented endpoint, all DTOs, entity models, repository interfaces, and the patterns required to extend or debug the system.

**Stack**: C# (.NET 10 Minimal APIs) · MySQL (InnoDB, utf8mb4) · Vue 3 + Tailwind (frontend) · Cookie-based session auth · Raw WebSocket  
**Last verified**: August 8, 2026

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [Project Structure & File Map](#2-project-structure--file-map)
3. [Design Principles & Conventions](#3-design-principles--conventions)
4. [Authentication & Security](#4-authentication--security)
5. [Route Map — All Endpoints](#5-route-map--all-endpoints)
6. [Endpoint Specifications (Implemented)](#6-endpoint-specifications-implemented)
7. [Response DTOs (Complete)](#7-response-dtos-complete)
8. [Core Entities](#8-core-entities)
9. [Repository Interfaces](#9-repository-interfaces)
10. [Application Services](#10-application-services)
11. [Query Filtering System](#11-query-filtering-system)
12. [Error Response Contract](#12-error-response-contract)
13. [WebSocket — Real-time Sync](#13-websocket--real-time-sync)
14. [Planned Endpoints (Not Yet Implemented)](#14-planned-endpoints-not-yet-implemented)
15. [New Endpoint Implementation Checklist](#15-new-endpoint-implementation-checklist)

---

## 1. Architecture Overview

```
┌──────────────────────────────────────────────────────────────────┐
│  Client (Vue 3 + Tailwind + Headless UI)                         │
│  SPA at client/src/ — calls /api/v2/* with session cookie        │
└──────────────────┬───────────────────────────────────────────────┘
                   │ HTTP + WS (cookie auth)
┌──────────────────▼───────────────────────────────────────────────┐
│  API Layer — server/Mongoose.Api/Application/Endpoints/            │
│  Minimal API endpoints implementing IEndpoint                     │
│  Registered via MongooseApiApplication.ConfigureEndpoints()       │
├──────────────────────────────────────────────────────────────────┤
│  Application Services — server/Mongoose.Api/Application/Services/  │
│  LoginSyncService, PuuidResolutionService                          │
├──────────────────────────────────────────────────────────────────┤
│  Core Layer — server/Mongoose.Api/Core/                            │
│  Entities · Interfaces · Enums · QueryModels                      │
│  Domain Services: TrendBadgeCalculator, MainChampionRecommender   │
├──────────────────────────────────────────────────────────────────┤
│  Infrastructure — server/Mongoose.Api/Infrastructure/              │
│  Database/Repositories · Riot API client · Email · Jobs · WebSocket│
├──────────────────────────────────────────────────────────────────┤
│  MySQL Database (schema.sql + Infrastructure/Database/Migrations/) │
│  20 tables in schema.sql: users, user_identity_providers,          │
│  riot_accounts, user_riot_accounts, matches, participants, ...     │
│  + 10 analytics-pipeline tables added by migrations 001–002        │
└──────────────────────────────────────────────────────────────────┘
```

**Clean Architecture + DDD layers** (dependency flows inward):

- **Core** (`server/Mongoose.Api/Core/`) — Entities, Interfaces, Enums, ValueObjects, QueryModels, Domain Services. Zero dependencies on Application or Infrastructure layers.
- **Application** (`server/Mongoose.Api/Application/`) — Endpoints, DTOs (stub shells), Application Services (orchestration). Depends only on Core.
- **Infrastructure** (`server/Mongoose.Api/Infrastructure/`) — Repository implementations, Riot API, email, jobs. Implements Core interfaces. Depends only on Core (plus `Application.Endpoints.Shared` for cross-cutting `LogSanitizer`).

**DDD conventions**:
- Keep business invariants in domain entities/value objects inside Core.
- Domain services (pure calculation logic like `TrendBadgeCalculator`, `MainChampionRecommender`) live in `Core/Services/`.
- Query result types shared across layers live in `Core/QueryModels/`.
- Maintain ubiquitous language consistency across endpoint names, DTOs, services, and repositories.
- Respect bounded contexts when adding features to avoid cross-domain leakage.

**SOLID usage**:
- Treat SOLID as a tactical design aid within the DDD model and Clean Architecture layers, not as a competing architecture style.
- Prefer abstractions that protect domain clarity, testability, and dependency direction.
- Avoid introducing interfaces or indirection mainly for reuse when the result obscures business concepts.

**Key patterns**:
- Endpoints implement `IEndpoint` interface (Route + Configure method)
- Repositories are registered as Scoped services via DI
- Domain logic lives in Core first; Application orchestrates, Infrastructure integrates
- M:M relationship between Users and RiotAccounts via `user_riot_accounts` junction table
- All JSON uses camelCase via `[JsonPropertyName]` attributes on record types

---

## 2. Project Structure & File Map

> This tree is illustrative, not exhaustive — `Application/Endpoints/` alone has 38 files and `Infrastructure/Database/Repositories/` has 23. For the complete, authoritative endpoint list use [§5 Route Map](#5-route-map--all-endpoints); for the complete repository interface list use [§9](#9-repository-interfaces). Don't hand-maintain a second endpoint/repo inventory here — update those sections instead.

```
server/
├── mongoose.sln
├── NuGet.config
├── Mongoose.Api/
│   ├── Program.cs                          # DI registration, middleware, endpoint wiring, /ws/sync mapping
│   ├── schema.sql                          # Core MySQL schema (see database-schema.spec.md)
│   ├── appsettings.json                    # Production config
│   ├── appsettings.Development.json        # Dev config
│   ├── Application/
│   │   ├── MongooseApiApplication.cs       # Endpoint registration orchestrator
│   │   ├── Extensions/
│   │   │   └── EndpointDiscoveryExtension.cs  # Reflection-based IEndpoint discovery
│   │   ├── Telemetry/
│   │   │   └── event-schema.yml            # Analytics event schema definitions
│   │   ├── DTOs/                           # Auth, Feedback, ChampionSelect, Matches, Overview, Solo DTOs
│   │   │   └── ...                         # Trends/TrendDto.cs and Solo/RadarChartDto.cs are stubs —
│   │   │                                   # those response types now live in Core/QueryModels/
│   │   ├── Endpoints/                      # One class per file, grouped by subdomain (Auth, Solo,
│   │   │   │                               # Trends, Matches, Overview, ChampionSelect, Analytics,
│   │   │   │                               # Feedback, Diagnostics, Shared) — see §5 for the full list
│   │   │   └── Shared/
│   │   │       ├── IEndpoint.cs            # Interface: Route + Configure(WebApplication)
│   │   │       ├── AuthResults.cs          # Standard 401/403 JSON helpers
│   │   │       ├── HomeEndpoint.cs         # GET / — sitemap
│   │   │       ├── PublicStatsEndpoint.cs  # GET /api/v2/public/stats
│   │   │       ├── ClientIpAddressResolver.cs
│   │   │       └── LogSanitizer.cs
│   │   └── Services/
│   │       ├── LoginSyncService.cs         # Post-login Riot data refresh
│   │       ├── PuuidResolutionService.cs   # Resolves a user's primary Riot account PUUID
│   │       └── AuthorizationHelper.cs      # Shared authenticated/authorized-user route checks
│   ├── Core/
│   │   ├── Entities/                       # POCOs: User, RiotAccount, Match, Participant, etc.
│   │   ├── Interfaces/                     # Repository + service contracts
│   │   ├── Enums/
│   │   ├── QueryModels/                    # MatchListSummaryItem, RoleBaseline, TrendQueryModels, etc.
│   │   ├── Services/
│   │   │   ├── MainChampionRecommender.cs  # Champion scoring algorithm (MScore) — domain logic, lives in Core
│   │   │   └── TrendBadgeCalculator.cs
│   │   └── ValueObjects/
│   └── Infrastructure/
│       ├── Database/
│       │   ├── DbConnectionFactory.cs
│       │   ├── QueryFilterBuilder.cs
│       │   ├── Migrations/                 # Hand-applied SQL migrations layered on schema.sql
│       │   │                               # (analytics_events_v2, Phase 3 analytics, user_identity_providers)
│       │   └── Repositories/               # MySQL implementations of all Core interfaces
│       ├── Riot/
│       │   ├── RiotApiClient.cs            # Riot Games API integration
│       │   ├── RiotSignOnClient.cs         # RSO OAuth code exchange + identity resolution
│       │   ├── RiotUrlBuilder.cs
│       │   ├── SeasonHelper.cs
│       │   ├── LimitHandler/               # Rate-limit token bucket for Riot API calls
│       │   └── Mappers/                    # RiotMatchMapper, RiotTimelineMapper
│       ├── Google/
│       │   └── GoogleSignOnClient.cs       # Google OAuth code exchange + identity resolution
│       ├── Services/
│       │   ├── MatchDataPersistenceService.cs
│       │   └── Analytics/                  # Aggregation, abuse guards, pipeline monitor, queue processor,
│       │                                   # dimension extraction, journey/funnel detection
│       ├── Telemetry/
│       │   ├── EventSchemaRegistry.cs
│       │   ├── EventValidator.cs
│       │   └── Metrics.cs
│       ├── Email/
│       │   ├── SmtpEmailService.cs
│       │   └── VerificationCodeGenerator.cs
│       ├── GitHub/GitHubService.cs         # Feedback → GitHub issue creation
│       ├── Helpers/LeagueDataHelper.cs
│       ├── Serialization/UtcDateTimeJsonConverter.cs
│       ├── Jobs/
│       │   ├── MatchHistorySyncJob.cs      # Background: syncs match history
│       │   ├── MatchCleanupJob.cs          # Background: deletes old matches
│       │   ├── SyncQueueSignal.cs
│       │   └── Analytics/AnalyticsBackgroundJobs.cs
│       ├── WebSocket/
│       │   ├── SyncProgressHub.cs          # Raw WebSocket hub for real-time sync updates
│       │   ├── SyncProgressAggregator.cs   # Combined progress across a "sync all" run
│       │   └── ISyncProgressBroadcaster.cs
│       ├── Configuration/Secrets.cs
│       ├── Security/{AesEncryptor,IEncryptor}.cs
│       ├── Middleware/JsonExceptionMiddleware.cs
│       └── RateLimiting/EndpointRateLimiter.cs
└── Mongoose.Api.Tests/                     # xUnit test project
```

---

## 3. Design Principles & Conventions

### Response Shape Rules
- **Dashboard-ready**: Backend returns pre-aggregated data. Frontend does minimal transformation.
- **No nested array expansion**: Avoid `List<List<>>` responses.
- **camelCase JSON**: All DTO properties use `[JsonPropertyName("camelCase")]`.
- **Nullable for optional**: Use `?` notation (e.g., `double?`) for stats that may not exist.
- **Records for DTOs**: All request/response models are C# `record` types.

### Endpoint Pattern
Every endpoint follows this structure:
```csharp
public sealed class MyEndpoint : IEndpoint
{
    public string Route { get; }
    
    public MyEndpoint(string basePath)
    {
        Route = basePath + "/my-feature";  // basePath = "/api/v2"
    }
    
    public void Configure(WebApplication app)
    {
        app.MapGet(Route + "/{userId}", async (
            long userId,
            HttpContext httpContext,
            [FromQuery] string? queueType,
            [FromServices] MyRepository repo,
            [FromServices] ILogger<MyEndpoint> logger
        ) =>
        {
            // 1. Auth check (userId from claims matches route param)
            // 2. Get primary Riot account via IUserRiotAccountsRepository
            // 3. Validate + normalize query params via IQueryFilterBuilder
            // 4. Call repository method
            // 5. Return Results.Ok(dto)
        }).RequireAuthorization();
    }
}
```

### User→RiotAccount Resolution Pattern
Most endpoints receive `userId` and must resolve to a `puuid`:
```csharp
// Standard pattern used across all dashboard endpoints:
var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
if (!long.TryParse(userIdClaim, out var authenticatedUserId) || authenticatedUserId != userId)
    return AuthResults.Forbidden();

var primary = await userRiotAccountsRepo.GetPrimaryByUserIdAsync(userId);
if (primary == null) return Results.NotFound(new { error = "No linked Riot account" });
string puuid = primary.Value.Account.Puuid;
```

### Versioning
- Current version: `/api/v2/`
- Breaking changes only in v3+ (if needed)
- DTOs are versioned by name (e.g., `SoloPerformanceResponse`)

---

## 4. Authentication & Security

### Session Auth Flow
1. Client calls `POST /api/v2/auth/login` with username + password
2. Server validates credentials (BCrypt), creates `ClaimsPrincipal` with claims:
   - `ClaimTypes.NameIdentifier` → userId (string)
   - `ClaimTypes.Name` → username
   - `ClaimTypes.Email` → email
   - `email_verified` → bool
   - `tier` → "free" | "pro"
3. Server sets httpOnly, Secure, SameSite=Lax session cookie via `HttpContext.SignInAsync()`
4. All subsequent requests include cookie automatically
5. Protected endpoints use `.RequireAuthorization()`

### Auth Error Responses (from `AuthResults.cs`)
```csharp
// 401 — session expired or missing
{ "error": "Your session has expired. Please log in again.", "code": "SESSION_EXPIRED" }
{ "error": "Authentication required.", "code": "NOT_AUTHENTICATED" }

// 403 — wrong user or insufficient permissions
{ "error": "Access denied.", "code": "FORBIDDEN" }

// 401 — bad credentials (login only)
{ "error": "Invalid username or password", "code": "INVALID_CREDENTIALS" }
```

### Rate Limits (per endpoint)
| Endpoint | Limit | Window |
|----------|-------|--------|
| `POST /auth/login` | 10 | 15 min / IP |
| `POST /auth/register` | 3 | 1 hour / IP |
| `GET /auth/riot/callback` | 10 | 15 min / IP |
| `POST /auth/resend-verification` | 5 | 1 hour / user |
| `POST /feedback` | 5 | 1 hour / IP |
| `GET /public/stats` | 60 | 1 min / IP |

### CORS
Allowed origins: `localhost:5173-5175`, `mongoose.gg`, `www.mongoose.gg`, `beta.mongoose.gg`  
Credentials: allowed. Methods & Headers: any.

---

## 5. Route Map — All Endpoints

Endpoints are discovered via reflection (`EndpointDiscoveryExtension.DiscoverEndpoints()`) — every `IEndpoint` implementation under `Application/Endpoints/` is picked up automatically and wired in `MongooseApiApplication.ConfigureEndpoints()`. There is no manual registration list to keep in sync, but this route map must still be kept in sync by hand.

### Implemented Endpoints

| Method | Route | Auth | Rate Limited | Handler File | Response DTO |
|--------|-------|------|--------------|--------------|-------------|
| `POST` | `/api/v2/auth/login` | No | 10/15min/IP | `Auth/LoginEndpoint.cs` | `LoginResponse` |
| `POST` | `/api/v2/auth/register` | No | 3/hr/IP | `Auth/RegisterEndpoint.cs` | `RegisterResponse` |
| `GET` | `/api/v2/auth/riot/login` | No | No | `Auth/RiotSignOnEndpoint.cs` | redirect (302) |
| `GET` | `/api/v2/auth/riot/callback` | No | 10/15min/IP | `Auth/RiotSignOnEndpoint.cs` | redirect (302) |
| `GET` | `/api/v2/auth/google/login` | No | No | `Auth/GoogleSignOnEndpoint.cs` | redirect (302) |
| `GET` | `/api/v2/auth/google/callback` | No | 10/15min/IP | `Auth/GoogleSignOnEndpoint.cs` | redirect (302) |
| `POST` | `/api/v2/auth/logout` | Yes | No | `Auth/LogoutEndpoint.cs` | `LogoutResponse` |
| `DELETE` | `/api/v2/auth/account` | Yes | No | `Auth/DeleteAccountEndpoint.cs` | `DeleteAccountResponse` |
| `POST` | `/api/v2/auth/verify` | Yes | No | `Auth/VerifyEndpoint.cs` | `VerifyResponse` |
| `POST` | `/api/v2/auth/resend-verification` | Yes | 5/hr/user | `Auth/ResendVerificationEndpoint.cs` | `ResendVerificationResponse` |
| `POST` | `/api/v2/auth/forgot-password` | No | 5/hr/IP | `Auth/ForgotPasswordEndpoint.cs` | — |
| `POST` | `/api/v2/auth/reset-password` | No | 10/15min/IP | `Auth/ResetPasswordEndpoint.cs` | — |
| `POST` | `/api/v2/auth/change-password` | Yes | No | `Auth/ChangePasswordEndpoint.cs` | — |
| `GET` | `/api/v2/users/me` | Yes | No | `Auth/UsersMeEndpoint.cs` | `UserMeResponse` |
| `PUT` | `/api/v2/users/me/icon` | Yes | No | `Auth/UsersMeEndpoint.cs` | — |
| `POST` | `/api/v2/users/me/riot-accounts` | Yes | No | `Auth/RiotAccountsEndpoint.cs` | — |
| `DELETE` | `/api/v2/users/me/riot-accounts/{puuid}` | Yes | No | `Auth/RiotAccountsEndpoint.cs` | — |
| `PUT` | `/api/v2/users/me/riot-accounts/{puuid}/primary` | Yes | No | `Auth/RiotAccountsEndpoint.cs` | — |
| `POST` | `/api/v2/users/me/riot-accounts/sync` | Yes | No | `Auth/RiotAccountsEndpoint.cs` | `SyncAllResponse` (sync all linked accounts) |
| `POST` | `/api/v2/users/me/riot-accounts/{puuid}/sync` | Yes | No | `Auth/RiotAccountsEndpoint.cs` | — |
| `GET` | `/api/v2/users/me/riot-accounts/{puuid}/sync-status` | Yes | No | `Auth/RiotAccountsEndpoint.cs` | — |
| `GET` | `/api/v2/overview/{userId}` | Yes | No | `Overview/OverviewEndpoint.cs` | `OverviewResponse` |
| `GET` | `/api/v2/solo/dashboard/{userId}` | Yes | No | `Solo/SoloPerformanceEndpoint.cs` | `SoloPerformanceResponse` |
| `GET` | `/api/v2/solo/matchups/{userId}` | Yes | No | `Solo/SoloMatchupsEndpoint.cs` | `ChampionMatchupsResponse` |
| `GET` | `/api/v2/solo/activity/{userId}` | Yes | No | `Solo/MatchActivityEndpoint.cs` | `MatchActivityResponse` |
| `GET` | `/api/v2/solo/death-positions/{userId}` | Yes | No | `Solo/DeathPositionsEndpoint.cs` | `DeathPositionsResponse` |
| `GET` | `/api/v2/solo/radar-chart/{userId}` | Yes | No | `Solo/RadarChartEndpoint.cs` | `RadarChartResponse` |
| `GET` | `/api/v2/matches/{userId}` | Yes | No | `Matches/MatchListEndpoint.cs` | `MatchListResponse` |
| `GET` | `/api/v2/matches/{matchId}/details` | Yes | No | `Matches/MatchDetailsEndpoint.cs` | `MatchDetailsResponse` |
| `GET` | `/api/v2/matches/{matchId}/narrative` | Yes | No | `Matches/MatchNarrativeEndpoint.cs` | `MatchNarrativeResponse` |
| `GET` | `/api/v2/trends/winrate/{userId}` | Yes | No | `Trends/WinrateTrendEndpoint.cs` | `WinrateTrendResponse` |
| `GET` | `/api/v2/trends/cs-per-minute/{userId}` | Yes | No | `Trends/CsPerMinuteTrendEndpoint.cs` | `CsPerMinuteTrendResponse` |
| `GET` | `/api/v2/trends/deaths/{userId}` | Yes | No | `Trends/DeathsTrendEndpoint.cs` | `DeathsTrendResponse` |
| `GET` | `/api/v2/trends/dragon-participation/{userId}` | Yes | No | `Trends/DragonParticipationTrendEndpoint.cs` | `DragonParticipationTrendResponse` |
| `GET` | `/api/v2/trends/gold-at-15/{userId}` | Yes | No | `Trends/GoldAt15TrendEndpoint.cs` | `GoldAt15TrendResponse` |
| `GET` | `/api/v2/trends/vision-score/{userId}` | Yes | No | `Trends/VisionScoreTrendEndpoint.cs` | `VisionScoreTrendResponse` |
| `GET` | `/api/v2/champion-select/{userId}` | Yes | No | `ChampionSelect/ChampionSelectEndpoint.cs` | `ChampionSelectResponse` |
| `POST` | `/api/v2/analytics` | No | No | `Analytics/AnalyticsEndpointV2.cs` | `TrackEventResponse` |
| `POST` | `/api/v2/analytics/v2` | No | No | `Analytics/AnalyticsEndpointV2.cs` | `TrackEventResponse` |
| `POST` | `/api/v2/analytics/batch` | No | No | `Analytics/AnalyticsEndpointV2.cs` | `TrackBatchResponse` |
| `POST` | `/api/v2/analytics/v2/batch` | No | No | `Analytics/AnalyticsEndpointV2.cs` | `TrackBatchResponse` |
| `GET` | `/api/v2/analytics/health` | No | No | `Analytics/AnalyticsEndpointV2.cs` | dynamic JSON |
| `GET` | `/api/v2/analytics/schema` | No | No | `Analytics/AnalyticsEndpointV2.cs` | dynamic JSON |
| `POST` | `/api/v2/analytics/async` | No | No | `Analytics/AnalyticsAsyncEndpoint.cs` | dynamic JSON |
| `GET` | `/api/v2/analytics/async/queue` | No | No | `Analytics/AnalyticsAsyncEndpoint.cs` | dynamic JSON |
| `GET` | `/api/v2/analytics/explore/events` | No | No | `Analytics/AnalyticsExploreEndpoint.cs` | dynamic JSON |
| `GET` | `/api/v2/analytics/explore/dimensions` | No | No | `Analytics/AnalyticsExploreEndpoint.cs` | dynamic JSON |
| `GET` | `/api/v2/analytics/explore/events/{eventName}` | No | No | `Analytics/AnalyticsExploreEndpoint.cs` | dynamic JSON |
| `GET` | `/api/v2/analytics/journey/flows` | No | No | `Analytics/AnalyticsJourneyAndFunnelEndpoints.cs` | dynamic JSON |
| `GET` | `/api/v2/analytics/journey/user/{userId}` | No | No | `Analytics/AnalyticsJourneyAndFunnelEndpoints.cs` | dynamic JSON |
| `GET` | `/api/v2/analytics/journey/paths` | No | No | `Analytics/AnalyticsJourneyAndFunnelEndpoints.cs` | dynamic JSON |
| `GET` | `/api/v2/analytics/funnels` | No | No | `Analytics/AnalyticsJourneyAndFunnelEndpoints.cs` | dynamic JSON |
| `GET` | `/api/v2/analytics/funnels/{funnelId}` | No | No | `Analytics/AnalyticsJourneyAndFunnelEndpoints.cs` | dynamic JSON |
| `GET` | `/api/v2/analytics/realtime/events` | No | No | `Analytics/AnalyticsRealtimeEndpoint.cs` | dynamic JSON |
| `GET` | `/api/v2/analytics/realtime/stats` | No | No | `Analytics/AnalyticsRealtimeEndpoint.cs` | dynamic JSON |
| `GET` | `/api/v2/diagnostics` | No | No | `Diagnostics/DiagnosticsEndpoint.cs` | dynamic JSON |
| `POST` | `/api/v2/feedback` | No | 5/hr/IP | `Feedback/FeedbackEndpoint.cs` | `FeedbackResponse` |
| `GET` | `/api/v2/public/stats` | No | 60/min/IP | `Shared/PublicStatsEndpoint.cs` | dynamic JSON |
| `GET` | `/` | No | No | `Shared/HomeEndpoint.cs` | sitemap JSON |
| `WS` | `/ws/sync` | Yes (cookie) | No | `Program.cs` inline | WebSocket JSON messages |

**Note**: The `analytics/*` sub-routes (`async`, `explore`, `journey`, `funnels`, `realtime`) are internal analytics-admin surfaces — currently unauthenticated at the endpoint level, same as the public event-tracking routes. No `[Authorize]`/`RequireAuthorization()` guard exists on any of them as of this writing; treat that as a known gap rather than intentional design if extending this area.

### Planned Endpoints (Not Implemented)
See [Section 14](#14-planned-endpoints-not-yet-implemented).

---

## 6. Endpoint Specifications (Implemented)

### 6.1 Auth — Login
**Route**: `POST /api/v2/auth/login`  
**Auth**: None (public)  
**Rate limit**: 10 requests / 15 min / IP  
**Request body**: `LoginRequest(username, password, consentLevel?)`  
**Response**: `LoginResponse(userId, username, email, emailVerified, tier, message)`  
**Side effects**: Sets httpOnly session cookie, updates `last_login_at`, fires `LoginSyncService.CheckAccountsOnLoginAsync()` (updates profile data, rank, checks for new matches)  
**Tables**: `users`, `riot_accounts`, `user_riot_accounts`

### 6.2 Auth — Register
**Route**: `POST /api/v2/auth/register`  
**Auth**: None (public)  
**Rate limit**: 3 / hour / IP  
**Request body**: `RegisterRequest(username, email, password)`  
**Validation**: Username 3-30 chars (alphanumeric + underscore), email valid format, password ≥8 chars  
**Response**: `RegisterResponse(userId, username, email, emailVerified, message)`  
**Side effects**: Sets session cookie, sends verification email (fire-and-forget)  
**Tables**: `users`, `verification_tokens`

### 6.2a Auth — Riot Sign-On (RSO)
**Routes**: `GET /api/v2/auth/riot/login`, `GET /api/v2/auth/riot/callback`  
**Auth**: None (public)  
**Feature flag**: `Auth:EnableRiotSignOn` (off by default; requires `Auth:Riot:ClientId`/`ClientSecret`/`RedirectUri` or `RSO_CLIENT_ID`/`RSO_CLIENT_SECRET` env vars)  
**Rate limit** (callback only): 10 / 15 min / IP  
**Flow**: OAuth 2.0 authorization code flow against Riot's identity provider.
1. `GET /auth/riot/login` sets an httpOnly, SameSite=Lax CSRF `state` cookie (`mongoose-rso-state`) and redirects the browser to `https://auth.riotgames.com/authorize`.
2. Riot redirects back to `GET /auth/riot/callback?code=...&state=...`. The endpoint verifies `state` against the cookie, then exchanges `code` server-side (`IRiotSignOnClient`) for an access token and calls Riot's `account/v1/accounts/me` to resolve the authoritative PUUID, game name, and tag line — **never accepted from client input**.
3. If no user is linked to this PUUID via `user_identity_providers` (provider `riot`), one is created: synthetic placeholder email (`{puuid}@riot-signon.invalid`), random unusable password hash, `email_verified = true`, username derived from the Riot game name (deduplicated), and the identity is linked.
4. The Riot account is auto-linked to the user via `user_riot_accounts` (no manual link step, since Riot already authenticated the identity) — set as primary if it's the user's first linked account.
5. Server signs in with the standard session cookie (`AuthSessionFactory`) and redirects to `{Auth:Riot:ClientBaseUrl}/app/overview`.
**Error redirects**: `/auth?error=<code>` for `riot_signon_disabled`, `riot_signon_denied`, `riot_signon_state`, `riot_signon_failed`, `riot_signon_rate_limited`, `account_deactivated`.  
**Side effects**: May create `users` + `riot_accounts` rows, links `user_riot_accounts` and `user_identity_providers`, sets session cookie, updates `last_login_at`, fires `LoginSyncService.CheckAccountsOnLoginAsync()`.  
**Tables**: `users`, `riot_accounts`, `user_riot_accounts`, `user_identity_providers`

### 6.2b Auth — Google Sign-On
**Routes**: `GET /api/v2/auth/google/login`, `GET /api/v2/auth/google/callback`  
**Auth**: None (public)  
**Feature flag**: `Auth:EnableGoogleSignOn` (off by default; requires `Auth:Google:ClientId`/`ClientSecret`/`RedirectUri` or `GSO_CLIENT_ID`/`GSO_CLIENT_SECRET` env vars)  
**Rate limit** (callback only): 10 / 15 min / IP  
**Flow**: OAuth 2.0 authorization code flow against Google's identity provider.
1. `GET /auth/google/login` sets an httpOnly, SameSite=Lax CSRF `state` cookie (`mongoose-gso-state`) and redirects the browser to Google's consent page (scope `openid email profile`).
2. Google redirects back to `GET /auth/google/callback?code=...&state=...`. The endpoint verifies `state` against the cookie, then exchanges `code` server-side (`IGoogleSignOnClient`) for an access token and calls Google's userinfo endpoint to resolve the authoritative `sub` (Google user id), email, `email_verified`, and name — **never accepted from client input**.
3. If no user is linked to this `sub` via `user_identity_providers` (provider `google`):
   - If Google reports the email as verified and a local account already has that exact email, the Google identity is **auto-linked** to that existing account (and its `email_verified` is set true) instead of creating a new user.
   - Otherwise a new user is created: real email from Google, random unusable password hash, `email_verified` mirrors Google's `email_verified` claim, username derived from the Google display name or email local-part (deduplicated). An unverified Google email never auto-links to an existing account, to prevent an attacker who doesn't control a mailbox from hijacking a local account through it.
4. Server signs in with the standard session cookie (`AuthSessionFactory`) and redirects to `{Auth:Google:ClientBaseUrl}/app/overview`.
**Error redirects**: `/auth?error=<code>` for `google_signon_disabled`, `google_signon_denied`, `google_signon_state`, `google_signon_failed`, `google_signon_rate_limited`, `account_deactivated`.  
**Side effects**: May create a `users` row, links `user_identity_providers`, sets session cookie, updates `last_login_at`, fires `LoginSyncService.CheckAccountsOnLoginAsync()`.  
**Tables**: `users`, `user_identity_providers`

### 6.3 Auth — Logout
**Route**: `POST /api/v2/auth/logout`  
**Auth**: Yes  
**Response**: `LogoutResponse(message)`  
**Side effects**: Clears session cookie via `SignOutAsync`

### 6.4 Auth — Delete Account
**Route**: `DELETE /api/v2/auth/account`  
**Auth**: Yes  
**Request body**: `DeleteAccountRequest(password)` — password confirmation required  
**Response**: `DeleteAccountResponse(success, message)`  
**Side effects**: Cascading delete of user data, signs out  
**Tables**: `users` (CASCADE deletes linked data)

### 6.5 Auth — Verify Email
**Route**: `POST /api/v2/auth/verify`  
**Auth**: Yes  
**Request body**: `{ "code": "123456" }`  
**Validation**: Exactly 6 digits. Max 5 attempts per token (brute-force protection). Token expires after 15 min.  
**Response**: `VerifyResponse(verified, message)`  
**Side effects**: Updates `email_verified`, marks token used, re-signs in with updated claims  
**Tables**: `users`, `verification_tokens`

### 6.6 Auth — Resend Verification
**Route**: `POST /api/v2/auth/resend-verification`  
**Auth**: Yes  
**Rate limit**: 5 / hour / user, 60s cooldown between codes  
**Response**: `ResendVerificationResponse(success, message)`  
**Side effects**: Invalidates old tokens, generates new 6-digit code (15 min expiry), sends email  
**Tables**: `verification_tokens`

### 6.7 Auth — User Profile
**Route**: `GET /api/v2/users/me`  
**Auth**: Yes  
**Response**: `UserMeResponse(userId, username, email, emailVerified, tier, createdAt, riotAccounts[])`  
**Resolves**: User + all linked Riot accounts via junction table, includes rank data, sync status, profile icons  
**Tables**: `users`, `user_riot_accounts`, `riot_accounts`

### 6.8 Auth — Riot Accounts (CRUD)
**Base route**: `/api/v2/users/me/riot-accounts`  
**Auth**: Yes  

| Method | Sub-route | Purpose | External API |
|--------|-----------|---------|-------------|
| `POST` | `/` | Link new account (gameName, tagLine, region) | Riot API: Account, Summoner, League |
| `DELETE` | `/{puuid}` | Unlink account | None |
| `POST` | `/{puuid}/sync` | Trigger match sync (sets status=pending) | None |
| `GET` | `/{puuid}/sync-status` | Check sync progress | None |

**Tables**: `riot_accounts`, `user_riot_accounts`

### 6.9 Overview Dashboard
**Route**: `GET /api/v2/overview/{userId}`  
**Auth**: Yes  
**Query params**: None  
**Response**: `OverviewResponse(playerHeader, lastMatch, activeGoals[], suggestedActions[])`  
**Logic**:
1. Get primary Riot account
2. Build player header (name, level, region, icon, contexts)
3. Build rank metadata in player header
4. Get latest match and overview cards
**Tables**: `users`, `user_riot_accounts`, `riot_accounts`  
**Repos**: `IOverviewStatsRepository`, `IUserRiotAccountsRepository`

### 6.10 Solo Dashboard
**Route**: `GET /api/v2/solo/dashboard/{userId}`  
**Auth**: Yes  
**Query params**: `?queueType=` (ranked_solo|ranked_flex|normal|aram|all), `?timeRange=` (7d|14d|30d|60d|90d|season|all)  
**Response**: `SoloPerformanceResponse` (see DTOs section for full shape)  
**Logic**: Single query returning aggregated solo stats — win rate, KDA, side stats, champion pool, recent trends, phase performance, role breakdown, death efficiency  
**Tables**: `matches`, `participants`, `participant_metrics`, `participant_checkpoints`  
**Repos**: `ISoloPerformanceRepository`, `IUserRiotAccountsRepository`

### 6.11 Solo Matchups
**Route**: `GET /api/v2/solo/matchups/{userId}`  
**Auth**: Yes  
**Query params**: `?queueType=`, `?timeRange=`  
**Response**: `ChampionMatchupsResponse(matchups[], queueType, timeRange)`  
**Logic**: Grouped by your champion+role, with per-opponent win/loss data (in-lane vs out-of-lane)  
**Tables**: `matches`, `participants`  
**Repos**: `IMatchupRepository`

### 6.12 Match Activity Heatmap
**Route**: `GET /api/v2/solo/activity/{userId}`  
**Auth**: Yes  
**Response**: `MatchActivityResponse(dailyMatchCounts, startDate, endDate, totalMatches)`  
**Logic**: Returns daily match counts for past 182 days  
**Tables**: `matches`, `participants`  
**Repos**: `ITrendRepository`

### 6.13 Match List
**Route**: `GET /api/v2/matches/{userId}`  
**Auth**: Yes  
**Query params**: `?queueType=`  
**Response**: `MatchListResponse(matches[], baselinesByRole, queueType, totalMatches)` (limit 20)  
**Logic**: Gets role baselines first, then match summaries with baseline comparisons  
**Tables**: `matches`, `participants`, `participant_metrics`, `participant_checkpoints`  
**Repos**: `IMatchesRepository`

### 6.14 Match Details
**Route**: `GET /api/v2/matches/{matchId}/details`  
**Auth**: Yes  
**Query params**: `?puuid=`  
**Validation**: Verifies puuid ownership via junction table  
**Response**: `MatchDetailsResponse(match, baseline)`  
**Tables**: `matches`, `participants`, `participant_metrics`, `participant_checkpoints`  
**Repos**: `IMatchesRepository`

### 6.15 Match Narrative
**Route**: `GET /api/v2/matches/{matchId}/narrative`  
**Auth**: Yes  
**Query params**: `?puuid=`  
**Response**: `MatchNarrativeResponse(matchId, userRole, laneMatchups[], isAram)`  
**Logic**: Gets all 10 participants, creates 5 lane matchups (by role). Lane winner determined by gold diff at 10 min (±300g threshold). ARAM: pairs by damage share rank.  
**Tables**: `matches`, `participants`, `participant_metrics`, `participant_checkpoints`  
**Repos**: `IMatchesRepository`

### 6.16 Winrate Trend
**Route**: `GET /api/v2/trends/winrate/{userId}`  
**Auth**: Yes  
**Query params**: `?queueType=`, `?timeRange=`, `?limit=` (max 500)  
**Response**: `WinrateTrendResponse(winrateTrend[])`  
**Repos**: `ITrendRepository`

### 6.18 Champion Select
**Route**: `GET /api/v2/champion-select/{userId}`  
**Auth**: Yes  
**Query params**: `?queueType=`, `?timeRange=`  
**Response**: `ChampionSelectResponse(mainChampions[], gamesPlayed, winRate)`  
**Repos**: `IChampionSelectRepository`

### 6.19 Analytics
**Routes**: `POST /api/v2/analytics`, `POST /api/v2/analytics/batch`  
**Auth**: Optional (captures userId if authenticated)  
**Validation**: eventName required, max 100 chars. Payload max 4KB. Batch max 50 events.  
**Response**: `TrackEventResponse(success)` / `TrackBatchResponse(success, count)`  
**Design**: Never fails the user experience — errors return `Ok(success: false)`  
**Tables**: `analytics_events`

### 6.20 Feedback
**Route**: `POST /api/v2/feedback`  
**Auth**: Optional  
**Rate limit**: 5 / hour / IP  
**Request body**: `FeedbackRequest(type, summary, details?, route?, environment?, browser?, os?)`  
**Validation**: type must be "bug" or "feature", summary required  
**Response**: `FeedbackResponse(success, message)`  
**Side effects**: Creates GitHub issue via `IGitHubService`

### 6.21 Diagnostics
**Route**: `GET /api/v2/diagnostics`  
**Auth**: None  
**Response**: System health check — config status, metrics counters, environment info

### 6.22 Public Stats
**Route**: `GET /api/v2/public/stats`  
**Auth**: None  
**Rate limit**: 60 / min / IP  
**Response**: `{ totalMatches, activePlayers }`

---

## 7. Response DTOs (Complete)

### Auth DTOs

```csharp
// LoginDto.cs
public record LoginRequest(string Username, string Password, string? ConsentLevel = null);
public record LoginResponse(long UserId, string Username, string Email, bool EmailVerified, string Tier, string Message);

// RegisterDto.cs
public record RegisterRequest(string Username, string Email, string Password);
public record RegisterResponse(long UserId, string Username, string Email, bool EmailVerified, string Message);

// LogoutDto.cs
public record LogoutResponse(string Message);

// DeleteAccountDto.cs
public record DeleteAccountRequest(string Password);
public record DeleteAccountResponse(bool Success, string Message);
```

### Analytics DTOs

```csharp
// AnalyticsDto.cs
public record TrackEventRequest(string EventName, Dictionary<string, object>? Payload = null, string? SessionId = null);
public record TrackEventResponse(bool Success, long? EventId = null);
public record TrackBatchRequest(TrackEventRequest[] Events);
public record TrackBatchResponse(bool Success, int Count);
```

### Feedback DTOs

```csharp
// FeedbackDto.cs
public record FeedbackRequest(string Type, string Summary, string? Details, string? Route, string? Environment, string? Browser, string? Os);
public record FeedbackResponse(bool Success, string Message);
```

### Overview DTOs

```csharp
// OverviewDto.cs
public record OverviewResponse(
    PlayerHeader PlayerHeader,
    LastMatch? LastMatch,
    GoalPreview[] ActiveGoals,
    SuggestedAction[] SuggestedActions
);

public record PlayerHeader(
    string SummonerName,
    int Level,
    string Region,
    string ProfileIconUrl,
    string[] ActiveContexts,
    string? Rank,
    int? Lp,
    string? PrimaryQueueLabel
);

public record LastMatch(string MatchId, string ChampionIconUrl, string ChampionName, string Result, string Kda, long Timestamp, string QueueType);
public record GoalPreview(string GoalId, string Title, string Context, double Progress);
public record SuggestedAction(string ActionId, string Text, string DeepLink, int Priority);
```

### Solo Performance DTOs

```csharp
// SoloPerformanceDto.cs
public record SoloPerformanceResponse(
    int GamesPlayed, int Wins, double WinRate, double AvgKda, double AvgGameDurationMinutes,
    double AvgKills, double AvgDeaths, double AvgAssists,
    double OverallWinRate, double OverallAvgKills, double OverallAvgDeaths, double OverallAvgAssists, double OverallAvgKda,
    SideWinDistribution SideStats,
    int UniqueChampsPlayedCount,
    ChampionSummary? MainChampion,
    MainChampionRoleGroup[] MainChampions,
    TrendMetric? Last10Games,
    TrendMetric? Last20Games,
    PerformancePhase[] PerformanceByPhase,
    RolePerformance[] RoleBreakdown,
    DeathEfficiency DeathEfficiency,
    string QueueType
);

public record SideWinDistribution(int BlueWins, int RedWins, int BlueGames, int RedGames, int TotalGames, double BlueWinDistribution, double RedWinDistribution);
public record ChampionSummary(int ChampionId, string ChampionName, int Picks, double WinRate, double PickRate);
public record TrendMetric(int Games, int Wins, double WinRate, double AvgKda, double AvgKills, double AvgDeaths, double AvgAssists);
public record PerformancePhase(string Phase, int Games, int Wins, double WinRate, double AvgKda, double AvgGoldPerMin, double AvgDamagePerMin);
public record RolePerformance(string Role, int GamesPlayed, int Wins, double WinRate, double AvgKda);
public record DeathEfficiency(int DeathsPre10, int Deaths10To20, int Deaths20To30, int Deaths30Plus, double? AvgFirstDeathMinute, double? AvgFirstKillParticipationMinute);
```

### Solo Matchups DTOs

```csharp
// SoloMatchupsDto.cs
public record ChampionMatchupsResponse(ChampionMatchup[] Matchups, string QueueType, string TimeRange);
public record ChampionMatchup(int ChampionId, string ChampionName, string Role, int TotalGames, int Wins, double WinRate, OpponentMatchup[] Opponents);
public record OpponentMatchup(int OpponentChampionId, string OpponentChampionName, int InLaneWins, int InLaneLosses, int OutOfLaneWins, int OutOfLaneLosses);
```

### Main Champion DTOs

```csharp
// MainChampionDto.cs
public record MainChampionRoleGroup(string Role, MainChampionEntry[] Champions);
public record MainChampionEntry(string ChampionName, int ChampionId, string Role, double WinRate, int GamesPlayed, int Wins, int Losses, double MScore);
```

### Match DTOs

```csharp
// MatchListDto.cs
public record MatchListResponse(MatchListSummaryItem[] Matches, Dictionary<string, RoleBaseline> BaselinesByRole, string QueueType, int TotalMatches);
public record MatchDetailsResponse(MatchDetailsItem Match, RoleBaseline? Baseline);

// MatchNarrativeDto.cs
public record MatchNarrativeResponse(string MatchId, string UserRole, LaneMatchup[] LaneMatchups, bool IsAram = false);
public record LaneMatchup(string Role, MatchupParticipant AllyParticipant, MatchupParticipant EnemyParticipant, string LaneWinner);
public record MatchupParticipant(
    string Puuid, string SummonerName, int ChampionId, string ChampionName, string ChampionIconUrl,
    int TeamId, bool Win, int Kills, int Deaths, int Assists,
    int? GoldAt10, int? CsAt10, int? GoldDiffAt10, int? CsDiffAt10,
    int DeathsPre10, int SoloKills, double DamageShare, double KillParticipation,
    int VisionScore, int CreepScore, int GoldEarned
);

// MatchActivityDto.cs
public record MatchActivityResponse(Dictionary<string, int> DailyMatchCounts, string StartDate, string EndDate, int TotalMatches);
```

### Champion Select DTOs

```csharp
// ChampionSelectDto.cs
public record ChampionSelectResponse(MainChampionRoleGroup[] MainChampions, int GamesPlayed, double WinRate);
```

### Trend DTOs

```csharp
// TrendDto.cs
public record WinrateTrendPoint(int GameIndex, double WinRate, DateTime Timestamp);
public record WinrateTrendResponse(WinrateTrendPoint[] WinrateTrend);
```

> **Note**: All DTOs use `[JsonPropertyName("camelCase")]` attributes. Shown without for readability.

---

## 8. Core Entities

All entities extend `EntityBase` (provides `ToJson()` and `ToString()`).

### User
```csharp
// server/Mongoose.Api/Core/Entities/User.cs
public class User : EntityBase
{
    public long UserId { get; set; }
    public string Email { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; }     // BCrypt
    public bool EmailVerified { get; set; }
    public bool IsActive { get; set; } = true;
    public string Tier { get; set; } = "free";   // "free" | "pro"
    public string? MollieCustomerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
```

### RiotAccount
```csharp
// server/Mongoose.Api/Core/Entities/RiotAccount.cs
public class RiotAccount : EntityBase
{
    public string Puuid { get; set; }             // PK — Riot PUUID (78 chars)
    public string GameName { get; set; }
    public string TagLine { get; set; }
    public string SummonerName { get; set; }      // "GameName#TagLine"
    public string Region { get; set; }            // na1, euw1, kr, etc.
    public string? SummonerId { get; set; }
    public string SyncStatus { get; set; } = "pending";  // pending|syncing|completed|failed
    public int SyncProgress { get; set; }
    public int SyncTotal { get; set; }
    public int? ProfileIconId { get; set; }
    public int? SummonerLevel { get; set; }
    public string? SoloTier { get; set; }         // IRON..CHALLENGER
    public string? SoloRank { get; set; }         // I..IV
    public int? SoloLp { get; set; }
    public string? FlexTier { get; set; }
    public string? FlexRank { get; set; }
    public int? FlexLp { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### UserRiotAccountLink (Junction Table)
```csharp
// M:M relationship — one user can link multiple Riot accounts, one Riot account can be linked by multiple users
public class UserRiotAccountLink : EntityBase
{
    public long UserId { get; set; }
    public string Puuid { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime LinkedAt { get; set; }
}
```

### Match
```csharp
public class Match : EntityBase
{
    public string MatchId { get; set; }           // Riot match ID (e.g., "NA1_12345")
    public int QueueId { get; set; }              // 420=ranked_solo, 440=ranked_flex, etc.
    public int GameDurationSec { get; set; }
    public long GameStartTime { get; set; }       // Unix epoch ms
    public string PatchVersion { get; set; }
    public string? SeasonCode { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### Participant
```csharp
public class Participant : EntityBase
{
    public long Id { get; set; }                  // PK
    public string MatchId { get; set; }           // FK → matches
    public string Puuid { get; set; }
    public int TeamId { get; set; }               // 100 (blue) or 200 (red)
    public string? Role { get; set; }             // TOP, JUNGLE, MIDDLE, BOTTOM, UTILITY
    public string? Lane { get; set; }
    public int ChampionId { get; set; }
    public string ChampionName { get; set; }
    public bool Win { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Assists { get; set; }
    public int CreepScore { get; set; }
    public int GoldEarned { get; set; }
    public int TimeDeadSec { get; set; }
    public int? LpAfter { get; set; }
    public string? TierAfter { get; set; }
    public string? RankAfter { get; set; }
    public DateTime CreatedAt { get; set; }
}
// UNIQUE(match_id, puuid)
```

### ParticipantCheckpoint (Timeline Data)
```csharp
public class ParticipantCheckpoint : EntityBase
{
    public long Id { get; set; }
    public long ParticipantId { get; set; }       // FK → participants
    public int MinuteMark { get; set; }           // 10, 15, 20, etc.
    public int Gold { get; set; }
    public int Cs { get; set; }
    public int Xp { get; set; }
    public int? GoldDiffVsLane { get; set; }
    public int? CsDiffVsLane { get; set; }
    public bool? IsAhead { get; set; }
    public DateTime CreatedAt { get; set; }
}
// UNIQUE(participant_id, minute_mark)
```

### ParticipantMetric (Derived Stats)
```csharp
public class ParticipantMetric : EntityBase
{
    public long Id { get; set; }
    public long ParticipantId { get; set; }       // FK → participants, UNIQUE
    public decimal KillParticipationPct { get; set; }
    public decimal DamageSharePct { get; set; }
    public int DamageDealt { get; set; }
    public int DamageTaken { get; set; }
    public int DamageMitigated { get; set; }
    public int VisionScore { get; set; }
    public decimal VisionPerMin { get; set; }
    public int DeathsPre10 { get; set; }
    public int Deaths10To20 { get; set; }
    public int Deaths20To30 { get; set; }
    public int Deaths30Plus { get; set; }
    public int? FirstDeathMinute { get; set; }
    public int? FirstKillParticipationMinute { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### ParticipantObjective
```csharp
public class ParticipantObjective : EntityBase
{
    public long Id { get; set; }
    public long ParticipantId { get; set; }       // UNIQUE
    public int DragonsParticipated { get; set; }
    public int HeraldsParticipated { get; set; }
    public int BaronsParticipated { get; set; }
    public int TowersParticipated { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### Team-Level Entities
```csharp
// TeamObjective — UNIQUE(match_id, team_id)
public class TeamObjective : EntityBase
{
    public long Id { get; set; }
    public string MatchId { get; set; }
    public int TeamId { get; set; }               // 100 or 200
    public int DragonsTaken { get; set; }
    public int HeraldsTaken { get; set; }
    public int BaronsTaken { get; set; }
    public int TowersTaken { get; set; }
}

// TeamMatchMetric — UNIQUE(match_id, team_id)
public class TeamMatchMetric : EntityBase
{
    public long Id { get; set; }
    public string MatchId { get; set; }
    public int TeamId { get; set; }
    public int? GoldLeadAt15 { get; set; }
    public int? LargestGoldLead { get; set; }
    public int? GoldSwingPost20 { get; set; }
    public bool? WinWhenAheadAt20 { get; set; }
}

// TeamRoleResponsibility — UNIQUE(match_id, team_id, role)
public class TeamRoleResponsibility : EntityBase
{
    public long Id { get; set; }
    public string MatchId { get; set; }
    public int TeamId { get; set; }
    public string Role { get; set; }
    public decimal DeathsSharePct { get; set; }
    public decimal GoldSharePct { get; set; }
    public decimal DamageSharePct { get; set; }
}
```

### DuoMetric
```csharp
public class DuoMetric : EntityBase
{
    public long Id { get; set; }
    public string MatchId { get; set; }
    public long ParticipantId1 { get; set; }
    public long ParticipantId2 { get; set; }
    public int? EarlyGoldDelta10 { get; set; }
    public int? EarlyGoldDelta15 { get; set; }
    public decimal? AssistSynergyPct { get; set; }
    public decimal? SharedObjectiveParticipationPct { get; set; }
    public bool? WinWhenAheadAt15 { get; set; }
}
```

### Supporting Entities
```csharp
// AiSnapshot — AI-generated performance summaries
public class AiSnapshot : EntityBase
{
    public long Id { get; set; }
    public string Puuid { get; set; }
    public string ContextType { get; set; } = "solo";  // solo|duo|team
    public string? ContextPuuidsJson { get; set; }
    public int? QueueId { get; set; }
    public string SummaryText { get; set; }
    public string? GoalsJson { get; set; }
    public DateOnly SnapshotDate { get; set; }
}

// Season, Subscription, SubscriptionEvent, VerificationToken, AnalyticsEvent
// (See schema.sql for complete definitions)
```

---

## 9. Repository Interfaces

### Data Access Pattern
- All repositories are registered as **Scoped** services
- Use `MySqlConnector` with parameterized queries (no ORM)
- Connection obtained via `IDbConnectionFactory`
- All write operations use `Upsert` pattern where applicable (INSERT ... ON DUPLICATE KEY UPDATE)

### Key Repository Interfaces

```csharp
// Match data
public interface IMatchesRepository
{
    Task UpsertAsync(Match match);
    Task<long> GetTotalMatchCountAsync();
    Task<IList<MatchListSummaryItem>> GetMatchListSummaryAsync(string puuid, string queueFilter, int limit = 20, Dictionary<string, RoleBaseline>? baselines = null);
    Task<MatchDetailsItem?> GetMatchDetailsAsync(string matchId, string puuid);
    Task<Dictionary<string, RoleBaseline>> GetRoleBaselinesAsync(string puuid, string queueFilter);
    Task<IList<MatchupParticipantRaw>> GetMatchParticipantsAsync(string matchId);
    Task<int> DeleteOldMatchesAsync(long cutoffTimestamp, int batchSize);
}

// Overview stats
public interface IOverviewStatsRepository
{
    Task<(int QueueId, string QueueLabel, int MatchCount)> GetPrimaryQueueAsync(string puuid);
    Task<List<MatchResultData>> GetLast20MatchesAsync(string puuid, int queueId);
    Task<LastMatchData?> GetLastMatchAsync(string puuid);
    Task<int?> GetCurrentLpAsync(string puuid, int queueId);
}

// Solo performance (returns full dashboard DTO)
public interface ISoloPerformanceRepository
{
    Task<SoloPerformanceResponse?> GetSoloPerformanceAsync(string puuid, string? queueType = null, string? timeRange = null);
}

// Champion matchups
public interface IMatchupRepository
{
    Task<ChampionMatchupsResponse> GetChampionMatchupsAsync(string puuid, string? queueType = null, string? timeRange = null);
}

// Trend data — one method pair per solo trend chart, plus the activity heatmap query
public interface ITrendRepository
{
    Task<WinrateTrendPoint[]> GetWinrateTrendAsync(string puuid, string? queueType = null, string? timeRange = null, int? limit = null);
    Task<CsPerMinuteTrendPoint[]> GetCsPerMinuteTrendAsync(string puuid, string? queueType = null, string? timeRange = null, int? limit = null);
    Task<(DeathsTrendPoint[] DataPoints, double AverageDeaths, double OverallAverage, string Trend)> GetDeathsTrendAsync(string puuid, string? queueType = null, string? timeRange = null, int? limit = null);
    Task<(DragonParticipationTrendPoint[] DataPoints, double AverageParticipation, double OverallAverage, string Trend)> GetDragonParticipationTrendAsync(string puuid, string? queueType = null, string? timeRange = null, int? limit = null);
    Task<(VisionScoreTrendPoint[] DataPoints, double AverageVisionPerMinute, double OverallAverage, double RoleTarget, string Trend)> GetVisionScoreTrendAsync(string puuid, string? queueType = null, string? timeRange = null, int? limit = null);
    Task<Dictionary<string, int>> GetDailyMatchCountsAsync(string puuid, int daysBack = 91);
}

// Champion select
public interface IChampionSelectRepository
{
    Task<ChampionSelectResponse?> GetChampionSelectDataAsync(string puuid, string? queueType = null, string? timeRange = null);
}

// Death position heatmap (solo/death-positions)
public interface IDeathPositionsRepository
{
    Task<DeathPositionsResult?> GetDeathPositionsAsync(string puuid, string? queueType = null, string? timeRange = null);
    Task<DeathPositionsResult?> GetDeathPositionsAsync(IReadOnlyList<string> puuids, string? queueType = null, string? timeRange = null);
}

// Radar chart (solo/radar-chart)
public interface IRadarChartRepository
{
    Task<RadarChartResponse?> GetRadarChartAsync(string puuid, string? queueType = null, string? timeRange = null);
    Task<RadarChartResponse?> GetRadarChartAsync(IReadOnlyList<string> puuids, string? queueType = null, string? timeRange = null);
}

// User ↔ Riot account (junction table M:M)
public interface IUserRiotAccountsRepository
{
    Task LinkAsync(long userId, string puuid, bool isPrimary);
    Task UnlinkAsync(long userId, string puuid);
    Task<bool> IsLinkedAsync(long userId, string puuid);
    Task<IList<(UserRiotAccountLink Link, RiotAccount Account)>> GetByUserIdAsync(long userId);
    Task<IList<long>> GetUserIdsByPuuidAsync(string puuid);
    Task SetPrimaryAsync(long userId, string puuid);
    Task<(UserRiotAccountLink Link, RiotAccount Account)?> GetPrimaryByUserIdAsync(long userId);
    Task<bool> HasAnyLinksAsync(string puuid);
    Task<int> GetLinkCountAsync(string puuid);
}

// Users
public interface IUsersRepository
{
    Task<long> UpsertAsync(User user);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(long userId);
    Task<User?> GetByUsernameAsync(string username);
    Task<bool> UsernameExistsAsync(string username);
    Task<bool> EmailExistsAsync(string email);
    Task<long> GetActiveUserCountAsync();
    Task UpdateEmailVerifiedAsync(long userId, bool verified);
    Task UpdateUserIconIdAsync(long userId, int? userIconId);
    Task UpdatePasswordHashAsync(long userId, string passwordHash);
    Task<string?> GetSecurityStampAsync(long userId);
    Task<bool> DeleteUserAsync(long userId);
}

// Social sign-on identities (Riot Sign-On, Google Sign-On, ...). Generic
// (provider, providerUid) mapping to a user_id — new providers need no
// schema or interface change, just a new "provider" string.
public interface IUserIdentityProvidersRepository
{
    Task<long?> GetUserIdByProviderIdentityAsync(string provider, string providerUid);
    Task LinkProviderIdentityAsync(long userId, string provider, string providerUid);
}

// Verification tokens (email verification, password reset)
public interface IVerificationTokensRepository
{
    Task<long> CreateTokenAsync(long userId, string tokenType, string code, DateTime expiresAt);
    Task<VerificationToken?> GetActiveTokenAsync(long userId, string tokenType);
    Task MarkTokenAsUsedAsync(long tokenId);
    Task IncrementAttemptsAsync(long tokenId);
    Task<int> CountRecentTokensAsync(long userId, string tokenType, int seconds);
    Task InvalidateActiveTokensAsync(long userId, string tokenType);
}

// Riot Sign-On OAuth code exchange (Infrastructure/Riot/RiotSignOnClient.cs)
public interface IRiotSignOnClient
{
    Task<RiotSignOnIdentity> ExchangeCodeAsync(string code, CancellationToken ct = default);
}

// Subscriptions (Mollie billing — see subscriptions/subscription_events tables)
public interface ISubscriptionsRepository
{
    Task<long> UpsertAsync(Subscription subscription);
    Task<Subscription?> GetByUserIdAsync(long userId);
}

public interface ISubscriptionEventsRepository
{
    Task<long> InsertAsync(SubscriptionEvent ev);
    Task<IList<SubscriptionEvent>> GetBySubscriptionIdAsync(long subscriptionId);
}

// Riot accounts
public interface IRiotAccountsRepository
{
    Task UpsertAsync(RiotAccount account);
    Task<RiotAccount?> GetByPuuidAsync(string puuid);
    Task DeleteAsync(string puuid);
    Task UpdateSyncStatusAsync(string puuid, string syncStatus, DateTime? lastSyncAt = null);
    Task<RiotAccount?> ClaimNextPendingForSyncAsync();
    Task UpdateSyncProgressAsync(string puuid, int progress, int total);
    Task UpdateProfileDataAsync(string puuid, int? profileIconId, int? summonerLevel);
    Task UpdateRankDataAsync(string puuid, string? summonerId, string? soloTier, string? soloRank, int? soloLp, string? flexTier, string? flexRank, int? flexLp);
}

// Analytics event pipeline v2 (analytics_events_v2 table — see database-schema.spec.md §10)
public interface IAnalyticsEventsV2Repository
{
    Task<long> InsertAsync(AnalyticsEventV2 evt);
    Task<int> InsertBatchAsync(IEnumerable<AnalyticsEventV2> events);
    Task<long> GetEventCountAsync(string eventName, DateTime from, DateTime to, bool includeRejected = false);
    Task<long> GetUniqueUserCountAsync(string eventName, DateTime from, DateTime to);
    Task<long> GetAcceptedEventCountAsync(DateTime from, DateTime to);
    Task<Dictionary<string, long>> GetRejectionsByReasonAsync(DateTime from, DateTime to);
    Task<double> GetAcceptanceRateAsync(DateTime from, DateTime to);
    Task<int> DeleteOlderThanAsync(DateTime cutoffDate);
    Task<Dictionary<string, long>> GetEventDistributionByCategoryAsync(DateTime from, DateTime to);
}

// Phase 3 analytics — exploration dimensions, journey flows, funnel tracking
// (Core/Interfaces/IAnalyticsPhase3Repositories.cs; backs the /analytics/explore, /analytics/journey,
// /analytics/funnels admin routes in §5). Three interfaces, trimmed to representative methods:
public interface IAnalyticsEventDimensionsRepository
{
    Task InsertDimensionAsync(AnalyticsEventDimension dimension);
    Task<IEnumerable<DimensionValue>> GetDimensionValuesAsync(string dimensionName, DateTime startUtc, DateTime endUtc, int limit = 50);
    Task<EventDimensionDetail?> GetEventDetailAsync(string eventName, DateTime startUtc, DateTime endUtc);
}
public interface IAnalyticsJourneyRepository
{
    Task InsertJourneyStepAsync(AnalyticsJourneyStep step);
    Task<IEnumerable<NavigationFlow>> GetTopFlowsAsync(DateTime startUtc, DateTime endUtc, int minTransitions = 5, int limit = 50);
    Task<IEnumerable<PathSequence>> GetPathSequencesAsync(string startEvent, DateTime startUtc, DateTime endUtc, int maxSteps = 5, int limit = 100);
}
public interface IAnalyticsFunnelRepository
{
    Task InsertFunnelStepAsync(AnalyticsFunnelStep step);
    Task<AnalyticsFunnelDefinition?> GetFunnelDefinitionAsync(string funnelName);
    Task<FunnelAnalysis> AnalyzeFunnelAsync(string funnelName, DateTime startUtc, DateTime endUtc, string? tierFilter = null);
}

// Event schema validation (Infrastructure/Telemetry/EventSchemaRegistry.cs, backs /analytics/schema)
public interface IEventSchemaRegistry
{
    Task ReloadAsync();
    // + schema lookup methods used by IEventValidator during ingest
}

// Query filter builder
public record TimeRangeFilter(DateTime? TimeRangeStart, string? SeasonCode, string NormalizedTimeRange);
public interface IQueryFilterBuilder
{
    string ValidateQueueType(string? queueType);
    string BuildQueueFilter(string queueType);
    Task<TimeRangeFilter> ResolveTimeRangeAsync(string? timeRange);
    string BuildTimeRangeFilter(TimeRangeFilter filter);
    void AddTimeRangeParameters(MySqlCommand cmd, TimeRangeFilter filter);
}

// Rate limiting
public record RateLimitResult(bool IsAllowed, int RemainingRequests, TimeSpan? RetryAfter);
public interface IRateLimiter
{
    Task<RateLimitResult> CheckAsync(string key, int limit, TimeSpan window);
    Task<RateLimitResult> CheckEndpointAsync(string endpointName, string? ipAddress, long? userId, int limit, TimeSpan window);
}
```

---

## 10. Application Services

### LoginSyncService
**File**: `server/Mongoose.Api/Application/Services/LoginSyncService.cs`  
**Purpose**: Refreshes Riot account data on login (fire-and-forget from LoginEndpoint)  
**Dependencies**: `RiotAccountsRepository`, `IUserRiotAccountsRepository`, `IRiotApiClient`, `ISyncProgressBroadcaster`

**Flow**:
1. `CheckAccountsOnLoginAsync(userId)` — gets all linked accounts via junction table
2. For each account: `UpdateProfileDataAsync` → Riot API (summoner data + league entries)
3. `CheckForNewMatchesAsync` → gets match IDs since last sync (or 30 days ago)
4. If new matches found: sets `sync_status='pending'`, broadcasts via WebSocket

**Sync cooldown**: 5 minutes between syncs for the same account.

### PuuidResolutionService
**File**: `server/Mongoose.Api/Application/Services/PuuidResolutionService.cs`  
**Purpose**: Centralizes primary-Riot-account resolution that was duplicated across 15+ endpoints  
**Dependencies**: `IUserRiotAccountsRepository`, `IUsersRepository`  
**Returns**: `ResolvedAccount(RiotAccount Account, bool IsPrimary, string AccountId)` — resolves the user's primary linked account, falling back to the first linked account if none is marked primary.

### AuthorizationHelper
**File**: `server/Mongoose.Api/Application/Services/AuthorizationHelper.cs`  
**Purpose**: Shared static helpers so every route-scoped endpoint enforces "authenticated AND userId in route matches the session" the same way, instead of each endpoint re-implementing the check from [§3 User→RiotAccount Resolution Pattern](#user-riotaccount-resolution-pattern)  
**Key method**: `ValidateAuthenticatedUser(HttpContext, string? userIdParam, ILogger)` → returns an `IResult` 401/403/400 error, or `null` on success (then call `GetAuthorizedUser` for the validated `AuthorizedUser(long UserId, string? Username)`).

### MainChampionRecommender
**File**: `server/Mongoose.Api/Core/Services/MainChampionRecommender.cs` — lives in Core, not Application, since it's pure domain scoring logic with no orchestration dependencies (consistent with §1's Core Layer description)  
**Purpose**: Scores and ranks champions per role using the MScore algorithm  
**Max champions per role**: 3

**MScore Algorithm**:
1. **Confidence** = 0.25 + 0.75 × (games/20), capped at 1.0
2. **WinRate** = normalized 35%-65% → [0, 1]
3. **KDA** = (kills + assists) / max(1, deaths), normalized to /5.0
4. **Laning score** (role-specific):
   - UTILITY: 35% goldDiff + 25% earlyDeaths + 40% vision
   - JUNGLE: 20% goldDiff + 50% earlyDeaths + 30% vision
   - Laners: 50% goldDiff + 40% earlyDeaths + 10% vision
5. With laning data: **50% winRate + 30% laning + 20% KDA**
6. Without laning: **60% winRate + 40% KDA**
7. **MScore** = performance × confidence × 100 (0-100 scale)

---

## 11. Query Filtering System

### Queue Type Filter
**Valid values**: `ranked_solo`, `ranked_flex`, `normal`, `aram`, `all` (default)

Queue ID mapping:
| queueType | Queue ID |
|-----------|----------|
| ranked_solo | 420 |
| ranked_flex | 440 |
| normal | 400, 430 |
| aram | 450 |
| all | (no filter) |

### Time Range Filter
**Valid values**: `7d`, `14d`, `30d`, `60d`, `90d`, `season`, `all` (default)

- Numeric ranges compute `DateTime.UtcNow - TimeSpan.FromDays(N)`
- `season` resolves to current season start date via `ISeasonsRepository`
- `all` applies no time filter

### Usage in Endpoints
```csharp
var queueType = filterBuilder.ValidateQueueType(queueTypeParam);
var queueFilter = filterBuilder.BuildQueueFilter(queueType);
var timeRange = await filterBuilder.ResolveTimeRangeAsync(timeRangeParam);
```

---

## 12. Error Response Contract

All error responses follow consistent shapes defined in `AuthResults.cs` and individual endpoints:

### Authentication Errors (401)
```json
{ "error": "Your session has expired. Please log in again.", "code": "SESSION_EXPIRED" }
{ "error": "Authentication required.", "code": "NOT_AUTHENTICATED" }
{ "error": "Invalid username or password", "code": "INVALID_CREDENTIALS" }
{ "error": "This account has been deactivated", "code": "ACCOUNT_DEACTIVATED" }
```

### Authorization Errors (403)
```json
{ "error": "Access denied.", "code": "FORBIDDEN" }
```

### Validation Errors (400)
```json
{ "error": "descriptive message about what's wrong" }
```

### Rate Limit Errors (429)
```json
{ "error": "Too many requests. Please try again later.", "retryAfter": 300 }
```

### Not Found (404)
```json
{ "error": "No linked Riot account" }
{ "error": "Match not found" }
```

---

## 13. WebSocket — Real-time Sync

**Endpoint**: `WS /ws/sync`  
**Auth**: Session cookie (rejects unauthenticated with close code 1008)  
**Implementation**: `SyncProgressHub` (raw WebSocket, in `Infrastructure/WebSocket/`)

### Client → Server Messages
```json
{ "type": "subscribe", "puuid": "abc123..." }
{ "type": "unsubscribe", "puuid": "abc123..." }
```

### Server → Client Messages
```json
// Progress update
{ "type": "sync_progress", "puuid": "abc", "status": "syncing", "progress": 5, "total": 20, "matchId": "NA1_12345" }

// Completed
{ "type": "sync_complete", "puuid": "abc", "status": "completed", "totalSynced": 20 }

// Error
{ "type": "sync_error", "puuid": "abc", "status": "failed", "error": "Rate limited by Riot API" }
```

### Reconnection Strategy
Clients should implement exponential backoff: 1s → 2s → 4s → 8s → max 30s.

---

## 14. Planned Endpoints (Not Yet Implemented)

### Duo Dashboard
| Route | Purpose | Relevant Entities |
|-------|---------|-------------------|
| `GET /api/v2/duo/summary/{userId1}/{userId2}` | Duo overview, lane performance, role stats | `duo_metrics`, `participants` |
| `GET /api/v2/duo/performance/{userId1}/{userId2}` | Time-phased duo performance with kill/gold deltas | `duo_metrics`, `participant_checkpoints` |
| `GET /api/v2/duo/synergy/{userId1}/{userId2}` | Champion combo analysis, synergy scores | `participants`, `duo_metrics` |
| `GET /api/v2/duo/kills/{userId1}/{userId2}` | Kill analysis by phase, multi-kills | `participants`, `participant_metrics` |
| `GET /api/v2/duo/deaths/{userId1}/{userId2}` | Death patterns by phase and position | `participant_metrics` |
| `GET /api/v2/duo/vs-enemy/{userId1}/{userId2}` | Matchups against enemy duos | `participants`, `duo_metrics` |

**Database support**: `duo_metrics` table exists with: `early_gold_delta_10/15`, `assist_synergy_pct`, `shared_objective_participation_pct`, `win_when_ahead_at_15`. Repository interface `IDuoMetricsRepository` exists.

### Team Dashboard
| Route | Purpose | Relevant Entities |
|-------|---------|-------------------|
| `GET /api/v2/team/summary` | 5-player team overview, cohesion scores | `team_match_metrics`, `team_role_responsibility` |
| `GET /api/v2/team/performance` | Time-phased team performance | `team_match_metrics`, `participant_checkpoints` |
| `GET /api/v2/team/composition` | Team comp patterns, role flexibility | `participants` |
| `GET /api/v2/team/objectives` | Objective control (dragons, barons, towers) | `team_objectives`, `participant_objectives` |
| `GET /api/v2/team/synergy` | Offensive/defensive/utility synergy scores | derived from multiple tables |

**Database support**: `team_match_metrics`, `team_objectives`, `team_role_responsibility` tables exist. Repository interfaces `ITeamMatchMetricsRepository`, `ITeamObjectivesRepository`, `ITeamRoleResponsibilitiesRepository` exist.

### AI & Goals
| Route | Purpose | Relevant Entities |
|-------|---------|-------------------|
| `GET /api/v2/goals/recommendations/{userId}` | AI-generated improvement goals | `ai_snapshots` |
| `POST /api/v2/goals/{userId}` | Save goal | `ai_snapshots` |
| `GET /api/v2/goals/{userId}/progress` | Goal progress tracking | `ai_snapshots`, match data |

**Database support**: `ai_snapshots` table exists with `context_type` (solo/duo/team), `summary_text`, `goals_json`. Repository interface `IAiSnapshotsRepository` exists.

---

## 15. New Endpoint Implementation Checklist

When adding a new endpoint, follow this checklist:

### Files to Create/Modify

1. **DTO** — `server/Mongoose.Api/Application/DTOs/{Feature}/{Feature}Dto.cs`
   - Use `record` types with `[JsonPropertyName]` attributes
   - Static class wrapper (e.g., `public static class MyFeatureDto { ... }`)

2. **Endpoint** — `server/Mongoose.Api/Application/Endpoints/{Feature}/{Feature}Endpoint.cs`
   - Implement `IEndpoint` (Route property + Configure method)
   - Accept `basePath` in constructor
   - Use `.RequireAuthorization()` for protected routes
   - Follow the User→PUUID resolution pattern (see Section 3)

3. **Repository interface** — `server/Mongoose.Api/Core/Interfaces/I{Feature}Repository.cs`
   - Return DTOs or query models, not entities

4. **Repository implementation** — `server/Mongoose.Api/Infrastructure/Database/Repositories/{Feature}Repository.cs`
   - Use `IDbConnectionFactory`, `MySqlCommand`, parameterized queries
   - Register as Scoped in `Program.cs`

5. **Register in DI** — `server/Mongoose.Api/Program.cs`
   - `builder.Services.AddScoped<I{Feature}Repository, {Feature}Repository>();`

6. **Register endpoint** — `server/Mongoose.Api/Application/MongooseApiApplication.cs`
   - Add to endpoint list in `ConfigureEndpoints()`

7. **Tests** — `server/Mongoose.Api.Tests/{Feature}EndpointTests.cs`
   - Test auth, validation, happy path, error cases

### Security Checklist
- [ ] Input validation and sanitization
- [ ] SQL injection prevention (parameterized queries)
- [ ] Auth enforcement (`.RequireAuthorization()`)
- [ ] User-ownership validation (userId from claims matches route param)
- [ ] Rate limiting (if public or sensitive)
- [ ] Payload size limits (if accepting body)

### Testing Checklist
- [ ] Unauthenticated request → 401
- [ ] Wrong user's data → 403
- [ ] No Riot account linked → 404 with message
- [ ] Valid request → 200 with expected shape
- [ ] Invalid query params → 400 or defaults gracefully
- [ ] Empty data set → 200 with empty arrays/null fields (not 404)
