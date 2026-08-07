# Mongoose.gg — Test Strategy Specification

> **Purpose**: Single-source-of-truth for AI agents and developers writing, running, and maintaining tests across backend, frontend, and E2E layers. Contains infrastructure details, file maps, patterns, helpers, and checklists.

**Layers**: Backend (xUnit + .NET 10) · Frontend Unit (Vitest + Vue Test Utils) · E2E (Playwright)  
**Last verified**: August 8, 2026

---

## Table of Contents

1. [Testing Philosophy](#1-testing-philosophy)
2. [Critical Paths Analysis](#2-critical-paths-analysis)
3. [Backend Testing Strategy](#3-backend-testing-strategy)
4. [Frontend Testing Strategy](#4-frontend-testing-strategy)
5. [E2E Testing Strategy](#5-e2e-testing-strategy)
6. [CI/CD Integration](#6-cicd-integration)
7. [Quick Reference](#7-quick-reference)

---

## 1. Testing Philosophy

### 1.1 The Testing Pyramid

```
        /\
       /E2E\        (Few) - Critical user journeys only
      /______\
     /  API    \    (More) - Backend endpoint coverage
    /____________\
   / Unit (BE+FE) \ (Many) - Business logic, components, utils
  /________________\
```

### 1.2 Core Principles

1. **Test the Right Things**: Focus on high-risk, high-value areas first
2. **Fast Feedback Loop**: Unit tests < 5s, API tests < 30s, E2E < 2min
3. **Reliability Over Coverage**: A failing test must mean a real problem
4. **Shift Left**: Catch bugs as early as possible in the pipeline
5. **Test Behavior, Not Implementation**: Focus on what, not how

---

## 2. Critical Paths Analysis

### 2.1 Critical (Must Test Thoroughly)

| Area | Risk Level | Current Coverage |
|------|------------|------------------|
| **Authentication Flow** (Login/Register/Verify) | High | ✅ Good |
| **Match Sync Job** (Data ingestion) | High | Partial |
| **Riot API Integration** (External dependency) | High | None — needs mocking |
| **Security/Encryption** (AES, password hashing) | High | ✅ Good |
| **Email Verification** (Account security) | High | ✅ Good |

### 2.2 High Priority (Business Value)

| Area | Risk Level | Current Coverage |
|------|------------|------------------|
| **Overview Dashboard** | Medium | ✅ Good |
| **Solo Performance Stats** | Medium | Minimal |
| **Match List & Details** | Medium | ✅ Complete (19 tests) |
| **LP Calculations** | Medium | ✅ Complete (82 tests) |
| **Auth Store (Pinia)** | Medium | ✅ Complete (39 tests) |
| **WebSocket Sync** | Medium | ✅ Complete (19 tests) |
| **Riot Match Mapper** | Medium | ✅ Complete (28 tests) |
| **Session Expiry Handling** | Medium | ✅ Complete (32 tests) |
| **Analysis Status** | Medium | ✅ Complete (57 tests) |

### 2.3 Standard Priority

| Area | Risk Level | Current Coverage |
|------|------------|------------------|
| **Static Pages** (Terms, Privacy, Landing) | Low | ✅ Good |
| **UI Components** (Base components) | Low | ✅ Complete (137 tests) |
| **Formatting Utils** | Low | ✅ Complete (56 tests) |
| **Chart Components** | Low | None |
| **Feedback Feature** | Low | None |

---

## 3. Backend Testing Strategy

### 3.1 Test Infrastructure

- `TestWebApplicationFactory` provides dependency injection mocking for integration tests
- Fake repositories for Users, RiotAccounts, Tokens, Analytics
- Integration tests use real HTTP client against test server
- FluentAssertions for readable assertions

### 3.2 Test File Map

All 55 test files (558 `[Fact]`/`[Theory]` tests total) live **flat** in `server/Mongoose.Api.Tests/` — there are no `Endpoints/`/`Services/`/`Mappers/` subdirectories. Grouped below by what they cover, not by folder:

```
server/Mongoose.Api.Tests/
├── Endpoint tests (one file per endpoint, or per closely-related group)
│   ├── LoginEndpointTests.cs, RegisterEndpointTests.cs, LogoutEndpointTests.cs,
│   │   VerifyEndpointTests.cs, ResendVerificationEndpointTests.cs
│   ├── AccountSecurityEndpointsTests.cs    # covers change/forgot/reset-password + delete-account
│   ├── RiotAccountsEndpointTests.cs, RiotSignOnEndpointTests.cs
│   ├── ChampionSelectEndpointTests.cs, DiagnosticsEndpointTests.cs, FeedbackEndpointTests.cs
│   ├── MatchEndpointTests.cs               # covers list/details/narrative together
│   ├── MatchActivityEndpointTests.cs, OverviewEndpointTests.cs, PublicStatsEndpointTests.cs
│   ├── SoloPerformanceEndpointTests.cs, DeathPositionsEndpointTests.cs, RadarChartEndpointTests.cs
│   ├── CsPerMinuteTrendEndpointTests.cs, DeathsTrendEndpointTests.cs,
│   │   DragonParticipationTrendEndpointTests.cs, GoldAt15TrendEndpointTests.cs,
│   │   VisionScoreTrendEndpointTests.cs, WinrateTrendEndpointTests.cs
│   └── AnalyticsEndpointTests.cs, AnalyticsAsyncEndpointTests.cs, AnalyticsV2EndpointTests.cs
├── Services & domain logic
│   ├── LoginSyncServiceTests.cs, PuuidResolutionServiceTests.cs, AuthorizationHelperTests.cs
│   ├── MainChampionRecommenderTests.cs, TrendBadgeCalculatorTests.cs
│   └── MatchDataPersistenceServiceTests.cs, QueryFilterBuilderTests.cs
├── Mappers & Riot integration
│   ├── RiotMatchMapperTests.cs, RiotTimelineMapperTests.cs, RiotApiClientTests.cs
│   ├── SeasonHelperTests.cs, LeagueDataHelperTests.cs, TokenBucketTests.cs
├── Infrastructure
│   ├── AesEncryptorTests.cs, VerificationCodeGeneratorTests.cs, VerificationTokenTests.cs
│   ├── SyncProgressHubTests.cs, SyncProgressAggregatorTests.cs, SyncQueueSignalTests.cs
│   ├── UtcDateTimeJsonConverterTests.cs, EndpointDiscoveryExtensionTests.cs
│   └── MatchesRepositoryIntegrationTests.cs
├── Jobs
│   ├── MatchHistorySyncJobTests.cs, MatchCleanupJobTests.cs
└── Helpers (not test classes)
    ├── TestWebApplicationFactory.cs, AuthCookieTestHelper.cs
    └── EnvironmentVariableScope.cs, EnvIsolationCollection.cs
```

### 3.3 Gaps to Fill

Everything the previous revision of this spec listed as missing (`LoginSyncServiceTests`, `SeasonHelperTests`, `RiotTimelineMapperTests`, Riot API client mocking via `RiotApiClientTests`) now exists. Current gaps, checked directly against `Application/Endpoints/` (§5 of architecture.spec.md):

1. **`AnalyticsExploreEndpoint`, `AnalyticsJourneyAndFunnelEndpoints`, `AnalyticsRealtimeEndpoint`** — no test coverage at all. These are also the endpoints flagged in architecture.spec.md §5 as missing an auth guard; tests would be a natural place to also assert/document that gap.
2. **`UsersMeEndpoint`** — no dedicated test file; only incidentally exercised via `LoginEndpointTests` and `RiotAccountsEndpointTests`.
3. **`SoloMatchupsEndpoint`** and **`HomeEndpoint`** — no test coverage found.

### 3.4 Backend Test Pattern

```csharp
// Integration test using TestWebApplicationFactory
public class MyEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public MyEndpointTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetEndpoint_ReturnsExpectedData()
    {
        // Arrange — seed fake repos via factory

        // Act
        var response = await _client.GetAsync("/api/v2/my-endpoint/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MyResponse>();
        body.Should().NotBeNull();
    }
}
```

---

## 4. Frontend Testing Strategy

### 4.1 Test Infrastructure

- Vitest with happy-dom for fast component testing
- Vue Test Utils for component mounting and interaction
- Coverage reporting via `@vitest/coverage-v8`
- Centralized test helpers in `client/test/helpers/`

### 4.2 Test File Map

Test layout mostly mirrors `src/`, but has accumulated a handful of files at the `test/unit/` root that belong in a subdirectory (noted below). Rather than list all ~90 spec files, here's the structure with counts — cross-check against `src/` for any specific file's status rather than trusting a hand-maintained list, since this table goes stale the moment a component is added:

```
client/test/unit/
├── components/
│   ├── (flat)                              # Base*, AppSidebar, NavBar, modals, cards, etc.
│   ├── matches/                            # 12 of 12 components tested
│   ├── overview/                           # 6 of 8 components tested — MatchActivityHeatmap untested;
│   │                                       #   DeathInsightsCard tested but as test/unit/DeathInsightCard.spec.js
│   │                                       #   (stray name, missing "s", wrong directory — fix if touching this area)
│   ├── solo/                               # 11 of 11 components tested
│   └── sidebar/                            # 2 of 2 components tested
│   # Stray root-level specs that actually belong under components/: DeathInsightCard.spec.js,
│   # OverviewLayout.spec.js, TodaySessionCard.spec.js (duplicates components/overview/TodaySessionCard.spec.js)
├── composables/                            # 9 of 10 tested — useAnalyticsQueue untested
├── stores/                                 # 2 of 2 tested
├── services/                               # 10 of 11 tested — apiConfig untested (trivial constants module)
├── utils/                                  # 4 of 4 tested, plus helpers.spec.js (tests test/helpers/, not src/utils/)
└── views/                                  # 12 of 15 tested — ChampionSelectPage, FeedbackPage,
                                             #   UserSettingsPage untested
    # OverviewPage.spec.js and SoloStatsPage.spec.js also exist duplicated at test/unit/ root
```

### 4.3 Coverage Summary

| Category | Total | Tested | Coverage |
|----------|-------|--------|----------|
| **Composables** | 10 | 9 | 90% |
| **Services** | 11 | 10 | 91% |
| **Stores** | 2 | 2 | 100% |
| **Utils** | 4 | 4 | 100% |
| **Views** | 15 | 12 | 80% |
| **Components** (incl. Base) | 56 | 55 | 98% |

### 4.4 Gaps to Fill

1. **`ChampionSelectPage`, `FeedbackPage`, `UserSettingsPage`** — the three untested views
2. **`MatchActivityHeatmap`** — the one untested component
3. **`useAnalyticsQueue.spec.js`** — untested composable
4. **Stray/misplaced specs** — `DeathInsightCard.spec.js` (typo, should be `DeathInsightsCard.spec.js` under `components/overview/`), `OverviewLayout.spec.js` and duplicated `TodaySessionCard.spec.js`/`OverviewPage.spec.js`/`SoloStatsPage.spec.js` at `test/unit/` root — worth consolidating next time this area is touched, not urgent

### 4.5 Test Helpers (`client/test/helpers/`)

```javascript
import {
  createMockUser,
  createMockRiotAccount,
  createMockMatch,
  createAuthApiMock,
  createAnalyticsApiMock,
  createMockAuthStore,
  setupPinia,
  headlessUIStubs,
  createWrapper,
  waitFor,
  createDeferredPromise,
  createMockFetch,
  cleanupMocks
} from '@test/helpers';
```

| Helper | Purpose |
|--------|---------|
| `createMockUser(overrides)` | Create mock user data |
| `createMockRiotAccount(overrides)` | Create mock Riot account data |
| `createMockMatch(overrides)` | Create mock match data |
| `createAuthApiMock()` | Mock all authApi functions |
| `createAnalyticsApiMock()` | Mock all analyticsApi functions |
| `createMockAuthStore(overrides)` | Mock auth store for component tests |
| `setupPinia()` | Create and activate fresh Pinia instance |
| `headlessUIStubs` | Pre-configured HeadlessUI component stubs |
| `createWrapper(component, options)` | Mount component with common config |
| `waitFor(condition, timeout)` | Wait for async condition (real timers only) |
| `createDeferredPromise()` | Create controllable promise for async tests |
| `createMockFetch(response)` | Mock global fetch |
| `cleanupMocks()` | Clear mocks and restore fetch |

### 4.6 Component Testing Pattern

```javascript
import { mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import ComponentUnderTest from '@/components/ComponentUnderTest.vue';

describe('ComponentUnderTest', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
  });

  it('renders correctly with default props', () => {
    const wrapper = mount(ComponentUnderTest);
    expect(wrapper.exists()).toBe(true);
  });

  it('emits event on user interaction', async () => {
    const wrapper = mount(ComponentUnderTest);
    await wrapper.find('button').trigger('click');
    expect(wrapper.emitted('action')).toBeTruthy();
  });
});
```

### 4.7 Store Testing Pattern

```javascript
import { setActivePinia, createPinia } from 'pinia';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { useAuthStore } from '@/stores/authStore';

describe('authStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
  });

  it('initializes with default state', () => {
    const store = useAuthStore();
    expect(store.isAuthenticated).toBe(false);
    expect(store.user).toBeNull();
  });

  it('sets user on successful login', async () => {
    const store = useAuthStore();
    await store.login({ email: 'test@test.com', password: 'password' });
    expect(store.isAuthenticated).toBe(true);
  });
});
```

### 4.8 Singleton Composable Testing Pattern

When testing composables with module-level state (like `useSyncWebSocket`), reset modules between tests:

```javascript
let useSyncWebSocket;
beforeEach(async () => {
  vi.resetModules();
  const module = await import('@/composables/useSyncWebSocket');
  useSyncWebSocket = module.useSyncWebSocket;
});
```

### 4.9 Reactive Mock State Pattern

When mocking composables for component tests, use Vue's `ref()` for reactive state:

```javascript
const mockStatus = ref('idle');
vi.mock('@/composables/useAnalysisStatus', () => ({
  useAnalysisStatus: () => ({ status: mockStatus, ... })
}));
```

---

## 5. E2E Testing Strategy

### 5.1 Architecture

```
client/e2e/
├── global-setup.js            # Creates user, links Riot account, saves auth state
├── global-teardown.js         # Deletes test user, cleans up auth files
├── helpers/
│   └── app-shell.js           # gotoAppPage(), expectProtectedRouteRedirectsToAuth() shared helpers
├── .auth/
│   ├── user.json              # Saved auth state (auto-generated)
│   └── test-user.json         # Test user metadata (auto-generated)
├── app-smoke.spec.js          # Cross-page nav smoke test (route reachability, protected-route redirects)
├── solo-dashboard.spec.js     # Solo dashboard tests
└── overview-dashboard.spec.js # Overview dashboard tests
```

### 5.2 Global Setup/Teardown Flow

1. **Global Setup** (runs once before all tests):
   - Registers a unique test user (`e2e_test_{timestamp}`)
   - Auto-verifies email (non-production environments via `Auth:AutoVerifyEmail` flag)
   - Links hardcoded Riot account (`Doend#EUW`)
   - Saves auth state to `e2e/.auth/user.json`
   - Saves user metadata for teardown

2. **Test Execution**:
   - All browser projects load saved auth state
   - Tests run authenticated without repeated logins
   - Avoids rate limiting on login endpoint

3. **Global Teardown** (runs once after all tests):
   - Deletes the test user via API
   - Cleans up auth state files

### 5.3 Riot Account Configuration

Riot account credentials are hardcoded in `client/e2e/global-setup.js`:
```javascript
const RIOT_ACCOUNT = {
  gameName: 'Doend',
  tagLine: 'EUW',
  region: 'euw1',
};
```

### 5.4 Security: Auto-Verify Email

**CRITICAL: The `Auth:AutoVerifyEmail` setting must NEVER be enabled in production.**

| Environment | How `Auth:AutoVerifyEmail` is Set | Security |
|-------------|-----------------------------------|----------|
| **CI (GitHub Actions)** | Dynamically generated in `ci-e2e.yml` workflow | Never committed to repo |
| **Local Development** | Set via environment variable | Only active when running E2E tests |
| **Production** | Not set (defaults to `false`) | Email verification required |

### 5.5 Unauthenticated Tests

Override the storage state for tests that verify unauthenticated behavior:
```javascript
test('redirects unauthenticated users', async ({ browser }) => {
  const context = await browser.newContext({ storageState: undefined });
  const page = await context.newPage();
  // Test unauthenticated behavior...
});
```

### 5.6 E2E Test Pattern

```javascript
import { test, expect } from '@playwright/test';

test.describe('User Dashboard', () => {
  // Auth handled by global setup — no login needed

  test('displays user statistics', async ({ page }) => {
    await page.goto('/app/overview');
    await expect(page.locator('[data-testid="stats-card"]')).toBeVisible();
    await expect(page.locator('[data-testid="games-played"]')).not.toHaveText('0');
  });

  test('navigates to match details', async ({ page }) => {
    await page.goto('/app/matches');
    await page.click('[data-testid="match-row"]:first-child');
    await expect(page).toHaveURL(/\/matches\/\w+/);
  });
});
```

### 5.7 Gaps to Fill

- Match viewing tests
- Error handling / negative path tests
- Registration E2E flow

---

## 6. CI/CD Integration

### 6.1 Pipeline Overview

Four workflows, not three — `ci-unit.yml` is the PR-time fast-feedback gate; `ci-client.yml`/`ci-server.yml` build, test again, and deploy on push to `main`:

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  Unit CI        │     │  Client CI      │     │  Server CI      │     │  E2E CI         │
│  (ci-unit.yml)  │     │  (ci-client.yml)│     │  (ci-server.yml)│     │  (ci-e2e.yml)   │
├─────────────────┤     ├─────────────────┤     ├─────────────────┤     ├─────────────────┤
│ • npm run       │     │ • npm ci        │     │ • dotnet restore│     │ • Build both    │
│   test:unit     │     │ • Unit tests    │     │ • dotnet test   │     │ • Start servers │
│ • dotnet test   │     │   + coverage    │     │ • Publish       │     │ • Playwright    │
│   (no deploy)   │     │ • Build         │     │ • Deploy        │     │ • Upload traces │
│                 │     │ • Deploy        │     │                 │     │                 │
└─────────────────┘     └─────────────────┘     └─────────────────┘     └─────────────────┘
      ↓                       ↓                       ↓                       ↓
  PR + main                 main                    main                 PR + main
```

### 6.2 Test Execution Triggers

| Trigger | Unit Tests | API Tests | E2E Tests |
|---------|-----------|-----------|-----------|
| PR Created | ✅ All | ✅ All | ✅ All |
| Push to main | ✅ All | ✅ All | ✅ All |
| Scheduled (nightly) | — | — | ✅ Full matrix |

---

## 7. Quick Reference

### 7.1 Running Tests

```bash
# Backend
cd server && dotnet test

# Frontend unit tests
cd client && npm run test:unit
cd client && npm run test:unit:watch     # Watch mode
cd client && npm run test:unit:coverage  # With coverage

# E2E tests (requires backend running)
cd client && npm run test:e2e            # Headless, full Playwright project matrix
cd client && npm run test:e2e:smoke      # smoke-chromium project only — fast route/nav check
cd client && npm run test:e2e:full       # full-chromium + full-firefox projects
cd client && npm run test:e2e:headed     # With browser
cd client && npm run test:e2e:ui         # Playwright UI
cd client && npm run test:e2e:report     # Open the last HTML report
```

**E2E Local Setup:**
```bash
# Terminal 1: Start server with E2E flags
Auth__AutoVerifyEmail=true Email__DevMode=true dotnet run --project server/Mongoose.Api

# Terminal 2: Run E2E tests
cd client && npm run test:e2e
```

### 7.2 Key Files

| Purpose | Path |
|---------|------|
| Backend test factory | `server/Mongoose.Api.Tests/TestWebApplicationFactory.cs` |
| Riot Match Mapper tests | `server/Mongoose.Api.Tests/RiotMatchMapperTests.cs` |
| Match Endpoint tests | `server/Mongoose.Api.Tests/MatchEndpointTests.cs` |
| Auth Store tests | `client/test/unit/stores/authStore.spec.js` |
| API Client tests | `client/test/unit/services/apiClient.spec.js` |
| Formatters tests | `client/test/unit/utils/formatters.spec.js` |
| Analysis Status tests | `client/test/unit/composables/useAnalysisStatus.spec.js` |
| WebSocket tests | `client/test/unit/composables/useSyncWebSocket.spec.js` |
| Base Component tests | `client/test/unit/components/Base*.spec.js` (7 files) |
| Test helpers | `client/test/helpers/` |
| Vitest config | `client/vitest.config.js` |
| Playwright config | `client/playwright.config.js` |
| CI — Unit (PR gate, client + server) | `.github/workflows/ci-unit.yml` |
| CI — Client (build + deploy) | `.github/workflows/ci-client.yml` |
| CI — Server (build + deploy) | `.github/workflows/ci-server.yml` |
| CI — E2E | `.github/workflows/ci-e2e.yml` |

### 7.3 Adding New Tests

**Backend Test:**
1. Create test class in `server/Mongoose.Api.Tests/`
2. Inherit or use `TestWebApplicationFactory` for integration tests
3. Use FluentAssertions for readable assertions
4. Run `dotnet test` to verify

**Frontend Unit Test:**
1. Create `.spec.js` file in `client/test/unit/`
2. Import helpers from `@test/helpers` for common mocks
3. Import from `@vue/test-utils` and `vitest`
4. Use `setupPinia()` if testing components with stores
5. For singleton composables, use `vi.resetModules()` in `beforeEach`
6. Run `npm run test:unit:watch` during development

**E2E Test:**
1. Create `.spec.js` file in `client/e2e/`
2. Use Playwright's `test` and `expect`
3. Use `data-testid` attributes for selectors
4. Run `npm run test:e2e:headed` to debug
