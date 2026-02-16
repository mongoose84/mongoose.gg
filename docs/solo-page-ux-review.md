# Solo Page UX Review — Charts & Layout

> **Date**: February 16, 2026  
> **Reviewer**: UX/UI Design Review  
> **Scope**: Solo Dashboard charts (6 trend charts) vs research document + layout spec  
> **References**: `docs/win-prediction-metrics-research.md`, `.github/specs/ui-ux.spec.md`, `docs/planning/product_backlog.md` (Epic H)

---

## Part 1: Charts vs Research — Does It Make Sense?

### Metric Selection: Strong Alignment

The 6 implemented charts map cleanly to the research tiers:

| Chart | Research Tier | Research Priority | Implemented? |
|-------|--------------|-------------------|-------------|
| Deaths Over Time | Tier 1 — Highest Impact + Easiest | P0 Critical | Yes |
| Dragon Participation | Tier 1 — Highest Impact + Easiest | P1 High | Yes |
| Vision Score | Tier 1 — Highest Impact + Easiest | P1 High | Yes |
| Gold at 15 | Tier 2 — High Impact + Moderate | P0 Critical | Yes |
| CS Per Minute | Tier 2 — High Impact + Moderate | P2 Medium | Yes |
| Winrate Over Time | Overview metric | — | Yes |

**Verdict**: The metric selection is well-prioritized. All three Tier 1 research metrics are present, plus the two most important Tier 2 metrics. Winrate is the anchor orientation chart. No wasted charts.

### What's Working Well (Charts the User Likes)

**Winrate, Gold at 15, CS Per Minute** share a visual pattern that works:

1. **Single clean trend line** with smooth tension (0.3) and area fill
2. **No visible dots** (`pointRadius: 0`, hover-only interaction)
3. **No legend** (Winrate, CS) or minimal legend (Gold at 15 — "Your Gold" vs "Opponent Gold")
4. **One question answered**: "Am I improving?" → read the slope of one line
5. **One action implied**: The trend direction tells you whether to keep doing what you're doing or change

These charts embody the UX spec principle: *"Every insight answers one question and implies one action."*

### What's Not Working (Charts the User Dislikes)

**Deaths, Dragon Participation, Vision Score** share a different pattern:

1. **Dual-dataset display**: Raw per-game dots (`pointRadius: 3`, `tension: 0`) + separate rolling average smooth line (`pointRadius: 0`, `tension: 0.3`)
2. **Legend always visible** showing both "Deaths" + "Rolling Average" (or equivalent)
3. **Multiple annotation lines**: Target line (70% for dragon, 1.0/min for vision) + overall average line + the rolling average line itself = 3 reference elements competing with the data
4. **Scatter-plot appearance**: Per-game metrics like deaths (0, 2, 8, 3, 1) or dragon participation (0%, 100%, 50%, 100%) are inherently noisy. Showing them as connected dots creates a jagged zigzag that looks like a heart-rate monitor, not a trend.

**Why this feels "too technical" from a user perspective:**

| Problem | User Experience Impact |
|---------|----------------------|
| Two datasets visible | "Which line should I focus on?" — decision fatigue |
| Raw dots + smooth line | The dots compete with the trend; the trend IS the story |
| Target + overall + rolling avg lines | Three reference frames is analyst-level, not player-level |
| Legend taking up header space | Adds "dashboard" feel, reduces "tool" feel |
| Jagged raw data | Creates anxiety — every spike looks alarming even if the trend is fine |

### Research Alignment Issue: Framing

The research doc says these Tier 1 metrics are the **"Easiest to Improve"** — they should feel *approachable and encouraging*, not *analytical and overwhelming*. The current dot-graph treatment makes them feel like the hardest charts to interpret on the page.

This also violates the UX spec:

- **Principle #8**: *"Single-match insights framed as multi-game trends"* — The per-game dots are literally single-match insights. The rolling average is the multi-game trend. Only the trend should be visible at the default view level.
- **Bias Rule #4**: *"Progress illusion: Graphs/stats without actionable interpretation must be hidden or secondary"* — The raw dots are stats without interpretation. The rolling average is the interpretation.

### Recommendation: Harmonize the Three Problem Charts with the Three Good Charts

The fix is to make Deaths, Dragon Participation, and Vision Score *look like* Winrate, Gold at 15, and CS Per Minute:

1. **Show only the rolling average as the primary line** — smooth, filled, no dots
2. **Keep per-game data in the tooltip only** — hover to see the actual game value
3. **Keep the target/reference annotation lines** — these are useful, but limit to one (either target OR overall, not both)
4. **Remove the legend** — one dataset = no legend needed
5. **Use the same `tension: 0.3` + `pointRadius: 0` + `fill: true`** visual language

The per-game data doesn't disappear — it lives in the tooltip where it belongs. The chart surface tells the trend story at a glance.

### Chart-Specific Notes

**Deaths Over Time:**
- Rolling average as smooth line is the correct visualization for "am I dying less?"
- The overall average annotation line is useful as a "where I started" reference
- Consider: the trend color (green = improving, red = worsening) is a good idea but could be jarring on initial load. Neutral purple as default is better until there's clear trend data.

**Dragon Participation:**
- The 70% target line from research is excellent — keep this. It gives users a concrete goal.
- Drop the overall average line — having both creates visual clutter. The target line is more actionable because it's a forward-looking goal, not a backward-looking average.
- The binary nature of dragon participation (0% or 100% per game) makes raw dots especially misleading. A 5-game rolling average smooths this into a meaningful participation rate.

**Vision Score:**
- The role-aware target (Support: 2.0, others: 1.0) is smart differentiation
- Keep the target line, drop the overall average line (same reasoning as dragon)
- Vision per minute is the right normalization — raw vision score varies too much with game length

### Chart Order on the Page

Current order vs research priority:

| Position | Current Chart | Research Tier |
|----------|--------------|---------------|
| 1 | Winrate | Overview |
| 2 | Deaths | Tier 1 |
| 3 | Gold at 15 | Tier 2 |
| 4 | CS Per Minute | Tier 2 |
| 5 | Dragon Participation | Tier 1 |
| 6 | Vision Score | Tier 1 |

The Tier 1 "easy wins" (deaths, dragon, vision) are split apart by Tier 2 metrics. Consider grouping by the improvement narrative:

**Suggested order:**
1. **Winrate** — "How am I doing overall?"
2. **Deaths** — "What's the #1 thing I can fix?"
3. **Dragon Participation** — "Am I showing up for objectives?"
4. **Vision Score** — "Am I giving myself information?"
5. **Gold at 15** — "Am I winning my lane?"
6. **CS Per Minute** — "Am I farming efficiently?"

This tells a story: outcome → behavior → macro awareness → laning fundamentals. The first four are the "easy wins," the last two require more practice to improve. However, this is a minor polish item — the current order is not broken.

### Missing From Research (Future Consideration)

| Research Item | Status | Notes |
|---------------|--------|-------|
| Session Performance Deterioration (10%+ decline) | Not implemented | Backlog item (Solo v2). High user value — "when should I stop playing?" |
| Tower Participation (65.42% correlation) | Not implemented | Reasonable to defer; 6 charts is already substantial |
| Damage Dealt correlation (63-73%) | Not implemented | Context-dependent; better suited for match details than trends |
| First objective breakdown by type | Not implemented | Dragon participation covers the highest-correlation objective |

---

## Part 2: Layout Review — Are There Clashes with the Spec?

### Spec vs Implementation: Key Discrepancies

#### 1. Zone 3 was designed for 2 charts, now has 6

The original spec defines Zone 3 as:

> *"2-column chart grid | LP + Winrate charts"* — UI/UX Spec, Section 14 (AnalysisLayout)

And the Solo page spec (Section 9) says:

> *"Zone 3: TrendChartCard — Winrate trend (rolling 20-game)"*

The product backlog (G5 Solo MVP) confirms:

> *"Solo v1 MVP scope: Summary Stats Card (G5b18), Winrate Trend Chart (G5b4)"*

**Current state**: Zone 3 has 6 charts in a 3-column grid. This is 3x the original planned content and the grid was changed from 2-column to 3-column to accommodate.

**Impact**: This isn't necessarily wrong — the Epic H research metrics are a valuable addition. But it means Zone 3 now does heavy lifting that may eventually belong in Zone 4 (Deep Analysis) or benefit from a different organizational pattern (e.g., expandable sections, tab groups, or a "see more metrics" approach).

#### 2. 3-Column Grid at 1/3 Width is Tight for Complex Charts

The `AnalysisLayout.vue` uses `grid-template-columns: repeat(3, 1fr)`. Each chart card gets ~33% of the content area width.

- **Simple charts (Winrate, CS)**: Work fine at 1/3 width — one line, no legend, clean
- **Complex charts (Dragon, Vision)**: At 1/3 width, the target line label, overall average label, legend, and dual datasets create visual cramping
- **Gold at 15**: The two-line legend ("Your Gold" / "Opponent Gold") compressed at 1/3 width is borderline

After applying the Part 1 recommendations (simplifying the three problem charts to single-line displays), this pressure reduces significantly. But it's still worth noting that 3 charts across a row is tighter than the original 2-column design.

#### 3. Missing LP Chart

The spec originally planned for an LP trend chart as one of the two Zone 3 charts. It's absent from the current implementation. The `LpChart` component is mentioned in the spec (Section 13) but doesn't appear in the solo page.

This may be intentional (LP is visible in `SummaryStatsCard` via rank badges), but the spec hasn't been updated to reflect this decision. LP over time is a strong motivation metric that answers "am I climbing?" differently from "am I winning?" (winrate).

#### 4. No Spec Conflicts with Zone 1 and Zone 2

- **Zone 1 (Context Bar)**: Correctly implements `BaseQueueToggle` (centered) + `BaseTimeRangeSelect` (right-aligned). Matches spec exactly.
- **Zone 2 (Summary)**: `SummaryStatsCard` with rank badges, games, winrate, KDA breakdown. Matches spec + Epic H1 enhancement. Well-implemented.

#### 5. Expand/Collapse Pattern Works but Isn't in the Spec

`TrendChartCard` has a "Last 20 / Full Season" toggle button. This is a good interaction pattern (default to last 20, expand for full season) that matches the spec's guidance:

> *"Charts default to last 20 games. Expand button switches to full season in-place (no modal)."* — Section 9

However, with 6 charts each having this toggle, that's 6 independent expand states. A user wanting to see all charts at full season must click 6 buttons. Consider whether a global expand toggle (at the Zone level or in the context bar) would reduce friction.

#### 6. Page Scroll Depth

With Zone 1 (~50px) + Zone 2 (~120px) + Zone 3 (6 charts at min-height 280px in 2 rows = ~580px), the total page height is approximately 750-800px. This fits in one viewport on most screens.

The UX spec says: *"Tool over website — speed and clarity over exploration."* The current layout respects this — the page is dense but not scrolly. The 3-column grid keeps vertical footprint low.

However, expanding any chart to full season adds data points and may increase chart height. The interaction of expanding multiple charts simultaneously could push the page beyond one viewport.

---

## Summary of Findings

### What's Right
- Metric selection perfectly matches research priorities (all Tier 1 + key Tier 2)
- Winrate, Gold at 15, and CS Per Minute charts have the right visual language
- SummaryStatsCard is well-implemented with rank badges and trend comparisons
- Context bar (Zone 1) matches spec exactly
- Expand/collapse per chart is a good default pattern
- Page fits in approximately one viewport

### What Clashes or Needs Attention

| Finding | Severity | Category |
|---------|----------|----------|
| Deaths/Dragon/Vision use dot-graph + rolling avg dual-dataset pattern that feels too technical | High | Charts vs UX Principles |
| These 3 charts should use the same single smooth line pattern as Winrate/Gold/CS | High | Visual Consistency |
| Target + overall average = too many annotation lines on Dragon and Vision | Medium | Visual Clutter |
| Zone 3 expanded from 2 charts to 6 without spec update | Low | Spec Drift |
| 3-column grid is tight for complex charts (improves when charts are simplified) | Medium | Layout |
| LP chart mentioned in spec but absent from implementation | Low | Spec Drift |
| 6 independent expand toggles — consider global toggle option | Low | Interaction Polish |

### Recommended Next Steps (Prioritized)

1. **Simplify the three problem charts** to match the visual pattern of the three liked charts (single smooth rolling average line, dots in tooltip only, drop legend). This is the highest-impact change.
2. **Reduce annotation lines**: Keep one reference line per chart (target line for Dragon/Vision, overall average for Deaths). Don't show both.
3. **Update the UI/UX spec** to reflect that Zone 3 now contains 6 research-based performance charts in a 3-column grid (not the original 2 charts in 2 columns).
4. **(Optional)** Consider reordering charts to follow the improvement narrative: Winrate → Deaths → Dragon → Vision → Gold → CS.
5. **(Future)** Add a global "Show Full Season" toggle in the context bar if users frequently want to expand all charts at once.
