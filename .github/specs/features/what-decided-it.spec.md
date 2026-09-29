# Feature: What decided it

> Phase 4c of `design-migration.plan.md`. Replaces `WinPredictionStats` (six tiles, each judged on its own) with one `surface-highlight` card: a one-line finding, three UsualMeters as evidence, and one "Next match" fix. Retires the trend badge (`TrendBadgeCalculator`, `trendBadge` on list items, the chip on the match banner). Target: the "What decided it" card on the `MainVisual` and `PhoneMatchVisual` artboards (https://claude.ai/artifact/Ufv95okAfSYgmnaJgniRhL).

## Problem Statement
The open match shows six KPI tiles (`WinPredictionStats`), and each tile is judged with its own hard-coded thresholds (deaths ±1 from average, gold at 15 ±500, and so on). The player has to work out for themselves which one mattered. Separately, the banner shows a trend-badge chip from `TrendBadgeCalculator` that uses a different set of stats and thresholds, so the page can make two different claims about the same match. The design system asks for "show, then say": one finding, the comparison drawn, one fix.

Two data gaps block that:
- `RoleBaseline` has no usual value for gold lead at 10, CS at 10, deaths before 10 or vision per minute.
- The baseline is "the last 10 matches in this role", and that includes the match being opened. So the match is partly compared with itself, and an old match is compared with matches played after it.

## Proposed Solution
- **Backend.** A Core domain service, `DecidingStatCalculator`, scores five early-game and involvement stats against the player's own usual in this role. The usual comes from up to 20 matches before the opened one, never including it. The service picks the deciding stat, the two strongest supporting stats and the stat to fix. The match-details endpoint returns the result as a new `decidingStat` field.
- **Frontend.** The frontend owns all copy (finding and fix lines per stat) and renders the card. `TrendBadgeCalculator`, the `trendBadge` fields, the banner chip and `WinPredictionStats` are removed.

## User Stories
### Primary User Story
As a player opening a match, I want one line that says what decided it, with the numbers against my usual next to it, so I know what mattered without reading six tiles.

### Additional User Stories
- As a player who lost, I want the one thing to do differently next match, so the loss turns into something I can practise.
- As a player who won, I want to see the strength that carried the match compared with my usual, so I know what to keep doing.
- As a player whose match was ordinary, I want the page to say so rather than invent a story.

## Requirements

### Functional Requirements

**Stats**

1. There are five candidate stats:

   | Key | Label (sentence case) | This match | Better | Spread floor | Eligible when |
   |---|---|---|---|---|---|
   | `goldLeadAt10` | Gold lead at 10 | `participant_checkpoints.gold_diff_vs_lane` at minute 10 | higher | 400 | match ≥ 10 min and a lane opponent exists (not null) |
   | `csAt10` | CS at 10 | `participant_checkpoints.cs` at minute 10 | higher | 8 | match ≥ 10 min; role is not `UTILITY` |
   | `deathsBefore10` | Deaths before 10 | `participant_metrics.deaths_pre_10` | lower | 0.7 | match ≥ 10 min |
   | `killParticipation` | Kill participation | `participant_metrics.kill_participation_pct` | higher | 8 (points) | team kills ≥ 5 |
   | `visionPerMin` | Vision per minute | `vision_score / (game_duration_sec / 60)` | higher | 0.25 | always |

   The spread floor stops a very steady stat from producing huge scores, for example deaths before 10 that are almost always 0.

**Usual**

2. The usual for a stat is its average and sample standard deviation over the player's most recent 20 matches **before** the opened match's `game_start_time`, where all of these hold:
   - the same account;
   - the same `role`;
   - duration ≥ `MinValidGameDurationSec`;
   - queue in the Summoner's Rift set: 400, 420, 430, 440, 490. ARAM, Arena and other modes are excluded.

   Averages and deviations are computed per stat over rows where that stat is not null, so a match without a checkpoint at minute 10 doesn't pull the gold average towards 0.
3. A stat is eligible only when both hold:
   - it has at least 5 matches in its usual;
   - it meets its "Eligible when" rule in FR 1.

**Scoring**

4. Score per eligible stat: `score = (value − usual) / max(stddev, floor)`. For `deathsBefore10` the sign is flipped. After that, a positive score always means "better than your usual".
5. A stat **stands out** when `|score| ≥ 1.0`.

**Choosing the deciding stat**

6. The deciding stat is chosen as follows:
   - **Win:** the highest score among stats with `score ≥ 1.0`. If there is none, the lowest score among stats with `score ≤ −1.0` ("won despite").
   - **Loss:** the lowest score among stats with `score ≤ −1.0`. If there is none, the highest score among stats with `score ≥ 1.0` ("you did your part").
   - **No stat stands out:** `outcome = "none"` and `stat = null`.
   - **Tie-break:** the order of the table in FR 1.

   `outcome` is `"strength"` when the chosen score is positive and `"shortfall"` when it is negative.

**Meters and fix**

7. **Meters.** Up to three per card, each with the stat, this match's value, the usual and the score. The deciding stat comes first. The rest are filled by `|score|` descending among eligible stats. With `outcome = "none"`, all three are chosen by `|score|`.
8. **Fix.** The stat with the lowest score, when that score is ≤ −0.5. It may be the deciding stat. If no stat scores ≤ −0.5, `fix = null`: every stat was at or above the usual, and there's nothing honest to fix.

**When the card is omitted**

9. `decidingStat` is `null` and the card is left out when any of these holds:
   - the match is a remake (`isRemake`, same rule as the list);
   - the queue is not in the Summoner's Rift set;
   - the role is `UNKNOWN`;
   - no stat is eligible.

   This is the "sections without real data are left out" rule from the plan.

**Retiring the trend badge**

10. Retire the trend badge:
    - delete `TrendBadgeCalculator`, the `TrendBadge` record, `trendBadge` on `MatchListItem` and `MatchListSummaryItem`, and `TrendBadgeCalculatorTests`;
    - in `MatchHeader`: delete the `badge` prop and chip; in `MatchesPage`: delete `openMatchBadge`;
    - delete the `trendBadge` mentions in tests and the test factory.

    `GetRoleBaselinesAsync` stays: `MatchDetailsResponse.baseline` still feeds `StatSnapshot` ("▲ n above usual").
11. Delete `WinPredictionStats.vue` and its unit test. The card takes its slot in `MatchDetails`, between the banner and the lanes.

### Non-Functional Requirements
- **Performance:** one extra query per details request (the usual, FR 2), which reads at most 20 rows for one account and role. The details endpoint p95 must not rise by more than 30 ms on the dev database. The match-list endpoint gets faster: it no longer computes badges, and it only fetches baselines for the unused `baselinesByRole`.
- **Security:** no new endpoint. The existing ownership check and server-side account resolution in `MatchDetailsEndpoint` apply. The new SQL is parameterized (`puuid`, `role`, `before`, queue ids). Nothing new is logged.
- **Accessibility:**
  - each UsualMeter is `role="meter"` with `aria-valuemin`, `aria-valuemax`, `aria-valuenow` and an `aria-valuetext` that states both numbers ("+1,240 gold, your usual is +180");
  - the card has `aria-labelledby` pointing at the finding;
  - the fix icon is `aria-hidden`.
- **Compatibility:** the same breakpoints as the rest of the Matches page (1440 / 1024 / 390).

## Technical Approach

### Backend Changes
**Language**: C#
**Components**:
- [ ] Query models (`Core/QueryModels/MatchQueryModels.cs`):
  - `StatUsual(double Average, double StdDev, int Matches)`;
  - `DecidingStatInput`: this match's five values (nullable), role, queue, duration, win, team kills, remake;
  - the response records `DecidingStat`, `DecidingStatMeter`, `DecidingStatFix` (FR 6–8), with `[JsonPropertyName]` camelCase.
- [ ] Business logic, `Core/Services/DecidingStatCalculator.cs`: a pure static calculator for FR 1 and 3–9. The thresholds are named constants with a one-line reason each, like `TrendBadgeCalculator` today. Input: `DecidingStatInput` plus `IReadOnlyDictionary<string, StatUsual>`.
- [ ] Repository:
  - `IMatchesRepository.GetStatUsualsAsync(string puuid, string role, long beforeGameStartTime)` in `MatchesRepository`. It uses the `ROW_NUMBER()` pattern of `GetRoleBaselinesAsync`, with `LEFT JOIN participant_checkpoints` at `minute_mark = 10`, `AVG` / `STDDEV_SAMP` / `COUNT` per stat, and the Summoner's Rift queue list as parameters.
  - Add `GoldDiffAt10` and `CsAt10` to `MatchDetailsItem`, from the `pc10` join that already exists at `MatchesRepository.cs:604`.
- [ ] Endpoint, `Application/Endpoints/Matches/MatchDetailsEndpoint.cs`: fetch the usuals and call the calculator. Add `DecidingStat? DecidingStat` to `MatchDetailsResponse`.
- [ ] Removal (FR 10): `TrendBadgeCalculator.cs`, the `TrendBadge` record and the fields, the badge code in `MatchesRepository` (lines ~155 and ~281), `TrendBadgeCalculatorTests.cs`.
- [ ] Database migrations: none.

### Frontend Changes
**Framework**: Vue

**Components**:
- [ ] UI components:
  - `client/src/components/base/BaseUsualMeter.vue`: the design-system UsualMeter (`mp-usual*` classes). Props: `label`, `value` (formatted), `now`, `min`, `max`, `usual`, `better` (`'higher'|'lower'`), `valueText`.
  - `client/src/components/matches/DecidingStatCard.vue`: the card (`mp-card mp-card--highlight`).
- [ ] Copy and formatting, `client/src/utils/decidingStat.js`:
  - the finding and fix line per stat and outcome (tables below);
  - value formatting (signed gold with a real minus, whole %, one decimal per minute);
  - meter scale per stat (FR "Meter scale" below);
  - the fix icon per stat.
- [ ] Wiring: `MatchDetails.vue` renders `DecidingStatCard` when `response.decidingStat` is not null, in place of `WinPredictionStats`. The store/page passes `decidingStat` through beside `baseline`.
- [ ] Removal (FR 10–11):
  - `WinPredictionStats.vue` and its spec;
  - the `badge` prop and chip in `MatchHeader.vue`, and `openMatchBadge` in `MatchesPage.vue`;
  - the `trendBadge` assertions in `MatchesPage.spec.js`.
- [ ] Styles: port `mp-usual*` from `reference/components.css` into `client/src/style.css` if 4a didn't. `mp-card--highlight` exists.

### Database Changes
**Database**: MySQL
- [ ] New tables: none
- [ ] Modified tables: none
- [ ] New indexes: none needed at this size. The query filters `participants` by `puuid` (indexed) and orders by `matches.game_start_time`. Check `EXPLAIN` on the dev database; add `(puuid, role)` only if it scans.
- [ ] Data migrations: none. Older matches without a minute-10 checkpoint simply make those two stats ineligible.

### API Contracts
#### Match details (extended)
```
GET /api/v2/matches/{matchId}/details?accountId=
```
**Response** (new field only):
```json
{
  "match": { "...": "unchanged, plus goldDiffAt10 and csAt10 (int | null)" },
  "baseline": { "...": "unchanged" },
  "decidingStat": {
    "outcome": "strength",
    "stat": "goldLeadAt10",
    "meters": [
      { "stat": "goldLeadAt10", "value": 1240, "usual": 180, "score": 2.4 },
      { "stat": "killParticipation", "value": 71, "usual": 58, "score": 1.3 },
      { "stat": "csAt10", "value": 84, "usual": 71, "score": 1.2 }
    ],
    "fix": { "stat": "visionPerMin", "value": 0.6, "usual": 0.9, "score": -0.8 },
    "usualMatches": 20
  }
}
```
- `outcome`: `"strength" | "shortfall" | "none"`. With `"none"`, `stat` is `null`.
- `decidingStat` is `null` in the cases listed in FR 9.
- `usualMatches` is the size of the usual sample (the maximum across stats), for the card's note.
- `trendBadge` is removed from the list items of `GET /api/v2/matches/{userId}` (6.13). Update `architecture.spec.md` 6.13 and 6.14.

## UI/UX Requirements

All views follow the Mongoose.gg design system (built with `/mongoose-design`). Components: UsualMeter, the `surface-highlight` card, Lucide icons through `BaseIcon`.

### DecidingStatCard (open match, `MatchDetails`)

**Layout**: The first card under the ChampionHero banner, full width of the match column. It is the only `surface-highlight` card on the Matches view. On desktop the three meters sit side by side; below 900px there is one per row.

**Structure**:
```
┌ surface-highlight, 1px border-highlight, radius-xl ─────────────────────┐
│ WHAT DECIDED IT                                              (eyebrow) │
│ You won your lane by 1,240 gold at 10                  (h2, finding)   │
│                                                                        │
│ Gold lead at 10  +1,240 │ Kill participation  71% │ CS at 10       84  │
│ ████████░░░|░░░░░░░░░░  │ ███████████|░░░░░░░░░  │ ████████|░░░░░░░   │
│                                                                        │
│ ▏Your usual · last 20 matches as Mid                          (note)   │
│ ┌ surface ───────────────────────────────────────────────────────────┐ │
│ │ (eye) Next match: buy a control ward on your first back.           │ │
│ └────────────────────────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────────────────────┘
```

**Components**:
- Eyebrow: `mp-eyebrow`, "What decided it".
- Finding: `h2`, Clash Display, one line (it may wrap on phones). Copy from the table below with the values filled in.
- Meters: `BaseUsualMeter` × 1–3. The fill is `primary` when the score is > 0 and `warn` when it is < 0. The `ink` tick marks the usual. The track is `track-strong`.
- Usual key: one line under the meters, "Your usual · last {n} matches as {Role}". Role names follow the rest of the app (Top, Jungle, Mid, Bot, Support).
- Fix row: `surface` inset, a 20px Lucide icon in `ink-muted`, then "**Next match:** {fix}". Left out when `fix` is null.

**Finding copy** (`{v}` = this match's formatted value, `{d}` = difference from usual, formatted):

| Stat | Strength | Shortfall |
|---|---|---|
| goldLeadAt10 | You won your lane by {v} gold at 10 | You were {v} gold behind your lane at 10 |
| csAt10 | {v} CS at 10, {d} more than usual | {v} CS at 10, {d} fewer than usual |
| deathsBefore10 | No early deaths to hold you back | {v} deaths before 10 put you behind early |
| killParticipation | You were in {v} of your team's kills | You were in only {v} of your team's kills |
| visionPerMin | Your vision was well above usual at {v} per minute | Your vision dropped to {v} per minute |

For `deathsBefore10` strength with a value > 0, use "Only {v} death(s) before 10, fewer than usual".

With `outcome = "none"` the finding is "A match like your usual: nothing stood out", and the meters still show.

**Fix copy** (by the `fix` stat; the icon is from the design-system vocabulary):

| Stat | Icon | Fix |
|---|---|---|
| goldLeadAt10 | `coins` | Trade when your wave is pushing into them, not before. |
| csAt10 (not Jungle) | `wheat` | Last-hit every cannon minion until 10 minutes. |
| csAt10 (Jungle) | `wheat` | Finish your full first clear before the first gank. |
| deathsBefore10 | `skull` | Back off when you can't see their jungler before 10. |
| killParticipation | `handshake` | Move with your team for the first dragon fight. |
| visionPerMin (Support) | `eye` | Place a control ward before every dragon. |
| visionPerMin (others) | `eye` | Buy a control ward on your first back. |

**Meter scale** (the value is clamped to the scale; the label shows the real number):

| Stat | min | max |
|---|---|---|
| goldLeadAt10 | −2,000 | +2,000; widens to the next 1,000 above max(\|value\|, \|usual\|) |
| csAt10 | 0 | 100; widens to the next 20 above max(value, usual) |
| deathsBefore10 | 0 | max(4, value, ⌈usual⌉) |
| killParticipation | 0 | 100 |
| visionPerMin | 0 | max(3, ⌈max(value, usual)⌉) |

For deaths, less is better. The bar still grows left to right (design system), and the fill colour follows the score, not the length.

**Behavior**:
- The card renders with the match details. There is no separate loading state, because it shares the details request and its skeleton.
- `decidingStat` null → no card, and the next section moves up.
- Numbers follow the voice rules: gold signed with a real minus (−850); percentages whole (71%); per minute with one decimal (0.6).

**Accessibility**:
- `<section aria-labelledby>` on the finding `h2`.
- Each meter: `role="meter"` plus `aria-valuetext`, for example "71 percent, your usual is 58 percent", or "1 death before 10, your usual is 0.4".
- The fix is plain text. The icon is `aria-hidden="true"`.

### MatchHeader (banner)
- Remove the trend-badge chip and the `badge` prop. The K/D/A, LP and rank chips stay.

## Testing Strategy

### Unit Tests
**Frameworks**: xUnit (backend), Vitest (frontend)
- [ ] `DecidingStatCalculatorTests`:
  - scores with the spread floor;
  - the flipped sign for deaths;
  - win picks the top strength; loss picks the worst shortfall;
  - the fallbacks in both directions;
  - `"none"` below the threshold;
  - tie-break order;
  - meters ordered and capped at 3;
  - fix at ≤ −0.5 and null above;
  - eligibility: fewer than 5 matches, null checkpoint, match under 10 min, Support CS, team kills < 5;
  - null for a remake, ARAM or `UNKNOWN` role.
- [ ] `BaseUsualMeter.spec.js`:
  - fill width and tick position against min/max, including a negative min for gold;
  - `primary` vs `warn` by score, with deaths coloured by score, not length;
  - aria attributes.
- [ ] `decidingStat.spec.js`:
  - every finding and fix line per stat and outcome;
  - role variants (Jungle CS, Support vision);
  - formatting (real minus, whole %, one decimal);
  - scale widening.
- [ ] `DecidingStatCard.spec.js`:
  - renders the finding, 1–3 meters, the usual note and the fix;
  - no fix row when `fix` is null;
  - the `"none"` copy.
- [ ] Update `MatchDetails.spec.js` (card instead of `WinPredictionStats`), `MatchHeader.spec.js` (no badge) and `MatchesPage.spec.js` (no `trendBadge`).

### Integration Tests
- [ ] Details endpoint returns `decidingStat` for a seeded ranked match with ≥ 5 earlier matches in the role.
- [ ] The usual excludes the opened match and every later match: seed a later match with extreme values and check it doesn't move the usual.
- [ ] The usual excludes other roles and ARAM.
- [ ] `decidingStat` is null for ARAM and for a player with < 5 earlier matches in the role.
- [ ] The ownership check still returns 403 or 404 for another user's account (existing test stays green).
- [ ] The match list no longer returns `trendBadge`.

### Manual Testing Scenarios
1. Open a recent win on the dev database: the finding names a strength, the meters' ticks sit at plausible usuals, and the fix is either missing or about a weaker stat.
2. Open a loss with early deaths: the finding is the deaths shortfall and the fix line is about deaths.
3. Open an ARAM match and a remake: no card. The banner has no trend chip.
4. Open the oldest match you have in a role: no card, or fewer meters (fewer than 5 earlier matches).
5. Check 1440 / 1024 / 390px: three meters side by side on desktop, stacked on the phone. The card is the only highlighted card.

## Validation Criteria
Feature is considered complete when:
- [ ] All functional requirements are implemented.
- [ ] Backend and frontend unit tests, integration tests, build and E2E pass. Update `client/e2e` if a test waited on `win-prediction-stats`.
- [ ] `architecture.spec.md` (6.13, 6.14, domain services list), `ui-ux.spec.md` (Matches component inventory) and `design-migration.plan.md` (4c) are updated.
- [ ] Code review is done. Security: no new endpoint, parameterized SQL, no new logging (checked in review).
- [ ] The details endpoint's p95 stays within +30 ms.
- [ ] Accessibility: meters announce both numbers, and the card is keyboard-neutral (no interactive parts except what exists).
- [ ] Visual pass with a real account at 1440 / 1024 / 390px against the artboards.
- [ ] Design system Step 5: the Matches artboards and `reference/` updated if the built card differs from the mockup (for example the usual note wording or the fix row).

## Dependencies
### Internal Dependencies
- [x] 4a: `MatchDetails` layout, `mp-card--highlight`, `MatchHeader` banner.
- [x] `participant_checkpoints` at minute 10 (sync already stores it) and `participant_metrics.deaths_pre_10` / `kill_participation_pct`.
- [ ] `mp-usual*` classes in `client/src/style.css` (port if missing).

### External Dependencies
- None.

## Risks and Mitigations
| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| A noisy usual (5–8 matches) names the wrong stat | Medium: a wrong finding costs trust | Medium | Minimum 5 matches, spread floors, threshold 1.0. `usualMatches` shown in the note so a small sample is visible |
| The finding sounds like blame on a team loss | Medium | Medium | Loss falls back to "you did your part" when there's no shortfall; copy never scolds (voice rules) |
| Gold lead at 10 missing for older matches or odd role assignments | Low | Medium | Per-stat eligibility; the other stats still fill the meters |
| The fix is generic and repeats across matches | Medium: players tune it out | High | Accepted for 4c. Role variants for CS and vision. Richer, situational fixes (for example the Baron fight in the mockup) come with 4d's timeline data |
| Removing `trendBadge` breaks a consumer | Low | Low | Only `MatchesPage` reads it (checked); the API is internal |

## Timeline and Milestones
- [ ] **Phase 1**: Spec review; decide the open questions
- [ ] **Phase 2**: Backend: usual query, calculator, endpoint field, trend-badge removal, tests
- [ ] **Phase 3**: Frontend: `BaseUsualMeter`, copy util, card, removals, tests
- [ ] **Phase 4**: Visual pass and manual scenarios on the dev database
- [ ] **Phase 5**: Docs and design-system Step 5

## Open Questions
- [ ] Is the 20-match window right, or should it follow the page's queue filter (ranked only when the Ranked Solo/Duo tab is selected)? The proposal ignores the tab and uses all Summoner's Rift queues, so the usual is stable and big enough.
- [ ] Should a win with only shortfalls ("won despite") really lead with the shortfall, or say "nothing stood out" and just show the fix? The proposal leads with it, because it's the honest finding.
- [ ] Should the fix copy stay static per stat for 4c, as proposed, or wait until 4d's per-minute gold and objective events allow situational fixes?

## Handoff Checklist
Before implementation begins:
- [ ] Open questions answered
- [ ] Copy tables approved (finding and fix per stat)
- [ ] API contract reviewed (`decidingStat`, `trendBadge` removal)
- [ ] Security considerations addressed (no new endpoint; parameterized usual query)
- [ ] UI/UX approved against the artboards
- [ ] Implementation ready for assignment (full stack; `fullstack-developer` or backend then frontend)

## References
- `server/Mongoose.Api/Core/Services/TrendBadgeCalculator.cs` (the logic this replaces)
- `server/Mongoose.Api/Infrastructure/Database/Repositories/MatchesRepository.cs` (`GetRoleBaselinesAsync`, the `pc10` join)
- `server/Mongoose.Api/Application/Endpoints/Matches/MatchDetailsEndpoint.cs`
- `client/src/components/matches/WinPredictionStats.vue`, `MatchDetails.vue`, `MatchHeader.vue`
- `.claude/skills/mongoose-design/reference/components.md` (UsualMeter), `components.css` (`mp-usual`), `design-system.md` (voice, "show, then say", icon vocabulary)
- `.github/specs/design-migration.plan.md` Phase 4c
