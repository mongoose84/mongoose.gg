---
name: run-all-tests
description: 'Run the full Mongoose.gg test suite. Use when running all tests, validating regressions, checking backend + frontend together, or doing final verification before completion.'
argument-hint: 'Optional: "full suite", "skip e2e", or a specific area to prioritize'
---

# Run All Tests

Run the repository's verification flow in this order:

1. **Backend tests** — .NET xUnit/integration tests
2. **Frontend unit tests** — Vitest + Vue Test Utils
3. **E2E tests** — Playwright with the backend in E2E mode

Use this skill when the user asks to:
- run all tests
- validate a change before merge
- do a full regression check
- verify backend and frontend still pass together

## Default Workflow

### Step 1 — Run backend tests

```powershell
# From server/
dotnet test Mongoose.Api.Tests/
```

### Step 2 — Run frontend unit tests

```powershell
# From client/
npm run test:unit
```

### Step 3 — Run Playwright E2E tests

Start the API in a **background terminal** with E2E-friendly flags:

```powershell
# From server/Mongoose.Api/
$env:Auth__AutoVerifyEmail="true"; $env:RateLimiting__Enabled="false"; $env:Email__DevMode="true"; dotnet run
```

Wait until the backend is responding on `http://localhost:5164`, then run:

```powershell
# From client/
npm run test:e2e
```

## Optional Variants

- **Quick regression check**: run only Steps 1 and 2
- **Headed E2E debugging**: `npm run test:e2e:headed`
- **Playwright UI mode**: `npm run test:e2e:ui`
- **Open the last E2E report**: `npm run test:e2e:report`

## Reporting Rules

After each step, report:
- the exact command that was run
- whether it passed or failed
- the fresh evidence (exit code, pass count, or failing test names)

Do **not** claim the suite passes without command output.

If one stage fails, report that failure clearly and ask whether to continue with the remaining stages.

## Repo-Specific Notes

- Backend test project: `server/Mongoose.Api.Tests/Mongoose.Api.Tests.csproj`
- Frontend test command: `client/package.json` → `npm run test:unit`
- E2E command: `client/package.json` → `npm run test:e2e`
- Frontend dev server uses `http://localhost:5174`
- Playwright artifacts are written to `client/playwright-report/` and `client/test-results/`
- If Firefox E2E becomes flaky, prefer deterministic readiness checks over `networkidle`
