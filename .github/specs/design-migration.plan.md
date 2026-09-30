# Design System Migration Plan

Moves the authenticated app (`/app/*`) to the Mongoose.gg design system, one phase per PR. The visual rules live in `.claude/skills/mongoose-design/reference/`; build every UI change through `/mongoose-design`. Navigation and layout behaviour is specified in `ui-ux.spec.md` §3 and §5.

Tick items off as they land so work can resume from any machine.

## Starting point (2026-09-27)

- **Foundation done**: Clash Display + Satoshi in `client/public/fonts/`, tokens in `client/src/style.css`, Tailwind mapping, Lucide icons via `BaseIcon`, `ScoreRing`, `mp-btn`, `useRevealOnView`.
- **Migrated**: public `NavBar`, Landing page, cookie banner, auth page, `BaseButton`, `BaseInput`.
- **Not yet ported to `style.css`**: `mp-nav`, `mp-tabbar`, `mp-card`, `mp-hero`, `mp-match-row`, `mp-insight`, `mp-skeleton`, `mp-empty`, `mp-chip`, `mp-seg`, `mp-meter`, `mp-champ-card` (source: `reference/components.css`).
- **Data gap**: the design's Overview order is hero → 3 score rings → your champions → readiness + today's matches → insights. `OverviewResponse` has no scores, readiness, insights or champion-pool stats. It does have `mostPlayedChampion`, `playerHeader` (rank, LP), `sessionStats`, `survivalStats`, `lastMatch`, `accountSummaries`.

## Decisions

- Sections without real data are **left out**, not shown as placeholders, until Phase 3 delivers the data.
- Team and Goals leave the navigation; they stay reachable by URL behind their feature flags until the "Advanced" page exists.
- The "analysis running" dot on the Matches nav item is dropped (no badges on pills); sync status shows on the page as SyncProgress.
- The tier label under the username is dropped; how Pro is marked is decided when Pro is built.

---

## Phase 1: App header replaces the sidebar

Frontend only, one PR. **Status: done on branch `create_plan_for_updating_ui`** (2026-09-27); unit tests (1671), build and E2E (80, Chromium + Firefox) green.

### Build
- [x] Port `mp-nav`, `mp-tabbar`, `mp-card` into `client/src/style.css`.
- [x] `client/src/components/AppHeader.vue`: 80px header, logo left (→ `/app/overview`), PillNav centre (Overview, Champion Select, Matches, Solo; `router-link` with `aria-current="page"`), avatar right opening a menu with the Riot account switcher (when several accounts are linked), Settings (`/app/user`), Feedback (`/app/feedback`), Log out. Keep test IDs `nav-overview`, `nav-champion-select`, `nav-matches`, `nav-solo`, `nav-feedback`; add `app-header`.
- [x] `client/src/components/AppTabBar.vue`: below 900px, fixed bottom, 64px + safe-area inset, icon above label (`house`, `shield`, `swords`, `chart-line`), "Champ Select" as the phone label; header shrinks to 56px (logo + avatar).
- [x] Move the account-switch logic from `components/sidebar/AccountSwitcher.vue` and `AccountDropdownList.vue` into the avatar menu.
- [x] `client/src/layouts/AppLayout.vue`: remove `AppSidebar` and the `marginLeft` binding; render header, tab bar and a content wrapper (max width 1328px, 56px desktop / 16px phone gutters, top padding for the header, bottom padding for the tab bar on phones). Keep idle detection as is.

### Remove
- [x] `client/src/components/AppSidebar.vue`
- [x] `client/src/components/sidebar/` (after moving what the header reuses)
- [x] Sidebar state in `client/src/stores/uiStore.js` (`sidebarCollapsed`, `sidebarWidth`, `toggleSidebar`, `initializeSidebar`, `sidebarCollapsed` localStorage key); delete the store if nothing else uses it.
- [x] Heroicons imports that only the sidebar used.

### Tests
- [x] Replace `client/test/unit/components/AppSidebar.spec.js` with `AppHeader.spec.js` (active pill, avatar menu, account switch, log out) and `AppTabBar.spec.js`.
- [x] Update `client/test/unit/layouts/AppLayout.spec.js` and `client/test/unit/stores/uiStore.spec.js`.
- [x] E2E: `client/e2e/helpers/app-shell.js` waits for `app-header` instead of `app-sidebar`; update sidebar tests in `app-smoke.spec.js`, `overview-dashboard.spec.js`, `solo-dashboard.spec.js`.
- [x] New `client/e2e/app-header.spec.js` (full suite, not smoke): active pill, avatar menu open / Escape / outside click, Settings link, log out, phone tab bar navigation.

### Docs
- [x] `ui-ux.spec.md`: remove the implementation note in §3, the "Legacy" line in §5, and the `AppSidebar` / uiStore inventory entries; update the migration-status line.

### Check
- [x] Solo, Matches, Champion Select, Settings and Feedback still lay out correctly at full width (they keep legacy styling until their own phase). Visual pass done 2026-09-27 at 1440px and 390px (Playwright screenshots).

### Outcome notes
- `uiStore` was deleted entirely (nothing else used it). `AccountSwitcher.vue` was retired; only `AccountDropdownList` was kept, now in `client/src/components/header/`.
- The avatar menu is a disclosure panel (`aria-expanded` + `aria-controls`), not an ARIA `menu`, since it has no arrow-key navigation. Escape returns focus to the avatar. No drop shadow.
- `MatchesPage.vue` height changed from `100vh` to `calc(100dvh - 5rem)` so it fits under the fixed header.

### Known leftovers
- Matches can still overflow slightly on phones (tab bar not subtracted) — fix in the Matches phase.
- Sync / analysis-running status has no indicator in the shell any more — comes back as SyncProgress in Phase 2.
- `AccountDropdownList` uses `role="listbox"` / `option` inside the disclosure panel; revisit if the switcher is redesigned.
- `.github/specs/test-strategy.spec.md` still lists `uiStore.spec.js` as missing coverage — remove when that spec is next touched.
- Running E2E on a new machine may need `npx playwright install` first.

---

## Phase 2: Overview re-skin with existing data

Frontend only, one PR. Order follows the design system; only sections with real data. **Status: built on branch `design_phase2_overview`** (2026-09-27); unit tests (1560), build and E2E (58 full + 8 smoke) green.

| Design section | Data | Component |
|---|---|---|
| Champion hero + summary sentence | `mostPlayedChampion` (splash), `playerHeader` (Riot ID · rank · LP); glass chips from `sessionStats` (matches this week, win rate); sentence built client-side from `sessionStats` / `survivalStats` | New `ChampionHero.vue`; `getChampionCenteredUrl` helper in `leagueAssets.js` if needed |
| Today's matches | `sessionStats` counts + `lastMatch` as a MatchRow | New base `MatchRow.vue` (links to `/app/matches` until `/app/matches/:matchId` exists) |
| Insights | `survivalStats` as a Pattern insight ("You win 64% with 4 or fewer deaths…") | New `InsightCard.vue` + `mp-chip` |
| Next steps | Champion Select and Solo CTAs as two plain cards | `mp-card` + `BaseButton` secondary |
| Sync | `AnalysisStatusCard` → inline SyncProgress at the top of content | New `SyncProgress.vue` |
| Overall mode | `OverviewAccountCards` re-skinned, in place of the hero | Existing component |

### Build
- [x] Port `mp-hero`, `mp-match-row`, `mp-insight`, `mp-chip`, `mp-skeleton`, `mp-empty` into `style.css` (plus `mp-eyebrow`, `mp-up` / `mp-down`, `mp-message`).
- [x] `BaseSkeleton` and `BaseEmptyState`; every card gets loading / empty / error-with-retry / content. The page frame renders immediately instead of blocking on one loading state.
- [x] Replace `OverviewLayout`'s legacy slots (`glance-*`, `recent-*`, `latest-match`) with sections in the new order, or drop the wrapper.
- [x] Remove legacy styling in touched files: gradients, glows, hover lifts, green/red win colours (use winrate tokens / purple-orange), Heroicons.
- [x] Retire components that no longer fit (`OverviewPlayerHeader`, `ChampionSelectCTA`, `SoloAnalyticsCTA`, `DeathInsightsCard`, `LastMatchCard`, `TodaySessionCard`, `AnalysisStatusCard`) once their replacements are in, with their unit tests.

### Tests
- [x] Unit tests for each new base component and for the summary-sentence logic.
- [x] Update Overview view tests and `client/e2e/overview-dashboard.spec.js`.

### Docs and design system
- [x] `ui-ux.spec.md` §5 (`OverviewLayout`) and the component inventory updated.
- [ ] New patterns added to the live design system and `reference/` snapshot (mongoose-design Step 5): SyncProgress bar styling, "Next steps" plain cards, the phone MatchRow layout (KDA in the meta line), the account cards for Overall mode.

### Outcome notes
- The MatchRow is `components/base/BaseMatchRow.vue`: `components/matches/MatchRow.vue` still exists until the Matches phase, when the two should merge.
- `ChampionHero`, `InsightCard` and `SyncProgress` live in `components/base/` under their design-system names, like `ScoreRing`.
- The summary sentence, chips, player line, today line and deaths insight are pure functions in `client/src/utils/overviewSummary.js`.
- Sync logic moved from `AnalysisStatusCard` into `composables/useSyncMatches.js`. The "Sync matches" button sits in the Today's matches card header, and SyncProgress shows at the top only while a sync runs, fails or has just finished.
- All cards come from one request, so a failed request shows one error message with "Try again" in place of the content instead of an error per card. No linked Riot account shows an EmptyState ("Link Riot account") before any request error. Skeletons wait 300ms (CSS delay).
- Fixed a shared bug: `BaseButton` with `to` passed `href="null"` through to `router-link`, which removed the link's href (no link role, not reachable with Tab). Regression test in `BaseButton.spec.js`.
- `test-strategy.spec.md` file map updated (retired specs and `uiStore` removed).

### Known leftovers
- Insights has a single card in a 3-column grid until Phase 3 adds more.
- The hero's primary action is "See your matches"; the design's "See today's matches" waits for `/app/matches/:matchId` and a today filter.

---

## Phase 3: New Overview data

Spec first (`feature-spec` / `architect`), then backend + frontend per item. Each defines a domain rule in Core, so it needs its own spec. One item per PR, end to end. The formulas for Score rings and Queue readiness are defined with the user in an interview before their specs are written.

- [x] **Your champions**: top 3 ChampionCards + "Also played" list. Spec: `features/overview-champion-pool.spec.md`. **Built on branch `design_phase3_champion_pool`** (2026-09-27).
- [ ] **Insights**: Strength / Pattern / Trend findings with evidence, extending `TrendBadgeCalculator`.
- [ ] **Score rings**: Laning, Teamfighting, Discipline (0–100, weekly delta, 3 contributors).
- [ ] **Queue readiness**: the one highlight card (0–100, verdict, advice).

Each adds fields to `OverviewResponse` (update `architecture.spec.md`) and its section to the Overview in the design-system position.

### Your champions: decisions and outcome notes
- Window: ranked Solo/Duo + Flex, current season. Ordered by M-Score (`MainChampionRecommender.ComputeMScore`, now public), one entry per champion across roles; the primary role sets the laning weights.
- Strength tag: the metric (laning, damage, KDA, farming, vision, involvement) where the champion leads the player's own average most; unique across the three cards, ≥5 matches and ≥10% lead required.
- Cards are **static** (no pick, no `aria-pressed`, no spotlight): champion-focused analysis belongs to Champion Select. This deviates from the design system's ChampionCard (a button) — add a static variant to the live system (Step 5).
- The hero keeps `mostPlayedChampion` (all queues), so card #1 can differ from the hero champion.
- `CurrentSeasonSubquery` is now shared in `OverviewStatsRepository`.
- The repository SQL is covered by E2E (the test user has no ranked matches, so only the empty path) and was checked read-only against the dev database on 2026-09-27 (40 champions for the busiest ranked account; cards Senna / Vladimir / Cho'Gath with Best vision / Best farming / Best laning). Unit tests 1574, backend 609, E2E 35 (Overview + smoke, Chromium + Firefox) green; visual pass at 1440 / 1024 / 390px.

### Overview endpoint cleanup (same branch)
- Removed from `OverviewResponse`: `activeGoals`, `suggestedActions` (always empty), `combinedStats` (unused; saved a `GetSoloPerformanceAsync` query per Overall-mode request) and `playerHeader.activeContexts` / `primaryQueueLabel` / `profileIconUrl`.
- Removed the uncalled repository methods `GetPrimaryQueueAsync`, `GetLast20MatchesAsync`, `GetCurrentLpAsync` and their query models.
- Kept `sessionStats.bestChampionToday`, `avgKdaToday` and `avgKdaThisWeek` (unused today) until the Insights and Queue readiness specs decide whether they need them.

### Your champions: known leftovers
- Champion names show Riot's internal ID ("Chogath", "MonkeyKing"); this is app-wide (hero, match rows too) — add a display-name mapping from Data Dragon in its own change.
- The strength-tag baseline mixes roles, so a support-heavy pool makes any laner "Best farming". Revisit with the Score rings, which need per-role baselines anyway.
- The static ChampionCard variant still has to be added to the live design system and `reference/` (Step 5).

---

## Phase 4: Matches visual redesign

Agreed 2026-09-28 after a UX audit: the open match leads with one finding instead of five tallies, and every comparison is drawn rather than written ("show, then say" in the design system). Target: the "visual" artboards on the Mongoose.gg Matches redesign canvas (https://claude.ai/artifact/Ufv95okAfSYgmnaJgniRhL): `MainVisual` (desktop), `PhoneMatchVisual`, `PhoneListVisual`. Components and rules are already in the design system (FormStrip, UsualMeter, LaneBar, ColumnChart, SplitBar, the ChampionHero match banner, time-chart moment markers).

Four PRs, in order; each is shippable alone. **Status: 4a built on branch `implement_phase_4`** (2026-09-28); unit tests (1552) and build green; visual pass at 1440 / 1024 / 390px with a real account. 4b built on the same branch (2026-09-28), see its notes.

> **Rank snapshots:** merged in PR #514 (`885c6e8`); migration `003_AddRankSnapshots.sql` applied to the dev database on 2026-09-29. The coverage measurement week runs until 2026-10-06.

### 4a. Visual pass with existing data (frontend only)
- [x] Port the classes the page needs from `reference/components.css` into `client/src/style.css`: `mp-form`, `mp-columns`, `mp-lane-bar`, `mp-split`, `mp-stat`, `mp-hero--match`, the MatchRow additions. Delete the scoped copies they replace (this covers the "Switch the Matches components to the design system's classes" item below).
- [x] Page header: short headline ("12 wins in your last 20") + `FormStrip` of the list (win / loss / remake from `matchesSummary.isRemake`). New base component `BaseFormStrip.vue`.
- [x] Under the list: `ColumnChart` "win rate by start time" from `gameStartTime` in the player's local time (afternoon / evening / after 11pm, groups under 3 matches left out); title states the weak spot or is omitted when nothing stands out. New `BaseColumnChart.vue`; grouping logic in `matchesSummary.js`.
- [x] Open match: `MatchHeader` becomes the ChampionHero match banner (splash via `getChampionSplashUrl`, glass chips K / D / A; LP and rank chips come in 4b). "Download data" moves off the banner into the "All your stats" section.
- [x] Lanes: `MatchNarrative` rows become `LaneBar`s from `allyParticipant.goldDiffAt10` (±1,500 fills a half, under 300 is even); same button, `aria-expanded` and `LaneMatchupDetails` underneath. ARAM keeps the two team lists.
- [x] Team: `TeamComparison` becomes `SplitBar`s for damage, dragons, barons and towers beside the lanes (team gold totals aren't in the API, so no gold bar; the gold lead at 15 stays as the caption).
- [x] `WinPredictionStats` stays in the "What decided it" slot until 4c replaces it; `StatSnapshot` moves behind a closed "All your stats" disclosure with "▲ n above usual ▼ n below" in its summary row. `MatchActions` stays.
- [x] Tests: unit tests for the new base components and the grouping logic; update `MatchesPage`, `MatchHeader`, `MatchNarrative`, `TeamComparison` specs. Visual pass at 1440 / 1024 / 390px with a real account.

#### 4a outcome notes
- The headline keeps leaving remakes out ("10 wins in your last 20" counts non-remakes); the FormStrip shows remakes as stubs. No wins reads "No wins in your last n".
- Start-time groups: morning 5–12, afternoon 12–18, evening 18–23, after 11pm 23–5, local time. The chart shows only when two groups have 3+ matches; the weak spot is the single lowest group at least 15 points below the other groups together (`START_TIME_WEAK_GAP`). Without a weak spot the caption becomes the title. The account used for the visual pass plays almost only in the evening, so the chart stayed hidden there; it is covered by unit tests.
- The list column (`matches-side`) is now the sticky part: the list scrolls inside it and the chart stays under it.
- Lanes sit beside the team card through a `match-detail` container query at 760px (1440px desktop); at 1024px and on phones they stack.
- The lane title counts lanes from the drawn gold difference ("2 of 5 lanes won"), no longer the server's `laneWinner`; both use 300 gold at 10.
- TeamComparison: an objective neither team took (0–0) shows an empty `track-strong` bar and "none taken" rather than a 50/50 split. This is a new state for SplitBar; add it to the live design system (Step 5).
- `StatSnapshot` gained `defaultOpen` (closed on the page) and closes again when another match opens. There is no `download` icon in the vocabulary, so "Download data" has none.
- The MatchHeader banner is built in `MatchHeader.vue` on the `mp-hero` / `mp-hero--match` classes rather than through `ChampionHero` (which owns the page `h1`).
- Ported to `style.css`: `mp-form`, `mp-columns`, `mp-lane-bar`, `mp-split`, `mp-stat` / `mp-stat-grid`, `mp-hero--match`, and the MatchRow additions (`mp-match-list`, `mp-portrait--remake`, `mp-result--remake`, `mp-match-kda`, `mp-match-meta-kda`, the selected row). `mp-lane-row` was not ported: LaneBar replaced LaneRow on this page.
- Leftovers: no E2E beyond the smoke load (the E2E user has no matches); the Step 5 design-system update for the empty SplitBar is still to do.

### 4b. LP change and rank after each match (backend + frontend)
- [x] Add `lpChange` (and `tierAfter` / `rankAfter`) to `MatchListSummaryItem` and `MatchDetailsItem`: from `participants.lp_after`, compared with the same player's previous ranked match in the same queue; null for unranked queues, the first ranked match, and tier or division changes it can't resolve (decide in the PR whether promotions are computed or shown as "Promoted"). Parameterized SQL, update `architecture.spec.md`.
- [x] Wire `lpChange` into `BaseMatchRow` (the prop exists), the page header total ("+86 LP over 20", ranked queues only) and the banner chips (LP, rank with its tier colour).
- [x] Backend integration tests for the delta, including queue separation and missing `lp_after`.

#### 4b outcome notes
- Sync writes the current rank onto the newest ranked match only (`MatchHistorySyncJob.UpdateLpForMostRecentRankedMatchAsync`), so the change compares with the match right before in the same queue (SQL `LAG` over `participants` × `matches` per player and queue), never the last match that happens to have LP. Several matches played between two syncs therefore get no change except when each was synced on its own.
- Promotions and demotions are computed, not shown as "Promoted": one ladder with 100 LP per division from Iron IV to Diamond I, and Master / Grandmaster / Challenger sharing one LP count above it (`LpChangeCalculator` in Core).
- Guards (null instead of a wrong number): a jump over 100 LP, a win with no gain or a loss, a loss that gained LP (the LP was read before Riot applied the match, or a match is missing), an unknown tier or division.
- API: `lpChange`, `lpAfter`, `tierAfter`, `rankAfter` on list items and on the open match.
- Rows show "±0 LP" in neutral for no change; in the narrow list the LP sits above the result.
- Header total only for one ranked queue (Solo/Duo or Flex are separate ladders), over the matches with a known change ("+23 LP over 3"), hidden with fewer than two.
- Banner chips: signed LP, and the rank with its `--color-rank-*` dot ("Emerald II · 64 LP"; no division for Master and above). Only known tiers reach the colour variable.
- The repository integration tests are opt-in (`RUN_DB_INTEGRATION_TESTS` + `Database_test`) like the existing ones.
- Checked against real data on 2026-09-28: correct values (+20, −21, Bronze III · 17 LP), but coverage is thin (2 of 20 Flex matches) because sync reads LP only at login or "Sync matches".

#### 4b follow-up: rank snapshots
- [x] Spec: `features/rank-snapshots.spec.md` (2026-09-28). A `RankSnapshotJob` reads League-v4 every 20 minutes for active accounts, stores snapshots with wins and losses, and attributes a snapshot to a match only when exactly one ranked match ended in its window (by end time, never "the newest match"). Replaces `UpdateLpForMostRecentRankedMatchAsync`. Backend only; keeps the 4b contract. Decided: all linked accounts of users active in the last 7 days, every 20 minutes, a detected match always syncs.
- [x] Built (2026-09-28): `RankSnapshotRules` (Core), `RankSnapshotService` (the only writer of `lp_after`, write-once), `RankSnapshotsRepository`, `RankSnapshotJob` (`Jobs:EnableRankSnapshots`, off in tests), login and end-of-sync readings; migration `003_AddRankSnapshots.sql` (new table + `riot_accounts.rank_checked_at`). Backend tests 668 green.
- [x] Migration applied to the dev database (2026-09-29).
- [ ] Run the job for a week (until 2026-10-06) and measure coverage (spec, manual scenario 4).

### 4c. "What decided it" (spec first, then backend + frontend)
- [x] Feature spec drafted (2026-09-29): `features/what-decided-it.spec.md`; three open questions to answer before building. Covers how the deciding stat is picked (extend `TrendBadgeCalculator`'s biggest-deviation logic across gold at 10, kill participation, CS at 10, deaths before 10, vision), the "usual" baselines it needs (`RoleBaseline` has no gold-at-10 or CS-at-10 averages yet), the finding and fix copy per stat, and what shows when nothing stands out.
- [x] Built (2026-09-29): `DecidingStatCalculator` (Core, replaces `TrendBadgeCalculator`) scores five stats (gold lead, CS, deaths before 10, kill participation, vision per minute) against `IMatchesRepository.GetStatUsualsAsync` (up to 20 prior Summoner's Rift matches in the same role, strictly before the opened match). `MatchDetailsResponse.decidingStat` carries the outcome, up to three meters and an optional fix. Frontend: `BaseUsualMeter`, `DecidingStatCard` (`mp-card mp-card--highlight`), copy and scale rules in `utils/decidingStat.js`; wired into `MatchDetails` in `WinPredictionStats`'s old slot. Retired `TrendBadgeCalculator`, `TrendBadge`, `trendBadge` on list items, and the `MatchHeader` badge chip. Backend tests 682 green (24 new `DecidingStatCalculatorTests`); frontend unit tests 1563 green.

### 4d. Gold over time (data first)
- [ ] Sync already downloads the match-v5 timeline for `participant_checkpoints`; persist per-minute team gold and the objective events (dragon, Baron, Herald, towers) from the same payload. Schema change + `database-schema.spec.md`; backfill is optional (older matches fall back to the 10/15/20/25 checkpoints or hide the chart).
- [ ] Endpoint field + the line chart per the design system (takeaway title, `primary` line, moment markers on the axis, orange point for the biggest swing, text alternative).

### Afterwards
- Overview and Champion Select visual passes (artboards `OverviewVisual`, `ChampionSelectVisual` on the same canvas; desktop only for now, phone versions deferred). Champion Select works with existing data. The Overview needs per-match results for its week and today strips, LP from 4b, and a high-deaths win rate next to `winRateLowDeaths` for its deaths chart.

---

## Phase 5: Solo page redesign

Proposed 2026-09-29 through `/mongoose-design`: Solo becomes the "am I improving?" page. Every card compares the player with their own past, draws the comparison ("show, then say"), and leads to one fix. Target: the "Solo" row on the redesign canvas (https://claude.ai/artifact/Ufv95okAfSYgmnaJgniRhL): `SoloVisual` (desktop), `PhoneSoloVisual`. Metric choice follows `docs/win-prediction-metrics-research.md` (deaths, gold lead at 15, dragon participation, vision, CS, then kill participation; session fatigue).

**Page order:** headline ("+148 LP over your last 50 matches" + the one reason) with queue and range SegmentedControls → climb card (LP line, promotions, biggest drop) beside the "Your focus" highlight card → "n of 6 match-deciding stats improved" (six trend tiles) → "Where your deaths cost you" (death zones) → win factors (hit vs miss win rate) beside LP per champion → three pattern cards (session length, after a loss, match length). Below 900px everything stacks; trend tiles become compact rows.

### What exists and what is missing

| Card | Data today | Gap |
|---|---|---|
| Range control | Solo endpoints take `timeRange` in days (7d … all) | Ranges must count matches: `range=last20\|last50\|season` |
| Climb | `participants.lp_after` / `lpChange` (4b), rank snapshots | Coverage is thin until the rank-snapshot week ends (2026-10-06); decide the fallback |
| Stat trends | Per-stat trend endpoints under `Endpoints/Trends` (deaths, CS, vision, gold at 15, dragons, win rate); kill participation per match | One endpoint returning all six rolling series with start/end values; "Emerald average" benchmark has no source |
| Your focus | Nothing | Rule that picks the stat (see 5a); goal action depends on Goals (flagged off) |
| Win factors | Checkpoints (gold diff at 15), deaths, `participant_objectives`, `vision_per_min`, CS per minute | Aggregation only |
| LP per champion | `lpChange` per match | Same coverage caveat as the climb |
| Patterns | `game_start_time`, results, match length | Session grouping rule (gap between matches) |
| Death zones | `participant_death_events`: minute, x/y, killer champion, assist count (table missing from `database-schema.spec.md`) | Death time in seconds, killer's role, allies near the victim, victim's side, objective events with timestamps (shared with 4d) |

### 5a. Spec (first)
- [x] `features/solo-trends.spec.md` drafted (2026-09-29); its open question is answered (backfill with visible progress). It covers:
  - Ranges: Last 20 / Last 50 / Season, counted in matches for the selected queue; Summoner's Rift only (ARAM left out).
  - Trend tiles: 10-match rolling average, "now" = last point, "was" = first point; verdict thresholds for Improving / Slipping / Steady; "less is better" stats (deaths) flip the verdict, not the chart.
  - Win-factor marks (ahead at 15, 4 or fewer deaths, 2+ dragons, 0.9+ vision per minute, 7+ CS per minute) and the minimum matches per side (hide a row under 5).
  - Focus rule: among Slipping stats, the one with the largest hit/miss win-rate gap; with none slipping, the lowest-hit stat among the top three win factors; with fewer than 20 matches, no focus card. Finding, evidence and fix copy per stat (reuse the fixes in `utils/decidingStat.js` where they fit).
  - Sessions: matches less than 30 minutes apart (end to next start) form a session; patterns need 3+ matches per group.
  - Death zones: see 5e/5f for the classification rules; decide them here.
  - Copy for every title (takeaway form) and every empty state.
- [x] Open questions answered (see Decisions below).

### 5b. Page shell, ranges, trends and win factors (backend + frontend)
- [x] Backend: `range` parameter (match counts) on the new Solo endpoints; keep `timeRange` on the old ones until they are removed. New `GET /api/v2/solo/stat-trends/{userId}` (six series + start/end/benchmark per stat) and `GET /api/v2/solo/win-factors/{userId}` (factor rows, patterns). Ownership check, resolve the Riot account server-side, parameterized SQL, `LogSanitizer` on logged values. Rules (rolling average, verdicts, session grouping) in Core services with unit tests. Update `architecture.spec.md`.
- [x] Frontend: rebuild `SoloStatsPage.vue` without `AnalysisLayout`: headline, SegmentedControls, `TrendTile` grid, win-factor card, pattern cards (`BaseColumnChart`). Every card has its own skeleton / error-with-retry / empty / content. The benchmark line shows "Your season average" until 5g.
- [x] Retire `SummaryStatsCard`, `TrendChartCard`, `TrendLineChart`, the six Chart.js trend charts, `RadarChart` and `RadarChartEndpoint`, and the match-activity card on this page, with their tests; remove the old `Trends/*` endpoints once nothing calls them.
- [ ] Tests: Core rule tests, endpoint integration tests (ownership, range, empty), unit tests for the new components and copy helpers; E2E smoke for the page.
- 5b notes (2026-09-29):
  - Retired on the frontend: `SummaryStatsCard`, `TrendChartCard`, `TrendLineChart`, the six Chart.js trend charts, `RadarChart`, `AnalysisLayout`, `trendsApi.js`, `chartConfigs.js` and the match-activity card on this page, with their tests. Then removed (same day): the six `Trends/*` endpoints and `RadarChartEndpoint` with their DTOs, query models, radar repository and tests (`ITrendRepository` keeps only the daily match counts for `MatchActivityEndpoint`), Chart.js (`chart.js`, `chartjs-plugin-annotation`, `vue-chartjs`, `plugins/chartjs.js`), and the Settings "Chart display mode" preference with `useChartDisplayMode`.
  - Until 5c the headline counts matches ("Your last 20 matches"); the rank line waits for the climb card. The legacy `DangerZonesMap` stays until 5f, fed with this season.
  - The E2E smoke is updated but not yet run against a backend.

### 5c. Climb and LP per champion
- [x] Can ship before the rank-snapshot coverage result (2026-10-06) thanks to the win-rate fallback (decision 1); the result tells how often the fallback shows.
- [x] Core rule for LP coverage (80% of ranked matches in range, at least 10) deciding LP vs win-rate mode for the headline, climb card and champion card; returned as a `mode` field.
- [x] `GET /api/v2/solo/climb/{userId}`: the LP ladder per ranked match (100 LP per division, Master+ shared, as `LpChangeCalculator`), promotions, biggest drop, LP per champion (champions under 3 matches left out). Ranked queues only.
- [x] Climb card (line chart per the design system: takeaway title, labelled divisions, promotion labels, one orange point for the biggest drop, text alternative) and the LP-per-champion card (LaneBar-style bars around zero).
- 5c notes (2026-09-29):
  - `LpLadder` (Core/Services/Solo) now owns the ladder; `LpChangeCalculator` calls it. The previous-rank window moved to `PreviousRankSql`, shared by the Matches and Solo repositories, so both pages read LP the same way.
  - The climb response also carries `queueType`, `range` and `rank` (the rank line, FR 8). The headline and rank line come from it; until it answers, the headline counts matches.
  - New patterns: `BaseLineChart` (primary line, labelled guides and moments, HTML labels over a stretched SVG) and `BaseDivergingBar` (LaneBar without icons). Promotions closer than 5 matches keep their dot but only the last is labelled, so bouncing on a boundary stays readable.
  - The climb card is full width until the focus card (5d) sits beside it; LP per champion sits beside win factors.

### 5d. Your focus
- [x] Core `SoloFocusPicker` per the 5a rule; returned with `stat-trends` (or its own field on the Solo summary).
- [x] Focus card (`surface-highlight`, the page's one highlight): finding, evidence, the 20-match hit/miss strip, and one "Next match" fix. No goal button until Goals ship (decision 2).
- 5d notes (2026-09-29): `SoloFocusCard` sits beside the climb card (2:1, stacked below 900px) and is left out when `focus` is null, with the climb card taking the full width. New pattern `BaseGoalStrip` (20 cells: hit filled primary, missed a warn ring, not applicable a track stub, the same language as the win-factor dots). The evidence names each factor's miss ("when you're behind", "with more") where FR 20's "below it" reads wrong; the Support vision and Jungle CS fixes are chosen from the mark.

### 5e. Death data (backend first)
- [x] Extend `participant_death_events`: `timestamp_sec`, `killer_participant_id` (gives role and whether it was the lane opponent), `allies_nearby` (allies within ~1,500 units in the nearest participant frame; per-minute positions, so approximate), and `participants.riot_participant_id` so timeline IDs map to rows (the victim's side for mirroring comes from `participants.team_id`). Migration `004_SoloTrendsDeathDetail.sql` + `database-schema.spec.md` (add the missing table).
- [x] Persist objective events from the same timeline payload (`ELITE_MONSTER_KILL`, `BUILDING_KILL`: type, team, timestamp). Shared with 4d; build it once.
- [x] Backfill (decided 2026-09-29): raw timelines are not kept, so `DeathDetailBackfillJob` re-fetches them for the last 50 Summoner's Rift matches of active accounts. It runs at the lowest priority through `RiotLimitHandler` (at most 20 of the 50 requests per 2 minutes, yielding to user syncs) and reports progress and "Waiting on Riot's servers" in the death-zones card. The same line replaces the temporary `sync_rate_limited` copy (spec FR 38–40).
- 5e notes (2026-09-29):
  - Allies nearby uses 2,000 units (the spec's FR number; the older ~1,500 above is superseded).
  - `DeathDetailWriter` writes a match's deaths and objective events for both sync and the backfill, replacing the match's rows in one transaction each, so a re-read timeline never duplicates deaths. A match counts as done once its row has `riot_participant_id` and no death lacks `timestamp_sec` (or it is skipped).
  - The limit handler is now one DI singleton shared by every Riot call; it implements `IRiotThrottleState`. `RiotBackgroundActivity` marks rank-snapshot ticks so the backfill waits for them.
  - Fixed on the way: sync WebSocket messages sent as their base type (`sync_aggregate_*`) lost every field but `type` and `status`; they now serialize by runtime type (`SyncMessageSerializer`), so multi-account sync progress reaches the client.
  - Not run against a real database or Riot yet (no MySQL or Riot access in the build container): migration 004 and the backfill need a check on dev. The death-zones endpoint, its `backfill` field and the card's progress, and replacing the `sync_rate_limited` copy are 5f.

### 5f. Death zones card
- [x] Core: map every death to one of a fixed set of named regions (lanes split at the river, top and bottom river, dragon pit, Baron pit, the four jungle quadrants, both bases), mirrored so the player's base is bottom left. Named regions over clustering: stable between visits and easy to label. Classify each death: ganked in lane (before 14 min, in own lane, killer not the lane opponent), caught alone (no allies nearby), in a teamfight (3+ per side involved); "cost an objective" = the enemy took a dragon, Baron, Herald or tower within 60 seconds.
- [x] `GET /api/v2/solo/death-zones/{userId}`: top 5 zones (deaths, lost objectives, the timing note), totals and the three breakdowns, filterable by zone. Zones under 5 deaths left out.
- [x] Frontend: `DeathMap` (outline map, zone circles sized by deaths, orange when 30%+ cost an objective, `role="img"` with every zone in the label), zone list (`aria-pressed` rows that filter the breakdowns), breakdown bars. Retire `DangerZonesMap` and the heatmap canvas; replace or remove `DeathPositionsEndpoint`.
- 5f notes (2026-09-29):
  - Rules in `Core/Services/Solo/`: `MapRegions` (16 regions, tested pits → bases → lanes → mid → river → jungles, each with a fixed anchor for the map), `DeathClassifier` (phase, how, cost) and `DeathZonesCalculator` (top 5 zones by objectives lost then deaths, costly at 30%, a timing note when one phase holds 60% of a zone's deaths).
  - Counted deaths need a time and an allies count but not a killer: executions by towers or minions have none and are still deaths (a change to FR 31, recorded in the feature spec).
  - The endpoint's `backfill` is set only while matches in range lack death detail; asking for it moves the account to the front of the backfill queue. The card follows `detail_backfill_progress` on the sync WebSocket (refetching when done) and polls every 30 seconds without a socket.
  - `SyncProgress`'s waiting line now reads "Waiting on Riot's servers. We'll continue automatically.", the same copy as the card.
  - Retired: `DangerZonesMap` (and its heatmap canvas), `getDeathPositions`, `DeathPositionsEndpoint`, `IDeathPositionsRepository` and their tests. `ExtractDeathPositions` stays; sync and the backfill use it.
  - Checked with unit and endpoint tests and visually with a mocked API. Still not run against MySQL or Riot (migration 004, the backfill, real death positions) or in the E2E smoke.

### 5g. Rank averages (optional, later)
- [x] Source the "Emerald average" benchmark: aggregate Mongoose.gg matches per tier and role once there are enough (minimum sample in 5a), else keep "Your season average". No third-party stats sites without checking their terms.
- 5g notes (2026-09-30):
  - Only linked accounts carry a rank (`tier_after` comes from their rank snapshots), so the pool is Mongoose.gg players only. Minimum sample: 200 matches from 20 players after leaving out the player's own; a stat needs 100 of them to draw a line. The rank average replaces the season average for the whole card, so the key names one benchmark.
  - Only for one ranked queue of one account with a known tier and a role in 70% of the range; otherwise "Your season average" stays. Divisions aren't split (all of Emerald together); Master, Grandmaster and Challenger are separate pools.
  - `RankBenchmarkService` caches each pool (newest 2,000 matches) for an hour per queue, tier and role; the query stays on `idx_puuid` through an `EXISTS` on `user_riot_accounts`.
  - Before 5g was built, migration 004 was applied on dev and the page checked there (2026-09-30). 5g itself has only run against the test doubles; how often the pool qualifies depends on how many players share a tier and role.

### Design system (Step 5, alongside the PRs that first use each piece)
- [x] Add TrendTile, the win-factor row (two dots, hit vs missed), the goal strip, DeathMap and the zone list to the live system, `reference/`, and the canvas "Current" row. Record LaneBar's reuse for LP per champion.
- Design system (Step 5, done 2026-09-29): new TrendTile (`mp-trend`), WinFactorRow (`mp-win-factor`), GoalStrip (`mp-goal`) and DeathMap (`mp-death-map` with the zone list `mp-zone` and breakdown bars `mp-breakdown`); LaneBar's reuse for LP per champion recorded; "Solo order" in the brand book; `reference/` refreshed; "Current · Solo" artboard added to the mockup canvas. As with Matches, the app's components keep their own scoped styles rather than these `mp-*` classes.

### Decisions (2026-09-29)
1. **Climb fallback:** the climb card always shows. When fewer than 80% of the ranked matches in the range have a known LP change (or fewer than 10 have one), it draws the 10-match rolling win rate instead, titled with the takeaway ("Win rate up from 52% to 58%"), with one caption line saying LP appears as matches sync. The headline falls back to wins ("28 wins in your last 50"). LP per champion falls back to net wins per champion (wins minus losses, same bars around zero). Same threshold everywhere, one Core rule.
2. **Goal button:** hidden until Goals ship inside Advanced. The focus card ends with the "Next match" fix.
3. **Death zones:** always visible on the page (as drawn); the Deaths tile does not open it.
4. **Arrows:** as drawn. The arrow shows the direction of the number, the colour and the verdict word show whether that is good (fewer deaths: ▼ in purple, "Improving").
5. **Highlight:** the focus card is the Solo page's one highlight card (Readiness keeps that role on the Overview).

---

## Later phases

One PR each, through `/mongoose-design`:

- [x] Matches page — **built on branch `claude/champion-select-rewrite-im52lv`** (2026-09-28). Frontend only, existing endpoints (`/matches/{userId}`, `/matches/{matchId}/details`, `/matches/{matchId}/narrative`).
  - Route is now `/app/matches/:matchId?` (one record, so the Matches pill and tab stay current on a match; header and tab bar mark sub-paths current). Overview's last-match row links to `/app/matches/<id>`.
  - Order: headline from the list ("You won 12 of your last 20 matches") + streak / most-played line + queue SegmentedControl → list card (`BaseMatchRow`s, sticky 420px column on desktop) beside the open match → `MatchHeader` → `WinPredictionStats` → `MatchNarrative` → `TeamComparison` → `StatSnapshot` → `MatchActions`. Desktop opens the newest match on arrival; phones show the list, and a match replaces it with an "All matches" link.
  - `BaseMatchRow` gained `remake` and `riotId` props, the selected row (`aria-current="page"` on `surface-selected`) and a `match-list` container query; `components/matches/MatchRow.vue` is merged into it. Copy and small rules in `utils/matchesSummary.js` (incl. `formatSigned` with a real minus).
  - Every card has skeleton / error-with-retry / empty / content; the lanes card retries on its own. Green/red, emoji, uppercase badges, gradients and Heroicons-style inline SVGs are gone; your team purple, enemy orange.
  - Retired: `MatchList`, `matches/MatchRow`, `TrendBadge` (its finding is now a chip in `MatchHeader`), the unused `ImpactStats`, and their unit tests; the disabled "View Goal Impact" button.
  - Queue filter: All queues / Solo/Duo / Flex / Normal (four options max); ARAM matches show under All queues.
  - Design system (Step 5, done 2026-09-28): MatchRow gained the selected, remake and narrow-list (`mp-match-list`) states; new StatTile (`mp-stat`), LaneRow (`mp-lane-row`) and SplitBar (`mp-split`); token notes for `surface-raised`, `surface-selected` and `track-strong` extended; "Matches order" in the brand book; `reference/` refreshed; "Current · Matches" artboard added to the mockup canvas. The app's components keep their own scoped styles for now rather than these `mp-*` classes.
  - Next: the visual redesign, planned as Phase 4 above.
  - Leftovers: no LP change in the list yet (the API has none); champion names still show Riot's internal ID; no visual pass with real match data yet (the E2E user has no matches); E2E only covers the page loading (smoke).
- [ ] Solo page — planned as Phase 5 above.
- [x] Champion Select page — **built on branch `claude/champion-select-rewrite-im52lv`** (from `design_phase3_champion_pool`, 2026-09-27). Frontend only, existing endpoints (`/champion-select`, `/solo/matchups`).
  - Order: filters → ChampionHero for the selected pick → "Your picks" (role SegmentedControl + three selectable ChampionCards) → matchups for the pick beside "Check a matchup".
  - New: `BaseSegmentedControl` (`mp-seg`), the selectable ChampionCard (`aria-pressed`, `mp-spotlight` ported from Vue Bits SpotlightCard, `--color-spotlight`), `ChampionHero` `chipsLabel`, `components/championSelect/ChampionSelectMatchups.vue` and `ChampionSelectSearch.vue`, `utils/championSelectSummary.js`.
  - Retired: `MainChampionCard`, `OpponentSearchBar` and their unit tests. The matchups request is no longer made twice (page + card).
  - Filters are now SegmentedControls: ARAM is gone from the queue options (no lanes), and the time range is This season / Last 3 months / All time (last week, last month and 6 months dropped). The API still counts days, not matches.
  - Leftovers: add the matchup list rows, the "Check a matchup" card and the Strong into / Weak into headings to the live design system and `reference/` (Step 5); champion names still show Riot's internal ID ("TwistedFate"); E2E only covers the page loading (smoke).
- [ ] Settings and Feedback pages
- [x] Switch the Matches components to the design system's classes, so the app and the system share one source of styling. Today they copy the look in their own scoped styles:
  - `BaseMatchRow` (open-match row, remake, narrow list) → `mp-match-list`, `mp-portrait--remake`, `mp-result--remake`, `mp-match-kda` / `mp-match-meta-kda`
  - `WinPredictionStats` (`kpi-tile`) and `StatSnapshot` (`stat-item`) → `mp-stat-grid` / `mp-stat`
  - `MatchNarrative` (`lane-header`) → `mp-lane-row`
  - `TeamComparison` damage bar → `mp-split`

  Port the classes from `reference/components.css` into `client/src/style.css`, then delete the duplicated scoped rules. Keep the existing `data-testid`s and the class hooks the unit tests rely on (`kpi-tile`, `stat-item`, `lane-row`, the sentiment classes).
- [ ] Advanced page (Team + Goals combined) back into the navigation
