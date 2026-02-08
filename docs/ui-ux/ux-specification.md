# League Helper App – One‑Pager UX Specification

## Purpose

Define a clear, implementation‑ready UX structure for the League of Legends helper app. This document focuses on **navigation, landing behavior, page responsibilities, component breakdowns, and design principles** to ensure a fast, tool‑like experience for ranked players, duos, and amateur teams.

---

## Target Users

* Casual ranked grinders (majority)
* Dedicated duos (especially Bot/Supp)
* Amateur teams / Clash players

**Usage context:**

* During Champion Select (high stress, low time)
* Between games (short attention bursts)
* After sessions (calm analysis)

Platform: **Desktop-first**, future Windows native app

---

## Core UX Principles

1. **Tool over website** – prioritize speed and clarity over exploration
2. **Context > Pages** – same underlying data, different perspectives (Solo/Duo/Team). Each context gets its own page and route for clearer perceived value and gating, but all share the same zone-based layout structure.
3. **Fast paths for stressed moments** – Champion Select and Match Review
4. **Overview is orientation, not work**
5. **Goals are horizontal** – visible everywhere, managed centrally
6. **Premium value appears early in the journey**, not deeper in navigation
7. **Every insight or graph must answer one question and imply one possible action**
8. **Single-match insights must be framed as part of a multi-game trend**

**Bias-aware rules to prevent common UX errors:**

* Power-user bias: Overview components must be understandable without expert knowledge
* Feature-parity bias: App does not aim to replicate global champion databases; focuses on personal/relational performance
* Mode-based thinking: Users enter flows by event (Match/Champion Select), not by mode selection
* Progress illusion: Graphs or stats without actionable interpretation must be hidden or secondary
* Survivorship bias: Track unused pages/components and post-loss behavior
* Optimism bias: Goals are surfaced passively; users can forget or ignore them
* Recency bias: Last match information is always shown with trend context

---

## Navigation Model

**Primary navigation:** Left-side vertical navigation

* Collapsible (icons + labels → icons only)
* Persistent across app
* Auto-collapses in Champion Select

### Navigation Structure

```
Overview
Champion Select
Matches
Solo
Duo          (Pro tier – lock icon for free users)
Team         (Pro tier – lock icon for free users)
Goals
User
```

> **Architecture decision:** Solo, Duo, and Team are **separate top-level pages** with distinct routes (`/app/solo`, `/app/duo`, `/app/team`), not tabs or toggles within a shared "Analysis" page. This was chosen for three reasons:
> 1. Better perceived value when upgrading ("You get Duo Analysis and Team Analysis" vs "You get two extra tabs")
> 2. Content will diverge significantly in v2 (Champion Matrix, team comp patterns)
> 3. Cleaner gating UX – locked page with preview/teaser is a well-understood pattern
>
> Duo and Team show a preview/teaser page with upgrade CTA for free users (not a 403 or blank wall).

---

## Landing Behavior

### Default (current implementation)

* **After login → Overview**

### Future (context-aware, optional)

Redirect before Overview when context is available:

1. Active Champion Select → Champion Select
2. Match just ended → Match Details (last match)
3. Otherwise → Overview

Overview must remain usable even when not the landing page.

---

## Page Responsibilities

### Overview (Summary / Orientation)

**Role:** Situational awareness and routing

**Time budget:** 5–15 seconds, one scroll max

#### Overview Component Breakdown (Exact Components)

> All components are **read‑only**, fast to render, and optimized for click‑through. No component owns business logic beyond formatting.

1. `<OverviewPlayerHeader>`

   * Props: `summonerName`, `level`, `region`, `profileIconUrl`, `activeContexts`
   * Shows profile icon, summoner name + region, context badges (Solo/Duo/Team)
   * Static, no interactions

2. `<RankSnapshot>`

   * Data scope: **Computed primary queue** (shown explicitly) + **Last 20 games** within that queue
   * Props (suggested):
     * `primaryQueueLabel` (e.g. "Ranked Solo/Duo", "Ranked Flex", "Normal Draft")
     * `rank`, `lp` (current)
     * `lpDeltaLast20` (current LP vs LP 20 games ago)
     * `last20Wins`, `last20Losses` (sampleSize is always 20 when available)
     * `lpDeltasLast20[]` (length 20; per-game LP change used for a micro bar-sparkline)
     * `wlLast20[]` (length 20; W/L strip)
   * Shows rank emblem, current LP, **ΔLP (Last 20)**, and winrate formatted as **`X% (W–L)`**
   * Micro-visuals (must remain overview-level; no analysis UI):
     * LP: tiny green/red bar sparkline using `lpDeltasLast20[]` (no axes, no hover required)
     * Winrate: tiny W/L strip using `wlLast20[]` (no smoothing/rolling winrate line)
   * Non-goals: axes, filters, time selectors, comparisons

   **Primary queue selection rule (computed upstream; RankSnapshot remains formatting-only):**
   * Determine `primaryQueue` as the queue with the **highest match count** in a recent window (recommended: last 50 matches or last 30 days).
   * Tie-breaker (in order): Ranked Solo/Duo → Ranked Flex → Normal Draft → ARAM → other.
   * Once selected, all "Last 20" metrics and sparklines are computed **within that primary queue only**.

3. `<LastMatchCard>`

   * Props: `matchId`, `championIconUrl`, `result`, `kda`, `timestamp`
   * Shows champion icon, win/loss, KDA, relative time
   * Click → `/matches/:matchId`

4. `<GoalProgressPreview>`

   * Props: array of goals (max 3)
   * Shows title, context, progress bar
   * CTA: "View all goals" → `/goals`
   * No editing, no sorting

5. `<SuggestedActions>`

   * Props: array of actions (max 3, sorted by priority)
   * Shows short human-readable suggestions with deep links

`<OverviewLayout>` (container)

* Single-column layout
* Enforce one-scroll maximum
* Handles loading/empty states
* Must keep `primaryQueueLabel` visible next to `<RankSnapshot>` (e.g. `Primary: Ranked Solo/Duo`) so the user always understands what the "Last 20" window refers to
* Must NOT include deep graphs, champion matrices, comparative analysis, or editable controls

---

### Champion Select (Play Mode)

**Role:** Real-time decision support

**Bias-aware rules:**

1. **One primary recommendation only** – avoid cognitive overload
2. **Communicate confidence, not certainty** – avoid absolute claims
3. **Frame all feedback as trends** – prevent tilt from last game
4. **Personal performance > global meta** – meta is secondary
5. **Limit visible choices to 2–3 champions** – avoid choice paralysis
6. **Support user intent first** – show data on currently hovered/locked pick
7. **Avoid predicting outcomes** – focus on preparation, not guaranteed win rates
8. **Scannable in <1 second** – icons, short labels, zero required reading
9. **No learning required** – all metrics immediately understandable

**Components:**

* `<ChampionSelectHeader>`: Summoner, context, remaining time
* `<PrimaryRecommendation>`: Champion, key stats, confidence label
* `<AlternateOptions>`: Hidden or secondary picks
* `<OpponentInsight>`: Key matchup warnings, visually minimal
* `<GoalReminder>`: Optional, shows relevant goal status
* Layout: Full-screen cockpit, side nav collapsed, high contrast, large typography

---

### Matches

**Role:** Review what just happened

* Match list, quick summaries
* Click to Match Details
* Primary entry into Analysis

### Match Details

**Role:** Immediate post-game understanding

* Timeline highlights, key stats, goal progress updates
* Frame all info in multi-game trends
* Links into Analysis

### Solo / Duo / Team (Analysis Pages)

**Role:** Long-term improvement tracking

Solo, Duo, and Team are **separate pages** sharing a common zone-based layout (`AnalysisLayout.vue`) filled with context-specific content.

**Zone model (shared layout):**

| Zone | Purpose | v1 | v2 |
|------|---------|----|-----|
| Zone 1 | Context bar (filters, time range) | Queue toggle + time range | Same |
| Zone 2 | Summary stats (lightweight context row) | Games, Winrate, KDA | Per-context stats |
| Zone 3 | Trend charts (2-column grid) | LP chart (left) + Winrate chart (right) | Same |
| Zone 4 | Deep analysis | Not rendered | Danger Zones, Champion Matrix |
| Zone 5 | Goals | Not rendered | Active goals with progress |

**Chart behavior:**
* Charts default to **last 20 games** for quick momentum reading ("Am I climbing right now?")
* Expand button switches data range to full season **within the same half-width space** (no modal, no full-width expansion)
* Both LP and Winrate charts are side-by-side in a 2-column grid (half-width each)

**"View Analysis" from Matches:**
* Match Details page includes a "View Analysis" button
* Navigates to the relevant Analysis page with a `matchId` query parameter
* Charts highlight the specific game point for contextual analysis

**Context-specific content:**
* **Solo:** Personal stats, LP trend, winrate trend. v2: Danger Zones (personal death heatmap)
* **Duo:** Pair stats, shared LP/winrate trends. v2: Champion Matrix (champion combo synergy), Danger Zones (two players in different colors)
* **Team:** Team stats, winrate trends. v2: Team comp patterns, Danger Zones (all players in different colors)

**Gating:**
* Solo: Free tier, always accessible
* Duo: Pro tier – free users see a preview/teaser page with feature description and upgrade CTA
* Team: Pro tier – same pattern as Duo

* Trend-based graphs (must answer one question and imply one action)
* Non-goals: global champion coverage, encyclopedic data

### Goals

**Role:** Central goal management

* Create/edit/archive goals
* Filter by context (Solo/Duo/Team)
* Long-term progress views
* Displayed elsewhere but managed centrally
* Always assume users forget or partially care

### User

* Account(profilepicture, username, email, password, tier, subscription status), preferences,

---

## Design Constraints (Non‑Negotiable)

* Champion Select reachable in one click
* Overview never blocks user flow
* No duplicated deep analysis across pages
* Context (Solo/Duo/Team) always visible via separate sidebar entries; Duo/Team show lock icon for free users
* Navigation hierarchy remains stable
* Every chart/stat must have actionable meaning
* Single-match insights framed as trends
* Premium features appear early
* Champion Select is scannable and requires no learning

---

## Measurement & Validation

* Track page entry frequency
* Track unused components
* Track post-loss behavior
* Analyze whether recommendations are being clicked/followed

---

**This document defines the UX contract. UI details and visual design should serve these rules, not override them. All decisions are bias-aware to prevent common pitfalls in stressful, fast-paced, and emotionally loaded contexts like League of Legends Champion Select.**
