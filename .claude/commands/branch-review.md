---
description: Review current branch against origin/main with test validation and a structured verdict (distinct from the `code-review` skill's two-axis Standards/Spec review).
model: sonnet
---

# Branch Code Review

Review all changes on the current branch against `origin/main`, run relevant tests, and produce a structured assessment.

## Workflow

1. Discover the scope with branch history and diff against `origin/main`.
2. Run the narrowest affected tests before reporting.
3. Review against project standards.

Use `CLAUDE.md` and the relevant files under `.github/instructions/` as the review standard.

Focus on:
- correctness, regressions, and security issues
- contract mismatches across backend, frontend, and tests
- missing or weak test coverage for changed behavior
- misuse of repo-specific patterns called out in the targeted instruction files
- unnecessary complexity or drift from nearby implementation patterns

## Output Format

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
