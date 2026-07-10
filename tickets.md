# Tickets: AI Goals

Tracer-bullet slices building the AI Goals pro feature — AI coaching through structured, trackable improvement goals. Source spec: `.github/specs/features/ai-goals.spec.md`.

Work the **frontier**: any ticket whose blockers are all done. After the tracer bullet lands, tickets 4, 6, and 7 can run in parallel with the 2 → 3 chain.

## 1. Tracer bullet: first goal proposal, end to end

**What to build:** A pro user opens the Goals page (replacing the "coming soon" stub) and sees one or two real proposal cards, each grounded in their own match data: the deaths-by-game-phase heuristic compared against a self-relative baseline (the player's own trailing average), with templated goal and coaching text — no LLM, no cohort table yet. Recommendations are persisted as an `ai_snapshots` row (`context_type = 'solo'`) so each proposal has a stable id, and a fresh analysis runs only when new matches exist since the last snapshot. Free-tier users receive a 403 with an upgrade error code for now. Follows the standard endpoint pattern: userId-claim check, primary Riot account resolution, ranked queue filter, dashboard-ready camelCase response.

**Blocked by:** None — can start immediately.

- [ ] `GET /api/v2/goals/recommendations/{userId}` returns proposals with metric key, observed value, baseline value, gamesAnalyzed (n), proposed target, window in games, goal text, and coaching text
- [ ] The heuristic produces no Finding below its minimum sample size (default 10 games), and the endpoint returns an explanatory empty state instead
- [ ] Recommendations are cached in `ai_snapshots` and not regenerated when no new matches have synced
- [ ] Free-tier requests get 403 with a distinct error code (teaser comes in ticket 7)
- [ ] Goals page renders proposal cards from the endpoint using design-system tokens, including the n badge and an empty state
- [ ] Heuristic and finding logic live as pure Core domain services with direct unit tests (TrendBadgeCalculator prior art)
- [ ] Endpoint integration tests via TestWebApplicationFactory cover the happy path, thin-data, caching, and tier-403 cases

## 2. Accept, edit, and reject proposals

**What to build:** From a proposal card, the user can edit the target value and window within bounds derived from the Finding, then accept — creating an active Goal (new `goals` table, status lifecycle from the spec) — or reject, which records the rejection so the same finding is not immediately re-proposed. At most 3 goals can be active; a fourth accept is refused with a clear error. The Goals page gains an active-goals section showing each goal's text, target, and window (no graph yet).

**Blocked by:** 1. Tracer bullet: first goal proposal, end to end.

- [ ] `POST /api/v2/goals/{userId}` accepts a proposal by recommendation id with optional edited targetValue/windowGames; out-of-bounds edits are rejected with validation errors
- [ ] Accepting a fourth goal while 3 are active returns 409
- [ ] `POST /api/v2/goals/{userId}/rejections` records a rejection; the next recommendations response excludes that finding
- [ ] `GET /api/v2/goals/{userId}` lists active goals (and later, history)
- [ ] Goals page supports edit-then-accept and reject on proposal cards, and renders active goal cards
- [ ] Edit-bounds validation is a pure Core service with unit tests; all endpoint behavior covered by integration tests

## 3. Goal tracking, resolution, and history

**What to build:** Every active goal tracks itself: the Goals page shows a per-game metric series since acceptance with the target line overlaid, updating as new matches sync. The Core evaluation state machine resolves goals to met (target reached within the window) or failed (window exhausted), persisting the transition when observed on read. The user can abandon an active goal to free a slot. A history section lists resolved goals with their outcomes.

**Blocked by:** 2. Accept, edit, and reject proposals.

- [ ] `GET /api/v2/goals/{userId}/progress` returns a dashboard-ready per-goal series (metric value per game since acceptance) with target overlay data
- [ ] Goal status transitions to met/failed per the evaluation rule; transitions are persisted and reflected in the goals list
- [ ] `PATCH /api/v2/goals/{userId}/{goalId}` abandons an active goal and frees an active slot
- [ ] Goals page renders a tracking graph per active goal (design-system tokens) and a history section for resolved goals
- [ ] Evaluation state machine is a pure Core service with unit tests covering met, failed-by-window, and in-progress cases; endpoint integration tests cover progress math and abandon

## 4. Cohort baselines

**What to build:** Findings stop being self-relative: proposals compare the player against the typical value for their tier and role. A new `metric_baselines` table holds percentile stats (median for v1) per metric/tier/role, refreshed by a scheduled background job that aggregates over all ten participants of every stored match (the sample is the match pool, not the user base). The baseline fallback chain applies when a cohort cell is thin: widen to the adjacent tier band, then fall back to self-relative with the finding labeled accordingly. Proposal cards now read "your X vs. typical for your rank and role".

**Blocked by:** 1. Tracer bullet: first goal proposal, end to end.

- [ ] `metric_baselines` table populated by a scheduled job (existing background-job infrastructure pattern) from stored participants data
- [ ] Findings carry the cohort baseline and its sample count; the fallback chain (adjacent tier → self-relative) is a pure Core service with unit tests
- [ ] Fallback-derived findings are labeled in the API response and on the proposal card
- [ ] Recommendations integration tests cover cohort, widened-tier, and self-relative fallback paths

## 5. Full heuristic library

**What to build:** The coach considers the whole v1 metric set, not just deaths by phase: vision per minute, CS per minute and lane CS differential at checkpoints, gold differential at 15, kill participation, damage share, and objective participation. Findings are ranked by severity and only the top 2–3 become proposals. Each heuristic declares its own minimum sample size. Metrics already covered by an active goal are excluded from new proposals, so suggestions stay complementary.

**Blocked by:** 1. Tracer bullet: first goal proposal, end to end; 2. Accept, edit, and reject proposals.

- [ ] All v1 heuristics from the spec produce Findings from existing match/checkpoint/metrics data
- [ ] Severity ranking selects the top 2–3 findings for proposal; ranking is a pure Core service with unit tests
- [ ] Heuristics below their minimum sample size produce no Finding
- [ ] Recommendations exclude metrics covered by currently active goals (integration test proves it)
- [ ] Heuristic library is versioned so past findings remain reproducible

## 6. LLM phrasing behind the seam

**What to build:** Proposal text goes from templated to genuinely coached: a Core LLM-client interface (the feature's single new seam) is implemented in Infrastructure with the official Anthropic C# SDK, receiving the deterministic Findings and returning goal text and coaching text as structured JSON validated against a schema. The LLM never alters metric, target, or window values. On failure or timeout, the templated text from ticket 1 is the fallback, so proposals always succeed. Tests replace the interface with a fake — no test calls a real model.

**Blocked by:** 1. Tracer bullet: first goal proposal, end to end.

- [ ] Core interface for the LLM client; Infrastructure implementation using the official Anthropic C# SDK with structured JSON output; model id is configuration
- [ ] Numbers in proposals provably come from Findings, never from the LLM response (integration test asserts values unchanged under a mischievous fake)
- [ ] LLM failure/timeout falls back to templated text; proposals still return 200 (throwing-fake integration test)
- [ ] Fake LLM client registered in the test factory (faked Riot client prior art); no test performs network calls

## 7. Free-tier teaser and trial

**What to build:** The paywall moment: a free user opening the Goals page sees "We found N things slipping in your gameplay" with the findings locked and an upgrade CTA, replacing the 403 from ticket 1 — the findings themselves are never sent to free clients. A one-time free trial grants pro-tier access: trial start/end stored on the user, tier resolution treats an active trial as pro (feeding the existing `tier` session claim), and expiry freezes goals (preserved but not updated) until upgrade.

**Blocked by:** 1. Tracer bullet: first goal proposal, end to end.

- [ ] Free-tier `GET /goals/recommendations/{userId}` returns the teaser shape (lockedFindingsCount, trialAvailable) with no finding details in the payload
- [ ] Goals page renders the locked-teaser state with trial/upgrade CTA (design-system tokens)
- [ ] `POST /api/v2/users/me/trial` starts the one-time trial; a second attempt is refused; active trial resolves to pro tier
- [ ] Trial expiry returns the user to the teaser with goals frozen and preserved
- [ ] Integration tests cover teaser shape, trial start, one-time enforcement, active-trial access, and post-expiry behavior

## 8. E2E verification

**What to build:** The full user journey proven in a real browser via the existing Playwright E2E setup: a pro user sees proposals, edits one, accepts it, and watches it appear as an active goal with its tracking graph; a free user hits the locked teaser wall. This is the final integrate-and-verify pass for the feature.

**Blocked by:** 3. Goal tracking, resolution, and history; 7. Free-tier teaser and trial.

- [ ] Playwright E2E: pro flow — see proposals → edit target → accept → active goal card with graph visible
- [ ] Playwright E2E: free flow — Goals page shows the locked "things slipping" teaser with CTA
- [ ] Full suite (backend + frontend + E2E) green
