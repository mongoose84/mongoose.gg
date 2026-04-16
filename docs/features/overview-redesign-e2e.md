# Overview Redesign — E2E Test Tasks

> Source: [overview-page-redesign.md](overview-page-redesign.md) — Task 9
>
> **Depends on**: All backend (Tasks 1–3) and frontend (Tasks 4–8) tasks must be complete before running E2E validation.

---

## Task 9: E2E test updates

**Scope**: Update Playwright tests to validate the new layout in both Overview and Solo dashboards.

**Files to modify**:
- `client/e2e/overview-dashboard.spec.js`
- `client/e2e/solo-dashboard.spec.js`

### Overview E2E tests

- [ ] "Today's Session" and "Survival Check" cards are visible
- [ ] Section heading reads "At a glance" (not "Today at a glance")
- [ ] Section heading reads "Quick actions" (not "Recent matches")
- [ ] `ChampionSelectCTA` is visible in the quick actions section
- [ ] `RankSnapshot` component is NOT present on the overview page
- [ ] Rank emblem and LP are visible in the player header (individual mode)
- [ ] `MatchActivityHeatmap` is NOT present on the overview page

### Solo E2E tests

- [ ] `MatchActivityHeatmap` is visible in Zone 4 below Performance Profile

### Selector updates

Existing tests may reference the following — update to match new names:

| Old                              | New                              |
|----------------------------------|----------------------------------|
| "Today at a glance" heading      | "At a glance"                    |
| "Recent matches" heading         | "Quick actions"                  |
| `#recent-left` / `#recent-right` | `#actions-left` / `#actions-right` |
| `RankSnapshot` `data-testid`     | Removed from Overview            |
| `MatchActivityHeatmap` on Overview | Removed from Overview (now on Solo page) |
