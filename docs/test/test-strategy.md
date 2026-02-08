# Mongoose.gg Test Strategy

> **Goal**: Build a "Feature Factory" — a secure, high-confidence test infrastructure that enables rapid delivery of high-quality software.

## 1. Executive Summary

This document outlines a comprehensive test strategy for Mongoose.gg, covering three testing layers:
- **Backend Tests** (`server/Mongoose.Api.Tests/`) - .NET 9 xUnit tests
- **Frontend Unit Tests** (`client/test/`) - Vitest + Vue Test Utils
- **End-to-End Tests** (`client/e2e/`) - Playwright

### Current State Assessment

| Layer | Test Files | Tests | CI Integration | Status |
|-------|-----------|-------|----------------|--------|
| Backend | 15 test files | ~298 tests | ✅ GitHub Actions | ✅ Strong foundation |
| Frontend Unit | 30 test files | 583 tests | ✅ GitHub Actions | ✅ Strong foundation |
| E2E | 2 test files | ~23 tests | ✅ GitHub Actions | 🟡 Expanding |

### Phase 1 Completion Summary ✅

The following critical gap tests have been implemented:

| Test | Location | Tests | Status |
|------|----------|-------|--------|
| Auth Store Tests | `client/test/unit/authStore.spec.js` | 39 tests | ✅ Complete |
| LP Calculation Tests | `server/Mongoose.Api.Tests/LpCalculationServiceTests.cs` | 82 tests | ✅ Complete |
| Riot Match Mapper Tests | `server/Mongoose.Api.Tests/RiotMatchMapperTests.cs` | 28 tests | ✅ Complete |
| API Service Mocking Pattern | `client/test/helpers/` | Foundation | ✅ Complete |
| Session Expiry Tests | `client/test/unit/apiClient.spec.js` | 23 tests | ✅ Complete |
| Session Expired Banner | `client/test/unit/SessionExpiredBanner.spec.js` | 9 tests | ✅ Complete |
| Analysis Status Composable | `client/test/unit/useAnalysisStatus.spec.js` | 31 tests | ✅ Complete |
| Analysis Status Card | `client/test/unit/AnalysisStatusCard.spec.js` | 26 tests | ✅ Complete |
| Champion Select CTA | `client/test/unit/ChampionSelectCTA.spec.js` | 18 tests | ✅ Complete |
| **Base Components** | `client/test/unit/Base*.spec.js` | 137 tests | ✅ Complete |

---

## 2. Testing Philosophy

### 2.1 The Testing Pyramid

```
        /\
       /E2E\        (Few) - Critical user journeys only
      /______\
     /  API    \    (More) - Backend endpoint coverage
    /____________\
   / Unit (BE+FE) \ (Many) - Business logic, components, utils
  /________________\
```

### 2.2 Core Principles

1. **Test the Right Things**: Focus on high-risk, high-value areas first
2. **Fast Feedback Loop**: Unit tests < 5s, API tests < 30s, E2E < 2min
3. **Reliability Over Coverage**: A failing test must mean a real problem
4. **Shift Left**: Catch bugs as early as possible in the pipeline
5. **Test Behavior, Not Implementation**: Focus on what, not how

---

## 3. Critical Paths Analysis

Based on codebase analysis, these are the **highest-impact areas** requiring robust testing:

### 3.1 🔴 Critical (Must Test Thoroughly)

| Area | Risk Level | Current Coverage | Priority |
|------|------------|------------------|----------|
| **Authentication Flow** (Login/Register/Verify) | 🔴 High | ✅ Good | Maintain |
| **Match Sync Job** (Data ingestion) | 🔴 High | 🟡 Partial | Expand |
| **Riot API Integration** (External dependency) | 🔴 High | ⚪ None | Add mocking |
| **Security/Encryption** (AES, password hashing) | 🔴 High | ✅ Good | Maintain |
| **Email Verification** (Account security) | 🔴 High | ✅ Good | Maintain |

### 3.2 🟡 High Priority (Business Value)

| Area | Risk Level | Current Coverage | Priority |
|------|------------|------------------|----------|
| **Overview Dashboard** (Core feature) | 🟡 Medium | ✅ Good | Maintain |
| **Solo Performance Stats** | 🟡 Medium | 🔴 Minimal | Expand |
| **Match List & Details** | 🟡 Medium | ⚪ None | Add |
| **LP Calculations** | 🟡 Medium | ✅ Complete (82 tests) | ✅ Done |
| **Auth Store (Pinia)** | 🟡 Medium | ✅ Complete (39 tests) | ✅ Done |
| **WebSocket Sync** | 🟡 Medium | ✅ Complete (19 tests) | ✅ Done |
| **Riot Match Mapper** | 🟡 Medium | ✅ Complete (28 tests) | ✅ Done |
| **Session Expiry Handling** | 🟡 Medium | ✅ Complete (32 tests) | ✅ Done |
| **Analysis Status** | 🟡 Medium | ✅ Complete (57 tests) | ✅ Done |

### 3.3 🟢 Standard Priority

| Area | Risk Level | Current Coverage | Priority |
|------|------------|------------------|----------|
| **Static Pages** (Terms, Privacy, Landing) | 🟢 Low | ✅ Good | Maintain |
| **UI Components** (Base components) | 🟢 Low | 🟡 Partial | Expand |
| **Formatting Utils** | 🟢 Low | ⚪ None | Add |
| **Chart Components** | 🟢 Low | ⚪ None | Consider |
| **Feedback Feature** | 🟢 Low | ⚪ None | Add |

---

## 4. Backend Testing Strategy

### 4.1 Current Test Infrastructure

**Strengths:**
- ✅ `TestWebApplicationFactory` provides excellent dependency injection mocking
- ✅ Fake repositories for Users, RiotAccounts, Tokens, Analytics, LP snapshots
- ✅ Integration tests using real HTTP client against test server
- ✅ FluentAssertions for readable test assertions

**Gaps:**
- ~~⚪ No tests for Match endpoints (MatchList, MatchDetails, MatchNarrative)~~ ✅ Match endpoint tests added (19 tests)
- ~~⚪ No tests for ChampionSelect endpoints~~ ✅ ChampionSelect endpoint tests added (16 tests)
- ⚪ No Riot API client mocking
- ⚪ Limited negative path testing
- ⚪ No LoginSyncService tests
- ~~⚪ No data mapper tests (RiotMatchMapper, RiotTimelineMapper)~~ ✅ RiotMatchMapper tests added

### 4.2 Recommended Test Categories

```
server/Mongoose.Api.Tests/
├── Endpoints/                    # API integration tests
│   ├── Auth/                     # ✅ Exists (Login, Verify, Resend, etc.)
│   ├── Overview/                 # ✅ Exists
│   ├── Solo/                     # ✅ Exists (needs expansion)
│   ├── Matches/                  # ✅ Complete (MatchEndpointTests.cs - 19 tests)
│   ├── ChampionSelect/           # ✅ Complete (ChampionSelectEndpointTests.cs - 16 tests)
│   ├── Trends/                   # 🔴 Missing (WinrateTrend)
│   └── Feedback/                 # ✅ Exists
├── Services/                     # Unit tests for business logic
│   ├── LpCalculationServiceTests.cs    # ✅ Complete (82 tests)
│   ├── MainChampionRecommenderTests.cs # ✅ Exists
│   ├── LoginSyncServiceTests.cs        # 🔴 Missing
│   └── SeasonHelperTests.cs            # 🔴 Missing
├── Mappers/                      # Data transformation tests
│   ├── RiotMatchMapperTests.cs         # ✅ Complete (28 tests)
│   └── RiotTimelineMapperTests.cs      # 🔴 Missing
├── Infrastructure/               # Security, email, etc
│   ├── AesEncryptorTests.cs            # ✅ Exists
│   ├── VerificationCodeGeneratorTests.cs # ✅ Exists
│   └── SyncProgressHubTests.cs         # ✅ Exists
└── Jobs/                         # Background job tests
    ├── MatchHistorySyncJobTests.cs     # ✅ Exists
    └── MatchCleanupJobTests.cs         # ✅ Exists
```

### 4.3 Priority Backend Tests to Add

1. ~~**LpCalculationService Tests** - Pure functions, easy to test, high business value~~ ✅ Complete
2. ~~**RiotMatchMapper Tests** - Critical data transformation layer~~ ✅ Complete
3. ~~**Match Endpoints Tests** - Core feature (MatchList, MatchDetails, MatchNarrative)~~ ✅ Complete (19 tests)
4. ~~**ChampionSelect Endpoints Tests** - Core feature for champion recommendations~~ ✅ Complete (16 tests)
5. **LoginSyncService Tests** - Triggers on login, important for data freshness
6. **Riot API Client Mocking** - Enable testing sync flows without real API

---

## 5. Frontend Testing Strategy

### 5.1 Current Test Infrastructure

**Strengths:**
- ✅ Vitest configured with happy-dom for fast component testing
- ✅ Vue Test Utils for component mounting and interaction
- ✅ Coverage reporting with @vitest/coverage-v8
- ✅ Good page-level tests (AuthPage, TermsPage, PrivacyPage, LandingPage, VerifyPage)
- ✅ Comprehensive WebSocket composable tests (19 tests)
- ✅ Comprehensive auth store tests (39 tests)
- ✅ Session expiry handling tests (32 tests across apiClient + SessionExpiredBanner)
- ✅ Analysis status tests (57 tests across composable + component)
- ✅ Reusable test helpers in `client/test/helpers/`

**Gaps:**
- ~~⚪ No auth store (Pinia) tests~~ ✅ Complete (39 tests)
- ~~⚪ No API service tests~~ ✅ apiClient tested (23 tests)
- ⚪ No utility function tests (`formatters.js` - 23 functions, `leagueAssets.js`)
- ⚪ No uiStore tests (sidebar state, responsive behavior)
- ⚪ No feedbackApi tests (browser/OS detection helpers)
- ⚪ Limited component coverage (~35% of components tested)
- ⚪ No chart component tests (LpTrendChart, WinrateChart)
- ~~⚪ No base component tests (BaseButton, BaseCard, BaseInput, BaseModal)~~ ✅ Complete (137 tests)

### 5.2 Current Test Coverage

| Category | Total | Tested | Coverage |
|----------|-------|--------|----------|
| **Composables** | 3 | 2 | 67% |
| **Services** | 5 | 2 | 40% |
| **Stores** | 2 | 1 | 50% |
| **Utils** | 2 | 1 | 50% |
| **Views** | 14 | 5 | 36% |
| **Components** | ~43 | ~16 | ~37% |
| **Base Components** | 6 | 6 | 100% |

### 5.3 Recommended Test Categories

```
client/test/unit/
├── components/                   # Component tests
│   ├── AnalysisStatusCard.spec.js      # ✅ Complete (26 tests)
│   ├── ChampionSelectCTA.spec.js       # ✅ Complete (18 tests)
│   ├── LastMatchCard.spec.js           # ✅ Exists
│   ├── LinkRiotAccountModal.spec.js    # ✅ Exists
│   ├── NavBar.spec.js                  # ✅ Exists
│   ├── OverviewPlayerHeader.spec.js    # ✅ Exists
│   ├── RankSnapshot.spec.js            # ✅ Exists
│   ├── SessionExpiredBanner.spec.js    # ✅ Complete (9 tests)
│   ├── VersionBadge.spec.js            # ✅ Exists
│   ├── BaseButton.spec.js              # ✅ Complete (27 tests)
│   ├── BaseCard.spec.js                # ✅ Complete (17 tests)
│   ├── BaseInput.spec.js               # ✅ Complete (32 tests)
│   ├── BaseModal.spec.js               # ✅ Complete (22 tests)
│   ├── BaseQueueToggle.spec.js         # ✅ Complete (17 tests)
│   ├── BaseTimeRangeSelect.spec.js     # ✅ Complete (18 tests)
│   ├── matches/                        # 🔴 Missing (13 components)
│   └── overview/                       # 🟡 Partial (2 of 7 tested)
├── composables/                  # Composable tests
│   ├── useSyncWebSocket.spec.js        # ✅ Complete (19 tests)
│   ├── useAnalysisStatus.spec.js       # ✅ Complete (31 tests)
│   └── useWinRateColor.spec.js         # 🔴 Missing
├── stores/                       # Pinia store tests
│   ├── authStore.spec.js               # ✅ Complete (39 tests)
│   └── uiStore.spec.js                 # 🔴 Missing
├── services/                     # API service tests
│   ├── apiClient.spec.js               # ✅ Complete (23 tests)
│   ├── analyticsApi.spec.js            # ✅ Exists (15 tests)
│   ├── feedbackApi.spec.js             # 🔴 Missing
│   └── authApi.spec.js                 # 🟡 Partial (via authStore)
├── utils/                        # Utility function tests
│   ├── formatters.spec.js              # 🔴 Missing (23 functions!)
│   └── leagueAssets.spec.js            # 🔴 Missing
└── pages/                        # Page-level tests
    ├── AuthPage.spec.js                # ✅ Exists
    ├── LandingPage.spec.js             # ✅ Exists
    ├── PrivacyPage.spec.js             # ✅ Exists
    ├── TermsPage.spec.js               # ✅ Exists
    ├── VerifyPage.spec.js              # ✅ Exists
    └── ...                             # 🔴 9 views missing
```

### 5.4 Priority Frontend Tests to Add

1. ~~**authStore.spec.js** - Test login/logout state, token handling, user persistence~~ ✅ Complete
2. ~~**apiClient.spec.js** - Session expiry, 401 handling, HTTP methods~~ ✅ Complete
3. ~~**useAnalysisStatus.spec.js** - Analysis status composable~~ ✅ Complete
4. ~~**formatters.spec.js** - 23 pure functions, heavily used across app~~ ✅ Complete (56 tests)
5. **useWinRateColor.spec.js** - Pure function, simple to test
6. **uiStore.spec.js** - Sidebar state, localStorage, responsive behavior
7. **feedbackApi.spec.js** - Browser/OS detection helpers
8. ~~**BaseButton.spec.js** - Most used component, test variants & interactions~~ ✅ Complete (27 tests)
9. ~~**Base Components** - All 6 base components~~ ✅ Complete (137 tests total)

### 5.4 Component Testing Patterns

```javascript
// Pattern for testing Vue 3 components with Composition API
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

### 5.5 Store Testing Patterns

```javascript
// Pattern for testing Pinia stores
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

### 5.6 Test Helpers (NEW) ✅

A centralized test helpers module has been established at `client/test/helpers/`:

```javascript
// Import helpers using the @test alias
import {
  createMockUser,
  createMockRiotAccount,
  createAuthApiMock,
  createMockAuthStore,
  setupPinia,
  headlessUIStubs,
  createWrapper,
  waitFor,
  cleanupMocks
} from '@test/helpers';

// Example: Testing a component with mocked store
vi.mock('@/stores/authStore', () => ({
  useAuthStore: () => createMockAuthStore({ isAuthenticated: true })
}));

// Example: Testing with HeadlessUI components
const wrapper = createWrapper(MyModal, {
  attachToBody: true,  // For modals/portals
  props: { isOpen: true }
});
afterEach(() => wrapper.cleanup()); // Clean up DOM

// Example: Mocking fetch
beforeEach(() => createMockFetch({ ok: true, json: () => ({}) }));
afterEach(() => cleanupMocks()); // Restores original fetch
```

**Available Helpers:**

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

---

## 6. E2E Testing Strategy

### 6.1 Current State

**What Exists:**
- ✅ Playwright configured with Chromium and Firefox
- ✅ CI workflow starts both backend and frontend
- ✅ Global setup/teardown for dynamic user creation
- ✅ Riot account linking in global setup
- ✅ Auto-verified email in non-production environments
- ✅ Solo dashboard flow tested (login, navigation, auth guards)
- ✅ Overview dashboard flow tested (18 tests)

**Remaining Gaps:**
- 🔴 No match viewing tests
- 🔴 No error handling/negative path tests

### 6.2 E2E Architecture

The E2E tests use Playwright's global setup/teardown pattern for efficient test execution:

```
client/e2e/
├── global-setup.js          # ✅ Creates user, links Riot account, saves auth state
├── global-teardown.js       # ✅ Deletes test user, cleans up auth files
├── .auth/
│   ├── user.json            # Saved auth state (auto-generated)
│   └── test-user.json       # Test user metadata (auto-generated)
├── solo-dashboard.spec.js   # ✅ Solo dashboard tests
└── overview-dashboard.spec.js # ✅ Overview dashboard tests (18 tests)
```

**Flow:**
1. **Global Setup** (runs once before all tests):
   - Registers a unique test user (`e2e_test_{timestamp}`)
   - Auto-verifies email (non-production environments)
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

### 6.3 Key Implementation Details

**Auto-Verification via Config Flag:**
The `RegisterEndpoint` checks `Auth:AutoVerifyEmail` config flag. When set to `true`, email is auto-verified, bypassing the email verification flow. This is required for E2E tests because:
1. Tests cannot access email inboxes to retrieve verification codes
2. The verification code is only logged in dev mode, not returned in API responses
3. Manual verification would break automated test flows

**Riot Account Configuration:**
Riot account credentials are hardcoded in `client/e2e/global-setup.js`:
```javascript
const RIOT_ACCOUNT = {
  gameName: 'Doend',
  tagLine: 'EUW',
  region: 'euw1',
};
```

**Unauthenticated Tests:**
For tests that need to verify unauthenticated behavior, override the storage state:
```javascript
test('redirects unauthenticated users', async ({ browser }) => {
  const context = await browser.newContext({ storageState: undefined });
  const page = await context.newPage();
  // Test unauthenticated behavior...
});
```

### 6.4 E2E Security Configuration ⚠️

**CRITICAL: The `Auth:AutoVerifyEmail` setting must NEVER be enabled in production.**

The E2E test infrastructure uses this setting to bypass email verification. Here's how it's secured:

| Environment | How `Auth:AutoVerifyEmail` is Set | Security |
|-------------|-----------------------------------|----------|
| **CI (GitHub Actions)** | Dynamically generated in `ci-e2e.yml` workflow | ✅ Never committed to repo |
| **Local Development** | Set via environment variable in `playwright.config.js` | ✅ Only active when running E2E tests |
| **Production** | Not set (defaults to `false`) | ✅ Email verification required |

**Local E2E Test Configuration:**

Start the backend manually with E2E-specific environment variables:

```bash
# Start server with E2E flags (in one terminal)
Auth__AutoVerifyEmail=true Email__DevMode=true dotnet run --project server

# Run E2E tests (in another terminal)
cd client && npm run test:e2e
```

**Why This Approach is Secure:**

1. **No config file changes**: The setting is passed via environment variable, not stored in any config file
2. **Explicit naming**: `Auth__AutoVerifyEmail` clearly indicates its purpose
3. **Scoped to test runner**: Only active when Playwright starts the server
4. **Defense in depth**: Production deployments use separate config management (secrets, environment-specific settings)
5. **Code review**: Any attempt to add this to committed config files would be caught in PR review

**Alternative Approaches Considered (and rejected):**

| Approach | Why Rejected |
|----------|--------------|
| Return verification code in API response | Exposes codes if `Email:DevMode` accidentally enabled in production |
| Hardcode test verification code | Creates a backdoor that could be exploited |
| Skip verification in router guards | Would require client-side changes that could leak to production |
| Use real email service in tests | Slow, flaky, requires email infrastructure |

### 6.5 E2E Testing Best Practices

```javascript
// Good E2E test patterns for Playwright
import { test, expect } from '@playwright/test';

test.describe('User Dashboard', () => {
  // Auth is handled by global setup - no login needed in beforeEach!

  test('displays user statistics', async ({ page }) => {
    await page.goto('/app/overview');
    // Wait for data to load, not just page render
    await expect(page.locator('[data-testid="stats-card"]')).toBeVisible();
    // Assert on user-visible outcomes
    await expect(page.locator('[data-testid="games-played"]')).not.toHaveText('0');
  });

  test('navigates to match details', async ({ page }) => {
    await page.goto('/app/matches');
    await page.click('[data-testid="match-row"]:first-child');
    await expect(page).toHaveURL(/\/matches\/\w+/);
  });
});
```

### 6.6 Test Data Management

**Current Implementation:**
1. **Dynamic Test User**: Created fresh each test run (`e2e_test_{timestamp}@test.mongoose.gg`)
2. **Dedicated Riot Account**: `Doend#EUW` linked during global setup
3. **Environment Isolation**: Uses test database (`Database_test`)
4. **Automatic Cleanup**: Test user deleted in global teardown

**Database Requirements:**
- `username` column must be `VARCHAR(255)` to accommodate encrypted values (64+ chars)
- `email` column is `VARCHAR(255)` (already sufficient)



---

## 7. Implementation Roadmap

### 7.1 Phase 1: Critical Gaps ✅ COMPLETE

Focus on highest-risk areas with missing coverage.

| Priority | Test | Layer | Effort | Status |
|----------|------|-------|--------|--------|
| P0 | `authStore.spec.js` | Frontend | 2h | ✅ Complete (39 tests) |
| P0 | `LpCalculationServiceTests.cs` | Backend | 2h | ✅ Complete (82 tests) |
| P0 | Registration E2E flow | E2E | 4h | 🔴 Pending |
| P1 | `RiotMatchMapperTests.cs` | Backend | 3h | ✅ Complete (28 tests) |
| P1 | API service mocking pattern | Frontend | 2h | ✅ Complete (helpers/) |
| P1 | `apiClient.spec.js` (session expiry) | Frontend | 2h | ✅ Complete (23 tests) |
| P1 | `SessionExpiredBanner.spec.js` | Frontend | 1h | ✅ Complete (9 tests) |
| P1 | `useAnalysisStatus.spec.js` | Frontend | 2h | ✅ Complete (31 tests) |
| P1 | `AnalysisStatusCard.spec.js` | Frontend | 2h | ✅ Complete (26 tests) |
| P1 | `ChampionSelectCTA.spec.js` | Frontend | 1h | ✅ Complete (18 tests) |

**Phase 1 Results:**
- 256+ new frontend tests added
- 110 new backend tests added (82 + 28)

**Phase 2 Results (In Progress):**
- 56 new frontend tests (formatters.spec.js)
- 19 new backend tests (MatchEndpointTests.cs)
- 16 new backend tests (ChampionSelectEndpointTests.cs)
- 18 new E2E tests (overview-dashboard.spec.js)
- Reusable test helper infrastructure established
- Backend business logic now has comprehensive coverage
- Frontend auth state management fully tested
- Session expiry handling fully tested
- Analysis status feature fully tested
- Fixed singleton state issue in useSyncWebSocket tests
- Overview dashboard E2E coverage complete
- **E2E global setup/teardown architecture implemented**
- **Riot account linking integrated into E2E flow**
- **Auto-email verification via `Auth:AutoVerifyEmail` config flag (CI only)**
- **Fixed database schema: username VARCHAR(255) for encrypted values**

### 7.2 Phase 2: Business Value (2-4 weeks)

Expand coverage for core user-facing features.

| Priority | Test | Layer | Effort | Status |
|----------|------|-------|--------|--------|
| P1 | `formatters.spec.js` | Frontend | 2h | ✅ Complete (56 tests) |
| P1 | Match endpoints tests | Backend | 4h | ✅ Complete (19 tests) |
| P1 | ChampionSelect endpoints tests | Backend | 3h | ✅ Complete (16 tests) |
| P1 | Overview dashboard E2E | E2E | 3h | ✅ Complete (18 tests) |
| P1 | Riot account linking E2E | E2E | 4h | ✅ Complete (global-setup.js) |
| P2 | `useWinRateColor.spec.js` | Frontend | 0.5h | 🟢 Medium |
| P2 | `uiStore.spec.js` | Frontend | 1h | 🟢 Medium |
| P2 | `feedbackApi.spec.js` | Frontend | 1h | 🟢 Medium |
| P2 | `LoginSyncServiceTests.cs` | Backend | 2h | 🟡 High |

### 7.3 Phase 3: Comprehensive Coverage (4-8 weeks)

Build out full test suite for long-term maintainability.

| Priority | Test | Layer | Effort | Impact |
|----------|------|-------|--------|--------|
| ~~P2~~ | ~~Base component tests (6 components)~~ | Frontend | ~~4h~~ | ✅ Complete (137 tests) |
| P2 | Riot API client mocking | Backend | 6h | 🟡 High |
| P2 | Match component tests (13 components) | Frontend | 8h | 🟢 Medium |
| P3 | All remaining endpoints | Backend | 8h | 🟢 Medium |
| P3 | Chart component tests | Frontend | 4h | 🟢 Low |
| P3 | Error handling E2E | E2E | 4h | 🟢 Medium |
| P3 | Remaining view tests (9 views) | Frontend | 6h | 🟢 Medium |

### 7.4 Coverage Targets

| Layer | Phase 1 (Done) | Phase 2 (Current) | Phase 3 |
|-------|----------------|---------|---------|
| Backend Tests | ~263 tests | ~298 tests | ~360 tests |
| Frontend Tests | 389 tests | 583 tests | ~650 tests |
| E2E Journeys | 2/8 | 4/8 | 8/8 |
| Components Tested | ~27% | ~37% | ~75% |
| Base Components | 0% | 100% | 100% |
| Utils/Formatters | 0% | 100% | 100% |

---

## 8. CI/CD Integration

### 8.1 Current Pipeline

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  Client CI      │     │  Server CI      │     │  E2E CI         │
│  (ci-client.yml)│     │  (ci-server.yml)│     │  (ci-e2e.yml)   │
├─────────────────┤     ├─────────────────┤     ├─────────────────┤
│ • npm ci        │     │ • dotnet restore│     │ • Build both    │
│ • Unit tests    │     │ • dotnet test   │     │ • Start servers │
│ • Build         │     │ • Publish       │     │ • Playwright    │
│ • Deploy        │     │ • Deploy        │     │ • Upload traces │
└─────────────────┘     └─────────────────┘     └─────────────────┘
      ↓                       ↓                       ↓
    main                    main                 PR + main
```

### 8.2 Recommendations

1. **PR Gate**: Require all three test suites pass before merge
2. **Coverage Enforcement**: Fail PRs that decrease coverage
3. **Parallel Execution**: Run client + server tests in parallel
4. **Flaky Test Tracking**: Monitor and fix tests that fail intermittently
5. **Performance Budgets**: Alert if test suite exceeds time limits

### 8.3 Test Execution Strategy

| Trigger | Unit Tests | API Tests | E2E Tests |
|---------|-----------|-----------|-----------|
| PR Created | ✅ All | ✅ All | ✅ All |
| Push to main | ✅ All | ✅ All | ✅ All |
| Scheduled (nightly) | - | - | ✅ Full matrix |

---

## 9. Success Metrics

### 9.1 Quality Indicators

| Metric | Current | Target | Measurement |
|--------|---------|--------|-------------|
| Test Suite Pass Rate | ~95% | 99%+ | CI dashboard |
| Mean Time to Fix | Unknown | <4h | PR merge time |
| Production Bugs | Unknown | -50% | Issue tracking |
| Test Execution Time | ~3min | <5min | CI timing |

### 9.2 Health Checks

**Weekly Review:**
- [ ] All tests passing on main
- [ ] No new flaky tests introduced
- [ ] Coverage trending up (or stable)
- [ ] No skipped/disabled tests without tickets

**Monthly Review:**
- [ ] Test strategy alignment with roadmap
- [ ] New features have adequate coverage
- [ ] Technical debt in tests addressed

---

## 10. Quick Reference

### 10.1 Running Tests

```bash
# Backend
cd server && dotnet test

# Frontend unit tests
cd client && npm run test:unit
cd client && npm run test:unit:watch     # Watch mode
cd client && npm run test:unit:coverage  # With coverage

# E2E tests (requires backend - see note below)
cd client && npm run test:e2e            # Headless
cd client && npm run test:e2e:headed     # With browser
cd client && npm run test:e2e:ui         # Playwright UI
```

**⚠️ E2E Test Local Setup:**

E2E tests require the backend server running with `Auth:AutoVerifyEmail=true`:

```bash
# Terminal 1: Start server with E2E flags
Auth__AutoVerifyEmail=true Email__DevMode=true dotnet run --project server

# Terminal 2: Run E2E tests
cd client && npm run test:e2e
```

In CI, this is handled automatically by the `ci-e2e.yml` workflow which generates the config with `AutoVerifyEmail: true`.

### 10.2 Key Files

| Purpose | Path |
|---------|------|
| Backend test factory | `server/Mongoose.Api.Tests/TestWebApplicationFactory.cs` |
| LP Calculation tests | `server/Mongoose.Api.Tests/LpCalculationServiceTests.cs` |
| Riot Match Mapper tests | `server/Mongoose.Api.Tests/RiotMatchMapperTests.cs` |
| Match Endpoint tests | `server/Mongoose.Api.Tests/MatchEndpointTests.cs` |
| Auth Store tests | `client/test/unit/authStore.spec.js` |
| API Client tests | `client/test/unit/apiClient.spec.js` |
| Formatters tests | `client/test/unit/formatters.spec.js` |
| Analysis Status tests | `client/test/unit/useAnalysisStatus.spec.js` |
| WebSocket tests | `client/test/unit/useSyncWebSocket.spec.js` |
| Base Component tests | `client/test/unit/Base*.spec.js` (6 files, 137 tests) |
| Test helpers | `client/test/helpers/` |
| Vitest config | `client/vitest.config.js` |
| Playwright config | `client/playwright.config.js` |
| CI - Client | `.github/workflows/ci-client.yml` |
| CI - Server | `.github/workflows/ci-server.yml` |
| CI - E2E | `.github/workflows/ci-e2e.yml` |

### 10.3 Adding New Tests

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

### 10.4 Testing Patterns Learned

**Singleton Composable Testing:**
When testing composables with module-level state (like `useSyncWebSocket`), reset modules between tests:
```javascript
let useSyncWebSocket;
beforeEach(async () => {
  vi.resetModules();
  const module = await import('@/composables/useSyncWebSocket');
  useSyncWebSocket = module.useSyncWebSocket;
});
```

**Reactive Mock State:**
When mocking composables for component tests, use Vue's `ref()` for reactive state:
```javascript
const mockStatus = ref('idle');
vi.mock('@/composables/useAnalysisStatus', () => ({
  useAnalysisStatus: () => ({ status: mockStatus, ... })
}));
```

---

*Last Updated: February 8, 2026*
*Version: 1.3 - Phase 2 Progress + Base Component Tests Complete*
