---
agent: agent
model: Claude Sonnet 4.6
tools: ['changes', 'codebase', 'editFiles', 'runCommands', 'search', 'problems', 'testFailure', 'terminalLastCommand']
description: 'Code review current branch against origin/main with test validation'
---
# Branch Code Review

Review all changes on the current branch against `origin/main`, run relevant tests, and produce a structured assessment.

## Step 1: Discover Changes

Run these commands to understand the scope:
```
git log --oneline origin/main..HEAD
git diff origin/main...HEAD --stat
git diff origin/main...HEAD
```

Note the branch name, number of commits, files changed, and lines added/removed.

## Step 2: Run Affected Tests

- For frontend changes: `cd client && npm run test:unit -- <changed-spec-files>`
- For backend changes: `cd server && dotnet test`
- Always run tests before reporting — failing tests are a critical finding.

## Step 3: Review Against Project Standards

Use `copilot-instructions.md` and the instruction files in `.github/instructions/` as the standard. Check:

### Security (OWASP / Project Rules)
- [ ] No PII exposure — PUUIDs never returned to client
- [ ] Log injection prevention — `LogSanitizer.Sanitize()` on all user inputs
- [ ] Parameterized SQL only — no string concatenation in queries
- [ ] Auth checks: `ClaimTypes.NameIdentifier` matches route `userId`
- [ ] No hardcoded secrets or API keys

### Frontend (Vue / Vitest)
- [ ] Props use correct types with validation; `required` and `default` not both set
- [ ] `data-testid` attributes on all testable elements
- [ ] Interactive elements use `<button>` (not `<div>`) for keyboard accessibility
- [ ] `aria-label` on icon-only buttons
- [ ] CSS class names in component match class names in tests
- [ ] `emit()` calls are wired to actual DOM event handlers
- [ ] `toLocaleString('en-US')` for consistent locale formatting

### Backend (C# / xUnit)
- [ ] All endpoints have integration tests
- [ ] `DateTime` values use `DateTime.UtcNow` / `DateTimeKind.Utc`
- [ ] Repository methods use named parameters (no raw string interpolation in SQL)
- [ ] Scoped vs singleton DI registrations are correct
- [ ] Error responses follow `{ error, code }` format

### Testing Quality
- [ ] Tests actually exercise real component behavior (not just stubs)
- [ ] Component implementation and spec file are in sync
- [ ] Edge cases (null, empty, error states) are covered
- [ ] No tests that trivially pass regardless of implementation

### Code Quality
- [ ] No over-engineering beyond the current task
- [ ] TODO comments are documented and intentional
- [ ] No magic numbers — hardcoded limits (like `.slice(0, 3)`) are noted

## Step 4: Output Format

### Branch
`branch-name` — N commits, N files changed (+N/-N lines)

### Test Results
`N passed / N failed` — list any failing test names

### Critical Issues 🔴
Issues that must be fixed before merging (bugs, broken tests, security issues).

### Warnings ⚠️
Issues worth fixing but not blocking (fragile logic, tech debt, missing documentation).

### Positives ✅
Notable things done well.

### Verdict
`APPROVE` / `REQUEST CHANGES` / `NEEDS DISCUSSION`

### Suggestions
[Recommended improvements]

### Positive Observations
[Good patterns or improvements worth noting]

## Human Validation Gate
🚨 **STOP**: Review feedback before posting.
Confirm: Feedback is constructive, specific, and actionable.
