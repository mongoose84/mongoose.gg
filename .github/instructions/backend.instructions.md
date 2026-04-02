---
applyTo: "**/*.cs"
description: "C# backend development guidelines with context engineering"
---
# C# Backend Development Guidelines

## Context Loading
Review these BEFORE starting:
- [Architecture Spec](../specs/architecture.spec.md) — Endpoint patterns, DTOs, repositories
- [Database Schema](../specs/database-schema.spec.md) — Table structure and relationships
- [Test Strategy](../specs/test-strategy.spec.md) — xUnit patterns and TestWebApplicationFactory usage
- [Server AGENTS.md](../../server/Mongoose.Api/AGENTS.md) — Build/run instructions and key patterns

## Critical Architecture Rules

### Clean Architecture Layers
```
Core/               # Entities, interfaces, enums (NO dependencies)
Application/        # Endpoints, DTOs, services (depends on Core only)
Infrastructure/     # Repos, Riot API, email, jobs (implements Core interfaces)
```

**Dependency Rule**: Dependencies point inward. Infrastructure → Application → Core.

### Endpoint Pattern (MANDATORY)

Every API endpoint MUST:
1. Implement `IEndpoint` interface
2. Be a sealed class in its own file
3. Be registered in `MongooseApiApplication.cs`
4. Follow the standard structure:

```csharp
public sealed class MyEndpoint : IEndpoint
{
    public string Route { get; }
    
    public MyEndpoint(string basePath)
    {
        Route = basePath + "/resource/{userId}";
    }
    
    public void Configure(WebApplication app)
    {
        app.MapGet(Route, async (
            HttpContext httpContext,
            [FromRoute] string userId,
            [FromQuery] string? queueType,
            [FromServices] IMyRepository repo,
            [FromServices] IUserRiotAccountsRepository userRiotAccountsRepo,
            [FromServices] ILogger<MyEndpoint> logger
        ) =>
        {
            // 1. Auth check
            if (httpContext.User?.Identity?.IsAuthenticated != true)
                return AuthResults.NotAuthenticated();
            
            // 2. Parse & validate route params
            if (!int.TryParse(userId, out var userIdInt))
            {
                logger.LogWarning("Invalid userId format {UserId}", LogSanitizer.Sanitize(userId));
                return Results.BadRequest(new { error = "Invalid userId format" });
            }
            
            // 3. Authorization — user can only access own data
            var authenticatedUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (authenticatedUserId != userIdInt.ToString())
            {
                logger.LogWarning("User {AuthUserId} attempted to access data for user {RouteUserId}",
                    LogSanitizer.Sanitize(authenticatedUserId), LogSanitizer.Sanitize(userIdInt.ToString()));
                return Results.Forbid();
            }
            
            // 4. Resolve PUUID from user → riot account link (CRITICAL PATTERN)
            var linkedAccounts = await userRiotAccountsRepo.GetByUserIdAsync(userIdInt);
            if (linkedAccounts == null || linkedAccounts.Count == 0)
            {
                logger.LogWarning("No riot accounts found for userId {UserId}", LogSanitizer.Sanitize(userIdInt.ToString()));
                return Results.NotFound(new { error = "No riot accounts found" });
            }
            
            var primaryPuuid = linkedAccounts.FirstOrDefault(la => la.Link.IsPrimary)?.Account?.Puuid
                ?? linkedAccounts[0].Account.Puuid;
            
            // 5. Query & return
            var result = await repo.GetDataAsync(primaryPuuid, queueType);
            return Results.Ok(result);
        }).RequireAuthorization();
    }
}
```

**CRITICAL**: Data endpoints must never accept PUUID as client input — always resolve PUUID server-side from User ID via `IUserRiotAccountsRepository`. Own-account management sub-routes may use PUUID as a URL sub-resource key when scoped to the authenticated user.

## DTOs and Records

### Use Immutable Records
```csharp
public record MyResponse(
    [property: JsonPropertyName("userId")] int UserId,
    [property: JsonPropertyName("winRate")] double WinRate,
    [property: JsonPropertyName("games")] int Games
);
```

**Rules**:
- ALL DTOs are records
- ALL properties use `[JsonPropertyName("camelCase")]`
- Use `?` for optional fields: `int?`, `double?`, `string?`
- DTOs organized by domain: `Application/DTOs/{Auth|Solo|Matches|Trends}/`

## Repository Pattern

### Extend RepositoryBase
```csharp
public class MyRepository : RepositoryBase, IMyRepository
{
    private readonly ILogger<MyRepository> _logger;
    
    public MyRepository(
        IDbConnectionFactory factory,
        ILogger<MyRepository> logger) : base(factory)
    {
        _logger = logger;
    }
    
    public async Task<MyData?> GetDataAsync(string puuid)
    {
        var sql = @"
            SELECT id, name, value
            FROM my_table
            WHERE puuid = @puuid";
        
        return await ExecuteSingleAsync(sql,
            reader => new MyData
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Value = reader.GetDouble(2)
            },
            ("@puuid", puuid));
    }
}
```

**Available Methods**:
- `ExecuteScalarAsync<T>` — single value
- `ExecuteSingleAsync<T>` — single row
- `ExecuteListAsync<T>` — multiple rows
- `ExecuteNonQueryAsync` — INSERT/UPDATE/DELETE
- `ExecuteWithConnectionAsync<T>` — raw connection
- `ExecuteTransactionAsync` — transactions

**Rules**:
- Use raw SQL with MySqlConnector (NO ORM)
- Named parameters: `("@paramName", value)`
- Always specify DateTimeKind.Utc for timestamps

## Logging (CRITICAL)

### ALWAYS Sanitize User Input
```csharp
// ✅ CORRECT
logger.LogWarning("Invalid userId: {UserId}", LogSanitizer.Sanitize(userId));
logger.LogInformation("Request: queue={Queue}, timeRange={Range}",
    LogSanitizer.Sanitize(queueType) ?? "all",
    LogSanitizer.Sanitize(timeRange) ?? "all");
logger.LogInformation("Claim user {UserId}", LogSanitizer.Sanitize(userId.ToString()));

// ❌ INCORRECT — log injection vulnerability
logger.LogWarning("Invalid userId: {UserId}", userId);
```

**Mandatory Rule**:
- If a log template has dynamic arguments, sanitize all string-like/untrusted values before passing them.
- For numeric/enum/boolean values derived from user/session claims or external payloads, convert to string and sanitize (`LogSanitizer.Sanitize(value.ToString())`).
- Use `LogSanitizer` from `Mongoose.Api.Application.Endpoints.Shared` in both `Application` and `Infrastructure` layers.

**What to Sanitize**:
- Route parameters (userId, challengeId, etc.)
- Query parameters (queueType, timeRange, filters)
- Request body fields (email, usernames, feedback text)
- External API responses
- IP addresses

**What can remain unsanitized** (trusted internal-only values):
- Compile-time constants and fixed literals
- Purely internal counters/timers not derived from user/external input

## Error Handling

### Standard Error Format
```csharp
return Results.BadRequest(new { error = "User message", code = "ERROR_CODE" });
```

**Common Codes**:
- `NOT_AUTHENTICATED` — No valid session
- `SESSION_EXPIRED` — Session timeout
- `FORBIDDEN` — User lacks permission
- `INVALID_PASSWORD` — Auth failure
- `RIOT_ACCOUNT_NOT_FOUND` — Missing Riot link
- `ACCOUNT_ALREADY_LINKED` — Duplicate link attempt

**Use AuthResults Helper**:
```csharp
return AuthResults.NotAuthenticated();  // 401
return AuthResults.Forbidden();         // 403
```

## Query Filtering (IQueryFilterBuilder)

### Standardize Filtering Across Endpoints
```csharp
// Inject IQueryFilterBuilder
public MyEndpoint(IQueryFilterBuilder filterBuilder) { ... }

// In endpoint handler:
var queueType = _filterBuilder.ValidateQueueType(queueType);
var queueFilter = _filterBuilder.BuildQueueFilter(queueType);
var timeRangeFilter = await _filterBuilder.ResolveTimeRangeAsync(timeRange);
var timeFilter = _filterBuilder.BuildTimeRangeFilter(timeRangeFilter);

var sql = $@"
    SELECT * FROM matches
    WHERE puuid = @puuid {queueFilter} {timeFilter}";
```

**Supported Filters**:
- Queue: `ranked_solo`, `ranked_flex`, `normal`, `aram`, `all`
- Time: `1w`, `1m`, `3m`, `6m`, `current_season`, `last_season`

## Security & PII

### Encrypt Sensitive Data
```csharp
// Inject IEncryptor
private readonly IEncryptor _encryptor;

// Encrypt before storage
var encryptedEmail = _encryptor.Encrypt(email);

// Decrypt when reading
var decryptedEmail = _encryptor.Decrypt(encryptedValue);
```

**Always Encrypt**: email, usernames, any PII

## Dependency Injection

### Lifetimes
- **Singleton**: `IRiotApiClient`, `IDbConnectionFactory`, `IEncryptor`, `IEmailService`, `IRateLimiter`, `SyncProgressHub`
- **Scoped** (per request): All repositories, `LoginSyncService`, `IQueryFilterBuilder`

## Testing Requirements

### Integration Tests (MANDATORY for all endpoints)
```csharp
public class MyEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;
    
    public MyEndpointTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task GetEndpoint_ReturnsData_WhenAuthenticated()
    {
        // Arrange
        var authenticatedClient = _factory.CreateAuthenticatedClient(userId: 1);
        
        // Act
        var response = await authenticatedClient.GetAsync("/api/v2/resource/1");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<MyResponse>(content);
        data.Should().NotBeNull();
        data!.UserId.Should().Be(1);
    }
    
    [Fact]
    public async Task GetEndpoint_Returns401_WhenNotAuthenticated()
    {
        // Act
        var response = await _client.GetAsync("/api/v2/resource/1");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

## Code Checklist

Before submitting code:
- [ ] Endpoint implements `IEndpoint` and registered in `MongooseApiApplication.cs`
- [ ] DTOs use records with `[JsonPropertyName]`
- [ ] User input sanitized in all log statements
- [ ] Data endpoints resolve PUUID from User ID (never accept PUUID as client input on data queries)
- [ ] Error responses use standard format with error codes
- [ ] PII encrypted via `IEncryptor`
- [ ] Query filtering uses `IQueryFilterBuilder`
- [ ] Integration tests cover happy path + auth failures
- [ ] XML doc comments on public APIs
- [ ] UTC timestamps (`DateTimeKind.Utc`)
