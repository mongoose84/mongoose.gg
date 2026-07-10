# Feature: AI Goals

> **Purpose**: Feature spec for the AI Goals pro feature — AI coaching through structured, trackable improvement goals. Synthesized from the 2026-07-10 design session (grilling + spec interview). Follows the conventions in [Architecture Spec](../architecture.spec.md), [Database Schema Spec](../database-schema.spec.md), [Test Strategy Spec](../test-strategy.spec.md), and [UI/UX Spec](../ui-ux.spec.md).
>
> **Triage**: ready-for-agent

## Problem Statement

Players use Mongoose.gg to look at their stats, but the product currently makes them do their own coaching. A Bronze ADC staring at the solo dashboard can see their CS@10, vision score, and death timings — but nothing tells them *which* of those numbers is the one holding them back, what a realistic target looks like, or whether they are actually improving once they start trying. Generic coaching content ("ward more") isn't grounded in their data, and matchup-specific advice ("why do I lose Ezreal into Nautilus?") can't be answered honestly from the handful of games any one player has in a given matchup.

The result: players know their stats but not their next step, resolutions to improve evaporate because nothing tracks them, and the product's core promise — "AI coaching that turns your stats into actionable goals" — is not yet shipped. The Goals page exists only as a "coming soon" stub.

## Solution

An AI coach that works **through goals** rather than through open-ended Q&A:

1. The system scans the player's recent matches against a **curated heuristic library** (deaths by game phase, vision per minute, CS differentials, kill participation, damage share, objective participation, gold diff at checkpoints).
2. Each heuristic compares the player's numbers to an **internal cohort baseline** — percentile stats for the same tier + role, aggregated from all ten participants of every match already stored (no external data pipeline needed).
3. The top findings become **2–3 proposed goals**, each a structured object: metric, target value, window in games, and evaluation rule. An LLM phrases the goal text and the coaching advice ("why this matters, how to act on it") from the deterministic finding — it never invents metrics or targets.
4. The user can **edit** the target and window within sane bounds, then **accept or reject** each proposal. Accepted goals become active (max 3 at a time).
5. The **Goals page** (replacing the stub) is the personal analysis page: every active goal is shown with its tracking graph, updated as new matches sync, and resolves to met, failed, or expired.

AI Goals is the flagship **pro** feature. Free users see the wall: *"We found 3 things slipping in your gameplay"* with the findings locked and an upgrade CTA, plus a one-time free trial.

## User Stories

1. As a pro player, I want the system to analyze my recent matches and propose 2–3 improvement goals, so that I know what to work on without diagnosing my own stats.
2. As a pro player, I want each proposed goal grounded in a specific finding — my value vs. the typical value for my tier and role — so that the advice reflects my actual play and not generic tips.
3. As a pro player, I want to see how many games each finding is based on, so that I can judge how much to trust it.
4. As a pro player, I want the system to refuse to propose goals from too few games, so that I'm never coached on statistical noise.
5. As a pro player, I want to edit a proposed goal's target value and window before accepting it, so that the goal fits my schedule and ambition.
6. As a pro player, I want edits constrained to sensible bounds, so that I can't accidentally create an unachievable or meaningless goal.
7. As a pro player, I want to accept a proposal and have it become an active tracked goal, so that my intention to improve is recorded and measured.
8. As a pro player, I want to reject a proposal and not see the same finding re-proposed immediately, so that the coach respects my judgment.
9. As a pro player, I want at most 3 active goals at a time, so that I stay focused instead of drowning in objectives.
10. As a pro player, I want each active goal to show a tracking graph of the metric over my games since acceptance, with the target line overlaid, so that I can see progress at a glance.
11. As a pro player, I want goal progress to update automatically as my new matches sync, so that tracking requires no manual input.
12. As a pro player, I want a goal to resolve as **met** when I reach the target within the window, so that success is acknowledged.
13. As a pro player, I want a goal to resolve as **failed/expired** when the window elapses without meeting the target, so that stale goals don't linger and I can get a fresh proposal.
14. As a pro player, I want to abandon an active goal I no longer care about, so that a slot frees up for something relevant.
15. As a pro player, I want a history of my past goals and their outcomes, so that I can see my improvement journey over time.
16. As a pro player, I want each goal to carry short coaching advice (why this metric matters, how to act on it in game), so that I know *how* to improve the number, not just that I should.
17. As a pro player, I want new proposals to exclude metrics already covered by my active goals, so that suggestions stay complementary.
18. As a pro player, I want fresh recommendations to be generated only when I have new matches since the last analysis, so that the page loads fast and doesn't churn.
19. As a pro player with multiple linked Riot accounts, I want goals based on my primary account, consistent with the rest of my dashboards.
20. As a pro player, I want goals computed from my ranked games by default, so that ARAM and normals don't pollute the coaching signal.
21. As a pro player, I want goal proposals to work even if the AI phrasing service is temporarily unavailable, so that an external outage never blocks my coaching.
22. As a free player, I want to see that the coach found things slipping in my gameplay — with the findings locked — so that I understand what pro offers before paying.
23. As a free player, I want a one-time free trial of the pro tier, so that I can experience AI Goals before committing.
24. As a free player whose trial expired, I want my previously active goals preserved but frozen, so that upgrading later restores my history.
25. As a new user with few synced matches, I want the Goals page to explain that more games are needed before coaching can start, so that an empty page doesn't feel broken.
26. As a returning player after a long break, I want proposals based on my recent window rather than my lifetime stats, so that the coaching reflects who I am now.
27. As a user, I want all goal data returned dashboard-ready by the backend, so that the page renders quickly without client-side aggregation.

## Implementation Decisions

### Domain model (ubiquitous language)

- **Finding** — a deterministic observation produced by one heuristic: metric key, the player's observed value, the cohort baseline value, games analyzed (n), and a severity used for ranking. Findings are computed, never stored as user-facing objects on their own.
- **Heuristic Library** — the curated, versioned set of rules that produce Findings. v1 metrics are drawn from data already ingested: deaths by phase (`deaths_pre_10`, `deaths_10_20`, `deaths_20_30`), vision per minute, CS per minute and `cs_diff_vs_lane` at checkpoints, `gold_diff_vs_lane` at 15, kill participation, damage share, and objective participation. Each heuristic declares a minimum sample size (default: 10 games in the analysis window) below which it produces no Finding.
- **Baseline** — a percentile statistic (median for v1) for a metric, per tier + role cohort, computed from **all ten participants** of every stored match — the sample is the match pool, not the user base. Baseline fallback when a cohort cell is thin: widen to the adjacent tier band; if still thin, fall back to self-relative (the player's own trailing average) and mark the Finding accordingly.
- **Goal** — the persistent, trackable object: metric key, direction (increase/decrease), target value, window in games, status, the originating Finding snapshot (observed value, baseline, n at proposal time), goal text, and coaching text. Status lifecycle: `proposed → active → met | failed | abandoned`, plus `rejected` for declined proposals. "Expired" is the failed state reached by window exhaustion — no separate status.
- **Recommendation** — a set of proposed Goals generated from one analysis run, cached until new matches arrive.

### Architecture & layering

- Pure logic lives in Core domain services, following the `TrendBadgeCalculator` / `MainChampionRecommender` pattern: the heuristic evaluation, baseline fallback rules, goal-edit bounds validation, and goal progress evaluation (met/failed decision from a series of per-game metric values) are all deterministic Core services with no I/O.
- An Application service orchestrates the proposal pipeline: load recent matches for the primary Riot account → run heuristic library against baselines → rank findings → call the LLM to phrase text → persist the recommendation snapshot.
- The **LLM client is the single new seam**: a Core interface (alongside the existing Riot API client interface) implemented in Infrastructure using the official Anthropic C# SDK. The LLM receives the deterministic Findings and returns phrased `goalText` and `coachingText` as structured JSON validated against a schema (`output_config.format`); it must never alter metric, target, or window values. Model ID is configuration, defaulting to the current recommended Claude model; on LLM failure or timeout, templated fallback text is generated from the Finding so proposals always succeed.
- Goal progress is computed on read from stored match data since acceptance (no per-match write-side evaluation job in v1); status transitions to met/failed are persisted when observed.

### Data & schema

- **`goals` table** (new): goal fields above, keyed to `puuid` (consistent with `ai_snapshots`), with `queue_id` filter, timestamps for proposed/accepted/resolved, and the finding snapshot as JSON.
- **`metric_baselines` table** (new): metric key, tier, role, percentile values, sample count, computed date. Refreshed by a scheduled job (existing background-job infrastructure pattern), aggregating over all participants of stored matches.
- **`ai_snapshots`** (existing, per Database Schema Spec §9): used as designed — `summary_text` holds the deterministic findings summary given to the LLM, `goals_json` holds the LLM response, `context_type = 'solo'` for v1. Serves as the recommendation cache: regenerate only when new matches exist since `snapshot_date`.
- **Trial**: two nullable columns on `users` (trial start/end). Tier resolution treats an active trial as pro; the `tier` claim already in the auth session is derived from this resolution at login/refresh.

### API contracts (all under `/api/v2`, `IEndpoint` pattern, `.RequireAuthorization()`, camelCase records, dashboard-ready)

- `GET /goals/recommendations/{userId}` — pro/trial: current proposals with findings (metric, observed, baseline, gamesAnalyzed, proposed target/window, goalText, coachingText). Free: `{ lockedFindingsCount, trialAvailable }` teaser shape — the findings themselves are never sent to free clients. Follows the standard userId-claim check and primary-Riot-account resolution pattern.
- `POST /goals/{userId}` — accept a proposal by recommendation id, with optional edited `targetValue` / `windowGames` (validated against bounds derived from the Finding). Enforces max 3 active goals (409 on violation). Pro/trial only (403 with upgrade code for free).
- `POST /goals/{userId}/rejections` — record a rejected proposal so the same finding is not immediately re-proposed.
- `GET /goals/{userId}` — list goals: active with current progress, plus history.
- `GET /goals/{userId}/progress` — per-goal metric series (per game since acceptance) with target overlay, dashboard-ready for graphs.
- `PATCH /goals/{userId}/{goalId}` — abandon an active goal.
- `POST /users/me/trial` — start the one-time free trial.
- Recommendation regeneration is implicitly cached (per snapshot + new-match check); no rate-limited "regenerate" button in v1.

### Frontend

- Goals page replaces the existing stub route; it is the personal analysis page — no separate builder or pinboard page exists.
- Views follow the UI/UX Spec design system (tokens only, no hardcoded values): proposal cards with edit-then-accept/reject affordances, active goal cards each with a tracking graph and goal line, a history section, and the locked-teaser state for free users.
- Free-tier wall copy pattern: "We found N things slipping in your gameplay" + locked cards + trial/upgrade CTA. This is the canonical conversion moment for the pricing story.

## Testing Decisions

- A good test exercises **external behavior at the highest seam** — the HTTP endpoint — and never asserts on implementation details. Endpoint integration tests via the existing `TestWebApplicationFactory` (prior art: `WinrateTrendEndpointTests`, `ChampionSelectEndpointTests`, and the other endpoint test suites) are the primary layer and cover: proposal generation shape, tier gating (free teaser vs. pro payload vs. 403s), trial start and expiry behavior, accept with edits (including bound violations), max-3-active enforcement, rejection suppressing re-proposals, progress series math, recommendation caching (no regeneration without new matches), and the LLM-failure fallback path.
- The LLM client interface is **faked in the test factory** (same approach as the faked Riot API client) — deterministic canned phrasings, plus a throwing variant for the fallback tests. No test ever calls a real model.
- Pure domain services (heuristic library, baseline fallback, goal evaluation state machine, edit-bounds validation) get direct unit tests, prior art: `TrendBadgeCalculatorTests`, `MainChampionRecommenderTests`.
- Repository behavior for the new tables follows the `MatchesRepositoryIntegrationTests` prior art where endpoint tests don't already cover it.
- Frontend: Vitest component and store tests per the Test Strategy Spec patterns (component testing pattern §4.6, store pattern §4.7); one Playwright E2E happy path — see proposals → edit → accept → active goal visible with graph — per the E2E strategy.

## Out of Scope

- **Ad-hoc conversational AI coach** (the "Ezreal into Nautilus" Q&A) — explicitly rejected in design: matchup-level personal sample sizes cannot support honest answers.
- **Drag-and-drop analysis builder / separate personal analysis page** — superseded; the Goals page with per-goal graphs is the analysis surface.
- **Duo and team goal contexts** — `ai_snapshots.context_type` already supports them; deferred.
- **Team page, magic-link snapshot sharing, champ-select comp analysis** — separate features, separate specs (comp analysis ships free inside champion select).
- **Global/external data ingestion** — baselines come exclusively from matches already stored; no crawler.
- **Champion- or matchup-specific coaching** — baseline cells too thin by design decision.
- **Billing and payments** — the trial is tier-resolution only; payment integration is its own feature.
- **Notifications** (email/push on goal resolution) — resolution is visible on the Goals page only.
- **Manual goal creation** — v1 goals originate from AI proposals only.

## Further Notes

- The tier logic this feature establishes: **free = you look at your data; pro = the product does work for you.** Future features should be sorted onto the paywall by that question.
- The heuristic library is the coaching IP of the product. v1 ships the metric list above; expanding it is a content exercise, not a schema change — heuristics should be versioned so past findings remain reproducible.
- Sample-size honesty is a product principle, not just a guard: every user-facing finding displays its n, and thin-cohort fallbacks are labeled.
- On implementation, update `architecture.spec.md` (route map §5, planned endpoints §14) and `database-schema.spec.md` (new tables) to reflect reality.
- No `CONTEXT.md` exists yet; the domain terms above (Finding, Heuristic Library, Baseline, Goal, Recommendation) are candidates for a first `/domain-modeling` pass.
