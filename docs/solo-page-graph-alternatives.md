# Solo Page Graph Alternatives — Analysis & Evaluation

> **Date**: February 16, 2026  
> **Context**: Evaluating new graph types for Solo page Zone 4 (Deep Analysis)  
> **Current State**: Solo v1 has Zones 1-3 (filters, summary stats, 6 trend charts). Zones 4-5 reserved for v2.

---

## Executive Summary

This document evaluates **two proposed graphs** (spider/radar chart and danger zones death heatmap) plus **five alternatives** for extending the Solo page with deeper analytical visualizations. The goal is to help players answer "what should I improve?" with visual, actionable insights.

**Key Constraint**: Death position coordinates (x, y) are not currently stored in the database, making map-based danger zones a significant infrastructure investment.

**Quick Recommendation**: **Spider Chart + Death Timing Heatmap** offer the best immediate value — both use existing data, are lightweight to implement, and complement the existing trend charts without duplication.

---

## Evaluation of Original Two Proposals

### 1. Spider/Radar Chart (Performance Profile)

**Concept**: Multi-axis chart showing your overall "shape" as a player — axes like Laning, Combat, Vision, Objectives, Survivability, Farming. Riot's old client had this with Fighting/Income/Vision/Toughness/Map Control/Teamwork.

**Data Readiness**: ✅ **Strong.** The API already returns `roleBreakdown`, `performanceByPhase`, `deathEfficiency`, and the database has `kill_participation_pct`, `damage_share_pct`, `vision_per_min`, `damage_dealt`, `damage_taken`, `damage_mitigated`, gold/CS checkpoints, and objective participation. Everything needed to compute 6–8 meaningful axes exists today.

**UX Strengths**:
- Instant "playstyle fingerprint" — a 1-second glance reveals strengths and weaknesses
- Fills a gap: the current Solo page has 6 independent trend lines but no holistic overview. The radar gives the gestalt that's missing between the summary stats (Zone 2) and the individual trends (Zone 3)
- Highly shareable — players love comparing shapes
- Answers "what should I work on?" which feeds directly into the Goals system (planned v2)

**UX Risks**:
- Riot removed theirs because axes weren't always intuitive and people treated it as a score rather than a diagnostic
- Without a comparison reference (rank average, role average, your own history), the shape means nothing — you need a "compared to what?"
- Can feel like a vanity metric if the axes aren't tied to actionable improvement
- Axis normalization is tricky — percentile-based? rank-based? role-based? Wrong choice makes the chart misleading

**Implementation Effort**: Medium
- Backend: New endpoint to aggregate and normalize 6-8 axes into percentiles
- Frontend: Chart.js radar component with baseline overlay
- Testing: Validate normalization across roles and ranks

**Verdict**: ⭐⭐⭐⭐ High value if done with a clear comparison baseline (e.g. your rank average for your role as a ghosted outline behind your shape). Medium implementation cost.

---

### 2. Danger Zones (Death Heatmap on Summoner's Rift)

**Concept**: A top-down Summoner's Rift minimap with heatmap overlay showing where you die most frequently. The redder the zone, the more deaths there.

**Data Readiness**: ❌ **Blocker.** The database currently stores death *timing* (`deaths_pre_10`, `deaths_10_20`, etc.) but **no death coordinates (x, y)**. The Riot API provides `CHAMPION_KILL` events with positions in timeline data, but the match sync job doesn't extract or store them. This requires:
- New DB table (`participant_death_events` with `x`, `y`, `minute_mark`, `killer_champion`)
- Changes to the match sync pipeline to extract kill events from timeline
- Backfill logic for existing matches (or only apply to new ones)

**UX Strengths**:
- Extremely visual and engaging — the "wow factor" graph
- Directly actionable: "I keep dying in river near dragon pit" → ward there, path differently
- Aligns perfectly with the #1 research finding (deaths = most impactful + easiest to improve)
- Scales beautifully to Duo/Team (different player colors) — building it once pays off 3x
- No competitor in the solo-improvement tracker space does this well

**UX Risks**:
- Needs enough data density to be useful — 20 games might only produce 60-100 death points, which can look sparse
- Map rendering + heatmap overlay is a non-trivial frontend challenge (canvas/WebGL)
- Without time-phase filtering (early/mid/late), the heatmap conflates laning deaths with teamfight deaths, which have completely different solutions
- Risk of being "cool but not used" if it doesn't connect to an action

**Implementation Effort**: Very High
- Backend: Schema change, sync pipeline modification, backfill strategy
- Frontend: Summoner's Rift minimap SVG + heatmap rendering (canvas or WebGL)
- Testing: Timeline parsing, coordinate transformation, heatmap density tuning

**Verdict**: ⭐⭐⭐⭐⭐ Very high long-term value, especially across Solo/Duo/Team pages. But significant backend work (schema + sync pipeline changes) makes this the heaviest-lift item. Best suited for a dedicated vertical slice rather than a quick addition.

---

## Five Alternatives

### A. Game Phase Performance (Early / Mid / Late Breakdown)

**What**: A three-panel or stacked bar visualization showing your win rate, KDA, and gold differential broken down by Early (0–15min), Mid (15–25min), and Late (25min+) game phases.

**Data Readiness**: ✅ **Immediate.** The API already returns `performanceByPhase[]` with games, wins, winRate, avgKda per phase — it's just not rendered on the frontend yet.

**Why It's Compelling**:
- Answers "when in the game am I losing?" which is a more actionable question than most trend charts
- Directly maps to practice strategies: weak early game → work on laning; weak late game → work on teamfight positioning
- Lightweight to implement — data exists, just needs a component
- Pairs beautifully with a spider chart (the spider shows *what*, the phase chart shows *when*)

**Implementation Effort**: Low
- Backend: None (data exists)
- Frontend: New PhasePerformanceCard component with 3-column bar chart
- Testing: Component unit tests

**Risk**: Could feel too simple — three bars might not feel "premium" enough for Zone 4.

**Verdict**: ⭐⭐⭐ Solid incremental value with almost zero cost. Good "quick win" for v2.

---

### B. Session Fatigue Tracker

**What**: A visualization showing how your performance degrades across consecutive games in a single session. Groups matches by session (games played within ~90-minute gaps), then plots KDA and win rate from game 1 → game N in each session.

**Data Readiness**: 🔶 **Backend work required.** Match timestamps exist (`game_start_time`), so sessions can be inferred by detecting gaps. Performance stats per game exist. Needs a new endpoint to group matches into sessions and compute per-position averages.

**Why It's Compelling**:
- Backed by strong research: 8–10% performance decline over extended sessions
- Highly actionable: "you win 58% in games 1–3 but only 42% after game 5" → stop playing
- Nobody else does this — genuine competitive differentiation
- Emotional resonance: every player has experienced tilt but never seen it quantified
- Direct feed into the AI Goals system: "Goal: Stop after 4 games when win rate drops below 45%"

**Implementation Effort**: Medium
- Backend: New session-grouping endpoint with per-position aggregation
- Frontend: Line chart with per-session average overlays
- Testing: Session detection logic, edge cases (long breaks, multi-day sessions)

**Risk**: Requires enough play sessions with 3+ games to be meaningful. Casual players with 1–2 games/day won't see useful data.

**Verdict**: ⭐⭐⭐⭐⭐ Extremely high value for engaged players. Unique differentiation. Medium effort. Strong candidate for v2.

---

### C. Role Performance Comparison

**What**: A horizontal bar chart or small-multiples view comparing your performance across roles (Top, Jungle, Mid, ADC, Support) — showing games played, win rate, KDA, and a "fit score" for each.

**Data Readiness**: ✅ **Immediate.** The API already returns `roleBreakdown[]` with per-role games, wins, winRate, avgKda. Not rendered yet.

**Why It's Compelling**:
- Answers "am I playing the right role?" which is a fundamental question many players ignore
- Can surface role-swap insights: "your Support win rate is 64% vs 48% Top"
- Lightweight to implement — data exists
- Feeds into champion select recommendations ("consider queueing as...")

**Implementation Effort**: Low
- Backend: None (data exists)
- Frontend: New RoleComparisonCard component with horizontal bars
- Testing: Component unit tests

**Risk**: Most players already know their role. Could feel obvious for one-tricks. More useful for players who flex across roles.

**Verdict**: ⭐⭐⭐ Moderate value, low cost. Good for fill players, less relevant for one-tricks.

---

### D. Death Timing Heatmap (Time-Based, Not Map-Based)

**What**: A visualization (heatmap grid or stacked area chart) showing *when* in the game your deaths cluster — broken into minute-by-minute or phase buckets. Think of it as a "Danger Zones" but on a timeline instead of a map.

**Data Readiness**: ✅ **Immediate.** `deaths_pre_10`, `deaths_10_20`, `deaths_20_30`, `deaths_30_plus`, and `first_death_minute` already exist in `participant_metrics`. No new backend work needed.

**Why It's Compelling**:
- Provides the "when" insight that the existing Deaths Over Time trend chart doesn't (that chart shows deaths-per-game over your match history, not when in a game you die)
- Directly actionable: "82% of my deaths happen between minutes 10–20" → your mid-game rotations are the problem
- Complements the deaths trend chart rather than replacing it
- Much lighter implementation than map-based Danger Zones while still delivering spatial-timing insight
- Natural stepping stone toward full Danger Zones: time-based first, map-based later

**Implementation Effort**: Low
- Backend: None (data exists)
- Frontend: Stacked area chart or heatmap grid component
- Testing: Component unit tests

**Risk**: Less visually exciting than a map heatmap. Could feel like "yet another deaths chart" given the existing Deaths Over Time trend.

**Verdict**: ⭐⭐⭐⭐ High actionability, low cost, complements existing charts. Strong incremental addition.

---

### E. Win Condition Patterns

**What**: A data card or small chart showing which game conditions correlate with YOUR wins specifically. For example: "When you're ahead at 15 min → 78% win rate. When you participate in first dragon → 71% win rate. When deaths < 3 → 69% win rate."

**Data Readiness**: 🔶 **Backend work required.** The raw data exists (`participant_checkpoints.is_ahead`, `participant_objectives.dragons_participated`, `participants.deaths`, `participants.win`) but needs a new endpoint that computes conditional win rates (win rate WHERE condition = true vs false).

**Why It's Compelling**:
- Personalized and surprising — global stats say "first dragon = 70.69% win rate" but YOUR number might be 82% or 55%
- Flips the narrative from "here's what you did" to "here's what matters for you"
- Directly feeds goal setting: the condition where your win rate delta is largest = your highest leverage improvement area
- Connects the research findings to the individual player's data
- Can be displayed as simple stat cards — clean, premium, easy to scan

**Implementation Effort**: Medium
- Backend: New endpoint computing conditional win rates across multiple dimensions
- Frontend: Stat card grid with highlighted deltas
- Testing: Statistical significance thresholds, edge cases with small samples

**Risk**: Needs a reasonable sample size per condition (at least 15–20 games each side) to be statistically meaningful. Small champion pools in specific matchups could produce noisy results.

**Verdict**: ⭐⭐⭐⭐ Very high conceptual value, medium implementation. Could feel magical when sample sizes are sufficient.

---

## Summary Matrix

| Option | Data Ready? | Impl. Effort | Actionability | Visual Impact | Uniqueness | Overall |
|--------|:-----------:|:------------:|:-------------:|:-------------:|:----------:|:-------:|
| **Spider Chart** | ✅ Yes | Medium | High (with baseline) | High | Medium | ⭐⭐⭐⭐ |
| **Danger Zones** | ❌ No (schema change) | Very High | Very High | Very High | Very High | ⭐⭐⭐⭐⭐ |
| **A. Phase Performance** | ✅ Yes (API exists) | Low | High | Medium | Low | ⭐⭐⭐ |
| **B. Session Fatigue** | 🔶 Needs endpoint | Medium | Very High | Medium | Very High | ⭐⭐⭐⭐⭐ |
| **C. Role Comparison** | ✅ Yes (API exists) | Low | Medium | Medium | Low | ⭐⭐⭐ |
| **D. Death Timing** | ✅ Yes | Low | High | Medium | Medium | ⭐⭐⭐⭐ |
| **E. Win Conditions** | 🔶 Needs endpoint | Medium | Very High | Medium | High | ⭐⭐⭐⭐ |

---

## Recommendations

### Immediate (v2.0)
1. **Spider Chart** — delivers holistic "what to improve" insight with existing data
2. **Death Timing Heatmap** — high actionability, low cost, complements existing deaths trend

### Near-Term (v2.1)
3. **Session Fatigue Tracker** — unique differentiation, research-backed, medium effort
4. **Phase Performance** — easy win, uses existing API data

### Long-Term (v2.2+)
5. **Danger Zones (Map)** — save for dedicated sprint due to schema/sync pipeline changes
6. **Win Condition Patterns** — high value but needs careful statistical handling

---

## Open Questions

1. **Spider Chart Baseline**: Should the comparison be:
   - Rank average (all players in your tier)?
   - Role average (all players in your role + tier)?
   - Your own historical average (last 3 months)?
   - All three with a toggle?

2. **Session Detection**: How long of a gap defines a new session?
   - 90 minutes (2x average game length)?
   - 4 hours (meal break)?
   - Midnight boundary?

3. **Death Timing Granularity**: Minute-by-minute or phase buckets?
   - Minute-by-minute is noisy but precise
   - Phase buckets (0-10, 10-20, 20-30, 30+) match existing data structure

4. **Zone 4 Capacity**: How many deep analysis components fit comfortably?
   - 2-column grid → 2 components side-by-side
   - Single-column stacked → 3-4 components
   - Tabbed interface → unlimited but hides content

---

## Related Documentation

- [Product Backlog](./product_backlog.md) — Epic H (Research-Based Performance Metrics)
- [Win Prediction Metrics Research](../win-prediction-metrics-research.md) — Academic research foundation
- [UI/UX Spec](../../.github/specs/ui-ux.spec.md) — Design system and layout patterns
- [Architecture Spec](../../.github/specs/architecture.spec.md) — API endpoints and DTOs
