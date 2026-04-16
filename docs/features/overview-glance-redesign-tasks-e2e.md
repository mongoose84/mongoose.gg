# Overview Redesign — E2E Test Tasks

Parent feature: [overview-glance-redesign.md](overview-glance-redesign.md)

## Prerequisites

All backend and frontend tasks must be complete before these E2E tests can pass.

---

## Task 1: Update Overview E2E tests

**Scope**: Update Playwright tests to validate the new Overview layout in Overall mode.

**File to modify**:
- `client/e2e/overview-dashboard.spec.js`

**Tests**:
- [ ] Overall mode shows "Today's Session" and "Survival Check" cards
- [ ] Section heading reads "Quick actions"
- [ ] `ChampionSelectCTA` is visible in the quick actions section
- [ ] `MatchActivityHeatmap` is NOT present on the overview page

**Selector updates**: Existing tests may reference "Recent matches" heading and `RankSnapshot` in Overall mode — update to match the new component `data-testid` attributes and heading text.

---

## Task 2: Update Solo Dashboard E2E tests

**Scope**: Add Playwright test coverage for the heatmap's new location on the Solo page.

**File to modify**:
- `client/e2e/solo-dashboard.spec.js`

**Tests**:
- [ ] `MatchActivityHeatmap` is visible in Zone 4 below Performance Profile

---

## Manual Testing Scenarios

These should be verified before the feature is considered complete:

1. Pro user with 2+ accounts, games played today → verify session card shows today's data
2. Pro user with 2+ accounts, no games today but games this week → verify fallback to "THIS WEEK"
3. Pro user with 2+ accounts, new account with < 5 games → verify survival card shows limited data state
4. Free user (single account) → verify layout is unchanged (`RankSnapshot` + `ChampionSelectCTA`)
5. Overall mode → Individual mode switch → verify cards swap correctly
6. Solo page → verify heatmap renders below Performance Profile in the Danger Zones grid
7. Solo page → change queue filter → verify heatmap updates with filtered data
