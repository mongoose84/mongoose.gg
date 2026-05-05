---
name: run-e2e-tests
description: 'Run Playwright E2E tests with backend in E2E mode. Use when running end-to-end tests, testing E2E, verifying user flows, or debugging Playwright failures.'
argument-hint: 'Optional: specific test file or describe what to test (e.g., "solo dashboard" or "e2e/overview-dashboard.spec.js")'
---

# Run E2E Tests

Starts the backend with E2E configuration, runs Playwright tests, and reports results.

## Prerequisites

- Backend must compile: `dotnet build` in `server/Mongoose.Api/`
- Frontend dependencies installed: `npm install` in `client/`
- Playwright browsers installed: `npx playwright install` in `client/`
- MySQL database accessible with test data

## Procedure

### Step 1 — Start the Backend in E2E Mode

Start the .NET backend with E2E environment flags in a **background terminal**:

```powershell
# From server/Mongoose.Api/ directory
$env:Auth__AutoVerifyEmail="true"; $env:RateLimiting__Enabled="false"; $env:Email__DevMode="true"; dotnet run
```

Or on Linux/macOS:
```bash
Auth__AutoVerifyEmail=true RateLimiting__Enabled=false Email__DevMode=true dotnet run
```

**Required flags:**
- `Auth__AutoVerifyEmail=true` — auto-verifies new user registrations (no email needed)
- `RateLimiting__Enabled=false` — disables rate limiting for test speed
- `Email__DevMode=true` — skips actual email sending

Wait for the backend to be listening on `http://localhost:5164` before proceeding.

### Step 2 — Run Tests

From the `client/` directory:

```powershell
# Run all E2E tests
npm run test:e2e

# Run a specific test file
npx playwright test e2e/overview-dashboard.spec.js

# Headed mode (see the browser)
npm run test:e2e:headed

# Playwright UI mode (interactive debugging)
npm run test:e2e:ui
```

### Step 3 — Review Results

```powershell
# Open the HTML report
npm run test:e2e:report
```

Reports are saved to `client/playwright-report/`.
Test result artifacts (screenshots, traces) go to `client/test-results/`.

## E2E Test Architecture

### Global Setup/Teardown
- `e2e/global-setup.js` — creates a unique test user, links a Riot account, saves auth state to `e2e/.auth/user.json`
- `e2e/global-teardown.js` — deletes the test user after all tests

### Auth State
Tests reuse the auth cookie from global setup. Playwright config references `e2e/.auth/user.json` as `storageState`.

### Test Browsers
Configured in `playwright.config.js`: Chromium and Firefox. Tests run in parallel by default.

### Base URLs
- Frontend: `http://localhost:5174` (Vite dev server with API proxy)
- Backend API: `http://localhost:5164`

## Troubleshooting

### Backend won't start
- Check MySQL is running and connection string is valid
- Verify `RIOT_API_KEY` env var is set (needed for sync endpoints)
- Check port 5164 isn't already in use

### Tests time out
- Verify backend is actually running and responding: `curl http://localhost:5164/api/v2/diagnostics`
- Avoid `page.waitForLoadState('networkidle')` — it can hang when WebSocket/background requests stay open
- Prefer `page.goto(url, { waitUntil: 'domcontentloaded' })` + `expect(locator).toBeVisible()`

### Auth failures in tests
- Ensure backend started with `Auth__AutoVerifyEmail=true`
- Check that global setup ran successfully (look for `e2e/.auth/user.json`)
- If `401 Unauthorized`, verify the login flow completed and the auth cookie was issued; sessions use a 14-day sliding persistent cookie

### Flaky Firefox tests
- Firefox can hang on `networkidle` when WebSocket connections stay open
- Use deterministic readiness checks: wait for a specific UI element instead

## Cleanup

After tests complete:
1. Stop the background backend process
2. Global teardown automatically deletes the test user
3. Auth state files in `e2e/.auth/` can be gitignored
