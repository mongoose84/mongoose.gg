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

Four PRs, in order; each is shippable alone. **Status: 4a not started.**

### 4a. Visual pass with existing data (frontend only)
- [ ] Port the classes the page needs from `reference/components.css` into `client/src/style.css`: `mp-form`, `mp-columns`, `mp-lane-bar`, `mp-split`, `mp-stat`, `mp-hero--match`, the MatchRow additions. Delete the scoped copies they replace (this covers the "Switch the Matches components to the design system's classes" item below).
- [ ] Page header: short headline ("12 wins in your last 20") + `FormStrip` of the list (win / loss / remake from `matchesSummary.isRemake`). New base component `BaseFormStrip.vue`.
- [ ] Under the list: `ColumnChart` "win rate by start time" from `gameStartTime` in the player's local time (afternoon / evening / after 11pm, groups under 3 matches left out); title states the weak spot or is omitted when nothing stands out. New `BaseColumnChart.vue`; grouping logic in `matchesSummary.js`.
- [ ] Open match: `MatchHeader` becomes the ChampionHero match banner (splash via `getChampionSplashUrl`, glass chips K / D / A; LP and rank chips come in 4b). "Download data" moves off the banner into the "All your stats" section.
- [ ] Lanes: `MatchNarrative` rows become `LaneBar`s from `allyParticipant.goldDiffAt10` (±1,500 fills a half, under 300 is even); same button, `aria-expanded` and `LaneMatchupDetails` underneath. ARAM keeps the two team lists.
- [ ] Team: `TeamComparison` becomes `SplitBar`s for damage, dragons, barons and towers beside the lanes (team gold totals aren't in the API, so no gold bar; the gold lead at 15 stays as the caption).
- [ ] `WinPredictionStats` stays in the "What decided it" slot until 4c replaces it; `StatSnapshot` moves behind a closed "All your stats" disclosure with "▲ n above usual ▼ n below" in its summary row. `MatchActions` stays.
- [ ] Tests: unit tests for the new base components and the grouping logic; update `MatchesPage`, `MatchHeader`, `MatchNarrative`, `TeamComparison` specs. Visual pass at 1440 / 1024 / 390px with a real account.

### 4b. LP change and rank after each match (backend + frontend)
- [ ] Add `lpChange` (and `tierAfter` / `rankAfter`) to `MatchListSummaryItem` and `MatchDetailsItem`: from `participants.lp_after`, compared with the same player's previous ranked match in the same queue; null for unranked queues, the first ranked match, and tier or division changes it can't resolve (decide in the PR whether promotions are computed or shown as "Promoted"). Parameterized SQL, update `architecture.spec.md`.
- [ ] Wire `lpChange` into `BaseMatchRow` (the prop exists), the page header total ("+86 LP over 20", ranked queues only) and the banner chips (LP, rank with its tier colour).
- [ ] Backend integration tests for the delta, including queue separation and missing `lp_after`.

### 4c. "What decided it" (spec first, then backend + frontend)
- [ ] Feature spec (`feature-spec` / `architect`): how the deciding stat is picked (extend `TrendBadgeCalculator`'s biggest-deviation logic across gold at 10, kill participation, CS at 10, deaths before 10, vision), the "usual" baselines it needs (`RoleBaseline` has no gold-at-10 or CS-at-10 averages yet), the finding and fix copy per stat, and what shows when nothing stands out.
- [ ] Build: one `surface-highlight` card with a one-line finding, three `UsualMeter`s, one "Next match" fix (Lucide `eye` for vision fixes; other fix icons from the vocabulary). Retire `WinPredictionStats` and the list-row trend badge chip.

### 4d. Gold over time (data first)
- [ ] Sync already downloads the match-v5 timeline for `participant_checkpoints`; persist per-minute team gold and the objective events (dragon, Baron, Herald, towers) from the same payload. Schema change + `database-schema.spec.md`; backfill is optional (older matches fall back to the 10/15/20/25 checkpoints or hide the chart).
- [ ] Endpoint field + the line chart per the design system (takeaway title, `primary` line, moment markers on the axis, orange point for the biggest swing, text alternative).

### Afterwards
- Overview and Champion Select visual passes (artboards `OverviewVisual`, `ChampionSelectVisual` on the same canvas; desktop only for now, phone versions deferred). Champion Select works with existing data. The Overview needs per-match results for its week and today strips, LP from 4b, and a high-deaths win rate next to `winRateLowDeaths` for its deaths chart.

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
  - Next: the visual redesign, planned as Phase 4 below.
  - Leftovers: no LP change in the list yet (the API has none); champion names still show Riot's internal ID; no visual pass with real match data yet (the E2E user has no matches); E2E only covers the page loading (smoke).
- [ ] Solo page
- [x] Champion Select page — **built on branch `claude/champion-select-rewrite-im52lv`** (from `design_phase3_champion_pool`, 2026-09-27). Frontend only, existing endpoints (`/champion-select`, `/solo/matchups`).
  - Order: filters → ChampionHero for the selected pick → "Your picks" (role SegmentedControl + three selectable ChampionCards) → matchups for the pick beside "Check a matchup".
  - New: `BaseSegmentedControl` (`mp-seg`), the selectable ChampionCard (`aria-pressed`, `mp-spotlight` ported from Vue Bits SpotlightCard, `--color-spotlight`), `ChampionHero` `chipsLabel`, `components/championSelect/ChampionSelectMatchups.vue` and `ChampionSelectSearch.vue`, `utils/championSelectSummary.js`.
  - Retired: `MainChampionCard`, `OpponentSearchBar` and their unit tests. The matchups request is no longer made twice (page + card).
  - Filters are now SegmentedControls: ARAM is gone from the queue options (no lanes), and the time range is This season / Last 3 months / All time (last week, last month and 6 months dropped). The API still counts days, not matches.
  - Leftovers: add the matchup list rows, the "Check a matchup" card and the Strong into / Weak into headings to the live design system and `reference/` (Step 5); champion names still show Riot's internal ID ("TwistedFate"); E2E only covers the page loading (smoke).
- [ ] Settings and Feedback pages
- [ ] Switch the Matches components to the design system's classes, so the app and the system share one source of styling. Today they copy the look in their own scoped styles:
  - `BaseMatchRow` (open-match row, remake, narrow list) → `mp-match-list`, `mp-portrait--remake`, `mp-result--remake`, `mp-match-kda` / `mp-match-meta-kda`
  - `WinPredictionStats` (`kpi-tile`) and `StatSnapshot` (`stat-item`) → `mp-stat-grid` / `mp-stat`
  - `MatchNarrative` (`lane-header`) → `mp-lane-row`
  - `TeamComparison` damage bar → `mp-split`

  Port the classes from `reference/components.css` into `client/src/style.css`, then delete the duplicated scoped rules. Keep the existing `data-testid`s and the class hooks the unit tests rely on (`kpi-tile`, `stat-item`, `lane-row`, the sentiment classes).
- [ ] Advanced page (Team + Goals combined) back into the navigation
