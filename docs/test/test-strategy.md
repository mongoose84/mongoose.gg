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
| Backend | 14 test files | ~200 tests | ✅ GitHub Actions | ✅ Strong foundation |
| Frontend Unit | 24 test files | 445 tests | ✅ GitHub Actions | ✅ Strong foundation |
| E2E | 1 test file | ~5 tests | ✅ GitHub Actions | 🔴 Needs expansion |

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
- ⚪ No tests for Match endpoints (MatchList, MatchDetails, MatchNarrative)
- ⚪ No tests for ChampionSelect endpoints
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
│   ├── Matches/                  # 🔴 Missing (MatchList, MatchDetails, MatchNarrative)
│   ├── ChampionSelect/           # 🔴 Missing (ChampionSelect, SoloMatchups)
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
3. **Match Endpoints Tests** - Core feature (MatchList, MatchDetails, MatchNarrative)
4. **ChampionSelect Endpoints Tests** - Core feature for champion recommendations
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
- ⚪ Limited component coverage (~27% of components tested)
- ⚪ No chart component tests (LpTrendChart, WinrateChart)
- ⚪ No base component tests (BaseButton, BaseCard, BaseInput, BaseModal)

### 5.2 Current Test Coverage

| Category | Total | Tested | Coverage |
|----------|-------|--------|----------|
| **Composables** | 3 | 2 | 67% |
| **Services** | 5 | 2 | 40% |
| **Stores** | 2 | 1 | 50% |
| **Utils** | 2 | 0 | 0% |
| **Views** | 14 | 5 | 36% |
| **Components** | ~37 | ~10 | ~27% |

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
│   ├── base/                           # 🔴 Missing (5 components)
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
4. **formatters.spec.js** - 23 pure functions, heavily used across app - **HIGH PRIORITY**
5. **useWinRateColor.spec.js** - Pure function, simple to test
6. **uiStore.spec.js** - Sidebar state, localStorage, responsive behavior
7. **feedbackApi.spec.js** - Browser/OS detection helpers
8. **BaseButton.spec.js** - Most used component, test variants & interactions

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
- ✅ Test credentials via environment variables
- ✅ Solo dashboard flow tested (login, navigation, auth guards)

**Critical Gaps:**
- 🔴 Only 1 test file covering minimal flow
- 🔴 No registration/email verification tests
- 🔴 No Riot account linking tests
- 🔴 No match viewing tests
- 🔴 No error handling/negative path tests

### 6.2 Critical User Journeys to Test

```
client/e2e/
├── auth/
│   ├── login.spec.js              # ✅ Partial (in solo-dashboard)
│   ├── registration.spec.js       # 🔴 Missing (critical)
│   └── logout.spec.js             # 🔴 Missing
├── dashboards/
│   ├── solo-dashboard.spec.js     # ✅ Exists
│   ├── overview.spec.js           # 🔴 Missing
│   └── navigation.spec.js         # 🔴 Missing
├── accounts/
│   ├── link-riot-account.spec.js  # 🔴 Missing (critical)
│   └── manage-accounts.spec.js    # 🔴 Missing
└── matches/
    ├── match-list.spec.js         # 🔴 Missing
    └── match-details.spec.js      # 🔴 Missing
```

### 6.3 Priority E2E Tests to Add

1. **Login Flow (expand)** - Error states, remember me, session persistence
2. **Overview Dashboard** - Data loads, charts render, navigation works
3. **Riot Account Linking** - Core feature, requires careful test data setup
4. **Match List & Details** - View match history, navigate to details
5. **Error Handling** - Network errors, API failures, graceful degradation

### 6.4 E2E Testing Best Practices

```javascript
// Good E2E test patterns for Playwright
import { test, expect } from '@playwright/test';

test.describe('User Dashboard', () => {
  // Use test fixtures for common setup
  test.beforeEach(async ({ page }) => {
    // Login helper that can be extracted to a fixture
    await page.goto('/auth');
    await page.fill('[data-testid="email"]', process.env.E2E_TEST_USER);
    await page.fill('[data-testid="password"]', process.env.E2E_TEST_PASSWORD);
    await page.click('[data-testid="login-button"]');
    await page.waitForURL(/\/dashboard/);
  });

  test('displays user statistics', async ({ page }) => {
    // Wait for data to load, not just page render
    await expect(page.locator('[data-testid="stats-card"]')).toBeVisible();
    // Assert on user-visible outcomes
    await expect(page.locator('[data-testid="games-played"]')).not.toHaveText('0');
  });

  test('navigates to match details', async ({ page }) => {
    await page.click('[data-testid="match-row"]:first-child');
    await expect(page).toHaveURL(/\/matches\/\w+/);
  });
});
```

### 6.5 Test Data Management

**Recommendations:**
1. **Dedicated Test User**: Create `e2e-test@mongoose.gg` with known Riot accounts
2. **Stable Test Data**: Ensure test accounts have consistent match history
3. **Environment Isolation**: Use test database (`DB_CONNECTIONSTRING_TEST`)
4. **Data Refresh**: Document how to reset test data if needed



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
- Reusable test helper infrastructure established
- Backend business logic now has comprehensive coverage
- Frontend auth state management fully tested
- Session expiry handling fully tested
- Analysis status feature fully tested
- Fixed singleton state issue in useSyncWebSocket tests

### 7.2 Phase 2: Business Value (2-4 weeks)

Expand coverage for core user-facing features.

| Priority | Test | Layer | Effort | Status |
|----------|------|-------|--------|--------|
| P1 | `formatters.spec.js` | Frontend | 2h | ✅ Complete (56 tests) |
| P1 | Match endpoints tests | Backend | 4h | 🟡 Pending |
| P1 | ChampionSelect endpoints tests | Backend | 3h | 🟡 Pending |
| P1 | Overview dashboard E2E | E2E | 3h | 🟡 Pending |
| P1 | Riot account linking E2E | E2E | 4h | 🟡 Pending |
| P2 | `useWinRateColor.spec.js` | Frontend | 0.5h | � Medium |
| P2 | `uiStore.spec.js` | Frontend | 1h | 🟢 Medium |
| P2 | `feedbackApi.spec.js` | Frontend | 1h | 🟢 Medium |
| P2 | `LoginSyncServiceTests.cs` | Backend | 2h | 🟡 High |

### 7.3 Phase 3: Comprehensive Coverage (4-8 weeks)

Build out full test suite for long-term maintainability.

| Priority | Test | Layer | Effort | Impact |
|----------|------|-------|--------|--------|
| P2 | Base component tests (5 components) | Frontend | 4h | 🟢 Medium |
| P2 | Riot API client mocking | Backend | 6h | 🟡 High |
| P2 | Match component tests (13 components) | Frontend | 8h | 🟢 Medium |
| P3 | All remaining endpoints | Backend | 8h | 🟢 Medium |
| P3 | Chart component tests | Frontend | 4h | 🟢 Low |
| P3 | Error handling E2E | E2E | 4h | 🟢 Medium |
| P3 | Remaining view tests (9 views) | Frontend | 6h | 🟢 Medium |

### 7.4 Coverage Targets

| Layer | Phase 1 (Done) | Phase 2 | Phase 3 |
|-------|----------------|---------|---------|
| Backend Tests | ~200 tests | ~250 tests | ~300 tests |
| Frontend Tests | 389 tests | ~450 tests | ~550 tests |
| E2E Journeys | 1/8 | 4/8 | 8/8 |
| Components Tested | ~27% | ~50% | ~75% |
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

# E2E tests
cd client && npm run test:e2e            # Headless
cd client && npm run test:e2e:headed     # With browser
cd client && npm run test:e2e:ui         # Playwright UI
```

### 10.2 Key Files

| Purpose | Path |
|---------|------|
| Backend test factory | `server/Mongoose.Api.Tests/TestWebApplicationFactory.cs` |
| LP Calculation tests | `server/Mongoose.Api.Tests/LpCalculationServiceTests.cs` |
| Riot Match Mapper tests | `server/Mongoose.Api.Tests/RiotMatchMapperTests.cs` |
| Auth Store tests | `client/test/unit/authStore.spec.js` |
| API Client tests | `client/test/unit/apiClient.spec.js` |
| Formatters tests | `client/test/unit/formatters.spec.js` |
| Analysis Status tests | `client/test/unit/useAnalysisStatus.spec.js` |
| WebSocket tests | `client/test/unit/useSyncWebSocket.spec.js` |
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

*Last Updated: February 7, 2026*
*Version: 1.2 - Phase 1 Complete + Gap Analysis Update*
