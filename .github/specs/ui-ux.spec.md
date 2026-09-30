# Mongoose.gg — UI/UX Specification

> **Purpose**: Single-source-of-truth for AI agents and developers building frontend features. Contains UX contracts (navigation, page responsibilities, bias rules), and the complete component inventory with props/slots.

**Stack**: Vue 3 (Composition API, `<script setup>`) · Tailwind CSS · Headless UI · Lucide (via `BaseIcon`) · TanStack Vue Query · Pinia  
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

**Migration status**: the foundation is in the app (Clash Display + Satoshi in `client/public/fonts/`, design tokens in `client/src/style.css`, Tailwind mapping in `client/tailwind.config.js`, Lucide icons through `BaseIcon`). The public header (`NavBar`), the Landing page, the cookie banner, the auth page (log in, sign up, forgot password), the shared `BaseButton` / `BaseInput`, the app header (`AppHeader` / `AppTabBar`, replacing the legacy sidebar), the Overview (with the data the API has today) and Champion Select are migrated; other screens are migrated one by one. Any old-theme styling still in the code (Inter, `hero-bg.svg`, glow shadows, hover lifts, Heroicons, red/green win-rate colours) is legacy to replace when a file is touched — never a pattern to copy.

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
- It stays visible on every app page. A match opened at `/app/matches/:matchId` replaces the list and shows an "All matches" link above the match that returns to it; the Matches tab stays current.

### Navigation Items

```
Overview         → /app/overview
Champion Select  → /app/champion-select
Matches          → /app/matches
Solo             → /app/solo
Advanced         → (planned)      Team and Goals combined; not implemented yet
```

Advanced (working name) combines Team and Goals into one page, which keeps the navigation at five items. It is not implemented for now: until then the existing Team and Goals pages stay hidden behind their feature flags (`VITE_FEATURE_TEAM_ANALYTICS`, `VITE_FEATURE_GOALS`) and are not reachable from `AppHeader` / `AppTabBar`. How Pro-only pages are marked is decided when Pro is implemented (no badges on pills).

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
| `/app/matches/:matchId?` | `app-matches` | `MatchesPage.vue`; with `matchId` one match is open | Free |
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
- **Structure**: `AppHeader` (logo, PillNav, avatar menu; Section 3) above a centered content wrapper (max width 1328px, 56px side gutters on desktop, 16px on phones) around `<router-view>`, with `AppTabBar` fixed to the bottom on phones.
- **Idle detection**: 30-minute threshold; on tab return, refreshes user data + triggers sync check
- **Activity tracking**: Throttled to 30s intervals (mousemove, keydown, click, scroll)

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
3. **Your champions**: three ChampionCards plus an "Also played" list. The cards are static on the Overview (decided 2026-09-27): champion-focused analysis lives on Champion Select.
4. **Queue readiness** (ReadinessMeter, the one highlight card) beside **Today's matches** (MatchRows linking to each match).
5. **Insights**: InsightCards, three per row, strongest first, six at most.

Sync progress (SyncProgress) sits at the top of the content while matches come in and never blocks the page. Every card has skeleton, empty, error and content states.

Data sources: `getOverview()` today; scores, readiness and insights need new backend data, built when the Overview is implemented (Section 1).

Built today (design migration Phases 2–3; sections without data are left out until Phase 3 adds scores, readiness and insights):
1. `SyncProgress` at the top while a sync runs (`useSyncMatches`).
2. `ChampionHero` with the most-played champion's splash (plain card without one). Headline, supporting sentence and chips come from `sessionStats` and `survivalStats` (`client/src/utils/overviewSummary.js`); primary action "See your matches". In Overall mode `OverviewAccountCards` replaces the hero and holds the page headline.
3. **Your champions** (`OverviewChampionPool`): `championPool` from the API — up to three static `ChampionCard`s (ranked this season, by M-Score, with a strength tag) and up to three "Also played" rows; EmptyState "No ranked matches this season" otherwise. Shown in Overall mode too, aggregated across accounts.
4. **Today's matches**: today's count and wins/losses, the last match as a `BaseMatchRow` (links to `/app/matches/:matchId`), "All matches", and the "Sync matches" button.
5. **Insights**: the deaths finding from `survivalStats` as one `InsightCard` (Strength or Pattern), or an EmptyState.
6. **Next steps**: Champion Select and Solo as two plain cards with secondary buttons.

States: the skeleton frame shows after 300ms while loading, an error message with "Try again" when the request fails, and an EmptyState with "Link Riot account" when no Riot account is linked.

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

Layout, top to bottom:
1. **Filters**: two SegmentedControls, right-aligned — queue (All queues, Solo/Duo, Flex, Normal; ARAM left out, it has no lanes) and time range (This season, Last 3 months, All time). Default All queues · This season.
2. **Hero** (`ChampionHero`): the selected pick's splash with the one headline ("Ahri is your best pick for Mid", "Syndra is your #2 pick for Mid"), the filter context line (Mid · All queues · This season), one evidence sentence (win rate, matches, KDA, strongest and weakest lane matchup; picks under 5 matches are called "a lean") and two glass chips (win rate, CS per minute).
3. **Your picks**: a Role SegmentedControl (roles with data, Top → Support; hidden with only one role; defaults to the most-played role) and the role's top three picks (by M-Score from the API) as selectable `ChampionCard`s. The first is tagged "Best pick" and selected by default; picking a card swaps the hero and matchups. Switching role resets to that role's best pick.
4. **Matchups + Check a matchup** side by side (matchups flexible, search a 420px column; stacked below 900px):
   - `ChampionSelectMatchups` — "{Champion} in lane": up to four Strong into (purple, best first) and Weak into (orange, worst first) opponents met 3+ times in lane, each with its lane record.
   - `ChampionSelectSearch` — type the enemy laner (2+ characters) to see your champions in the selected role against them: lane record, overall record and win rate.

States: skeleton frame after 300ms, error with "Try again" if the picks fail, EmptyState "No champions for these filters yet" (with "Show all matches" when the filters are narrowed), and "Link Riot account" without a linked account. The matchups request is separate: if it fails, the picks stay and the matchups card shows its own error with retry (search disabled).

Components: `ChampionHero`, `ChampionCard` (selectable), `BaseSegmentedControl`, `ChampionSelectMatchups`, `ChampionSelectSearch`; copy and matchup logic in `utils/championSelectSummary.js`.

### Matches (`/app/matches`)
**Role**: Review what just happened. Match list with quick summaries.

Every match has its own address, `/app/matches/:matchId`, so it can be shared and the back button works. Each `MatchRow` is one link to it. On desktop the match opens beside the list, which stays in place; below 900px it opens as its own page with a back link. Opening a match never animates.

Order (design system: summary → list → evidence):

1. `SyncProgress` at the top while a sync runs; "Sync matches" sits in the list card header.
2. Headline built from the list ("12 wins in your last 20", remakes left out), a line on the current streak (3 or more) and the most played champion, the list as a `BaseFormStrip` beside it, and the queue `BaseSegmentedControl` (All queues / Solo/Duo / Flex / Normal; ARAM shows under All queues). Copy lives in `client/src/utils/matchesSummary.js`.
3. List card ("Last 20 matches"): `BaseMatchRow` per match (remakes under 5 minutes say "Remake", Overall mode adds the Riot ID to the meta line). On desktop it is a 420px column (360px below 1100px) that stays in place while the match scrolls, and the newest match opens on arrival; the open row is `aria-current="page"` on `surface-selected`.
   Under it, "Win rate by start time" as a `BaseColumnChart` (morning / afternoon / evening / after 11pm in the player's local time; groups under 3 matches left out; shown only when two groups can be compared; the title names the weak spot, 15 or more points below the rest, or is the caption when nothing stands out).
4. The open match (`MatchDetails`): `MatchHeader` (the ChampionHero match banner: splash, result and time, "Champion, Role", queue and length, a K / D / A glass chip; the trend-badge chip is retired) → `DecidingStatCard` ("What decided it": one `surface-highlight` card with a one-line finding, up to three UsualMeters as evidence, the usual key and a "Next match" fix; left out when `decidingStat` is null) → `MatchNarrative` ("n of 5 lanes won", a LaneBar per role from the gold difference at 10 opening `LaneMatchupDetails`; ARAM lists both teams by damage) beside `TeamComparison` (SplitBars for damage, dragons, barons and towers, gold lead at 15 as the caption) when the column is 760px or wider → `StatSnapshot` (closed "All your stats" disclosure with the above / below counts; open, all ten stats and "Download data") → `MatchActions` ("See your trends" on Solo).

States: skeleton rows and a skeleton match after 300ms, inline error with "Try again" for the list, the match and the lanes separately, EmptyState "No matches yet" with "Sync matches" (or "Show all queues" when filtered), "We couldn't find this match" for an unknown match ID, and "Link Riot account" without a linked account. Your team is purple and the enemy orange; good stats purple ▲, needs-work orange ▼.

### Solo (`/app/solo`)
**Role**: "Am I improving?": the player's matches against their own past. Free tier. Rules and copy in [features/solo-trends.spec.md](features/solo-trends.spec.md); built so far through 5f of the design migration.

1. Header: the rank line for one ranked queue of one account (Riot ID · tier-colour dot, "Emerald II · 58 LP · Solo/Duo"), one `h1` from the climb ("+148 LP over your last 50 matches" in LP mode, "28 wins in your last 50" otherwise; the match count until the climb answers) and the second line from `stat-trends` (the Improving stat with the largest normalised change, else the Slipping one). Right: two `BaseSegmentedControl`s, Queue (Solo/Duo, Flex, All queues; the server picks the default and the control shows it) and Range (Last 20, Last 50, Season; ranges count matches).
2. `SoloClimbCard` beside `SoloFocusCard` (2:1; the climb takes the full width when there is no focus). The focus is the page's one highlight card: "Your focus" eyebrow, "Vision is the one stat slipping" / "… is your biggest lever", the evidence, a `BaseGoalStrip` of the last 20 matches against the mark, and the "Next match" fix; no goal button yet. The climb: "From Emerald III to Emerald II" / "Holding Emerald II" with win rate, LP per match and wins–losses beside it, and a `BaseLineChart` of the LP ladder (division guides, promotion labels, the biggest drop as one orange point). In win-rate mode, "Win rate up from 52% to 58%" over the 10-match win rate (needs 20 matches), with "LP appears here as your ranked matches sync." for one ranked queue of one account.
3. `SoloStatTrends`: "n of 6 match-deciding stats improved" with six `BaseTrendTile`s (3 columns; rows below 900px) and a key (Your average, One match, and the benchmark: Your season average, or "Emerald average" when the rank average applies).
4. `SoloDeathZones`: "Deaths in {zone} cost you the most objectives" (3+ lost) or "You die most in {zone}", with a `BaseDeathMap` (zone circles at fixed anchors, your base bottom left, warn when 30% or more cost an objective), a zone list whose buttons (`aria-pressed`) filter the When / How / What it cost bars, and "All zones" as the default filter. Needs 30 counted deaths; while older matches get their death detail, the card shows the backfill's progress instead ("Adding detail to your older matches · 12 of 50", indeterminate while the total is unknown, "Waiting on Riot's servers. We'll continue automatically."), updated over the sync WebSocket or every 30 seconds without it.
5. `SoloWinFactors`: "Your {factor} decides your matches most" with a `BaseWinFactorRow` per mark (needs 20 matches and 2 rows), beside `SoloChampionLp`: "Ahri earned most of your climb" with up to five champions (36px icon, matches and win rate, a `BaseDivergingBar`, signed LP or net wins) and the champions left out for having under 3 matches. One column below 900px.
6. `SoloPatterns`: "Your patterns", up to three InsightCard-style cards with a chip and a `BaseColumnChart` (session, after a loss, match length); hidden when none qualify.

Each card loads on its own (skeleton after 300ms, inline error with "Try again" for that card only, EmptyState, content) and keeps its old content while a filter change loads. EmptyStates: "Link your Riot account to see your trends"; "No {queue} matches yet" with "Show all queues" (or "Sync matches"); "Play {n} more matches to see what decides your matches". A finished sync reloads every card. Sections fade in once (`vRevealOnView`).

Data sources: `getSoloClimb()`, `getSoloStatTrends()`, `getSoloWinFactors()`, `getSoloDeathZones()` from `soloApi`, through `useSoloDashboardData`.

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

### Design-system components

Built from `.claude/skills/mongoose-design/reference/components.md`; imported directly (not through the barrel).

| Component | Props | Notes |
|------|------|------|
| `BaseSkeleton` | `variant` (`text`, `title`, `ring`, `portrait`, `block`), `width`, `height` | One loading shape; the container sets `aria-busy` and a hidden "Loading …" label |
| `BaseEmptyState` | `title`, `description`, `headingLevel` | Slot `#action` holds the one button that fixes it |
| `BaseMatchRow` | `to`, `championName`, `championIconUrl`, `win`, `kda`, `queue`, `durationSeconds`, `timestamp`, `lpChange`, `remake`, `riotId` | Whole row is one link; the open match's row (`aria-current="page"`) gets `surface-selected`; inside a `match-list` container narrower than 30rem it uses the phone layout |
| `ChampionHero` | `headline`, `text`, `playerLine`, `championName`, `chips` (max 2), `chipsLabel` | Page `h1`; slot `#action`; plain card without a champion |
| `InsightCard` | `kind` (`strength`, `pattern`, `trend`), `title`, `text`, `championName` | |
| `ChampionCard` | `championName`, `winRate`, `matches`, `avgKda`, `strengthTag`, `selectable`, `selected` | Static `<article>` by default (Overview); with `selectable` a `<button>` with `aria-pressed`, primary border when selected and the spotlight hover, emits `select` (Champion Select); centred art; 220px tall below 900px |
| `BaseSegmentedControl` | `modelValue`, `options` (`{ value, label }`), `ariaLabel`, `testIdPrefix` | `mp-seg`: `role="group"` of buttons with `aria-pressed`; 2–4 options |
| `BaseFormStrip` | `results` (`win` / `loss` / `remake`, oldest first) | `mp-form`: up to 20 bars, `role="img"` with the sequence and totals; not clickable |
| `BaseColumnChart` | `groups` (`{ key, label, value }`), `max`, `unit`, `weakKey`, `measure` | `mp-columns`: 2–4 columns, the weak spot in warn, `role="img"` listing every value |
| `BaseTrendTile` | `label`, `value`, `unit`, `was`, `verdict`, `tone` (`up`, `down`, `neutral`), `arrow`, `values`, `rolling`, `length`, `benchmark`, `description` | `mp-stat` tile with an SVG sparkline: a dot per match, the rolling line in primary, the benchmark dashed; `role="group"` labelled by `description`; a row below 900px (Solo) |
| `BaseWinFactorRow` | `label`, `hitWinRate`, `missWinRate`, `description` | Two dots on a 0–100% track (hit: filled primary, missed: warn ring), both rates above; the track is `role="img"` (Solo) |
| `BaseLineChart` | `points` (`{ x, y }`, x = match index), `length`, `yMin`, `yMax`, `guides` (`{ y, label }`), `markers` (`{ x, y, label, tone }`), `ariaLabel`, `height` | One primary line with the last point highlighted, divider guides labelled in ink-faint, moments as dots with labels (warn for a drop; an empty label draws only the dot); `role="img"` (Solo climb) |
| `BaseDivergingBar` | `value`, `max`, `description` | LaneBar shape without icons: primary right for a gain, warn left for a loss, scaled to `max`; `role="img"` (Solo LP per champion) |
| `BaseGoalStrip` | `results` (`hit` / `miss` / null, oldest first), `markLabel` | 20 cells: hit filled primary, missed a warn ring, not applicable a track stub; ends labelled; `role="img"` listing every match (Solo focus) |
| `BaseDeathMap` | `zones` (`{ key, deaths, costly, anchor: { u, v } }`), `selectedKey`, `description` | SVG map outline (bases, lanes, dashed river) with a circle per zone sized by deaths (area), warn when costly, the selected one outlined; `v` is flipped for SVG; `role="img"` with every zone in words (Solo death zones) |
| `SyncProgress` | `state` (`running`, `waiting`, `done`, `failed`), `current`, `total`, `syncedCount` | Emits `retry`; indeterminate while `total` is 0 |
| `ScoreRing` | `value`, `label`, `size` | `role="meter"` |

---

## 8. Overview Components

Located in `client/src/components/overview/`.

### `OverviewChampionPool`
"Your champions": props `champions` (cards, max 3) and `alsoPlayed` (max 3). Grid of three cards plus a 300px "Also played" card; below 1200px the list moves under the cards, below 900px everything is one column. "Also played" win rates under 50% use `warn-text`. EmptyState without champions.

### `OverviewAccountCards`
Overall mode only, in place of the ChampionHero: one card per linked account (at most three) with Riot ID, level, Flex and Solo rank. Holds the page `h1` ("Your accounts").

| Prop | Type | Description |
|------|------|-------------|
| `accounts` | `Array` | `accountSummaries` from the Overview response |
| `linkedAccounts` | `Array` | Linked Riot accounts from `authStore` (icons, levels, ranks) |
| `activeAccountPuuid` | `String` | Marks the active account |

### `MatchActivityHeatmap`

| Prop | Type | Description |
|------|------|-------------|
| `dailyMatchCounts` | `Array` | Per-day match count data |
| `startDate` | `String` | Heatmap start date |
| `endDate` | `String` | Heatmap end date |
| `totalMatches` | `Number` | Total match count in period |

---

## 9. Match Components

Located in `client/src/components/matches/`.

### `MatchDetails`
The open match: loading skeleton, `error` (`failed` with retry, `not-found`, `no-account`), empty and content states. Props `match`, `baseline`, `decidingStat`, `accountId`, `loading`, `error`; emits `retry`. Builds the "Download data" JSON when `StatSnapshot` asks for it. Lanes sit beside the team summary when the column is 760px or wider (container query).

### `MatchHeader`
The open match's banner (ChampionHero match banner): splash art (plain card when it fails), result eyebrow with the time (Victory purple / Defeat orange / Remake neutral), "Champion, Role" as an `h2`, queue · length and a K / D / A glass chip. LP and rank chips come with the LP data. The trend-badge chip is retired (Phase 4c) in favor of `DecidingStatCard`.

### `DecidingStatCard`
"What decided it" (Phase 4c): one `mp-card mp-card--highlight` card between the banner and `MatchNarrative`. An eyebrow, a one-line finding (`h2`, the deciding stat's strength/shortfall line, or "A match like your usual: nothing stood out"), up to three `BaseUsualMeter`s (the deciding stat first, the rest by \|score\| descending), the usual key ("Your usual · last {n} matches as {Role}") and an optional "Next match" fix row with a Lucide icon. Copy and formatting in `utils/decidingStat.js`; left out entirely when `decidingStat` is null (remake, non-Summoner's-Rift queue, unknown role, or no eligible stat).

### `MatchNarrative`
Lane by lane, fetched via `getMatchNarrative()`: one LaneBar button per role (`aria-expanded`, `aria-label` in words) growing right in purple when ahead and left in orange when behind at 10 minutes (±1,500 fills a half, under 300 is even), the player's icon ringed; the title counts lanes won. ARAM lists both teams by damage share. Own loading, error with retry, and empty states.

### `LaneMatchupDetails`
One sentence on how the lane went, then early laning (gold lead, CS lead, deaths) and match impact (damage share, kill participation, vision score) as tables.

### `TeamComparison`
SplitBars (your team purple, enemy orange, numbers on both ends, `role="img"` text alternatives) for damage, dragons, barons and towers; an empty track when neither team took an objective. The title states the damage share; the gold lead at 15 is the caption.

### `StatSnapshot`
"All your stats": a closed disclosure whose summary row counts the stats above and below usual. Open, all ten stats as StatTiles with the comparison to your average (adjusted for match length where it matters) and "Download data". Emits `download`.

### `MatchActions`
Next-step card linking to Solo ("See your trends").

---

## 10. Solo Analysis Components

Located in `client/src/components/solo/`. Copy and number formats in `utils/soloSummary.js`.

| Component | Props | Notes |
|------|------|------|
| `SoloCard` | `testId`, `titleId`, `title`, `caption`, `errorTitle`, `loadingLabel`, `loading`, `error`, `empty` | Shell with the four states; slots `#skeleton`, `#empty-action`, `#aside` (key), default; emits `retry` |
| `SoloStatTrends` | `data` (stat-trends response), `loading`, `error`, `queue`, `syncing` | Six `BaseTrendTile`s; title per FR 17 (the caption when no stat has a verdict); emits `retry`, `show-all-queues`, `sync` |
| `SoloWinFactors` | `data` (win-factors response), `loading`, `error`, `queue`, `syncing` | `BaseWinFactorRow`s in server order; EmptyState under 20 matches or 2 rows; same emits |
| `SoloClimbCard` | `data` (climb response), `loading`, `error`, `queue`, `syncing`, `singleAccount` | LP ladder or rolling win rate in a `BaseLineChart`; promotions closer than 5 matches keep the dot, only the last is labelled; same emits |
| `SoloChampionLp` | `data` (climb response), `loading`, `error`, `queue`, `syncing` | Up to five `BaseDivergingBar` rows and the left-out note; emits `retry`, `sync` |
| `SoloFocusCard` | `focus` (from stat-trends), `verdict`, `loading` | `mp-card--highlight`: finding, evidence, `BaseGoalStrip`, "Next match" fix; renders nothing without a focus |
| `SoloPatterns` | `data` (win-factors response), `loading`, `error` | Session, after-a-loss and match-length cards; the section is hidden when none qualify; emits `retry` |
| `SoloDeathZones` | `data` (death-zones response), `loading`, `error`, `queue`, `syncing` | `BaseDeathMap`, the zone list (toggles the breakdown filter) and the When / How / What it cost bars; the backfill's progress in the card's slot while `ready` is false; emits `retry`, `show-all-queues`, `sync` |

---

## 11. Shared Components

None at present. `AnalysisLayout` was retired with the Solo redesign (5b); pages compose design-system cards directly.

---

## 12. Root-Level Components

Located in `client/src/components/`.

### `AppHeader`
80px top header on every `/app/*` page (56px below 900px), used by `AppLayout`. Logo left (→ `/app/overview`); PillNav centre (Overview, Champion Select, Matches, Solo; `router-link` with `aria-current="page"`), hidden below 900px; avatar right opens a menu of plain buttons and links, all reachable with Tab, with the Riot account switcher (when more than one account is linked or the overall view is available; reuses `components/header/AccountDropdownList.vue`), Settings (`/app/user`), Feedback (`/app/feedback`) and Log out. Closes on Escape, outside click and route change.

### `AppTabBar`
Fixed bottom tab bar shown only below 900px, `<nav aria-label="Main" class="mp-tabbar">`. One tab per nav item with icon above label (`house` Overview, `shield` "Champ Select", `swords` Matches, `chart-line` Solo); active tab uses `aria-current="page"`.

### `SessionExpiredBanner`
Fixed top banner (z-index 400) with slide-down transition. Appears on 401 detection. Preserves current route for redirect after re-login.

### `LinkRiotAccountModal`
BaseModal with: Game Name, Tag Line (3–5 chars, alphanumeric), Region select (16 regions). Error mapping: `RIOT_ACCOUNT_NOT_FOUND`, `ACCOUNT_ALREADY_LINKED`. Resets form on open.

### `DeleteAccountModal`
BaseModal requiring "DELETE" confirmation text + password. Prevents close during deletion. Uses `destructive` button variant.

### `ChampionSelectMatchups`
Champion Select: lane matchups for the selected pick — Strong into / Weak into lists (opponents met 3+ times in lane) with lane records. Props `championName`, `strong`, `weak`, `status` (`loading`, `error`, `ready`); emits `retry`.

### `ChampionSelectSearch`
Champion Select: "Check a matchup" — a labelled `BaseInput` for the enemy champion and the matching results for your champions in the selected role. Props `matchups`, `role`, `roleName`, `disabled`.

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
Tracks analysis/sync status (stored status plus the live aggregate run).

### `useSyncMatches()`
Built on `useAnalysisStatus()`. Drives `SyncProgress` and the "Sync matches" button: `syncState` (`null`, `running`, `waiting`, `done`, `failed`), `isSyncing`, `progressCurrent`, `progressTotal` (0 while a multi-account run is still counting), `syncedCount`, `lastSyncAt`, `startSync()`.

---

## 14. Stores

Located in `client/src/stores/`. Using **Pinia**.

### `authStore`
- **State**: user object, session expiry tracking (`wasAuthenticated` pattern)
- **Actions**: `initialize()`, `login()`, `register()`, `verify()`, `logout()`, `changePassword()`, `linkRiotAccount()`, `unlinkRiotAccount()`, `triggerSync()`, `refreshUser()`
- **Computed**: `isAuthenticated`, `isVerified`, `isInitialized`, `username`, `email`, `tier`, `primaryRiotAccount`

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
- **Dashboards**: `getOverview()`, `getSoloDashboard()`, `getChampionSelectData()`, `getMatchActivity()`, `getSoloStatTrends()`, `getSoloWinFactors()`
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
| Root Components | `client/src/components/` |
| Views | `client/src/views/` |
| Layouts | `client/src/layouts/AppLayout.vue` |
| Router | `client/src/router/index.js` |
| Stores | `client/src/stores/` |
| Composables | `client/src/composables/` |
| Services / API | `client/src/services/` |
| Utilities | `client/src/utils/` |
| App Entry | `client/src/App.vue`, `client/src/main.js` |

### `MatchDetails`
The open match: loading skeleton, `error` (`failed` with retry, `not-found`, `no-account`), empty and content states. Props `match`, `baseline`, `decidingStat`, `accountId`, `loading`, `error`; emits `retry`. Builds the "Download data" JSON when `StatSnapshot` asks for it. Lanes sit beside the team summary when the column is 760px or wider (container query).

### `MatchHeader`
The open match's banner (ChampionHero match banner): splash art (plain card when it fails), result eyebrow with the time (Victory purple / Defeat orange / Remake neutral), "Champion, Role" as an `h2`, queue · length and a K / D / A glass chip. LP and rank chips come with the LP data. The trend-badge chip is retired (Phase 4c) in favor of `DecidingStatCard`.

### `DecidingStatCard`
"What decided it" (Phase 4c): one `mp-card mp-card--highlight` card between the banner and `MatchNarrative`. An eyebrow, a one-line finding, up to three `BaseUsualMeter`s (the deciding stat first, the rest by |score| descending), the usual key and an optional "Next match" fix row. Copy and formatting in `utils/decidingStat.js`; left out entirely when `decidingStat` is null.

### `MatchNarrative`
Lane by lane, fetched via `getMatchNarrative()`: one LaneBar button per role (`aria-expanded`, `aria-label` in words) growing right in purple when ahead and left in orange when behind at 10 minutes (±1,500 fills a half, under 300 is even), the player's icon ringed; the title counts lanes won. ARAM lists both teams by damage share. Own loading, error with retry, and empty states.

### `LaneMatchupDetails`
One sentence on how the lane went, then early laning (gold lead, CS lead, deaths) and match impact (damage share, kill participation, vision score) as tables.

### `TeamComparison`
SplitBars (your team purple, enemy orange, numbers on both ends, `role="img"` text alternatives) for damage, dragons, barons and towers; an empty track when neither team took an objective. The title states the damage share; the gold lead at 15 is the caption.

### `StatSnapshot`
"All your stats": a closed disclosure whose summary row counts the stats above and below usual. Open, all ten stats as StatTiles with the comparison to your average (adjusted for match length where it matters) and "Download data". Emits `download`.

### `MatchActions`
Next-step card linking to Solo ("See your trends").


