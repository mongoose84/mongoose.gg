# Mongoose.gg — UI/UX Specification

> **Purpose**: Single-source-of-truth for AI agents and developers building frontend features. Contains the design system (tokens, components, patterns), UX contracts (navigation, page responsibilities, bias rules), and the complete component inventory with props/slots.

**Stack**: Vue 3 (Composition API, `<script setup>`) · Tailwind CSS · Headless UI · Heroicons · Chart.js + vue-chartjs · TanStack Vue Query · Pinia  
**Theme**: Vercel Developer aesthetic adapted for gaming — dark, technical, premium  
**Platform**: Desktop-first (future Windows native app)  
**Last verified**: February 15, 2026

---

## Table of Contents

1. [Design Philosophy & UX Principles](#1-design-philosophy--ux-principles)
2. [Design Tokens (CSS Variables)](#2-design-tokens-css-variables)
3. [Typography](#3-typography)
4. [Spacing, Radius & Shadows](#4-spacing-radius--shadows)
5. [Tailwind Integration](#5-tailwind-integration)
6. [Navigation Model](#6-navigation-model)
7. [Route Map](#7-route-map)
8. [Layout Architecture](#8-layout-architecture)
9. [Page Responsibilities](#9-page-responsibilities)
10. [Base Components](#10-base-components)
11. [Overview Components](#11-overview-components)
12. [Match Components](#12-match-components)
13. [Solo Analysis Components](#13-solo-analysis-components)
14. [Shared Components](#14-shared-components)
15. [Root-Level Components](#15-root-level-components)
16. [Composables](#16-composables)
17. [Stores](#17-stores)
18. [Services & API Client](#18-services--api-client)
19. [Utilities](#19-utilities)
20. [Component Patterns (CSS)](#20-component-patterns-css)
21. [Animations & Transitions](#21-animations--transitions)
22. [Accessibility](#22-accessibility)
23. [Z-Index Scale](#23-z-index-scale)
24. [Icons](#24-icons)
25. [Win Rate Color System](#25-win-rate-color-system)
26. [Bias-Aware UX Rules](#26-bias-aware-ux-rules)
27. [Design Constraints (Non-Negotiable)](#27-design-constraints-non-negotiable)
28. [New Component Checklist](#28-new-component-checklist)

---

## 1. Design Philosophy & UX Principles

**Theme**: Dark-first gaming aesthetic with Vercel-inspired technical precision. Background image (`/hero-bg.svg`, fixed cover) overlaid with dark gradient (`rgba(0,0,0,0.30)`) via `body::before`.

**Core Principles**:
1. **Tool over website** — speed and clarity over exploration
2. **Context > Pages** — same data, different perspectives (Solo/Team); each gets its own route
3. **Fast paths for stressed moments** — Champion Select and Match Review must load instantly
4. **Overview is orientation, not work** — 5–15 seconds, one scroll max
5. **Goals are horizontal** — visible everywhere, managed centrally
6. **Premium value appears early** — not buried deep in navigation
7. **Every insight answers one question and implies one action**
8. **Single-match insights framed as multi-game trends**

**Target Users**:
- Casual ranked grinders (majority)
- Dedicated duos (Bot/Supp)
- Amateur teams / Clash players

**Usage Contexts**:
- During Champion Select (high stress, low time)
- Between games (short attention bursts)
- After sessions (calm analysis)

---

## 2. Design Tokens (CSS Variables)

Defined in `client/src/style.css`. All components MUST use these tokens — never hardcode colors or sizes.

### Core Colors

| Token | Value | Usage |
|-------|-------|-------|
| `--color-primary` | `#6d28d9` | Primary actions, links, focus states, accents |
| `--color-primary-soft` | `rgba(109, 40, 217, 0.1)` | Hover backgrounds, subtle highlights |
| `--color-primary-dark` | `#5b21b6` | Darker primary variant |
| `--color-primary-light` | `#7c3aed` | Lighter primary variant |
| `--color-primary-accent` | `#a855f7` | Accent/highlight variant |
| `--color-bg` | `#000000` | Page background |
| `--color-surface` | `rgba(255, 255, 255, 0.03)` | Cards, panels, elevated containers |
| `--color-elevated` | `rgba(255, 255, 255, 0.05)` | Nested elevated elements |
| `--color-text` | `#ffffff` | Primary text |
| `--color-text-secondary` | `#888888` | Secondary/muted text |
| `--color-border` | `rgba(109, 40, 217, 0.15)` | Borders, dividers |

### Semantic Colors

| Token | Value | Soft | Border | Usage |
|-------|-------|------|--------|-------|
| `--color-success` | `#22c55e` | `rgba(34,197,94,0.1)` | `rgba(34,197,94,0.3)` | Wins, positive |
| `--color-error` | `#ef4444` | `rgba(239,68,68,0.1)` | `rgba(239,68,68,0.3)` | Losses, errors |
| `--color-warning` | `#f59e0b` | `rgba(245,158,11,0.1)` | `rgba(245,158,11,0.3)` | Cautions, pending |
| `--color-info` | `#3b82f6` | `rgba(59,130,246,0.1)` | `rgba(59,130,246,0.3)` | Informational |
| `--color-muted` | `#6b7280` | `rgba(107,114,128,0.2)` | — | Neutral/muted |

### Win Rate Gradient Colors

| Token | Color | Threshold |
|-------|-------|-----------|
| `--color-winrate-terrible` | `#ef4444` | < 40% |
| `--color-winrate-bad` | `#f97316` | 40–45% |
| `--color-winrate-poor` | `#fdba74` | 45–48% |
| `--color-winrate-average` | `#eab308` | 48–52% |
| `--color-winrate-good` | `#84cc16` | 52–55% |
| `--color-winrate-great` | `#22c55e` | > 55% |

---

## 3. Typography

**Font**: `'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif`  
**Import**: `@import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800;900&display=swap')`  
**Letter spacing**: `-0.015em` (tight tracking for modern look)

### Font Sizes

| Token | Size | Line Height | Usage |
|-------|------|-------------|-------|
| `--font-size-xs` | `0.75rem` (12px) | 1.5 | Labels, badges, captions |
| `--font-size-sm` | `0.875rem` (14px) | 1.5 | Secondary text, form labels |
| `--font-size-md` | `1rem` (16px) | 1.6 | Body text, inputs |
| `--font-size-lg` | `1.125rem` (18px) | 1.6 | Subheadings |
| `--font-size-xl` | `1.5rem` (24px) | 1.4 | Section titles |
| `--font-size-2xl` | `2.5rem` (40px) | 1.2 | Page titles, hero text |

Tailwind also extends with: `text-4xs` (8px), `text-3xs` (9px), `text-2xs` (10px).

### Font Weights

| Token | Value | Usage |
|-------|-------|-------|
| `--font-weight-normal` | `400` | Body text |
| `--font-weight-medium` | `500` | Default weight, labels |
| `--font-weight-semibold` | `600` | Buttons, emphasis |
| `--font-weight-bold` | `700` | Headings, strong emphasis |

---

## 4. Spacing, Radius & Shadows

### Spacing Scale

| Token | Size | Usage |
|-------|------|-------|
| `--spacing-xs` | `0.5rem` (8px) | Tight gaps, inline spacing |
| `--spacing-sm` | `0.75rem` (12px) | Form element gaps |
| `--spacing-md` | `1rem` (16px) | Standard padding, gaps |
| `--spacing-lg` | `1.5rem` (24px) | Section spacing |
| `--spacing-xl` | `2rem` (32px) | Large section padding |
| `--spacing-2xl` | `3rem` (48px) | Major section breaks |

### Border Radius

| Token | Size | Usage |
|-------|------|-------|
| `--radius-sm` | `0.375rem` (6px) | Badges, small elements |
| `--radius-md` | `0.5rem` (8px) | Buttons, inputs, cards |
| `--radius-lg` | `0.75rem` (12px) | Modals, large cards |

### Shadows

| Token | Value | Usage |
|-------|-------|-------|
| `--shadow-sm` | `0 2px 8px rgba(0,0,0,0.5)` | Subtle elevation |
| `--shadow-md` | `0 8px 30px rgba(109,40,217,0.15)` | Cards, buttons on hover |
| `--shadow-lg` | `0 20px 60px rgba(109,40,217,0.25)` | Modals, dropdowns |

---

## 5. Tailwind Integration

Design tokens are bridged into Tailwind via `client/tailwind.config.js`. CSS variables are the source of truth; Tailwind consumes them.

```js
// Tailwind class → CSS variable mapping
bg-primary         → var(--color-primary)
bg-background-surface → var(--color-surface)
text-text-secondary → var(--color-text-secondary)
border-border      → var(--color-border)
text-success       → var(--color-success)
text-error         → var(--color-error)
rounded-md         → var(--radius-md)
shadow-md          → var(--shadow-md)
p-md               → var(--spacing-md)
tracking-tight     → var(--letter-spacing)
```

**Rule**: Use CSS variables for colors/typography/spacing. Use Tailwind for layout utilities (flex, grid, gap, positioning).

---

## 6. Navigation Model

**Primary navigation**: Left-side vertical sidebar (`AppSidebar.vue`)
- Collapsible: icons + labels → icons only
- Persistent across all `/app/*` routes
- Auto-collapses in Champion Select
- State persisted to `localStorage` via `uiStore`
- Mobile breakpoint: `1024px` (auto-collapse)

### Sidebar Entries

```
Overview         → /app/overview
Champion Select  → /app/champion-select
Matches          → /app/matches
Solo             → /app/solo
Team             → /app/team      (Pro tier — lock icon for free users)
Goals            → /app/goals
User             → /app/user
```

**Architecture decision**: Solo and Team are **separate top-level pages** (not tabs) for:
1. Better upgrade perceived value
2. Content diverges significantly in v2
3. Cleaner gating UX — locked page with preview/teaser

---

## 7. Route Map

All routes defined in `client/src/router/index.js`.

### Public Routes (no auth)

| Route | View | Notes |
|-------|------|-------|
| `/` | `LandingPage.vue` | Marketing page with NavBar |
| `/auth` | `AuthPage.vue` | Login/register/forgot-password, `?mode=login\|register` |
| `/auth/reset-password` | `ResetPasswordPage.vue` | Code + new password, `?email=` pre-fills |
| `/privacy` | `PrivacyPage.vue` | Static legal |
| `/terms` | `TermsPage.vue` | Static legal |

### Auth-Required Routes

| Route | View | Notes |
|-------|------|-------|
| `/auth/verify` | `VerifyPage.vue` | 6-digit email verification; auto-submit |

### App Routes (auth + verified, inside `AppLayout`)

| Route | Name | View | Tier |
|-------|------|------|------|
| `/app/overview` | `app-overview` | `OverviewPage.vue` | Free |
| `/app/champion-select` | `app-champion-select` | `ChampionSelectPage.vue` | Free |
| `/app/matches` | `app-matches` | `MatchesPage.vue` | Free |
| `/app/solo` | `app-solo` | `SoloStatsPage.vue` | Free |
| `/app/team` | `app-team` | `TeamAnalytics.vue` | Pro |
| `/app/goals` | `app-goals` | `GoalsPage.vue` | Free |
| `/app/user` | `app-user` | `UserSettingsPage.vue` | Free |
| `/app/feedback` | `app-feedback` | `FeedbackPage.vue` | Free |

### Navigation Guards

1. `requiresAuth` — redirects to `/auth?mode=login&redirect={path}`
2. `requiresVerified` — redirects to `/auth/verify`
3. Verified users auto-redirected away from verify page
4. All navigations fire `trackPageView()` analytics

### Legacy Redirects
- `/v2/auth` → `/auth`
- `/v2/app/solo` → `/app/solo`

---

## 8. Layout Architecture

### `AppLayout.vue` (authenticated shell)
- **Structure**: `AppSidebar` (fixed left) + `<router-view>` (flex-1 main content)
- **Sidebar width**: Dynamic from `uiStore.sidebarWidth`; content uses `margin-left` with CSS transition
- **Idle detection**: 30-minute threshold; on tab return, refreshes user data + triggers sync check
- **Activity tracking**: Throttled to 30s intervals (mousemove, keydown, click, scroll)

### `OverviewLayout.vue` (overview page container)
- Named slots: `#header`, `#glance-left`, `#glance-right`, `#recent-left`, `#recent-right`, `#latest-match`, `#empty-action`
- Handles loading (spinner), error (retry), empty (link account CTA) states
- Single-column layout, one-scroll max

### `AnalysisLayout.vue` (shared by Solo/Team)
- Zone-based layout with named slots:
  - `#context-bar` — Zone 1: Filters (queue toggle, time range)
  - `#summary` — Zone 2: Summary stats row
  - `#trend-charts` — Zone 3: 2-column chart grid
  - Zone 4 (deep analysis) and Zone 5 (goals) — not rendered in v1
- Prop: `pageTitle`

### `NavBar.vue` (public pages)
- Fixed top, glassmorphism (`rgba(0,0,0,0.8)` + `backdrop-blur-[12px]`)
- Desktop: Features/Pricing/How It Works/Login + "Get Started" CTA
- Mobile: hamburger toggle with slide-down animation
- Logo links to `/app/user` if authenticated, `/` if not

---

## 9. Page Responsibilities

### Overview (`/app/overview`)
**Role**: Situational awareness and routing. Time budget: 5–15 seconds.

Components used:
- `OverviewPlayerHeader` — profile icon, summoner name, region, context badges
- `RankSnapshot` — rank, LP, ΔLP last 20, W/L strip
- `ChampionSelectCTA` — quick link to champion select
- `MatchActivityHeatmap` — daily match counts grid
- `AnalysisStatusCard` — sync/analysis status
- `LastMatchCard` — last match summary, click → match details

Data sources: `getOverview()`, `getMatchActivity()` from `authApi`

**Non-goals**: Deep graphs, champion matrices, comparative analysis, editable controls.

### Champion Select (`/app/champion-select`)
**Role**: Real-time decision support during pick/ban phase.

**Scannable in < 1 second rules**:
1. One primary recommendation only
2. Communicate confidence, not certainty
3. Frame feedback as trends, not last-game reactions
4. Personal performance > global meta
5. Limit visible choices to 2–3 champions
6. Support user intent first (show data for hovered/locked pick)
7. No learning required — icons, short labels, zero required reading

Components: `ChampionMatchupsTable`, `OpponentSearchBar`, `MainChampionCard`

### Matches (`/app/matches`)
**Role**: Review what just happened. Match list with quick summaries.

Components: `MatchList` → `MatchRow` items → click expands `MatchDetails` with:
- `MatchHeader` — champion, result, KDA, timestamp, queue
- `MatchHighlights` — 4 key stat tiles (`HighlightTile`)
- `MatchNarrative` — AI-generated match story
- `StatSnapshot` — detailed stat breakdown
- `ImpactStats` — role-aware impact metrics (support vs non-support)
- `LaneMatchupDetails` — laning phase stats + AI insight
- `TeamComparison` — team damage/gold/objectives comparison
- `MatchActions` — navigation to analysis pages
- `TrendBadge` — inline trend indicators (↑/↓)

### Solo (`/app/solo`)
**Role**: Long-term personal improvement tracking. Free tier.

Zone layout via `AnalysisLayout`:
- Zone 1: `BaseQueueToggle` (centered) + `BaseTimeRangeSelect` (right-aligned)
- Zone 2: `SummaryStatsCard` — games played, win rate, average KDA (with overall comparisons)
- Zone 3: `TrendChartCard` — Winrate trend (rolling 20-game)

Charts default to last 20 games. Expand button switches to full season in-place (no modal).

Data sources: `getSoloDashboard()`, `getWinrateTrend()` from `authApi`

### Team (`/app/team`) — Pro tier
**Role**: Team performance analysis.

v2 additions: Team comp patterns, Danger Zones (all players, different colors).

### Goals (`/app/goals`)
**Role**: Central goal management. Create/edit/archive. Filter by context (Solo/Team).

### User Settings (`/app/user`)
**Role**: Account management — profile, email, password, tier, subscription, Riot account linking.

Components: `DeleteAccountModal`, `LinkRiotAccountModal`

### Feedback (`/app/feedback`)
**Role**: Bug reports / feature requests. Captures browser/OS context, referrer route.

### Auth (`/auth`)
**Role**: Login/register/forgot-password toggle. `?mode=login|register&redirect={path}`

Forgot-password is a third form state: email input → submit → redirect to reset page. "Forgot password?" link visible in login mode. Back link returns to login.

### Reset Password (`/auth/reset-password`)
**Role**: Consume 6-digit reset code + set new password. Public route, no auth required.

Pre-fills email from `?email=` query param. Code input uses same monospace `tracking-[0.5em]` pattern as VerifyPage. Submit disabled until code is 6 digits and password ≥ 8 chars. On success redirects to `/auth?mode=login`.

### Verify (`/auth/verify`)
**Role**: 6-digit email verification. Auto-submit on completion. Resend cooldown (60s, server-controlled).

---

## 10. Base Components

Located in `client/src/components/base/`. Exported via `index.js` barrel.

### `BaseButton`
Flexible button with router-link support.

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `variant` | `String` | `'primary'` | `primary`, `secondary`, `ghost`, `destructive` |
| `size` | `String` | `'md'` | `sm`, `md`, `lg` |
| `loading` | `Boolean` | `false` | Shows spinner, disables click |
| `disabled` | `Boolean` | `false` | Grayed out, no interaction |
| `to` | `String\|Object` | — | Router-link target |

### `BaseCard`
Container with header, body, footer slots.

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `title` | `String` | — | Card header text |
| `variant` | `String` | `'default'` | `default`, `interactive`, `highlighted`, `elevated` |

Slots: `default`, `#header`, `#footer`

### `BaseModal`
Accessible modal via Headless UI `Dialog`.

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `isOpen` | `Boolean` | required | Controls visibility |
| `title` | `String` | — | Dialog title |
| `size` | `String` | `'md'` | `sm`, `md`, `lg`, `xl`, `full` |

Events: `@close`  
Slots: `default`, `#footer`

### `BaseInput`
Form input with label, validation, and icon support.

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `modelValue` | `String` | — | v-model binding |
| `label` | `String` | — | Field label |
| `error` | `String` | — | Error message text |
| `type` | `String` | `'text'` | Input type |
| `placeholder` | `String` | — | Placeholder text |

### `BaseQueueToggle`
Queue filter toggle (Ranked Solo/Duo, Ranked Flex, All).

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `modelValue` | `String` | — | v-model binding for selected queue |

### `BaseTimeRangeSelect`
Time range dropdown (Last 20, Last 50, Season, etc.).

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `modelValue` | `String` | — | v-model binding for selected range |

---

## 11. Overview Components

Located in `client/src/components/overview/`.

### `OverviewLayout`
Page-level container with named slots and state management.

| Prop | Type | Description |
|------|------|-------------|
| `isLoading` | `Boolean` | Shows loading spinner |
| `error` | `String` | Shows error with retry button |
| `isEmpty` | `Boolean` | Shows empty state with link-account CTA |

Events: `@retry`  
Slots: `#header`, `#glance-left`, `#glance-right`, `#recent-left`, `#recent-right`, `#latest-match`, `#empty-action`

### `OverviewPlayerHeader`

| Prop | Type | Description |
|------|------|-------------|
| `summonerName` | `String` | Display name |
| `level` | `Number` | Summoner level |
| `region` | `String` | Server region |
| `profileIconUrl` | `String` | Profile icon URL |
| `activeContexts` | `Array` | Context badges (Solo/Team) |

### `RankSnapshot`

| Prop | Type | Description |
|------|------|-------------|
| `primaryQueueLabel` | `String` | e.g., "Ranked Solo/Duo" |
| `rank` | `String` | Tier + division |
| `lp` | `Number` | Current LP |
| `lpDeltaLast20` | `Number` | LP change over last 20 games |
| `last20Wins` | `Number` | Wins in last 20 |
| `last20Losses` | `Number` | Losses in last 20 |
| `wlLast20` | `Array` | Per-game W/L array for strip visualization |

**Primary queue selection rule** (computed upstream): Queue with highest match count in recent window (last 50 or 30 days). Tie-breaker: Solo/Duo → Flex → Normal → ARAM → other.

### `LastMatchCard`

| Prop | Type | Description |
|------|------|-------------|
| `matchId` | `String` | Match identifier |
| `championIconUrl` | `String` | Champion icon |
| `championName` | `String` | Champion name |
| `result` | `String` | Win/Loss |
| `kda` | `String` | KDA string |
| `timestamp` | `String` | Match time |
| `queueType` | `String` | Queue type label |

Click navigates to `/app/matches/:matchId`.

### `ChampionSelectCTA`
Static call-to-action linking to champion select page. No props.

### `MatchActivityHeatmap`

| Prop | Type | Description |
|------|------|-------------|
| `dailyMatchCounts` | `Array` | Per-day match count data |
| `startDate` | `String` | Heatmap start date |
| `endDate` | `String` | Heatmap end date |
| `totalMatches` | `Number` | Total match count in period |

### `AnalysisStatusCard`
Shows current sync/analysis status. No props (reads from store/composable internally).

---

## 12. Match Components

Located in `client/src/components/matches/`.

### `MatchList`
Scrollable match list container. Fetches via `getMatchList()`.

### `MatchRow`
Single match row in list. Shows champion icon, result, KDA, timestamp, queue. Click expands details.

### `MatchDetails`
Expanded match view containing all sub-components below.

### `MatchHeader`
Champion, result, KDA, timestamp, queue display.

### `MatchHighlights`
2×2 grid of `HighlightTile` components showing top 4 match stats.

### `HighlightTile`
Card with icon + stat name + insight text + trend indicator. 5 built-in SVG icons: `damage`, `kda`, `cs`, `vision`, `chart`.

### `MatchNarrative`
AI-generated match story text. Fetched via `getMatchNarrative()`.

### `StatSnapshot`
Detailed stat breakdown grid.

### `ImpactStats`
3-column impact grid with **role-aware metrics**:
- **Support**: Kill Participation, Gold @15, Vision/min
- **Non-Support**: Kill Participation, Gold @15, Dmg/Gold efficiency

Color-coded with sentiment borders (positive green / negative red).

### `LaneMatchupDetails`
Two-phase display:
1. **Early Laning** (0–10m): gold diff bar, CS diff, deaths
2. **Game Impact**: damage share, KP, vision

Includes AI-generated matchup insight text.

### `TeamComparison`
Team damage comparison bars (ally vs enemy), gold lead @15, objective counts (dragons/barons/towers). Uses Community Dragon CDN for icons.

### `TrendBadge`
Inline badge with ↑/↓ arrows. Props: `{ text, type, stat }` badge object. Types: positive (green), negative (red), neutral (gray).

### `MatchActions`
Navigation links from match detail to relevant analysis pages.

---

## 13. Solo Analysis Components

Located in `client/src/components/solo/`.

### `SummaryStatsCard`

| Prop | Type | Description |
|------|------|-------------|
| `gamesPlayed` | `Number` | Total games in filter window |
| `winRate` | `Number\|null` | Filtered win rate |
| `overallWinRate` | `Number\|null` | Season-wide win rate (comparison) |
| `avgKda` | `Number\|null` | Filtered average KDA ratio |
| `avgKills` / `avgDeaths` / `avgAssists` | `Number\|null` | Filtered averages |
| `overallAvgKills` / `overallAvgDeaths` / `overallAvgAssists` | `Number\|null` | Season-wide averages |
| `overallAvgKda` | `Number\|null` | Season-wide KDA |
| `loading` | `Boolean` | Loading state |

### `TrendChartCard`
Wrapper for trend charts with expand/collapse.

| Prop | Type | Description |
|------|------|-------------|
| `title` | `String` | Chart title |
| `subtitle` | `String` | Optional subtitle |
| `loading` | `Boolean` | Loading state |
| `testId` | `String` | data-testid for testing |

Events: `@toggle-expand`  
Slots: `#default` with `{ dataLimit }` slot prop

### `LpChart`
Chart.js line chart for LP over time. Prop: `data` (array of LP data points).

### `WinrateChart`
Chart.js line chart for rolling win rate. Prop: `data` (array of win rate data points). Subtitle: "Rolling 20-game average".

---

## 14. Shared Components

### `AnalysisLayout` (`client/src/components/shared/`)
Zone-based layout used by Solo and Team pages.

| Prop | Type | Description |
|------|------|-------------|
| `pageTitle` | `String` | Page heading |

Slots: `#context-bar`, `#summary`, `#trend-charts`

**Zone model**:

| Zone | Slot | Purpose | v1 | v2 |
|------|------|---------|-----|-----|
| 1 | `#context-bar` | Filters (queue + time) | Queue toggle + time range | Same |
| 2 | `#summary` | Summary stats row | Games, Winrate, KDA | Per-context stats |
| 3 | `#trend-charts` | 2-column chart grid | LP + Winrate charts | Same |
| 4 | — | Deep analysis | Not rendered | Danger Zones, Champion Matrix |
| 5 | — | Goals | Not rendered | Active goals with progress |

---

## 15. Root-Level Components

Located in `client/src/components/`.

### `AppSidebar`
Vertical navigation sidebar. Reads collapsed state from `uiStore`. Shows lock icons for Pro-tier pages (Duo, Team) when user is free tier.

### `AppHeader`
Header component (used within app layout context).

### `SessionExpiredBanner`
Fixed top banner (z-index 400) with slide-down transition. Appears on 401 detection. Preserves current route for redirect after re-login.

### `LinkRiotAccountModal`
BaseModal with: Game Name, Tag Line (3–5 chars, alphanumeric), Region select (16 regions). Error mapping: `RIOT_ACCOUNT_NOT_FOUND`, `ACCOUNT_ALREADY_LINKED`. Resets form on open.

### `DeleteAccountModal`
BaseModal requiring "DELETE" confirmation text + password. Prevents close during deletion. Uses `destructive` button variant.

### `MainChampionCard`
Champion detail card with stat bars, M-Score tooltip, matchup tooltips. Responsive breakpoints: 1024px (stat labels), 768px (single column).

### `ChampionMatchupsTable`
Table of champion matchup data for champion select context.

### `OpponentSearchBar`
Search input for looking up opponent data in champion select.

### `WinrateChart`
Root-level winrate chart variant.

### `VersionBadge`
Fixed bottom-left badge: "Mongoose.gg Beta • v{version}". Hidden inside `/app` routes.

---

## 16. Composables

Located in `client/src/composables/`.

### `useWinRateColor()`
Returns CSS class for win rate value based on threshold ranges. Maps to `winrate-terrible` through `winrate-great` CSS classes.

### `useSyncWebSocket()`
SignalR WebSocket connection to `/ws/sync`. Provides:
- `syncProgress` — reactive sync progress data
- `subscribe()` — connect to sync updates
- `resetProgress()` — clear sync state

Used by `OverviewPage` and `SoloPage` to reactively update after match sync completes.

### `useAnalysisStatus()`
Tracks analysis/sync status for display in `AnalysisStatusCard`.

---

## 17. Stores

Located in `client/src/stores/`. Using **Pinia**.

### `authStore`
- **State**: user object, session expiry tracking (`wasAuthenticated` pattern)
- **Actions**: `initialize()`, `login()`, `register()`, `verify()`, `logout()`, `changePassword()`, `linkRiotAccount()`, `unlinkRiotAccount()`, `triggerSync()`, `refreshUser()`
- **Computed**: `isAuthenticated`, `isVerified`, `isInitialized`, `username`, `email`, `tier`, `primaryRiotAccount`

### `uiStore`
- **State**: sidebar collapsed (persisted to `localStorage`), mobile breakpoint (1024px)
- **Computed**: `sidebarWidth` — auto-collapse on small screens

---

## 18. Services & API Client

Located in `client/src/services/`.

### `apiConfig.js`
- Dev: `http://localhost:5164`
- Prod: `https://api.mongoose.gg`
- Version prefix: `/api/v2`

### `apiClient.js`
Centralized fetch wrapper with:
- Cookie-based auth (`credentials: 'include'`)
- Global 401 session expiry detection with configurable callback
- Methods: `get()`, `post()`, `del()`
- `parseResponse()` with structured error codes

### `authApi.js` (main API surface — 505 lines)
- **Auth**: register, login, logout, deleteAccount, verifyEmail, resendVerification, forgotPassword, resetPassword, changePassword
- **Riot account**: link, unlink, triggerSync, getSyncStatus
- **Dashboards**: `getOverview()`, `getSoloDashboard()`, `getChampionSelectData()`, `getMatchActivity()`
- **Trends**: `getWinrateTrend()`
- **Matchups**: `getChampionMatchups()`
- **Matches**: `getMatchList()`, `getMatchDetails()`, `getMatchNarrative()`
- **Public**: `getPublicStats()`

### `analyticsApi.js`
Fire-and-forget event tracking with session ID. Events: page view, auth, nav click, filter change, feature usage, upgrade flow, match analytics.

### `feedbackApi.js`
Browser/OS detection, environment context capture, submit via `apiClient.post`.

---

## 19. Utilities

Located in `client/src/utils/`.

### `formatters.js` (233 lines)
- **Role**: `formatRole()`, `formatRoleWithAdc()`
- **Time**: `formatDuration()`, `formatRelativeTime(short|long)`, `formatDate()`
- **Numbers**: `formatNumber()` (K suffix), `formatWinRate()`, `formatPercent()`, `formatLpPerGame()`, `formatGoldDiff()`, `formatCsDiff()`
- **KDA**: `formatKda()`, `formatKdaFromParticipant()`, `calculateKdaRatio()`

### `leagueAssets.js`
CDN helpers for League of Legends assets:
- Data Dragon v16.1.1 + Community Dragon CDN
- `getChampionIconUrl()`, `getRoleIconUrl()`, `getProfileIconUrl()`, `getItemIconUrl()`, `getSummonerSpellIconUrl()`
- `normalizeChampionName()` — strips special chars for URL safety

---

## 20. Component Patterns (CSS)

### Cards
```css
background: var(--color-surface);
border: 1px solid var(--color-border);
border-radius: var(--radius-lg);
padding: var(--spacing-xl);
backdrop-filter: blur(10px);
```

### Primary Button
```css
background: var(--color-primary);
color: white;
padding: var(--spacing-md);
border-radius: var(--radius-md);
font-weight: var(--font-weight-semibold);
transition: all 0.2s;
box-shadow: var(--shadow-sm);
/* Hover: shadow-md + translateY(-2px) */
/* Disabled: opacity 0.6, cursor not-allowed */
```

### Ghost Button
```css
background: transparent;
color: var(--color-primary);
border: 1px solid var(--color-border);
/* Hover: border-color primary, bg primary-soft */
```

### Form Inputs
```css
padding: var(--spacing-md);
background: var(--color-bg);
border: 1px solid var(--color-border);
border-radius: var(--radius-md);
color: var(--color-text);
/* Focus: border primary, box-shadow 0 0 0 3px primary-soft */
/* Error: border #ef4444, box-shadow rgba(239,68,68,0.2) */
```

### Dropdowns
```css
background: var(--color-surface);
border: 1px solid var(--color-border);
border-radius: var(--radius-md);
box-shadow: var(--shadow-lg);
/* Items: hover bg elevated */
/* Divider: 1px border color */
```

---

## 21. Animations & Transitions

### Standard Transition
```css
transition: all 0.2s ease;
```

### Hover Lift Effect
```css
:hover {
  transform: translateY(-2px);
  box-shadow: var(--shadow-md);
}
```

### Dropdown Animation
```css
.dropdown-enter-from, .dropdown-leave-to {
  opacity: 0;
  transform: translateY(-8px);
}
```

### Loading Spinner
```css
.spinner {
  width: 16px; height: 16px;
  border: 2px solid rgba(255,255,255,0.3);
  border-radius: 50%;
  border-top-color: white;
  animation: spin 0.8s linear infinite;
}
```

---

## 22. Accessibility

- **Focus states**: Always visible — `box-shadow: 0 0 0 3px var(--color-primary-soft)`
- **Color contrast**: 4.5:1 ratio minimum for text on backgrounds
- **Disabled states**: `opacity: 0.6` + `cursor: not-allowed`
- **Touch targets**: Minimum 44×44px on mobile
- **Screen reader**: `.visually-hidden` utility class (position absolute, 1×1px, overflow hidden)
- **Headless UI**: Used for all complex interactive components (Dialog, Menu, Transition) for built-in ARIA support
- **Semantic HTML**: Required — use proper heading hierarchy, button vs link distinction, form labels

---

## 23. Z-Index Scale

| Layer | Z-Index | Usage |
|-------|---------|-------|
| Background | 0 | Background image/overlay |
| Content | 1 | Main app content (`#app`) |
| Header | 100 | Fixed navigation |
| Dropdowns | 200 | Menus, popovers |
| Modals | 300 | Modal dialogs (Headless UI Dialog) |
| Toasts/Banners | 400 | SessionExpiredBanner, notifications |

---

## 24. Icons

Use [Heroicons](https://heroicons.com/) via `@heroicons/vue`:

```js
import { UserIcon, CogIcon } from '@heroicons/vue/24/solid'
import { UserIcon, CogIcon } from '@heroicons/vue/24/outline'
```

Standard sizes:
- Small: `w-4 h-4` (16px)
- Medium: `w-5 h-5` (20px)
- Large: `w-6 h-6` (24px)

`HighlightTile` also uses 5 custom inline SVG icons (`damage`, `kda`, `cs`, `vision`, `chart`) via render functions.

---

## 25. Win Rate Color System

Implemented via `useWinRateColor()` composable + CSS classes in `style.css`.

| Class | CSS Variable | Range |
|-------|-------------|-------|
| `winrate-terrible` | `--color-winrate-terrible` (#ef4444) | < 40% |
| `winrate-bad` | `--color-winrate-bad` (#f97316) | 40–45% |
| `winrate-poor` | `--color-winrate-poor` (#fdba74) | 45–48% |
| `winrate-average` | `--color-winrate-average` (#eab308) | 48–52% |
| `winrate-good` | `--color-winrate-good` (#84cc16) | 52–55% |
| `winrate-great` | `--color-winrate-great` (#22c55e) | > 55% |
| `winrate-neutral` | `--color-text` (#ffffff) | No data |

Legacy aliases (`winrate-red`, `winrate-green`, etc.) exist for backward compatibility.

---

## 26. Bias-Aware UX Rules

These prevent common UX errors in stressful gaming contexts:

1. **Power-user bias**: Overview components must be understandable without expert knowledge
2. **Feature-parity bias**: App focuses on personal/relational performance, not global champion databases
3. **Mode-based thinking**: Users enter flows by event (match, champion select), not by mode selection
4. **Progress illusion**: Graphs/stats without actionable interpretation must be hidden or secondary
5. **Survivorship bias**: Track unused pages/components and post-loss behavior
6. **Optimism bias**: Goals are surfaced passively; users can forget or ignore them
7. **Recency bias**: Last match information always shown with trend context

---

## 27. Design Constraints (Non-Negotiable)

1. Champion Select reachable in one click from any page
2. Overview never blocks user flow
3. No duplicated deep analysis across pages
4. Context (Solo/Team) always visible via separate sidebar entries
5. Team shows lock icon for free users (not 403 or blank wall)
6. Navigation hierarchy remains stable across all pages
7. Every chart/stat must have actionable meaning
8. Single-match insights always framed as trends
9. Premium features appear early in the journey
10. Champion Select is scannable and requires no learning

---

## 28. New Component Checklist

When creating new UI components:

- [ ] Uses design tokens from `style.css` — no hardcoded colors, sizes, or shadows
- [ ] Follows `<script setup>` Composition API pattern
- [ ] Reuses base components (`BaseButton`, `BaseCard`, `BaseModal`, `BaseInput`) where applicable
- [ ] Includes loading and error states for async data
- [ ] Has accessibility attributes (aria-labels, roles, keyboard navigation)
- [ ] Meets 4.5:1 color contrast ratio
- [ ] Uses Heroicons for iconography
- [ ] Applies standard `transition: all 0.2s ease`
- [ ] Respects z-index scale
- [ ] Uses Tailwind for layout, CSS variables for visual properties
- [ ] Has matching unit test in `client/test/unit/`
- [ ] Every displayed metric answers one question and implies one action
- [ ] Follows bias-aware rules (Section 26)

---

## File Reference Map

| Category | Path |
|----------|------|
| CSS Variables / Design Tokens | `client/src/style.css` |
| Tailwind Config | `client/tailwind.config.js` |
| Base Components | `client/src/components/base/` |
| Overview Components | `client/src/components/overview/` |
| Match Components | `client/src/components/matches/` |
| Solo Components | `client/src/components/solo/` |
| Shared Layout | `client/src/components/shared/AnalysisLayout.vue` |
| Root Components | `client/src/components/` |
| Views | `client/src/views/` |
| Layouts | `client/src/layouts/AppLayout.vue` |
| Router | `client/src/router/index.js` |
| Stores | `client/src/stores/` |
| Composables | `client/src/composables/` |
| Services / API | `client/src/services/` |
| Utilities | `client/src/utils/` |
| App Entry | `client/src/App.vue`, `client/src/main.js` |
