# Client — Local Context

> Vue 3 SPA frontend for Mongoose.gg — League of Legends match analytics dashboard.
> For repo-wide invariants see [CLAUDE.md](../CLAUDE.md).
> For the complete design system, component inventory, UX contracts, and page responsibilities see [ui-ux.spec.md](../.github/specs/ui-ux.spec.md).
> For backend API endpoints and DTOs see [architecture.spec.md](../.github/specs/architecture.spec.md).
> Component/store/styling rules auto-load from [client/src/CLAUDE.md](src/CLAUDE.md); unit test rules from [client/test/unit/CLAUDE.md](test/unit/CLAUDE.md); E2E rules from [client/e2e/CLAUDE.md](e2e/CLAUDE.md).

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

Unit tests live in `test/unit/`. Playwright specs live in `e2e/`. For full E2E workflow details see [run-e2e-tests/SKILL.md](../.github/skills/run-e2e-tests/SKILL.md).

## Runtime Layout

- `src/main.js` — app bootstrap with Pinia, router, and query setup.
- `src/router/index.js` — routes, auth guards, and page navigation behavior.
- `src/layouts/AppLayout.vue` — authenticated shell.
- `src/views/` — route-level pages.
- `src/components/` — feature UI grouped by domain plus shared/base primitives.
- `src/services/apiClient.js` and `src/services/authApi.js` — API transport and app-facing API surface.
- `src/stores/` — auth and UI Pinia stores.
- `src/composables/useSyncWebSocket.js` — real-time sync progress.
- `test/helpers/testUtils.js` and `test/helpers/apiMocks.js` — unit test helpers.
- `e2e/global-setup.js` and `e2e/global-teardown.js` — Playwright test user lifecycle.

## Runtime Notes

- Vite serves the app on `http://localhost:5174` and proxies `/api` to `http://localhost:5164`.
- `services/apiClient.js` uses cookie auth and handles global 401 session-expiry behavior.
- `services/authApi.js` is the main frontend API surface for auth, dashboards, matches, trends, and sync.
- `authStore` manages session and Riot account state; `uiStore` persists sidebar state to `localStorage`.
- `useSyncWebSocket()` connects to `/ws/sync` for real-time match sync progress.
- `@` points to `src/` and `@test` points to `test/` in both Vite and Vitest.
- Playwright global setup stores auth state in `e2e/.auth/user.json` and reuses it across browser projects.
