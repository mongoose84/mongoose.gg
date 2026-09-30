# Feature: Solo trends

> Phase 5 of `design-migration.plan.md` (5a). Turns the Solo page into the "am I improving?" page: the headline and climb, one focus, six match-deciding stat trends, where your deaths cost you, win factors, LP per champion and three patterns. Target: the "Solo" row on the redesign canvas (https://claude.ai/artifact/Ufv95okAfSYgmnaJgniRhL), artboards `SoloVisual` (desktop) and `PhoneSoloVisual`. Metric choice follows `docs/win-prediction-metrics-research.md`. Decisions from 2026-09-29 are recorded in the plan and repeated here where they shape a rule.

## Problem Statement
The Solo page today is a stack of Chart.js cards (`AnalysisLayout`, `SummaryStatsCard`, six `TrendChartCard`s, a radar chart, a death heatmap and a match-activity calendar). Each card shows a metric, but none says whether the player is improving, what matters most for their wins, or what to do next. The time range counts days (7d … all), so a quiet week empties the charts, against the design system's "time ranges count matches" rule. The death heatmap shows where deaths happen (mostly in the player's own lane), not which deaths cost matches.

## Proposed Solution
- **Backend.** Four read endpoints under `/api/v2/solo/` (`climb`, `stat-trends`, `win-factors`, `death-zones`), all taking the same `queueType`, `range` and `accountId` parameters. Every rule (rolling averages, verdicts, focus pick, LP coverage mode, sessions, map regions, death classes) lives in Core services with unit tests. Sync stores more detail per death and the objective events from the timeline it already downloads.
- **Frontend.** `SoloStatsPage.vue` is rebuilt from design-system components. Each card loads on its own, with skeleton, error with retry, empty state and content. The frontend owns all copy, as it does for "What decided it".
- **Retired.** The old Solo components, the `Trends/*` endpoints, `RadarChartEndpoint` and the heatmap. Details under Technical Approach.

## User Stories
### Primary User Story
As a ranked player, I want to see whether I'm climbing and which of my habits are getting better or worse, so I know what to keep doing and what to work on.

### Additional User Stories
- As a player, I want one focus with evidence from my own matches and one fix, so I don't have to choose among ten charts.
- As a player, I want to know which stats decide my matches, so I practise what actually changes my results.
- As a player who dies a lot, I want to see which deaths cost my team objectives, so I know where and when to play safer.
- As a player, I want to see how my sessions, tilt and match length affect my win rate, so I know when to stop queueing.
- As a player with few ranked matches or thin LP data, I want the page to say what it can honestly say rather than show empty or invented numbers.

## Requirements

### Functional Requirements

**Scope**

1. **Matches in scope.** A match counts when all of these hold:
   - it belongs to the resolved account or accounts (`accountId`: primary by default, `all`, or a specific account, as on the other Solo endpoints);
   - it is in the Summoner's Rift set (queues 400, 420, 430, 440, 490);
   - it lasted at least `MinValidGameDurationSec` (remakes are excluded);
   - it matches the queue filter.
2. **Queue filter.** `queueType` is one of the following. The page defaults to Solo/Duo when the player has a Solo/Duo match this season, otherwise Flex, otherwise All queues.

   | Value | Label | Queues |
   |---|---|---|
   | `ranked_solo` | Solo/Duo | 420 |
   | `ranked_flex` | Flex | 440 |
   | `all` | All queues | 400, 420, 430, 440, 490 |

3. **Range.** Ranges count matches, never days. `range` is one of:
   - `last20`: the 20 most recent matches in scope, across seasons;
   - `last50`: the 50 most recent;
   - `season`: every match in scope in the current season (`matches.season_code`, same source as `QueryFilterBuilder`).

   Default `last20`. Matches are ordered by `game_start_time` ascending in every response ("oldest to newest").
4. **Minimums.** Minimum match counts are listed under each card below. When a card's minimum isn't met, the card shows its EmptyState. The page never shows a number computed from fewer matches than its rule allows.

**Headline**

5. **LP mode headline.** `{±n} LP over your last {n} matches` (Season: `{±n} LP this season`).
6. **Win-rate mode headline.** `{w} wins in your last {n}` (Season: `{w} wins in {n} matches this season`). With no wins: `No wins in your last {n}`.
7. **Second line.** Taken from `stat-trends`:
   - the Improving stat with the largest normalised change (change ÷ its Steady threshold, FR 13), phrased as `{Stat reason}: {now} {unit}, {up|down} from {was}.` (for example "Fewer deaths did most of it: 4.1 per match, down from 5.6.");
   - with no Improving stat, the Slipping stat with the largest normalised change (for example "Vision is slipping: 0.7 per minute, down from 0.9.");
   - with every stat Steady, or fewer than 20 matches, no second line.
8. **Rank line.** The rank line above the headline shows Riot ID · tier and division (tier colour dot) · LP · queue label, for a single ranked queue only.

**Climb (`climb` endpoint)**

9. **LP coverage mode.** One Core rule, `LpCoverageRule`, decides between two modes; the headline, the climb card and the champion card follow it:
   - **LP mode** when all of these hold: the queue is `ranked_solo` or `ranked_flex`, the scope is a single account, at least 80% of the ranked matches in range have a known `lpChange`, and at least 10 do.
   - **Win-rate mode** otherwise, which includes `all` queues and `accountId=all`.
10. **Ladder in LP mode.** Each match with a known `lp_after` gets a ladder value:
    - Iron IV = 0, with 100 LP per division through Diamond I;
    - Master, Grandmaster and Challenger share one count starting at the Master floor (same ladder as `LpChangeCalculator`).

    The line connects known points by match index; a match without LP is a gap in the data but keeps its x position.

    `netLp = ladder after the last known match − ladder before the first known match`, where "before" means `lp_after − lpChange` when the first match's change is known, and otherwise that match's `lp_after`.
11. **Events in LP mode.**
    - A promotion or demotion is recorded wherever the ladder crosses a division boundary, with the new tier and division.
    - The biggest drop is the most negative LP sum over a run of 2 or more consecutive losses (with known LP), reported at the run's last match. Leave it out when the drop is smaller than 40 LP.
    - The card title reads `From {start division} to {end division}` when they differ, `Holding {division}` otherwise.
12. **Win-rate mode.** The line is the 10-match rolling win rate (points from match 10 on). `was` = the first 10 matches in range, `now` = the last 10. The title:
    - `Win rate up from {was}% to {now}%`, or `down from … to …`;
    - `Win rate held at {now}%` when the change is under 3 points.

    Caption: `LP appears here as your ranked matches sync.` (only when the queue is ranked and the scope is a single account). Needs 20 matches, otherwise the card shows its EmptyState.

**Stat trends (`stat-trends` endpoint)**

13. **The six stats,** in this order (strongest win predictors first):

    | Key | Label | Per match | Better | Steady under | Needs |
    |---|---|---|---|---|---|
    | `deaths` | Deaths | `participants.deaths` | lower | 0.5 | always |
    | `goldLeadAt15` | Gold lead at 15 | `participant_checkpoints.gold_diff_vs_lane` at minute 15 | higher | 150 | match ≥ 15 min, lane opponent exists |
    | `dragonParticipation` | Dragon participation | `dragons_participated ÷` team `dragons_taken`, as % | higher | 5 points | team took ≥ 1 dragon |
    | `visionPerMin` | Vision | `participant_metrics.vision_per_min` | higher | 0.1 | always |
    | `csPerMin` | CS | `creep_score ÷ (game_duration_sec / 60)` | higher | 0.3 | role is not `UTILITY` |
    | `killParticipation` | Kill participation | `participant_metrics.kill_participation_pct` | higher | 3 points | team kills ≥ 5 |

    A match where a stat's "Needs" rule fails contributes nothing to that stat. Its values are never counted as 0.
14. **Series.**
    - For each stat: the per-match values (in order, `null` where excluded) and the 10-match rolling average over non-null values (a point once 10 values exist).
    - `was` = the average of the first 10 non-null values in range, `now` = the average of the last 10.
    - With more than 100 matches (Season), the server sends the rolling line sampled to 100 points and no per-match dots.
15. **Verdict.** Compare `now − was` with the stat's Steady threshold. For "lower is better" stats the sign is flipped before comparing.
    - **Improving:** the change is at least the threshold, in the good direction.
    - **Slipping:** the change is at least the threshold, in the bad direction.
    - **Steady:** the change is under the threshold.
    - **No verdict:** fewer than 20 non-null values. The tile shows the current average and "Needs 20 matches".

    The arrow shows the direction of the number and the colour and word show the meaning (decision 4). For example, fewer deaths shows ▼ in purple with "Improving".
16. **Benchmark.** Each stat carries a benchmark, labelled on the dashed line: the player's own season average in the same queue scope (`benchmark.kind = "season"`, label "Your season average"). A rank average (`kind = "rank"`, `tier`, label "Emerald average") replaces it for the whole card (5g) when:
    - the scope is one ranked queue of one account, the player's latest match this season carries a tier, and one role holds at least 70% of the range's matches (`RankBenchmarkRule.Target`);
    - the pool, other Mongoose.gg players' (linked accounts) matches this season at that tier after the match, in that role and queue, without the player's own, has at least 200 matches from at least 20 players (`RankBenchmarkRule.Qualifies`; the newest 2,000 are read, cached for an hour per queue, tier and role).

    The season benchmark is `null` when the season has fewer than 20 non-null values for that stat; a rank benchmark is `null` when fewer than 100 pool matches have the stat.
17. **Card title.** Uses the stats that have a verdict:
    - `{i} of {m} match-deciding stats improved` when at least one improved;
    - `{s} of {m} match-deciding stats slipped` when none improved and some slipped;
    - `Your match-deciding stats held steady` when all are Steady.

    With no verdicts at all, the caption becomes the title.

**Your focus (in the `stat-trends` response)**

18. **Picking the focus stat.** `SoloFocusPicker` needs 20 matches in range and at least one win-factor row (FR 22). Candidates are the stats with a shown win-factor row.
    1. Among Slipping candidates, pick the one with the largest hit/miss gap.
    2. With none Slipping, pick the candidate with the lowest hit rate among the three largest gaps.
    3. Ties are broken by the FR 13 order.
    4. With no candidate, `focus = null` and the card is left out.
19. **What the focus carries.**
    - the stat, its `now` and `was`;
    - the mark (FR 21) with its label;
    - the hit and miss win rates;
    - the last 20 matches as `hit | miss | null` (null where the stat doesn't apply), oldest first, and the hit count.
20. **Focus card copy.** Owned by the frontend.
    - Finding: `{Stat} is the one stat slipping` when Slipping, otherwise `{Stat} is your biggest lever`.
    - Evidence: `{Down|Up} from {was} to {now} {unit}. You win {hit}% of matches at {mark}, and {miss}% below it.` Where "below it" reads wrong, the factor names its miss: "when you're behind" (ahead at 15), "with more" (deaths), "with fewer" (dragons).
    - Fix: one per stat (FR 29).
    - There is no goal button until Goals ship inside Advanced (decision 2).

**Win factors (`win-factors` endpoint)**

21. **Marks.** A match hits or misses a mark depending on its role:

    | Factor key | Label | Hit when | Applies |
    |---|---|---|---|
    | `aheadAt15` | Ahead at 15 minutes | `gold_diff_vs_lane` at 15 > 0 | match ≥ 15 min, lane opponent exists |
    | `lowDeaths` | 4 or fewer deaths | deaths ≤ 4 | always |
    | `dragons` | In on 2 or more dragons | `dragons_participated` ≥ 2 | match ≥ 20 min |
    | `vision` | {mark} vision per minute | ≥ 0.9, or ≥ 2.0 for `UTILITY` | always |
    | `cs` | 7+ CS per minute | ≥ 7.0, or ≥ 5.5 for `JUNGLE` | role is not `UTILITY` |

    The vision label shows the role's mark when the scope is a single role, and "vision mark for your role" otherwise.
22. **Rows.** Each row has the hit and miss win rates, the counts on each side and `gap = hit − miss` in points.
    - A row shows only with at least 5 matches on each side.
    - Rows are sorted by gap, largest first.
    - Negative gaps are shown as they are, never hidden.
23. **Card title.** `Your {factor phrase} decides your matches most`, using the top row. Phrases: "gold at 15", "deaths", "dragon presence", "vision", "farming". Needs 20 matches and at least 2 rows, otherwise the card shows its EmptyState.

**LP per champion (in the `climb` response)**

24. **Rows.** One row per champion with at least 3 matches in range, showing matches, win rate and the value. At most 5 rows, sorted by value, largest first.
    - **LP mode:** the value is the sum of known `lpChange`.
    - **Win-rate mode:** the value is net wins (wins − losses).

    A footnote names the champions left out for having under 3 matches (up to three names, then "and n more").
25. **Card title.**
    - LP mode: `{Champion} earned most of your climb` when the top value is positive, otherwise `{Champion} cost you the most LP` (the lowest).
    - Win-rate mode: `{Champion} won you the most matches` / `{Champion} cost you the most matches`.

    Caption: `LP won or lost per champion` / `Wins minus losses per champion`.

**Patterns (in the `win-factors` response)**

26. **Session.** A session is a run of matches where each next match starts less than 30 minutes after the previous one ended (`game_start_time + game_duration_sec`).
    - Groups by position in the session: 1st, 2nd, 3rd, 4th and later.
    - A group needs 3 matches, and the chart needs 2 groups.
    - A group is the weak spot when it is the single lowest group and at least 15 points below the other groups taken together (same rule as `START_TIME_WEAK_GAP`).
    - Titles: `You drop off after your {ordinal before the weak group} match` (Pattern chip); with no weak spot, `Your win rate holds through a session` (Strength chip).
27. **After a loss.** Pairs of consecutive matches in the same session, grouped by the first match's result: "After a win", "After a loss". Each group needs 5 pairs.
    - When after a loss is 10 or more points below after a win: `Losses carry into your next match` (Pattern).
    - Otherwise: `A loss doesn't tilt you` (Strength).
28. **Match length.** Groups: under 25 min, 25 to 35 min, over 35 min, with the same 3-match and weak-spot rules as FR 26.
    - Titles: `Long matches slip away from you` / `Short matches get away from you` (Trend chip); with no weak spot, `You win at every match length` (Strength).
    - Each pattern card shows only when its rules are met; the "Your patterns" section is hidden when none are.

**Copy and fixes**

29. **Fix lines,** frontend, reusing `utils/decidingStat.js` wording where it fits:

    | Stat | Fix |
    |---|---|
    | deaths | Back off when you can't see their jungler. |
    | goldLeadAt15 | Trade when your wave is pushing into them, not before. |
    | dragonParticipation | Move to the river 30 seconds before dragon spawns. |
    | visionPerMin | Buy a control ward on every back. (Support: Place a control ward before every dragon.) |
    | csPerMin | Last-hit every cannon minion. (Jungle: Finish your full first clear before the first gank.) |

30. **Number formats.**
    - Per-minute stats: one decimal. Percentages and win rates: whole numbers.
    - Gold and LP: signed with a real minus, with thousands separators.
    - Match counts are written as matches, never games.

**Death zones (`death-zones` endpoint)**

31. **Data readiness.** A death counts when its event has `timestamp_sec` and `allies_nearby` (changed in 5f: `killer_participant_id` is not required, because executions by towers and minions have no killer and still are deaths). The card needs 30 such deaths in range.
    - While the backfill (FR 38) is running for the account, the card shows the backfill progress in its slot (FR 39).
    - When the backfill is done or not needed and there are still fewer than 30 deaths, it shows the EmptyState "Play a few more matches to see where your deaths cost you" with "Sync matches".
32. **Mirroring.** Positions are normalised to `u = x / 14870` and `v = y / 14870`. For deaths on the red team (`participants.team_id = 200`), `(u, v)` becomes `(1 − u, 1 − v)`, so the player's base is always bottom left.
33. **Regions.** Each death falls into exactly one region, tested in this order (`d_mid = |u − v|`, `d_river = |u + v − 1|`, "your half" = `u + v < 1`):

    | Region key | Label | Rule |
    |---|---|---|
    | `dragonPit` | Dragon pit | within 0.06 of (0.66, 0.30) |
    | `baronPit` | Baron pit | within 0.06 of (0.34, 0.70) |
    | `yourBase` | Your base | u < 0.18 and v < 0.18 |
    | `enemyBase` | Enemy base | u > 0.82 and v > 0.82 |
    | `topLaneYours` / `topLaneEnemy` | Top lane, your half / enemy half | u < 0.12 or v > 0.88 |
    | `botLaneYours` / `botLaneEnemy` | Bot lane, your half / enemy half | v < 0.12 or u > 0.88 |
    | `midLaneYours` / `midLaneEnemy` | Mid lane, your half / enemy half | d_mid < 0.06 |
    | `riverTop` / `riverBot` | Top river / Bot river | d_river < 0.07; top when v > u |
    | `jungleYoursTop` / `jungleYoursBot` | Your jungle, top side / bot side | your half; top when v > u |
    | `jungleEnemyTop` / `jungleEnemyBot` | Enemy jungle, top side / bot side | enemy half; top when v > u |

    Each region has a fixed anchor point for its circle on the map (defined next to the rules in Core, returned in the response). Anchors don't move with the data, so the map stays stable between visits.
34. **Phase.** early before 14:00, mid 14:00–24:59, late 25:00 on (`timestamp_sec`).
35. **How.** Exclusive, checked in this order:
    1. **In a teamfight:** the killer plus assisters number 3 or more, and `allies_nearby` ≥ 2.
    2. **Ganked in lane:** before 14:00, the region is the lane of the player's role (either half), and the killer or an assister is not the lane opponent.
    3. **Caught alone:** `allies_nearby = 0`.
    4. **Lane fights and skirmishes:** everything else.
36. **Cost.** A death cost an objective when the enemy team took a dragon, Baron, Herald or tower within 60 seconds after it. Each death counts once, attributed to the first such objective. Void Grubs and inhibitors don't count.
37. **Zones, title and breakdowns.**
    - **Zones:** the regions with at least 5 deaths, at most 5, sorted by lost objectives (then deaths). Each carries deaths, lost objectives, `costly = lostObjectives ÷ deaths ≥ 0.30`, its anchor, and a timing note: the phase holding the most of its deaths when that is 60% or more, as "{n} of them after 25 minutes" / "Mostly before 14 minutes" / "Mostly between 14 and 25 minutes"; otherwise "Spread over the match".
    - **Title:** `Deaths in {zone} cost you the most objectives` when the top zone lost 3 or more; otherwise `You die most in {zone with most deaths}`. Caption: `{n} deaths over your last {n} matches. Red-side matches are mirrored, so your base is always bottom left.`
    - **Breakdowns:** phase, how and cost (by objective type), for all deaths in range and for each listed zone, so the zone list can filter them without another request. Pressing a zone row filters and marks it `aria-pressed`; pressing it again returns to all zones. The default is all zones.

**Death detail backfill** (decided 2026-09-29: backfill)

38. **Backfill job.** `DeathDetailBackfillJob` re-fetches the match-v5 timeline for older matches and fills the new death columns and `match_objective_events`.
    - **Scope:** the last 50 Summoner's Rift matches of each account whose user was active in the last 7 days (the same "active" rule as `RankSnapshotJob`) that have death events without `timestamp_sec`, or no objective events.
    - **Order:** newest match first, one account at a time. Accounts are ordered by the user's last activity, so someone looking at the page right now goes first; opening the Solo page moves that account to the front.
    - **Priority:** the lowest. It takes a Riot request only when no user sync is queued or running and the rank-snapshot job is idle. It goes through `RiotLimitHandler` like every Riot call and never bypasses it.
    - **Budget:** at most 20 timeline requests per 2 minutes (of the 50 the handler allows), so a sync started meanwhile always finds tokens.
    - **Resumable:** a match counts as done once its events are written, so the job can stop and resume at any point.
    - **Failures:**
      - A `404` from Riot (match no longer served) marks the match as skipped (`participant_death_events` stay null, and it is recorded so it isn't retried).
      - A `429` or a timeout backs off using the handler's retry rules.
    - **Account state:** `riot_accounts.death_detail_backfilled_at` (UTC) is set when an account's 50 matches are done or skipped; new matches get the detail at sync and never need the backfill.
    - Controlled by `Jobs:EnableDeathDetailBackfill`, off in tests.
39. **Progress and waiting on Riot.** The `death-zones` response carries `backfill`:
    - `{ "status": "running", "done": 12, "total": 50 }`: the card shows the design system's SyncProgress in its slot: "Adding detail to your older matches · 12 of 50", with the line "Your death map appears when this finishes. You can keep using the rest of the page."
    - `{ "status": "waiting", "done": 12, "total": 50, "retryAt": "…Z" }` when the job is blocked on the Riot limit: the bar stays at 12 of 50 and the line becomes "Waiting on Riot's servers. We'll continue automatically." No error styling, since this is not a failure.
    - `{ "status": "queued" }` while other syncs run first: "Queued behind your match sync."
    - `null` when done or not needed.

    Updates arrive over the existing sync WebSocket as a user-scoped `detail_backfill_progress` message with the same fields, sent at most every 5 matches and on every status change. Without a socket the card refetches every 30 seconds while `backfill` is not null. When the backfill finishes, the card refetches once and shows the zones. The same "Waiting on Riot's servers" line replaces the temporary `sync_rate_limited` handling in SyncProgress, so both read the same.
40. **Rate-limit signal.** `RiotLimitHandler` exposes when a caller has been waiting for a token longer than 2 seconds, and until when (the bucket's next refill), through a small read-only interface in Core (`IRiotThrottleState`). Jobs use it to report `waiting` with `retryAt`; nothing else changes in how requests are throttled.

### Non-Functional Requirements
- **Performance.** Each endpoint answers in under 400 ms p95 for a Season range of 300 matches on the dev database. One SQL round trip per data set where possible; no per-match queries. The four cards load in parallel.
- **Security.**
  - Every endpoint uses `AuthorizationHelper.ValidateAndGetUser` and `PuuidResolutionService.ResolveRequestedAccountsAsync`; no PUUID is accepted from the client.
  - Parameterized SQL only.
  - Every user value that is logged goes through `LogSanitizer`, with PUUIDs hashed.
  - Responses never include PUUIDs or internal IDs (match IDs only where the UI links to a match).
- **Accessibility.**
  - Every chart is `role="img"` with an `aria-label` that states the range and the change.
  - Trend tiles are groups labelled with value, was, benchmark and verdict.
  - Win-factor rows and zone circles are described in words.
  - The zone list uses real buttons with `aria-pressed`.
  - The focus strip lists hit and miss per match.
  - Reduced motion: no entrances.
  - `rem` type and spacing, 44px targets.
- **Compatibility.** Desktop-first. Below 900px everything stacks and trend tiles become rows, as on `PhoneSoloVisual`.

## Technical Approach

### Backend Changes
**Language**: C#
**Components**:
- [x] Core rules, all pure and unit-tested:
  - `Core/Services/Solo/LpCoverageRule.cs`
  - `Core/Services/Solo/LpLadder.cs` (shared with `LpChangeCalculator`: extract, don't duplicate)
  - `Core/Services/Solo/StatTrendCalculator.cs` (series, was/now, verdict)
  - `Core/Services/Solo/SoloFocusPicker.cs`
  - `Core/Services/Solo/WinFactorCalculator.cs`
  - `Core/Services/Solo/SessionGrouper.cs`
  - `Core/Services/Solo/PatternCalculator.cs`
  - `Core/Services/Solo/MapRegions.cs`
  - `Core/Services/Solo/DeathClassifier.cs`
- [x] Query models: `Core/QueryModels/SoloTrendQueryModels.cs` (per-match rows: result, duration, start time, role, champion, LP fields, the six stat inputs; death rows; objective rows).
- [x] Repositories: `ISoloTrendsRepository` (Core/Interfaces) + `Infrastructure/Database/Repositories/SoloTrendsRepository.cs` (one query for match rows in range, one for deaths with their objective windows).
- [x] Endpoints: `Application/Endpoints/Solo/SoloClimbEndpoint.cs`, `SoloStatTrendsEndpoint.cs`, `SoloWinFactorsEndpoint.cs`, `SoloDeathZonesEndpoint.cs`; DTOs in `Application/DTOs/Solo/SoloTrendsDto.cs`. Shared parameter parsing (`queueType`, `range`) in one helper; unknown values return `400` with `INVALID_QUEUE` / `INVALID_RANGE`.
- [x] Sync: `RiotTimelineMapper.ExtractDeathPositions` also returns the death time in seconds, the killer and assisting participant IDs, and allies within 2,000 units of the death position in the participant frame closest in time. A new extractor returns objective events (`ELITE_MONSTER_KILL`, `BUILDING_KILL`). Persist both in `MatchDataPersistenceService`.
- [x] Retire when the page no longer calls them:
  - `Endpoints/Trends/*` (six endpoints) and `RadarChartEndpoint`;
  - `DeathPositionsEndpoint` / `IDeathPositionsRepository`, replaced by `death-zones`;
  - their tests, and their entries in `architecture.spec.md`.

  `SoloPerformanceEndpoint` stays until nothing else calls it (check Overview and Champion Select).

### Frontend Changes
**Framework**: Vue

**Components**:
- [x] Page: `client/src/views/SoloStatsPage.vue` rebuilt. Headline, rank line and the two SegmentedControls (queue, range) at the top, and the cards in the order of the artboards. `AnalysisLayout` is no longer used here.
- [x] Base components (design-system patterns):
  - `components/base/BaseTrendTile.vue` (sparkline with dots, benchmark line, verdict)
  - `BaseWinFactorRow.vue` (hit vs missed dots)
  - `BaseGoalStrip.vue` (hit/miss cells)
  - `BaseDeathMap.vue` (outline, zone circles)
  - `BaseDivergingBar.vue` (LaneBar shape without icons, for LP per champion)
  - Reuse `BaseColumnChart`, `BaseSegmentedControl`, `BaseFormStrip` where they fit.
- [x] Feature components in `components/solo/`: `SoloClimbCard.vue`, `SoloFocusCard.vue`, `SoloStatTrends.vue`, `SoloDeathZones.vue`, `SoloWinFactors.vue`, `SoloChampionLp.vue`, `SoloPatterns.vue`.
- [x] Copy and small rules: `client/src/utils/soloSummary.js` (headline, titles, verdict words, fixes, number formats with a real minus).
- [x] API: `getSoloClimb`, `getSoloStatTrends`, `getSoloWinFactors`, `getSoloDeathZones` in `client/src/services/soloApi.js`. Rewrite `composables/useSoloDashboardData.js` to load the four in parallel with their own loading and error state, and refetch on queue, range or account change and after sync (`useSyncWebSocket`).
- [x] Retire: `SummaryStatsCard`, `TrendChartCard`, `TrendLineChart`, `WinrateChart`, `DeathsChart`, `DragonParticipationChart`, `VisionChart`, `GoldAt15Chart`, `CsPerMinuteChart`, `RadarChart`, `DangerZonesMap`, and `useChartDisplayMode` if unused elsewhere, with their unit tests. `MatchActivityHeatmap` leaves this page only.
- [ ] Styles: port the needed `mp-*` classes to `client/src/style.css` when they are added to the design system (Step 5). No hard-coded colours.

### Database Changes
**Database**: MySQL

**Schema Changes** (migration `004_SoloTrendsDeathDetail.sql`):
- [x] Modified `participants`: `riot_participant_id TINYINT NULL` (Riot's 1–10), filled at sync, so timeline participant IDs map to rows.
- [x] Modified `participant_death_events`:
  - `timestamp_sec INT NULL`
  - `killer_participant_id TINYINT NULL`
  - `assisting_participant_ids VARCHAR(40) NULL` (comma-separated 1–10)
  - `allies_nearby TINYINT NULL`

  All null for events synced before the migration.
- [x] New `match_objective_events`:
  - `id` BIGINT UNSIGNED PK
  - `match_id` VARCHAR(50) FK → `matches` ON DELETE CASCADE
  - `team_id` INT
  - `type` ENUM('dragon','baron','herald','grubs','tower','inhibitor')
  - `subtype` VARCHAR(30) NULL (dragon kind, tower lane)
  - `timestamp_sec` INT
  - `killer_participant_id` TINYINT NULL
  - index `(match_id, timestamp_sec)`

  Shared with 4d's moment markers.
- [x] New index: `participant_death_events (participant_id, timestamp_sec)`.
- [x] Modified `riot_accounts`: `death_detail_backfilled_at DATETIME NULL` (UTC).
- [x] New `death_detail_backfill_skips`: `match_id` VARCHAR(50) PK, `reason` VARCHAR(30), `skipped_at` DATETIME (UTC). Matches Riot no longer serves, so they aren't retried.
- [x] Data migrations: none in SQL. Raw timelines are not stored; `DeathDetailBackfillJob` (FR 38) re-fetches them for the last 50 Summoner's Rift matches of active accounts.
- [x] Update `database-schema.spec.md`: add `participant_death_events` (currently missing), the new columns, and `match_objective_events`.

### API Contracts
All four take `?queueType=ranked_solo|ranked_flex|all&range=last20|last50|season&accountId=` and return `200` with a `matches` count, even when it is 0, so the page can show EmptyStates.

Errors:
- `400` with `INVALID_QUEUE` / `INVALID_RANGE`;
- `401` / `403` from the auth helper;
- `404` with `RIOT_ACCOUNT_NOT_FOUND` when no account is linked.

#### Climb
```
GET /api/v2/solo/climb/{userId}
```
```json
{
  "matches": 50,
  "queueType": "ranked_solo",
  "range": "last50",
  "mode": "lp",
  "wins": 28,
  "losses": 22,
  "lp": {
    "net": 148,
    "start": { "tier": "EMERALD", "division": "III", "lp": 10 },
    "end": { "tier": "EMERALD", "division": "II", "lp": 58 },
    "points": [ { "index": 0, "ladder": 2131 }, { "index": 1, "ladder": 2152 } ],
    "events": [ { "index": 31, "kind": "promotion", "tier": "EMERALD", "division": "II" } ],
    "biggestDrop": { "index": 17, "lp": -80, "losses": 4 }
  },
  "winRate": null,
  "champions": [
    { "championId": 103, "championName": "Ahri", "matches": 22, "wins": 14, "value": 134 }
  ],
  "championsLeftOut": ["Orianna"],
  "rank": { "tier": "EMERALD", "division": "II", "lp": 58 }
}
```
In win-rate mode `lp` is null and `winRate` is `{ "was": 52, "now": 58, "points": [ { "index": 9, "rate": 50 } ] }` (null under 20 matches); `champions[].value` is net wins. `rank` is the rank after the latest match that has one, for the rank line (FR 8); null unless the scope is one ranked queue of one account. `division` is null from Master up. `championsLeftOut` lists every name, most played first; the page shows three.

#### Stat trends
```
GET /api/v2/solo/stat-trends/{userId}
```
```json
{
  "matches": 50,
  "stats": [
    {
      "key": "deaths",
      "values": [6, 4, null],
      "rolling": [ { "index": 9, "value": 5.6 } ],
      "was": 5.6,
      "now": 4.1,
      "count": 50,
      "verdict": "improving",
      "benchmark": { "kind": "season", "value": 4.9, "tier": null }
    }
  ],
  "focus": {
    "stat": "visionPerMin",
    "factor": "vision",
    "mark": 0.9,
    "was": 0.9,
    "now": 0.7,
    "hitWinRate": 63,
    "missWinRate": 44,
    "last20": ["hit", "miss", null],
    "hits": 7
  }
}
```
`values` is omitted (null) when sampled (Season over 100 matches). `verdict` is `improving | slipping | steady | null`.

#### Win factors
```
GET /api/v2/solo/win-factors/{userId}
```
```json
{
  "matches": 50,
  "factors": [
    { "key": "aheadAt15", "hitWinRate": 71, "missWinRate": 34, "hitMatches": 28, "missMatches": 22, "gap": 37 }
  ],
  "patterns": {
    "session": { "groups": [ { "key": "1", "matches": 18, "winRate": 61 } ], "weak": "4plus" },
    "afterLoss": { "afterWin": { "pairs": 20, "winRate": 57 }, "afterLoss": { "pairs": 16, "winRate": 55 } },
    "length": { "groups": [ { "key": "under25", "matches": 14, "winRate": 68 } ], "weak": "over35" }
  }
}
```
A pattern is null when its rules aren't met; `weak` is null without a weak spot.

#### Death zones
```
GET /api/v2/solo/death-zones/{userId}
```
```json
{
  "matches": 50,
  "deaths": 236,
  "ready": true,
  "zones": [
    { "key": "jungleEnemyBot", "deaths": 38, "lostObjectives": 14, "costly": true,
      "anchor": { "u": 0.74, "v": 0.39 }, "timing": { "phase": "late", "count": 27 } }
  ],
  "breakdowns": {
    "all": {
      "phase": { "early": 88, "mid": 84, "late": 64 },
      "how": { "ganked": 71, "alone": 97, "teamfight": 68, "other": 0 },
      "cost": { "dragon": 26, "tower": 23, "baron": 9, "herald": 0 }
    },
    "byZone": { "jungleEnemyBot": { "phase": {}, "how": {}, "cost": {} } }
  }
}
```
`ready` is false below 30 counted deaths (FR 31); `timing.phase` is null for "Spread over the match". The response also carries `"backfill": null` or `{ "status": "running" | "waiting" | "queued", "done": 12, "total": 50, "retryAt": "2026-10-01T18:04:00Z" }` (FR 39). The map draws anchors with `v` up, so the frontend flips `v` for SVG.

## UI/UX Requirements

All views follow the Mongoose.gg design system ([UI/UX Spec §2](../ui-ux.spec.md#2-visual-design-system), built with `/mongoose-design`). Design-system tokens only; copy in the Mongoose.gg voice.

### Solo page (`/app/solo`)

**Layout**: The standard app header, then the page content with 56px gutters (16px on phones). The order and sizes follow `SoloVisual`; below 900px it follows `PhoneSoloVisual`.

**Structure**:
```
┌ rank line ─────────────────────────────────────────────┐
│ H1 headline                         [Solo/Duo|Flex|All] │
│ second line                         [Last 20|50|Season] │
├───────────────────────────────────────┬────────────────┤
│ Climb card (LP or win-rate line)      │ Your focus     │
│ title · win rate · LP/match · W–L     │ (highlight)    │
├───────────────────────────────────────┴────────────────┤
│ n of 6 match-deciding stats improved         key       │
│ [tile] [tile] [tile]                                   │
│ [tile] [tile] [tile]                                   │
├────────────────────────────────────────────────────────┤
│ Deaths in … cost you the most objectives               │
│ [map 340] [zone list 340] [phase / how / cost bars]    │
├──────────────────────────────┬─────────────────────────┤
│ Win factors (dumbbell rows)  │ LP per champion         │
├──────────────────────────────┴─────────────────────────┤
│ Your patterns: [session] [after a loss] [length]       │
└────────────────────────────────────────────────────────┘
```

**Components**:
- Headline: one `h1` in the `headline` style; rank line in `ink-soft` with the tier colour dot.
- Filters: two `BaseSegmentedControl`s (`aria-label` "Queue", "Range").
- Climb: `mp-card` with a line chart per the design system's chart rules.
- Focus: `mp-card mp-card--highlight`, the page's only highlight (decision 5): eyebrow "Your focus" with `target`, finding, evidence, `BaseGoalStrip`, the "Next match" fix row.
- Stat trends: `mp-card` with six `BaseTrendTile`s on `surface-raised` (3 columns; rows on phones), and a key (Your average, One match, benchmark label).
- Death zones: `mp-card`, `BaseDeathMap`, zone list buttons, bar groups. Always visible (decision 3).
- Win factors: `mp-card` with `BaseWinFactorRow`s and the two-dot key.
- LP per champion: `mp-card` with 36px square icons and `BaseDivergingBar`s.
- Patterns: three InsightCard-style cards with chips (Strength, Pattern, Trend) and `BaseColumnChart`.

**Behavior**:
- Each card loads on its own:
  - a skeleton after 300ms;
  - an inline error with "Try again" that refetches only that card;
  - an EmptyState in the card's slot;
  - the content.
- Changing queue, range or account keeps the old content until the new data arrives (no skeleton flash under 300ms). Nothing animates on a filter change.
- EmptyStates:
  - No linked account: "Link your Riot account to see your trends" → "Link Riot account".
  - No matches in scope: "No {queue} matches yet" → "Sync matches" (or "Show all queues" when the queue filter causes it).
  - Below a card's minimum: "Play {n} more matches to see {what}" → "Sync matches".
- Sections fade in once on first view (`useRevealOnView`). Sparklines and charts draw instantly.

**Accessibility**: As in the non-functional requirements. The zone list is keyboard reachable in order, and filtering announces the zone name politely (`aria-live="polite"` on the breakdown heading).

## Testing Strategy

### Unit Tests
**Frameworks**: xUnit (backend), Vitest (frontend)

- [x] `LpCoverageRule`: 80% and 10-match boundaries, `all` queue, multi-account.
- [x] `LpLadder`: division crossings, Master+ shared count, demotion, net LP with an unknown first change (in `LpLadderTests` and `ClimbCalculatorTests`).
- [x] `StatTrendCalculator`: nulls skipped, was/now on non-null values, verdicts at thresholds, "lower is better", fewer than 20 values, sampling above 100.
- [x] `SoloFocusPicker`: Slipping preferred, the lowest-hit fallback, ties, no candidate, under 20 matches.
- [x] `WinFactorCalculator`: role marks (support vision, jungle CS), the 5-per-side minimum, sorting, negative gaps.
- [x] `SessionGrouper` / `PatternCalculator`: the 30-minute gap using end time, same-session pairs, weak-spot rule, minimums.
- [x] `MapRegions`: each region's rule and precedence (pits before river, bases before lanes), red-side mirroring.
- [x] `DeathClassifier`: the order of classes, lane opponent vs ganker, allies thresholds, cost window at 60s exactly, first objective only, grubs and inhibitors ignored.
- [x] `RiotTimelineMapper`: new death fields and objective events from a fixture timeline.
- [x] `DeathDetailBackfillJob`:
  - order (newest first, most recently active account first, the Solo page bump);
  - yields while a user sync is queued or running;
  - budget cap of 20 per 2 minutes;
  - resume after a stop;
  - `404` → skip, `429` → back off;
  - `death_detail_backfilled_at` set at the end;
  - progress messages throttled to every 5 matches, plus every status change.
- [x] `IRiotThrottleState`: reports waiting after 2 seconds with the next refill time, and clears when a token is granted.
- [x] Frontend: the death-zones card's running / waiting / queued states, WebSocket updates and the 30-second polling fallback; the SyncProgress "Waiting on Riot's servers" line replaces `isRateLimited`'s temporary copy.
- [x] Frontend:
  - `soloSummary.js` copy (headline modes, titles, verdict words, fixes, formats with a real minus);
  - each base component (rendering, `aria-label`, `aria-pressed`, the `v` flip in `BaseDeathMap`);
  - each card's four states;
  - `SoloStatsPage` wiring (parallel loads, a per-card retry, filter changes).

### Integration Tests
- [x] Each endpoint: ownership (another user's `userId` → 403), `accountId` scopes, invalid `queueType` / `range` → 400, `matches: 0` → 200.
- [ ] Range counting across a season boundary (`last20` spans seasons, `season` doesn't).
- [x] Death zones with pre-migration events (nulls excluded, `ready` false).
- [ ] Repository queries run as parameterized SQL (opt-in DB tests, as the existing ones).

### Manual Testing Scenarios
1. An account with 50+ Solo/Duo matches and good LP coverage: LP mode, promotions drawn, headline LP matches the rank change.
2. Flex with thin LP: win-rate mode, caption shown, champion card in net wins.
3. `accountId=all`: win-rate mode, no rank line.
4. A new account with 8 matches: every card shows its EmptyState with the right count.
5. Season range with 150+ matches: the trend lines are sampled, no dots, response under 400 ms.
6. Death zones on an account synced before the migration: the backfill progress shows in the card (12 of 50…), and the zones appear when it finishes. Start a match sync meanwhile: the backfill switches to "Queued behind your match sync" and resumes afterwards. Force the limit (a low test budget): the line switches to "Waiting on Riot's servers" and continues on its own.
7. Phone width 390px: everything stacks as on `PhoneSoloVisual`; the tab bar doesn't cover the last card.

## Validation Criteria
Feature is considered complete when:
- [ ] All functional requirements are implemented across 5b–5f.
- [ ] Unit and integration tests pass; the frontend build is green.
- [ ] `architecture.spec.md`, `database-schema.spec.md` and `ui-ux.spec.md` (Solo section and §10 component list) are updated.
- [x] The new patterns are in the design system, `reference/` and the canvas "Current" row (Step 5).
- [x] Code review and a security review of the new endpoints are done (2026-09-29: ownership, account resolution and parameterized SQL shared by all four endpoints; backfill progress only reaches the account's linked users; fixed a `backfill` that stayed "queued" after the job had finished the account).
- [ ] A visual pass at 1440 / 1024 / 390px is done with a real account. (2026-09-30: migration 004 applied on dev and the page checked there by the user; the three widths not confirmed separately.)

## Dependencies
### Internal Dependencies
- [ ] Phase 4b LP change (`lpChange`, `lp_after`) and the rank-snapshot job (coverage decides how often LP mode shows).
- [ ] Phase 4d's objective events: built here once and reused there.
- [x] Design-system additions: TrendTile, win-factor row, goal strip, DeathMap, zone list.

### External Dependencies
- [ ] Riot match-v5 timeline (already fetched at sync). The backfill spends up to 20 of the 50 requests per 2 minutes, only while nothing else needs them.

## Risks and Mitigations
| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| LP coverage stays low | Medium | Medium | Win-rate mode everywhere (decision 1); the rank-snapshot result tells how often |
| Hit/miss win rates read as cause and effect | Medium | Medium | Titles say "decides your matches" only for the top row; captions state "win rate when you hit each mark"; minimum 5 per side |
| Per-minute frames make "allies nearby" inaccurate | Medium | High | Used only for broad classes; copy says "caught alone", never exact distances; revisit if players report wrong calls |
| Region rules misplace deaths near borders | Low | Medium | Fixed regions with tested precedence; anchors, not raw points, on the map |
| Older matches have no death detail | Medium | High | `DeathDetailBackfillJob` with visible progress; `ready` threshold |
| Backfill starves user syncs or the rank-snapshot job | High | Medium | Lowest priority, yields while a sync is queued, cap of 20 of 50 requests per 2 minutes, all through `RiotLimitHandler` |
| The limiter doesn't throttle as gracefully as intended | Medium | Medium | `429` backs off using the handler's retry rules; "waiting" is shown, never an error; manual scenario 6 forces the limit before release |
| Six stats × Season range is heavy | Low | Low | One query per endpoint, server-side sampling, index on death events |

## Timeline and Milestones
- [x] **5b**: page shell, ranges, stat trends, win factors, patterns.
- [x] **5c**: climb and LP per champion (LP / win-rate modes).
- [x] **5d**: focus card.
- [x] **5e**: death detail and objective events at sync (migration 004), `DeathDetailBackfillJob` and the throttle signal.
- [x] **5f**: death zones card; the heatmap is retired.
- [x] **5g**: rank-average benchmark (optional).

## Open Questions
- [x] Backfill older matches? Yes (2026-09-29): through the existing limiter, with the progress and "waiting on Riot" state shown in the card (FR 38–40).

## Handoff Checklist
Before implementation begins:
- [x] Page design proposed and decisions recorded (2026-09-29)
- [ ] Rules in this spec reviewed
- [ ] Database changes reviewed (migration 004)
- [ ] API contracts reviewed
- [x] Security considerations addressed (ownership, no PUUID input, sanitized logging)
- [x] Open question answered (backfill)

## References
- `design-migration.plan.md`, Phase 5
- `docs/win-prediction-metrics-research.md`
- Canvas: https://claude.ai/artifact/Ufv95okAfSYgmnaJgniRhL (Solo row)
- `features/what-decided-it.spec.md` (usual and fix copy conventions)
- `features/rank-snapshots.spec.md` (LP coverage)
