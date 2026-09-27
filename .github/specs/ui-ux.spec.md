# Mongoose.gg — UI/UX Specification

> **Purpose**: Single-source-of-truth for AI agents and developers building frontend features. Contains UX contracts (navigation, page responsibilities, bias rules), and the complete component inventory with props/slots.

**Stack**: Vue 3 (Composition API, `<script setup>`) · Tailwind CSS · Headless UI · Lucide (via `BaseIcon`) · Chart.js + vue-chartjs · TanStack Vue Query · Pinia  
**Design system**: Mongoose.gg design system — see Section 2 (visual rules live in `.claude/skills/mongoose-design/reference/`)  
**Platform**: Desktop-first, responsive to phones (grids stack below 900px); future Windows native app  
**Last verified**: September 27, 2026

---

## Table of Contents

1. [Design Philosophy & UX Principles](#1-design-philosophy--ux-principles)
2. [Visual Design System](#2-visual-design-system)
3. [Navigation Model](#3-navigation-model)
4. [Route Map](#4-route-map)
5. [Layout Architecture](#5-layout-architecture)
6. [Page Responsibilities](#6-page-responsibilities)
7. [Base Components](#7-base-components)
8. [Overview Components](#8-overview-components)
9. [Match Components](#9-match-components)
10. [Solo Analysis Components](#10-solo-analysis-components)
11. [Shared Components](#11-shared-components)
12. [Root-Level Components](#12-root-level-components)
13. [Composables](#13-composables)
14. [Stores](#14-stores)
15. [Services & API Client](#15-services--api-client)
16. [Utilities](#16-utilities)
17. [Accessibility](#17-accessibility)
18. [Z-Index Scale](#18-z-index-scale)
19. [Icons](#19-icons)
20. [Win Rate Color System](#20-win-rate-color-system)
21. [Bias-Aware UX Rules](#21-bias-aware-ux-rules)
22. [Design Constraints (Non-Negotiable)](#22-design-constraints-non-negotiable)
23. [New Component Checklist](#23-new-component-checklist)

---

## 1. Design Philosophy & UX Principles

**Theme**: a calm, dark improvement coach that reads like a health tracker (scores, readiness, plain-language insights) and dresses itself in the player's own champions. See Section 2.

**Core Principles**:
1. **Tool over website** — speed and clarity over exploration
2. **Context > Pages** — same data, different perspectives (Solo/Team); each gets its own route
3. **Fast paths for stressed moments** — Champion Select and Match Review must load instantly
4. **Overview opens with the answer** — one summary sentence the player can read in 5 seconds; everything below it is evidence to scroll into when they want it
5. **Goals are horizontal** — visible everywhere, managed centrally
6. **Free version first** — no upsell, pricing or premium prompts for now; premium placement is decided later
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

### UX decisions (2026-09-27)

Settled while aligning this description with the design system:

1. **Top navigation only.** No sidebar; see Section 3.
2. **Page names** stay as in this description: Overview, Champion Select, Matches, Solo, and Advanced (Team and Goals combined; not built yet).
3. **Overview follows the design system's layout** (Section 6).
4. **Time ranges count matches**, never days: Last 20 / Last 50 / Season.
5. **Matches**: every match has its own address (`/app/matches/:matchId`); desktop shows it beside the list, phones as its own page.
6. **Sign-up**: email account first, then link the Riot ID. Signing in with a Riot account is planned for later.
7. **Landing page** is a marketing page that shows what an account gives you (Section 6, Landing).
8. **Free version first**: no upsell for now.
9. **Team and Goals become one page, "Advanced"** (working name). It is not implemented for now; the navigation stays at five items.
10. **Phones get a bottom tab bar** (Section 3). Desktop comes first: phones get this light adaptation, no separate design.

**Deferred**
- How Pro-only pages are marked in the navigation (no badges on pills): decided when Pro is implemented.
- Backend for the Overview design (scores Laning / Teamfighting / Discipline, queue readiness, insights): built when we implement the Overview.

---

## 2. Visual Design System

All visual rules (colour, type, spacing, radius, depth, motion, iconography, champion art, copy voice, component look) come from the **Mongoose.gg design system**. This spec no longer defines any visual values; do not reintroduce them here.

| Source | Path / link |
|--------|-------------|
| Live system (Design System artifact) | https://claude.ai/artifact/CV2Jr6dfMG8A8wv2JoMW2n |
| Principles, voice, colour, type, layout, accessibility | `.claude/skills/mongoose-design/reference/design-system.md` |
| Every token with value and usage | `.claude/skills/mongoose-design/reference/tokens.json` |
| Component guidelines (ChampionHero, ScoreRing, MatchRow, InsightCard, Skeleton, EmptyState, Button, PillNav, …) | `.claude/skills/mongoose-design/reference/components.md` |
| Reference CSS (`mp-*` classes) | `.claude/skills/mongoose-design/reference/components.css` |
| Lucide icon set and fonts | `.claude/skills/mongoose-design/reference/icons/`, `reference/fonts/` |
| Build workflow (foundation setup, Vue rules, validation) | `.claude/skills/mongoose-design/SKILL.md` (`/mongoose-design`) |

**The essentials** (full rules in the files above):
- Dark only. Page `bg` → card `surface` → `surface-selected`; no drop shadows, no gradients except art scrims, one `surface-highlight` card per view.
- **Purple means good, orange means needs work** — never red/green for match or performance meaning. `--color-error` / `--color-success` stay only for system states (form validation, failed requests, destructive confirmations).
- **Clash Display** for headlines, titles and every number (tabular); **Satoshi** for everything else. Type and spacing in `rem`.
- Pill-shaped controls, 24px card radius, 44px minimum touch targets, `shadow-focus` on every focusable element.
- Lucide icons only, through `BaseIcon`; champion art from `client/src/utils/leagueAssets.js`.
- Summary sentence before scores before detail; "you" voice; glossary words (match, Riot ID, sync, score, readiness, insight, goal).

**Code mapping**: tokens live as CSS variables in `client/src/style.css` (existing `--color-*` names mapped to design-system values) and are exposed to Tailwind in `client/tailwind.config.js`. Use Tailwind for layout and sizing, CSS variables for themed values. Never hard-code a colour, radius, shadow or duration that is not a design-system token.

**Migration status**: the foundation is in the app (Clash Display + Satoshi in `client/public/fonts/`, design tokens in `client/src/style.css`, Tailwind mapping in `client/tailwind.config.js`, Lucide icons through `BaseIcon`). The public header (`NavBar`), the Landing page, the cookie banner, the auth page (log in, sign up, forgot password) and the shared `BaseButton` / `BaseInput` are migrated; other screens are migrated one by one. Any old-theme styling still in the code (Inter, `hero-bg.svg`, glow shadows, hover lifts, Heroicons, red/green win-rate colours) is legacy to replace when a file is touched — never a pattern to copy.

---

## 3. Navigation Model

**Primary navigation**: one 80px top header on every `/app/*` page, built from the design system's PillNav. There is no sidebar.

- **Left**: logo, links to `/app/overview`
- **Middle**: PillNav (`<nav aria-label="Main">`, `aria-current="page"` on the active pill)
- **Right**: the player's avatar (main champion icon). It opens a menu with the Riot account switcher (when several accounts are linked), Settings (`/app/user`), Feedback (`/app/feedback`) and Log out.
- The header is the same on every page, including Champion Select.
- **Phones** (below 900px): the header shrinks to 56px with only the logo and the avatar; the pills move to a fixed bottom tab bar (see below).

### Phone tab bar

Desktop comes first; this is the only phone-specific navigation.

- Fixed to the bottom, 64px tall plus the safe-area inset, `surface-raised` with a `divider` hairline on top.
- One tab per navigation item: icon above a short label (`house` Overview, `shield` Champ Select, `swords` Matches, `chart-line` Solo, `users` Advanced when enabled).
- "Champion Select" is shortened to "Champ Select" on phones only; the page title keeps the full name.
- The active tab uses `positive-text` (light purple) and `aria-current="page"`; the bar is the same `<nav aria-label="Main">` as on desktop.
- It stays visible on every app page. A match opened at `/app/matches/:matchId` shows a back arrow in the header that returns to the list.

### Navigation Items

```
Overview         → /app/overview
Champion Select  → /app/champion-select
Matches          → /app/matches
Solo             → /app/solo
Advanced         → (planned)      Team and Goals combined; not implemented yet
```

Advanced (working name) combines Team and Goals into one page, which keeps the navigation at five items. It is not implemented for now: until then the existing Team and Goals pages stay hidden behind their feature flags (`VITE_FEATURE_TEAM_ANALYTICS`, `VITE_FEATURE_GOALS`). How Pro-only pages are marked is decided when Pro is implemented (no badges on pills).

> **Implementation note**: the app still renders the legacy `AppSidebar.vue` (with the feature flags on its `<router-link>` elements). It is replaced by the top header during the design-system migration.

**Architecture decision**: Solo and Team are **separate top-level pages** (not tabs) for:
1. Better upgrade perceived value
2. Content diverges significantly in v2
3. Cleaner gating UX — locked page with preview/teaser

---

## 4. Route Map

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
| `/app/matches/:matchId` | *(planned)* | `MatchesPage.vue` with one match open | Free |
| `/app/solo` | `app-solo` | `SoloStatsPage.vue` | Free |
| `/app/team` | `app-team` | `TeamAnalytics.vue` | Pro (flagged off; moves into Advanced) |
| `/app/goals` | `app-goals` | `GoalsPage.vue` | Free (flagged off; moves into Advanced) |
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

## 5. Layout Architecture

### `AppLayout.vue` (authenticated shell)
- **Structure**: top header (logo, PillNav, avatar menu; Section 3) above `<router-view>`. Content max width 1328px, 56px side gutters on desktop, 16px on phones.
- **Legacy**: the current code still uses `AppSidebar` with `uiStore.sidebarWidth`; removed in the migration.
- **Idle detection**: 30-minute threshold; on tab return, refreshes user data + triggers sync check
- **Activity tracking**: Throttled to 30s intervals (mousemove, keydown, click, scroll)

### `OverviewLayout.vue` (overview page container)
- Named slots: `#header`, `#glance-left`, `#glance-right`, `#recent-left`, `#recent-right`, `#latest-match`, `#empty-action`
- Handles loading (Skeleton), error (retry), empty (link account CTA) states
- Single-column layout, one-scroll max

### `AnalysisLayout.vue` (shared by Solo/Team)
- Zone-based layout with named slots:
  - `#context-bar` — Zone 1: Filters (queue toggle, time range)
  - `#summary` — Zone 2: Summary stats row
  - `#trend-charts` — Zone 3: 2-column chart grid
  - Zone 4 (deep analysis) and Zone 5 (goals) — not rendered in v1
- Prop: `pageTitle`

### `NavBar.vue` (public pages)
- Fixed top header for public pages: 80px on desktop, 56px below 900px (pages offset their content by the same height)
- Logo, "Features" and "How it works" anchors, "Log in", and "Create free account" as the one filled button (no Pricing while we focus on the free version)
- Below 900px: logo, "Log in" and a menu button (Lucide `menu` / `x`); the menu slides down with the section links and "Create free account"
- Logo links to `/app/user` if authenticated, `/` if not

---

## 6. Page Responsibilities

### Landing (`/`)
**Role**: Marketing page that shows what a free account gives you, and sends players to sign-up. Like Blitz's landing page, it sells the product; unlike Blitz, the value starts after an account, because we store the player's matches (email sign-up, then Riot ID link).

Top to bottom:
1. **Header** (`NavBar`, Section 5).
2. **Hero**: one headline about the player's outcome and a concrete subline (design system headline rules), "Create free account" as the main button, "Log in" beside it, and one trust line ("Free · Built on Riot's official API"). Visual: a product preview built from our real components (champion hero, score rings, an insight) with data from a real example account, labelled "Example". No screenshots or illustrations.
3. **Features**: a grid of cards, one per thing you get, each tagged with the page it lives on (Overview, Champion Select, Matches, Solo) and showing a small real-component example.
4. **Your champion pool**: three champion cards (centred art, strength tag, win rate) from the example account, showing that every champion gets its own scores, matchups and fixes.
5. **How it works**: three steps: create an account with your email, link your Riot ID, we sync your recent matches (a few minutes) and your Overview fills in. Mention Riot sign-in only once it is planned for real.
6. **Proof**: the matches-analysed count from `/public/stats` joins the hero's trust line, next to the main button, once it reaches 10,000 (rounded down: "3.2M+ matches analysed"). Below that it is left out.
7. **Footer**: legal links, contact, "Not affiliated with Riot Games". No pricing section for now.

### Overview (`/app/overview`)
**Role**: Today at a glance. One summary sentence the player can read in 5 seconds, then the evidence below it.

Layout (design system, top to bottom):
1. **ChampionHero**: the player's main champion splash, one headline about them ("Your Ahri laning is elite. Your late-game discipline is not."), one supporting sentence, the player line (Riot ID · main champion · rank · LP), a primary action ("See today's matches") and up to two glass stat chips.
2. **Three ScoreRings**: Laning, Teamfighting, Discipline (0–100, change over the last matches, three contributors each).
3. **Your champions**: three ChampionCards plus an "Also played" list. Choosing a card swaps the hero's art, headline and chips.
4. **Queue readiness** (ReadinessMeter, the one highlight card) beside **Today's matches** (MatchRows linking to each match).
5. **Insights**: InsightCards, three per row, strongest first, six at most.

Sync progress (SyncProgress) sits at the top of the content while matches come in and never blocks the page. Every card has skeleton, empty, error and content states.

Data sources: `getOverview()` today; scores, readiness and insights need new backend data, built when the Overview is implemented (Section 1).

Current code (legacy, replaced in the migration): `OverviewPlayerHeader`, `TodaySessionCard`, `DeathInsightsCard`, `ChampionSelectCTA`, `AnalysisStatusCard`, `SoloAnalyticsCTA`, `LastMatchCard`.

**Non-goals**: Deep graphs, champion matrices, filters.

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

Every match has its own address, `/app/matches/:matchId`, so it can be shared and the back button works. Each `MatchRow` is one link to it. On desktop the match opens beside the list, which stays in place; below 900px it opens as its own page with a back link. Opening a match never animates.

Components: `MatchList` → `MatchRow` items → `MatchDetails` with:
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
- Zone 2: `SummaryStatsCard` — matches played, win rate, average KDA (with overall comparisons)
- Zone 3: `TrendChartCard` — Win rate trend (rolling 20-match)

Time ranges count matches, never days: each chart card has a segmented control (Last 20 / Last 50 / Season), default Last 20, switching in place (no modal).

Data sources: `getSoloDashboard()`, `getWinrateTrend()` from `authApi`

> **Advanced** (planned, not implemented): Team and Goals below will be combined into one page called Advanced (working name).

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

Onboarding order: create an account with email and password → verify the email (6-digit code) → link a Riot ID ("Link Riot account") → first sync fills the Overview. Signing in with a Riot account is planned and will replace the first two steps.

Forgot-password is a third form state: email input → submit → redirect to reset page. "Forgot password?" link visible in login mode. Back link returns to login.

### Reset Password (`/auth/reset-password`)
**Role**: Consume 6-digit reset code + set new password. Public route, no auth required.

Pre-fills email from `?email=` query param. Code input uses same monospace `tracking-[0.5em]` pattern as VerifyPage. Submit disabled until code is 6 digits and password ≥ 8 chars. On success redirects to `/auth?mode=login`.

### Verify (`/auth/verify`)
**Role**: 6-digit email verification. Auto-submit on completion. Resend cooldown (60s, server-controlled).

---

## 7. Base Components

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

## 8. Overview Components

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

## 9. Match Components

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

Color-coded with sentiment borders (positive purple / needs-work orange).

### `LaneMatchupDetails`
Two-phase display:
1. **Early Laning** (0–10m): gold diff bar, CS diff, deaths
2. **Game Impact**: damage share, KP, vision

Includes AI-generated matchup insight text.

### `TeamComparison`
Team damage comparison bars (ally vs enemy), gold lead @15, objective counts (dragons/barons/towers). Uses Community Dragon CDN for icons.

### `TrendBadge`
Inline badge with ↑/↓ arrows. Props: `{ text, type, stat }` badge object. Types: positive (`positive-text`), negative (`warn-text`), neutral (`ink-muted`).

### `MatchActions`
Navigation links from match detail to relevant analysis pages.

---

## 10. Solo Analysis Components

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

### `WinrateChart`
Chart.js line chart for rolling win rate. Prop: `data` (array of win rate data points). Subtitle: "Rolling 20-game average".

---

## 11. Shared Components

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

## 12. Root-Level Components

Located in `client/src/components/`.

### `AppSidebar`
*(Legacy — replaced by the top header, Section 3.)* Vertical navigation sidebar. Reads collapsed state from `uiStore`. Shows lock icons for Pro-tier pages (Duo, Team) when user is free tier.

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

## 13. Composables

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

## 14. Stores

Located in `client/src/stores/`. Using **Pinia**.

### `authStore`
- **State**: user object, session expiry tracking (`wasAuthenticated` pattern)
- **Actions**: `initialize()`, `login()`, `register()`, `verify()`, `logout()`, `changePassword()`, `linkRiotAccount()`, `unlinkRiotAccount()`, `triggerSync()`, `refreshUser()`
- **Computed**: `isAuthenticated`, `isVerified`, `isInitialized`, `username`, `email`, `tier`, `primaryRiotAccount`

### `uiStore`
- **State** *(legacy, goes with the sidebar)*: sidebar collapsed (persisted to `localStorage`), mobile breakpoint (1024px)
- **Computed**: `sidebarWidth` — auto-collapse on small screens

---

## 15. Services & API Client

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

## 16. Utilities

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

## 17. Accessibility

Follow "States and accessibility" in `.claude/skills/mongoose-design/reference/design-system.md`. In short:
- **Focus**: every interactive element shows `shadow-focus` on keyboard focus.
- **Contrast**: 4.5:1 minimum; each text token lists the grounds it is allowed on.
- **Touch targets**: at least 44px tall (buttons 48px).
- **Meters**: scores and meters use `role="meter"` with `aria-valuetext` in words.
- **Motion**: respect `prefers-reduced-motion`; **contrast**: `prefers-contrast: more` uses the High contrast token values.
- **Text size**: type and spacing in `rem` so 200% zoom works.
- **Screen reader**: `.visually-hidden` utility class; icon-only buttons get `aria-label`; decorative icons `aria-hidden="true"`.
- **Headless UI**: used for complex interactive components (Dialog, Menu, Transition) for built-in ARIA support.
- **Semantic HTML**: proper heading hierarchy, `<button>` vs `<a href>`, form labels, `aria-pressed` / `aria-current`.

---

## 18. Z-Index Scale

| Layer | Z-Index | Usage |
|-------|---------|-------|
| Background | 0 | Background image/overlay |
| Content | 1 | Main app content (`#app`) |
| Header | 100 | Fixed navigation |
| Dropdowns | 200 | Menus, popovers |
| Modals | 300 | Modal dialogs (Headless UI Dialog) |
| Toasts/Banners | 400 | SessionExpiredBanner, notifications |

---

## 19. Icons

Lucide only, via `BaseIcon` (`client/src/components/base/BaseIcon.vue`, SVGs in `client/src/assets/icons/`). Sizes 16 / 20 / 24px, `currentColor`, always next to a word. The icon vocabulary (one meaning per icon) is the Iconography table in `.claude/skills/mongoose-design/reference/design-system.md`. Champion, item, rune, role and rank emblems are Riot images, never icons.

Heroicons and hand-drawn inline SVGs are legacy: switch a component to `BaseIcon` when you touch it.

---

## 20. Win Rate Color System

Implemented via `useWinRateColor()` composable + `winrate-*` CSS classes in `style.css`. Values follow the design-system `winrate-*` tokens (purple = good, orange = needs work):

| Class | Design token | Range |
|-------|-------------|-------|
| `winrate-terrible` | `winrate-terrible` | < 40% |
| `winrate-bad` | `winrate-bad` | 40–47% |
| `winrate-average` | `winrate-average` | 48–52% |
| `winrate-good` | `winrate-good` | 53–59% |
| `winrate-great` | `winrate-great` | 60%+ |
| `winrate-neutral` | `ink` | No data |

Exact values and allowed grounds are in `.claude/skills/mongoose-design/reference/tokens.json`. Legacy aliases (`winrate-red`, `winrate-green`, etc.) exist only for backward compatibility.

---

## 21. Bias-Aware UX Rules

These prevent common UX errors in stressful gaming contexts:

1. **Power-user bias**: Overview components must be understandable without expert knowledge
2. **Feature-parity bias**: App focuses on personal/relational performance, not global champion databases
3. **Mode-based thinking**: Users enter flows by event (match, champion select), not by mode selection
4. **Progress illusion**: Graphs/stats without actionable interpretation must be hidden or secondary
5. **Survivorship bias**: Track unused pages/components and post-loss behavior
6. **Optimism bias**: Goals are surfaced passively; users can forget or ignore them
7. **Recency bias**: Last match information always shown with trend context

---

## 22. Design Constraints (Non-Negotiable)

1. Champion Select reachable in one click from any page
2. Overview never blocks user flow
3. No duplicated deep analysis across pages
4. Context (Solo/Team) always visible via separate navigation pills *(Team moves into Advanced, which is not built yet; see Section 3)*
5. Team is never a 403 or blank wall for free users *(how it is marked in the navigation is decided when Pro is implemented)*
6. Navigation hierarchy remains stable across all pages
7. Every chart/stat must have actionable meaning
8. Single-match insights always framed as trends
9. Premium features appear early in the journey *(deferred: free version first)*
10. Champion Select is scannable and requires no learning

---

## 23. New Component Checklist

When creating new UI components:

- [ ] Built from the Mongoose.gg design system (`/mongoose-design`): uses an existing design-system component where one fits; tokens only — no hard-coded colours, sizes, radii, shadows or durations
- [ ] Clash Display for titles and numbers, Satoshi for text; purple/orange semantics, never red/green for performance
- [ ] Follows `<script setup>` Composition API pattern
- [ ] Reuses base components (`BaseButton`, `BaseCard`, `BaseModal`, `BaseInput`, `BaseIcon`) where applicable
- [ ] Has Skeleton (loading), EmptyState (nothing yet), inline error with retry, and content states for async data
- [ ] Has accessibility attributes (aria-labels, roles, meters, keyboard navigation) and `shadow-focus`
- [ ] Meets 4.5:1 color contrast ratio and 44px touch targets
- [ ] Uses Lucide icons through `BaseIcon`, always with a word
- [ ] Motion only shows a change, with a `prefers-reduced-motion` path
- [ ] Respects z-index scale
- [ ] Uses Tailwind for layout, CSS variables for visual properties
- [ ] Has matching unit test in `client/test/unit/`
- [ ] Every displayed metric answers one question and implies one action
- [ ] Follows bias-aware rules (Section 21)

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
