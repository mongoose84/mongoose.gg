# Feature: MA-06 — Overview Page: Overall Mode

## Problem Statement
The Overview page currently shows data for a single primary account. When a user has multiple linked accounts, the Overview provides no cross-account awareness — they can't see their total activity, compare ranks, or get a snapshot of all their identities in one glance.

## Proposed Solution
When the active account is set to "Overall", the Overview page shows an enhanced layout:
1. Account summary cards (rank + activity per account)
2. Combined summary stats (total games, aggregate win rate, aggregate KDA)
3. Merged match activity heatmap (all accounts combined)
4. Most recent match across all accounts

When a specific account is active, the Overview behaves exactly as it does today.

## User Stories
### Primary User Story
As a player with multiple accounts, I want the Overview to show me a snapshot of all my accounts at a glance so I can quickly assess my overall activity and rank status.

### Additional User Stories
- As a player, I want to see which account I played on most recently
- As a player, I want to see my combined games played today/this week across all accounts
- As a player, I want to quickly switch to a specific account by clicking its card on the Overview

## Requirements

### Functional Requirements
1. When in Overall mode, Overview fetches data with `?account=all`
2. `OverviewPlayerHeader` replaced by an `OverviewAccountCards` component showing all accounts
3. Each account card shows: game name, tag, region, rank, LP, games today
4. Clicking an account card switches the active account to that card's PUUID
5. `RankSnapshot` shows the highest-ranked account's data (with label "Highest Rank")
6. `MatchActivityHeatmap` shows combined data from all accounts
7. `LastMatchCard` shows the most recent match across all accounts (with account name tag)
8. Combined stats summary: total games, aggregate winrate, aggregate KDA
9. When in individual account mode, Overview works exactly as today (no changes)

### Non-Functional Requirements
- **Performance**: Single API call (`?account=all`) returns all data — no N+1 calls per account
- **Layout**: One scroll max, consistent with Overview's time budget of 5–15 seconds
- **Accessibility**: Account cards are keyboard-navigable, clickable with Enter/Space

## Technical Approach

### Backend Changes
Already handled by MA-02. The Overview endpoint with `?account=all` returns `accountSummaries[]` and `combinedStats`.

### Frontend Changes
**Framework**: Vue
**Components**:

#### New Component: `OverviewAccountCards.vue`
Location: `client/src/components/overview/OverviewAccountCards.vue`

Renders a row of account summary cards. Only shown in Overall mode.

Props:
- `accounts: Array` — account summary objects from API
- `activeAccountPuuid: String` — current active (for highlight, though in Overall mode they're all shown)

Events:
- `@select(puuid)` — user clicked a specific account card

Each card shows:
```
┌──────────────┐
│ FakerMain    │
│ EUW · D2     │
│ 67 LP        │
│ 5 games today│
└──────────────┘
```

#### Modified View: `OverviewPage.vue`
- [ ] Watch `authStore.isOverallMode` to conditionally render Overall vs individual layout
- [ ] In Overall mode: render `OverviewAccountCards` in `#header` slot instead of `OverviewPlayerHeader`
- [ ] In Overall mode: pass combined heatmap data to `MatchActivityHeatmap`
- [ ] In Overall mode: add account name tag to `LastMatchCard` data
- [ ] API call includes `?account=` parameter (from MA-03 state)

#### Modified Component: `LastMatchCard.vue`
- [ ] Add optional `accountName` prop (shown as small tag when in Overall mode)

### Database Changes
None.

## UI/UX Requirements

### Overall Mode Overview Layout

**Structure**:
```
┌─────────────────────────────────────────────────────────────┐
│  Your Accounts                                               │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       │
│  │ FakerMain    │  │ FakerSmurf   │  │ FakerFlex    │       │
│  │ EUW · D2     │  │ EUW · P1     │  │ NA · G2      │       │
│  │ 67 LP        │  │ 45 LP        │  │ 20 LP        │       │
│  │ 5 games today│  │ 2 today      │  │ 0 today      │       │
│  └──────────────┘  └──────────────┘  └──────────────┘       │
│                                                              │
│  ─── At a Glance ──────────────────────────────────────────  │
│  ┌──────────────────────┐  ┌──────────────────────────────┐  │
│  │ Rank Snapshot        │  │ Champion Select CTA          │  │
│  │ (highest rank)       │  │ (unchanged)                  │  │
│  └──────────────────────┘  └──────────────────────────────┘  │
│                                                              │
│  ─── Recent Games ─────────────────────────────────────────  │
│  ┌──────────────────────┐  ┌──────────────────────────────┐  │
│  │ Match Heatmap        │  │ Analysis Status + Solo CTA   │  │
│  │ (combined all accts) │  │ (combined stats)             │  │
│  └──────────────────────┘  └──────────────────────────────┘  │
│                                                              │
│  ─── Latest Match ─────────────────────────────────────────  │
│  ┌──────────────────────────────────────────────────────────┐│
│  │ Win · Jinx · 12/3/8 · 2h ago · FakerMain               ││
│  └──────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────┘
```

### Account Cards

**Styling**:
- Container: `flex gap-md overflow-x-auto pb-xs` (horizontal scroll if many accounts)
- Card: `bg-background-surface border border-border rounded-lg p-lg min-w-[160px] cursor-pointer transition-all duration-200`
- Card hover: `hover:border-primary hover:bg-primary-soft`
- Game name: `text-sm font-semibold text-text`
- Region + rank: `text-xs text-text-secondary`
- LP: `text-sm font-bold text-text`
- Games today: `text-xs text-text-secondary`
- Today count > 0: `text-xs text-success` (green for active play)

### LastMatchCard Account Tag

When `accountName` is provided, show a small tag:
```
[Win · Jinx · 12/3/8 · 2h ago]  [FakerMain]
```
Tag styling: `text-3xs px-1.5 py-0.5 rounded-sm bg-background-elevated text-text-secondary ml-sm`

## Testing Strategy

### Unit Tests (Vitest)
- [ ] `OverviewAccountCards` renders correct number of account cards
- [ ] Clicking account card emits `select` with PUUID
- [ ] `LastMatchCard` shows account tag when `accountName` prop provided
- [ ] `LastMatchCard` hides account tag when `accountName` not provided
- [ ] Overview page renders account cards in Overall mode
- [ ] Overview page renders `OverviewPlayerHeader` in individual mode

### Integration Tests
- [ ] API call includes `?account=all` in Overall mode
- [ ] API call includes `?account={puuid}` in individual mode

## Dependencies
### Internal Dependencies
- [ ] MA-02 (backend returns `accountSummaries` in Overview response)
- [ ] MA-03 (active account state drives the `?account=` param and `isOverallMode` flag)

### External Dependencies
- None

## Risks and Mitigations
| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Too many account cards overflow the header | Low | Low | Horizontal scroll with subtle fade edge |
| RankSnapshot unclear in Overall mode (which rank?) | Medium | Medium | Label "Highest Rank" and show account name |
| Combined heatmap data makes individual activity hard to read | Low | Medium | Use opacity/color to differentiate per-account in future iteration |
