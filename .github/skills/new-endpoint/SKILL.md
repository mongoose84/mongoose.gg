---
name: new-endpoint
description: 'Scaffold a new API endpoint with DTO, repository interface, and integration test. Use when creating a new endpoint, adding a new API route, or implementing a new feature endpoint.'
argument-hint: 'Describe the endpoint (e.g., "GET solo champion stats for a user")'
---

# Create New Endpoint

Scaffolds the full vertical slice for a new Mongoose.gg API endpoint: endpoint class, DTO record, repository interface, repository implementation stub, and integration test.

This skill is the canonical home for backend endpoint scaffolding examples and vertical-slice checklists. Keep broad backend instructions terse and point here when full examples are needed.

## Prerequisites

Before starting, review:
- [Architecture Spec](../../specs/architecture.spec.md) — endpoint patterns, existing route map
- [Database Schema](../../specs/database-schema.spec.md) — available tables and columns
- `server/Mongoose.Api/CLAUDE.md` — coding patterns (auto-loaded when working there)

## Information Needed

Gather from the user:
1. **HTTP method** (GET, POST, PUT, DELETE)
2. **Route path** (e.g., `/solo/champion-stats/{userId}`)
3. **Auth required?** (almost always yes)
4. **Needs PUUID resolution?** (yes for any user-data endpoint)
5. **Query parameters** (e.g., `queueType`, `timeRange`)
6. **Response shape** (what data to return)
7. **Data source** (which tables / new query)

## Scaffold Steps

### Step 1 — Determine Domain Folder

Map the endpoint to an existing domain folder under `server/Mongoose.Api/Application/Endpoints/`:
- Auth routes → `Auth/`
- Solo dashboard data → `Solo/`
- Match data → `Matches/`
- Trend data → `Trends/`
- Overview data → `Overview/`
- New domain → create new folder

### Step 2 — Create the Endpoint Class

File: `server/Mongoose.Api/Application/Endpoints/{Domain}/{Name}Endpoint.cs`

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Core.Interfaces;

namespace Mongoose.Api.Application.Endpoints.{Domain};

public sealed class {Name}Endpoint : IEndpoint
{
    public string Route { get; }

    public {Name}Endpoint(string basePath)
    {
        Route = basePath + "/{route}/{userId}";
    }

    public void Configure(WebApplication app)
    {
        app.Map{Method}(Route, async (
            HttpContext httpContext,
            [FromRoute] string userId,
            [FromServices] I{Name}Repository repo,
            [FromServices] IUserRiotAccountsRepository userRiotAccountsRepo,
            [FromServices] ILogger<{Name}Endpoint> logger
        ) =>
        {
            // 1. Auth check
            if (httpContext.User?.Identity?.IsAuthenticated != true)
                return AuthResults.NotAuthenticated();

            // 2. Parse & validate
            if (!int.TryParse(userId, out var userIdInt))
            {
                logger.LogWarning("Invalid userId format {UserId}", LogSanitizer.Sanitize(userId));
                return Results.BadRequest(new { error = "Invalid userId format" });
            }

            // 3. Authorization
            var authenticatedUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (authenticatedUserId != userIdInt.ToString())
                return Results.Forbid();

            // 4. Resolve PUUID
            var linkedAccounts = await userRiotAccountsRepo.GetByUserIdAsync(userIdInt);
            if (linkedAccounts == null || linkedAccounts.Count == 0)
                return Results.NotFound(new { error = "No riot accounts found" });

            var primaryPuuid = linkedAccounts.FirstOrDefault(la => la.Link.IsPrimary)?.Account?.Puuid
                ?? linkedAccounts[0].Account.Puuid;

            // 5. Query & return
            var result = await repo.GetAsync(primaryPuuid);
            return Results.Ok(result);
        }).RequireAuthorization();
    }
}
```

Adapt: add `[FromQuery]` parameters, use `IQueryFilterBuilder` for queue/time filtering, adjust auth logic for public endpoints.

### Step 3 — Create the DTO Record

File: `server/Mongoose.Api/Application/DTOs/{Domain}/{Name}Response.cs`

```csharp
using System.Text.Json.Serialization;

namespace Mongoose.Api.Application.DTOs.{Domain};

public record {Name}Response(
    [property: JsonPropertyName("propertyName")] string PropertyName
);
```

All properties must use `[JsonPropertyName("camelCase")]`.

### Step 4 — Create the Repository Interface

File: `server/Mongoose.Api/Core/Interfaces/I{Name}Repository.cs`

```csharp
namespace Mongoose.Api.Core.Interfaces;

public interface I{Name}Repository
{
    Task<{ReturnType}?> GetAsync(string puuid);
}
```

### Step 5 — Create the Repository Implementation

File: `server/Mongoose.Api/Infrastructure/Database/Repositories/{Name}Repository.cs`

- Extend `RepositoryBase`
- Use `ExecuteSingleAsync<T>` / `ExecuteListAsync<T>` with raw SQL
- Parameterized queries only: `("@puuid", puuid)`

### Step 6 — Register in DI

Add to `server/Mongoose.Api/Program.cs` in the DI section:

```csharp
builder.Services.AddScoped<I{Name}Repository, {Name}Repository>();
```

### Step 7 — Create Integration Test

File: `server/Mongoose.Api.Tests/{Name}EndpointTests.cs`

Must include these test cases:
1. **Happy path** — authenticated user gets expected data
2. **Unauthenticated** — returns 401
3. **Forbidden** — user accessing another user's data returns 403
4. **Not found** — user without riot accounts returns 404

```csharp
using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Mongoose.Api.Tests;

[Collection("EnvIsolation")]
public class {Name}EndpointTests
{
    [Fact]
    public async Task {Name}_returns_401_when_not_authenticated()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/v2/{route}/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

Use `TestWebApplicationFactory`, `EnvironmentVariableScope`, and `AuthCookieTestHelper` — see existing tests for patterns.

## Validation Checklist

- [ ] Endpoint implements `IEndpoint` and is sealed
- [ ] Route follows `/api/v2/{domain}/{resource}/{userId}` convention
- [ ] PUUID resolved from `IUserRiotAccountsRepository` (never from client)
- [ ] All user input sanitized with `LogSanitizer.Sanitize()` before logging
- [ ] DTO is a record with `[JsonPropertyName]` on all properties
- [ ] Repository uses parameterized SQL (no string concatenation)
- [ ] Repository interface in `Core/Interfaces/`, implementation in `Infrastructure/Database/Repositories/`
- [ ] DI registration added in `Program.cs`
- [ ] Integration tests cover: auth, forbidden, not-found, happy path
- [ ] Build passes: `dotnet build` from `server/Mongoose.Api/`
- [ ] Tests pass: `dotnet test Mongoose.Api.Tests/` from `server/`
