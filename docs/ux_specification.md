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
2. **Context > Pages** – same data, different perspectives (Solo/Duo/Team)
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
Analysis
  ├─ Solo
  ├─ Duo
  └─ Team
Goals
Settings
```

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

   * Props: `summonerName`, `region`, `profileIconUrl`, `activeContexts`
   * Shows profile icon, summoner name + region, context badges (Solo/Duo/Team)
   * Static, no interactions

2. `<RankSnapshot>`

   * Props: `rank`, `lp`, `rankDelta`, `winrate`, `sampleSize`
   * Shows rank emblem, LP, delta indicator, winrate
   * No charts, no time selectors

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

### Analysis (Shared Layout)

**Role:** Long-term improvement

* Single layout reused across Solo/Duo/Team
* Trend-based graphs (must answer one question and imply one action)
* Context-specific widgets
* Embedded goal progress
* Duo/Team selection via dropdown inside page
* Navigation items remain stable
* Non-goals: global champion coverage, encyclopedic data

### Goals

**Role:** Central goal management

* Create/edit/archive goals
* Filter by context (Solo/Duo/Team)
* Long-term progress views
* Displayed elsewhere but managed centrally
* Always assume users forget or partially care

### Settings

* Account, preferences, integrations

---

## Design Constraints (Non‑Negotiable)

* Champion Select reachable in one click
* Overview never blocks user flow
* No duplicated deep analysis across pages
* Context (Solo/Duo/Team) always visible
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
