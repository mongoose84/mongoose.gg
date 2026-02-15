# Global Repository Instructions

## Project Overview
Mongoose.gg is a League of Legends performance analytics platform helping solo players, duos, and full teams understand their gameplay through rich match analytics, timeline-derived metrics, and AI-powered goal recommendations. The platform integrates with the Riot Games API to sync match history, calculate advanced performance metrics, and provide actionable improvement recommendations.

**Key Features**: Match history sync, solo/duo/team dashboards, LP/winrate trend charts, champion matchups analysis, real-time champion select support, AI goal recommendations, and performance-driven match narratives.

## Technology Stack

### Backend
- **Runtime**: .NET 9 (C#), Minimal API pattern
- **Architecture**: Clean Architecture (Core → Application → Infrastructure)
- **Database**: MySQL with MySqlConnector (no ORM, raw SQL)
- **External APIs**: Riot Games API v5 with custom rate limiting
- **Security**: AES encryption for PII, cookie-based authentication
- **Background Jobs**: BackgroundService for match sync and cleanup
- **WebSocket**: Raw WebSocket for real-time sync progress updates
- **Testing**: xUnit with TestWebApplicationFactory for integration tests

### Frontend
- **Framework**: Vue 3 with Composition API
- **Build Tool**: Vite (dev server on port 5174)
- **State Management**: Pinia stores (authStore, uiStore)
- **Routing**: Vue Router with auth guards and page view tracking
- **Styling**: Tailwind CSS with CSS custom properties for theming
- **Charts**: Chart.js with vue-chartjs and chartjs-plugin-annotation
- **Testing**: Vitest (unit tests with Vue Test Utils + jsdom), Playwright (E2E tests)

### Database
- **Engine**: MySQL 8.0+
- **Schema**: Normalized relational design with match timeline data
- **Access**: Raw SQL via MySqlConnector, no ORM
- **Migrations**: Manual SQL scripts
- **Key Tables**: users, riot_accounts, user_riot_accounts (junction), matches, participants, participant_checkpoints

## Architecture Patterns

### Backend (C# .NET 9)

#### Clean Architecture
```
Core/               # Domain entities, interfaces, enums (no dependencies)
Application/        # Use cases, endpoints, DTOs, services (depends on Core)
Infrastructure/     # External concerns: DB, Riot API, email, jobs (depends on Application + Core)
```

**Dependency Rule**: Dependencies point inward. Infrastructure → Application → Core.

#### Endpoint Pattern
Every API endpoint is a sealed class implementing `IEndpoint`. Registered in `MongooseApiApplication.cs`:

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
            [FromServices] IRepository repo,
            [FromServices] ILogger<MyEndpoint> logger
        ) => {
            // 1. Auth check
            if (httpContext.User?.Identity?.IsAuthenticated != true)
                return AuthResults.NotAuthenticated();
            
            // 2. Parse & validate
            if (!int.TryParse(userId, out var userIdInt))
                return Results.BadRequest(new { error = "Invalid userId" });
            
            // 3. Authorization (user can only access own data)
            var authenticatedUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (authenticatedUserId != userIdInt.ToString())
                return Results.Forbid();
            
            // 4. Resolve PUUID from user → riot account link
            var linkedAccounts = await userRiotAccountsRepo.GetByUserIdAsync(userIdInt);
            var primaryPuuid = linkedAccounts.FirstOrDefault(la => la.Link.IsPrimary)?.Account?.Puuid;
            
            // 5. Query & return
            var result = await repo.GetDataAsync(primaryPuuid);
            return Results.Ok(result);
        }).RequireAuthorization();
    }
}
```

**Critical**: All data endpoints resolve PUUID from User ID. Never expose PUUIDs to clients.

#### Repository Pattern
All repositories extend `RepositoryBase` with standard methods:
- `ExecuteScalarAsync<T>` — single value
- `ExecuteSingleAsync<T>` — single row with mapper function
- `ExecuteListAsync<T>` — multiple rows with mapper function
- `ExecuteNonQueryAsync` — INSERT/UPDATE/DELETE
- `ExecuteWithConnectionAsync<T>` — raw connection access
- `ExecuteTransactionAsync` — transactional multi-query operations

Use raw SQL with named parameters. No ORM.

#### DTOs and Records
Use C# records for immutable DTOs with JSON property names:
```csharp
public record MyResponse(
    [property: JsonPropertyName("userId")] int UserId,
    [property: JsonPropertyName("winRate")] double WinRate,
    [property: JsonPropertyName("games")] int Games
);
```

### Frontend (Vue 3)

#### Component Structure
Single-File Components (SFC) with Composition API:
```vue
<template>
  <div class="component-name" data-testid="component-name">
    <!-- Always include data-testid for testing -->
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'

// Props with validation
const props = defineProps({
  data: { type: Array, default: () => [] },
  loading: { type: Boolean, default: false }
})

// Emits declaration
const emit = defineEmits(['update', 'close'])

// Reactive state
const localState = ref(null)

// Computed properties
const hasData = computed(() => props.data && props.data.length > 0)

// Lifecycle
onMounted(() => {
  // Setup code
})
</script>

<style scoped>
/* Component-specific styles */
</style>
```

#### Component Organization
```
components/
├── base/           # Reusable primitives (BaseButton, BaseModal, BaseInput)
├── overview/       # Overview page specific
├── solo/           # Solo dashboard specific
├── matches/        # Match history specific
└── shared/         # Shared across multiple pages (AnalysisLayout)
```

#### State Management (Pinia)
```javascript
import { defineStore } from 'pinia'

export const useMyStore = defineStore('myStore', {
  state: () => ({
    data: null,
    isLoading: false
  }),
  
  getters: {
    hasData: (state) => state.data !== null
  },
  
  actions: {
    async fetchData() {
      this.isLoading = true
      try {
        this.data = await api.getData()
      } finally {
        this.isLoading = false
      }
    }
  }
})
```

## Coding Standards

### Backend (C#)

#### Logging
**Always sanitize user input** before logging to prevent log injection:
```csharp
// ✅ CORRECT
logger.LogWarning("Invalid userId: {UserId}", LogSanitizer.Sanitize(userId));
logger.LogInformation("Request: queue={Queue}, timeRange={Range}",
    LogSanitizer.Sanitize(queueType) ?? "all",
    LogSanitizer.Sanitize(timeRange) ?? "all");

// ❌ INCORRECT — potential log injection
logger.LogWarning("Invalid userId: {UserId}", userId);
```

Sanitize: route params, query params, request body fields, external API responses, IP addresses.
Don't sanitize: database-resolved IDs (PUUIDs, numeric IDs).

#### Error Responses
Standard JSON error format:
```csharp
return Results.BadRequest(new { error = "Error message", code = "ERROR_CODE" });
```

Common codes: `NOT_AUTHENTICATED`, `SESSION_EXPIRED`, `FORBIDDEN`, `INVALID_PASSWORD`, `RIOT_ACCOUNT_NOT_FOUND`, `ACCOUNT_ALREADY_LINKED`.

Use `AuthResults` helper for auth errors:
```csharp
return AuthResults.NotAuthenticated();
return AuthResults.Forbidden();
```

#### PII Handling
Encrypt PII (email, username) using `IEncryptor`:
```csharp
var encryptedEmail = _encryptor.Encrypt(email);
var decryptedEmail = _encryptor.Decrypt(encryptedValue);
```

#### Query Filtering
Use `IQueryFilterBuilder` for standardized filtering:
```csharp
var queueType = _filterBuilder.ValidateQueueType(queueType);
var queueFilter = _filterBuilder.BuildQueueFilter(queueType);
var timeRangeFilter = await _filterBuilder.ResolveTimeRangeAsync(timeRange);
var timeFilter = _filterBuilder.BuildTimeRangeFilter(timeRangeFilter);

var sql = $@"SELECT * FROM matches WHERE puuid = @puuid {queueFilter} {timeFilter}";
```

#### Dependency Injection
- **Singleton**: `IRiotApiClient`, `IDbConnectionFactory`, `IEncryptor`, `IEmailService`, `IRateLimiter`, `SyncProgressHub`
- **Scoped**: All repositories, `LoginSyncService`, `IQueryFilterBuilder`

### Frontend (Vue/JavaScript)

#### Naming Conventions
- **Components**: PascalCase (`BaseButton.vue`, `WinrateChart.vue`)
- **Composables**: camelCase with `use` prefix (`useWinRateColor.js`, `useSyncWebSocket.js`)
- **Stores**: camelCase with `Store` suffix (`authStore.js`, `uiStore.js`)
- **Services**: camelCase suffix (`authApi.js`, `analyticsApi.js`)
- **Utils**: camelCase (`formatters.js`, `leagueAssets.js`)

#### API Calls
Centralize in service modules:
```javascript
// services/authApi.js
export async function getSoloDashboard(userId, queueType, timeRange) {
  const params = new URLSearchParams()
  if (queueType && queueType !== 'all') {
    params.append('queueType', queueType)
  }
  if (timeRange) {
    params.append('timeRange', timeRange)
  }
  
  const endpoint = `/solo/performance/${userId}${params.toString() ? '?' + params.toString() : ''}`
  const response = await apiRequest(endpoint, { method: 'GET' })
  
  if (response.status === 404) {
    return null
  }
  
  return parseResponse(response, 'Failed to get solo dashboard')
}
```

#### Error Handling
Always handle loading and error states:
```javascript
const data = ref(null)
const isLoading = ref(false)
const error = ref(null)

async function fetchData() {
  isLoading.value = true
  error.value = null
  try {
    data.value = await api.getData()
  } catch (err) {
    console.error('Failed to fetch data:', err)
    error.value = err.message
  } finally {
    isLoading.value = false
  }
}
```

#### Accessibility
- Use semantic HTML (`<button>`, `<nav>`, `<main>`)
- Include `aria-label` for icon-only buttons
- Add `data-testid` attributes for testing
- Ensure keyboard navigation works
- Maintain color contrast ratios (WCAG AA)

## Testing Requirements

### Backend Tests (xUnit)

#### Integration Tests
Use `TestWebApplicationFactory` for in-process server tests:
```csharp
[Fact]
public async Task Endpoint_ReturnsData_WhenAuthenticated()
{
    // Arrange
    var client = _factory.CreateAuthenticatedClient(userId: 1);
    
    // Act
    var response = await client.GetAsync("/api/v2/solo/performance/1");
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var content = await response.Content.ReadAsStringAsync();
    var data = JsonSerializer.Deserialize<SoloPerformanceResponse>(content);
    data.Should().NotBeNull();
}
```

#### Unit Tests
Test business logic in isolation:
```csharp
[Fact]
public void Calculator_ComputesAverage_Correctly()
{
    // Arrange
    var values = new[] { 5.0, 10.0, 15.0 };
    
    // Act
    var result = Calculator.Average(values);
    
    // Assert
    result.Should().BeApproximately(10.0, 0.01);
}
```

### Frontend Tests

#### Unit Tests (Vitest)
Test components in isolation with mocks:
```javascript
import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import MyComponent from '@/components/MyComponent.vue'

// Mock Chart.js
vi.mock('vue-chartjs', () => ({
  Line: {
    name: 'Line',
    props: ['data', 'options'],
    template: '<div data-testid="mock-chart"></div>'
  }
}))

describe('MyComponent', () => {
  it('renders with data', () => {
    const wrapper = mount(MyComponent, {
      props: { data: [1, 2, 3] }
    })
    
    expect(wrapper.find('[data-testid="my-component"]').exists()).toBe(true)
  })
  
  it('shows empty state when no data', () => {
    const wrapper = mount(MyComponent, {
      props: { data: [] }
    })
    
    expect(wrapper.find('[data-testid="empty-state"]').exists()).toBe(true)
  })
})
```

#### E2E Tests (Playwright)
Test complete user flows:
```javascript
import { test, expect } from '@playwright/test'

test('user can view solo dashboard', async ({ page }) => {
  // Login
  await page.goto('/auth?mode=login')
  await page.fill('[data-testid="email-input"]', 'test@example.com')
  await page.fill('[data-testid="password-input"]', 'password123')
  await page.click('[data-testid="login-button"]')
  
  // Navigate to solo dashboard
  await page.click('[data-testid="solo-nav-link"]')
  await expect(page).toHaveURL('/solo')
  
  // Verify dashboard loads
  await expect(page.locator('[data-testid="summary-stats-card"]')).toBeVisible()
})
```

**Test Coverage Requirements**:
- Backend: All endpoints must have integration tests
- Frontend: All components with logic must have unit tests
- E2E: Critical user flows must be covered

## Security Requirements

### Authentication
- Cookie-based sessions with `HttpOnly`, `Secure`, `SameSite=Strict`
- Session timeout: 30 minutes (configurable via `Auth:SessionTimeout`)
- Idle detection on frontend with auto-logout
- Password requirements: minimum 8 characters

### Authorization
- Users can only access their own data
- PUUID resolution happens server-side (never expose PUUIDs to client)
- All authenticated endpoints verify `ClaimTypes.NameIdentifier` matches route `userId`

### Data Protection
- PII (email, username) encrypted at rest using AES-256
- Riot API key stored in environment variables, never in code
- Database connection strings in user secrets or environment variables
- CORS restricted to known origins

### Input Validation
- Sanitize all user input before logging (use `LogSanitizer.Sanitize()`)
- Validate route parameters, query parameters, and request bodies
- SQL injection prevention via parameterized queries (never string concatenation)

## API Design Guidelines

### RESTful Conventions
- Base path: `/api/v2/`
- Resource naming: plural nouns (`/matches`, `/trends`)
- Use standard HTTP verbs: GET (read), POST (create), PUT (update), DELETE (remove)
- Status codes: 200 (OK), 201 (Created), 400 (Bad Request), 401 (Unauthorized), 403 (Forbidden), 404 (Not Found), 500 (Internal Server Error)

### Response Format
Success:
```json
{
  "data": { ... },
  "additionalField": "value"
}
```

Error:
```json
{
  "error": "User-friendly message",
  "code": "ERROR_CODE"
}
```

### Filtering and Pagination
- Queue filtering: `?queueType=ranked_solo|ranked_flex|normal|aram|all`
- Time range: `?timeRange=1w|1m|3m|6m|current_season|last_season`
- Pagination: `?limit=20` for trend endpoints (null for all with downsampling)

## Database Patterns

### Raw SQL
No ORM. Use MySqlConnector with parameterized queries:
```csharp
var sql = @"
    SELECT id, username, email
    FROM users
    WHERE id = @userId";

var user = await ExecuteSingleAsync(sql, 
    reader => new User { 
        Id = reader.GetInt32(0),
        Username = reader.GetString(1),
        Email = reader.GetString(2)
    },
    ("@userId", userId));
```

### Transactions
Use `ExecuteTransactionAsync` for multi-query operations:
```csharp
await ExecuteTransactionAsync(async (conn, transaction) =>
{
    // Query 1
    await using var cmd1 = new MySqlCommand(sql1, conn, transaction);
    await cmd1.ExecuteNonQueryAsync();
    
    // Query 2
    await using var cmd2 = new MySqlCommand(sql2, conn, transaction);
    await cmd2.ExecuteNonQueryAsync();
});
```

### UTC Everywhere
All `DateTime` values must be UTC:
```csharp
var timestamp = DateTime.UtcNow;
var fromDb = DateTime.SpecifyKind(reader.GetDateTime(0), DateTimeKind.Utc);
```

## Documentation Standards

### Code Documentation
- XML doc comments for all public APIs, interfaces, and endpoints
- Document parameters, return values, and exceptions
- Include usage examples for complex features
- Reference related specs in `.github/specs/` where applicable

### Architecture Documentation
- Maintain AGENTS.md files in `server/` and `client/` directories
- Update specs in `.github/specs/` when architecture changes
- Document major patterns and conventions in copilot-instructions.md

### Feature Documentation
- Use template in `.github/specs/feature-template.md` for new features
- Document problem statement, solution, requirements, API contracts
- Include testing strategy and validation criteria

## Performance Considerations

### Backend
- Use `async`/`await` consistently for I/O operations
- Implement rate limiting for Riot API calls (429 handling with exponential backoff)
- Cache static data (champion names, item data) in memory
- Use database indexes on frequently queried columns (puuid, match_id, game_start_time)
- Background jobs run on separate threads (BackgroundService pattern)

### Frontend
- Lazy load routes with dynamic imports
- Debounce/throttle frequent operations (filters, search)
- Use `v-if` for conditional rendering of heavy components
- Implement virtual scrolling for long lists
- Compress and optimize images
- Enable Vite code splitting

### Database
- Index strategy: puuid + game_start_time for match queries
- Avoid SELECT * — always specify columns
- Use LIMIT for paginated queries
- Archive old matches (retention policy: 180 days configurable via `Jobs:MatchRetentionDays`)

## Development Workflow

### Branch Strategy
- `main` — production-ready code
- Feature branches: `feature/description`
- Bug fixes: `fix/description`

### Commit Messages
Follow conventional commits:
- `feat: add deaths over time chart`
- `fix: correct rolling average calculation`
- `refactor: extract query filtering to helper`
- `test: add unit tests for DeathsChart component`
- `docs: update API documentation`

### Code Review Checklist
- [ ] Tests added/updated for new functionality
- [ ] Error handling covers edge cases
- [ ] Logging includes proper sanitization
- [ ] Documentation updated (code comments, AGENTS.md, specs)
- [ ] No hardcoded secrets or PII in code
- [ ] Follows existing patterns and conventions
- [ ] Accessibility requirements met (frontend)
- [ ] Performance implications considered
