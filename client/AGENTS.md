# Client — Agent Instructions

> Vue 3 SPA frontend for Mongoose.gg — League of Legends match analytics dashboard.
> For the complete design system, component inventory, UX contracts, and page responsibilities see [ui-ux.spec.md](../.github/specs/ui-ux.spec.md).
> For backend API endpoints and DTOs see [architecture.spec.md](../.github/specs/architecture.spec.md).
> For coding patterns (components, stores, API, styling, testing) see [frontend.instructions.md](../.github/instructions/frontend.instructions.md).

## Build & Run

```bash
# From client/ directory
npm install                         # Install dependencies
npm run dev                         # Starts on http://localhost:5174 (proxies /api → localhost:5164)
npm run build                       # Production build to dist/
npm run preview                     # Preview production build
```

The backend must be running on `localhost:5164` for API calls to work.

## Test

```bash
# Unit tests (Vitest + Vue Test Utils + jsdom)
npm run test:unit                   # Run all unit tests
npm run test:unit:watch             # Watch mode
npm run test:unit:coverage          # With V8 coverage

# E2E tests (Playwright)
npm run test:e2e:smoke              # Fast PR smoke suite (Chromium only)
npm run test:e2e:full               # Comprehensive post-merge regression suite (Chromium + Firefox)
npm run test:e2e                    # Run all Playwright suites locally
npm run test:e2e:headed             # Headed mode for debugging
npm run test:e2e:ui                 # Playwright UI mode
npm run test:e2e:report             # Open last HTML report
```

**E2E prerequisite**: Backend must run with E2E flags:
```bash
Auth__AutoVerifyEmail=true Email__DevMode=true dotnet run --project server/Mongoose.Api
```

Unit tests: `test/unit/*.spec.js`. E2E tests: `e2e/*.spec.js`.

## Architecture

```
client/
├── src/
│   ├── App.vue                  # Root shell: RouterView + VersionBadge + SessionExpiredBanner
│   ├── main.js                  # Entry: creates Vue app with Pinia, Vue Router, TanStack Vue Query
│   ├── style.css                # Design tokens (CSS variables) + Tailwind directives + global styles
│   ├── router/index.js          # All routes, auth guards, page view tracking
│   ├── views/                   # Page components (one per route)
│   │   ├── OverviewPage.vue     # Post-login landing — orientation dashboard
│   │   ├── SoloStatsPage.vue    # Solo analysis — LP + winrate trends (free tier)
│   │   ├── TeamAnalytics.vue    # Team analysis (Pro tier — shows teaser for free)
│   │   ├── MatchesPage.vue      # Match history list + expandable details
│   │   ├── ChampionSelectPage.vue # Real-time champion select support
│   │   ├── GoalsPage.vue        # Goal management
│   │   ├── UserSettingsPage.vue  # Account, Riot link, subscription
│   │   ├── AuthPage.vue         # Login/register (?mode=login|register)
│   │   ├── LandingPage.vue      # Marketing page (public)
│   │   └── ...                  # FeedbackPage, VerifyPage, PrivacyPage, TermsPage
│   ├── layouts/
│   │   └── AppLayout.vue        # Authenticated shell: sidebar + main content + idle detection
│   ├── components/
│   │   ├── base/                # Reusable primitives: BaseButton, BaseCard, BaseModal, BaseInput,
│   │   │                        #   BaseQueueToggle, BaseTimeRangeSelect (barrel export via index.js)
│   │   ├── overview/            # OverviewLayout, PlayerHeader, RankSnapshot, LastMatchCard,
│   │   │                        #   MatchActivityHeatmap, AnalysisStatusCard, ChampionSelectCTA
│   │   ├── matches/             # MatchList, MatchRow, MatchDetails, MatchHeader, MatchHighlights,
│   │   │                        #   MatchNarrative, StatSnapshot, ImpactStats, LaneMatchupDetails,
│   │   │                        #   TeamComparison, TrendBadge, HighlightTile, MatchActions
│   │   ├── solo/                # SummaryStatsCard, TrendChartCard, LpChart, WinrateChart
│   │   ├── shared/              # AnalysisLayout (zone-based layout for Solo/Team)
│   │   ├── AppSidebar.vue       # Left nav with collapsible state, Pro tier lock icons
│   │   ├── AppHeader.vue
│   │   └── ...                  # Modals, NavBar, VersionBadge, chart components
│   ├── composables/             # useWinRateColor, useSyncWebSocket, useAnalysisStatus
│   ├── stores/                  # Pinia: authStore (user/session), uiStore (sidebar state)
│   ├── services/                # API client, authApi (main surface), analyticsApi, feedbackApi
│   └── utils/                   # formatters.js (KDA, time, numbers), leagueAssets.js (CDN URLs)
├── test/
│   ├── setup.js                 # Global mocks: ResizeObserver, IntersectionObserver, matchMedia
│   ├── helpers/                 # Test utilities barrel
│   │   ├── testUtils.js         # setupPinia(), createWrapper(), headlessUIStubs, waitFor()
│   │   └── apiMocks.js          # createMockUser(), createMockMatch(), createAuthApiMock(), etc.
│   └── unit/                    # *.spec.js files (one per component/service/store)
├── e2e/                         # Playwright E2E tests
│   ├── global-setup.js          # Creates test user via API before all tests
│   ├── global-teardown.js       # Cleans up test user after all tests
│   └── *.spec.js                # Page-level E2E tests
├── public/assets/               # Static assets (hero-bg.svg, etc.)
├── tailwind.config.js           # Tailwind ← CSS variable bridge
├── vite.config.js               # Dev server (port 5174), API proxy, @ alias
└── vitest.config.js             # jsdom env, coverage, @/@test aliases
```

## Key Patterns

> Component structure, store patterns, API service patterns, styling rules, and testing patterns are in [frontend.instructions.md](../.github/instructions/frontend.instructions.md). This section covers client-specific runtime details only.

### Page → Layout → Component Hierarchy

- **Public pages** (`LandingPage`, `AuthPage`, `PrivacyPage`, `TermsPage`): standalone, use `NavBar`
- **App pages**: wrapped in `AppLayout` (sidebar + content area)
  - **Overview**: uses `OverviewLayout` with named slots (`#header`, `#glance-left`, `#glance-right`, etc.)
  - **Solo/Team**: use `AnalysisLayout` with zone slots (`#context-bar`, `#summary`, `#trend-charts`)

### API Layer

`services/apiClient.js` is the centralized fetch wrapper:
- Cookie-based auth (`credentials: 'include'`)
- Global 401 session expiry detection → triggers `SessionExpiredBanner`
- All API functions in `services/authApi.js` (dashboards, matches, trends, auth, sync)

API base: dev `http://localhost:5164/api/v2`, prod `https://api.mongoose.gg/api/v2`.

### State Management

Two Pinia stores:
- **`authStore`** — user session, auth actions, Riot account management, `isAuthenticated`/`isVerified`/`tier` computed
- **`uiStore`** — sidebar collapsed state (persisted to `localStorage`), mobile breakpoint (1024px)

### Composables

- **`useSyncWebSocket()`** — WebSocket connection to `/ws/sync` for real-time match sync progress. Provides `syncProgress`, `subscribe()`, `resetProgress()`.
- **`useWinRateColor()`** — returns CSS class (`winrate-terrible` through `winrate-great`) based on win rate threshold
- **`useAnalysisStatus()`** — tracks sync/analysis state for `AnalysisStatusCard`

### Testing Patterns

**Unit tests** (`test/unit/`):
```javascript
import { createWrapper, setupPinia, headlessUIStubs } from '@test/helpers'
import { createMockUser, createAuthApiMock } from '@test/helpers'

beforeEach(() => { setupPinia() })

// Mount with standard config
const wrapper = createWrapper(MyComponent, {
  props: { ... },
  global: { stubs: headlessUIStubs }
})
```

- Mock API services at module level: `vi.mock('@/services/authApi', () => createAuthApiMock())`
- Mock stores for component tests: `vi.mock('@/stores/authStore', () => ({ useAuthStore: () => createMockAuthStore() }))`
- Use `waitFor()` for async assertions
- HeadlessUI components must be stubbed via `headlessUIStubs`

**E2E tests** (`e2e/`):
- Global setup creates a real test user via API; teardown deletes it
- Auth state stored in `e2e/.auth/user.json` and shared across browser projects
- Two browser targets: Chromium + Firefox

## Conventions

- **`@` alias** → `src/`, **`@test` alias** → `test/` (configured in both vite and vitest)
- **One component per file**, filename matches component name (PascalCase)
- **`.spec.js` test files** mirror component names (e.g., `BaseButton.vue` → `BaseButton.spec.js`)
- **Props are kebab-case** in templates, camelCase in `<script setup>`
- **Base components** (`BaseButton`, `BaseCard`, `BaseModal`, `BaseInput`) for all common UI — always reuse before creating custom
- **Heroicons** (`@heroicons/vue`) for all iconography — `/24/solid` or `/24/outline`
- **Chart.js + vue-chartjs** for data visualization (winrate charts)
- **Headless UI** (`@headlessui/vue`) for accessible interactive components (Dialog, Menu, Listbox, Transition)
- **No direct DOM manipulation** — use Vue reactivity and refs
- **UTC dates** — all timestamps from API are UTC, formatted client-side via `formatters.js`
