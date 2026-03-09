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
1. ✅ When in Overall mode, Overview fetches data with `?account=all`
2. ✅ `OverviewPlayerHeader` replaced by an `OverviewAccountCards` component showing all accounts
3. ✅ Each account card shows: game name, tag, Flex rank, Solo rank, profile icon, level badge
4. ✅ Clicking an account card switches the active account to that card's PUUID
5. ✅ `RankSnapshot` shows the highest-ranked account's data (with label "Highest Rank" in Overall mode)
6. ✅ `MatchActivityHeatmap` shows combined data from all accounts
7. ✅ `LastMatchCard` shows the most recent match across all accounts (with account name tag in Overall mode)
8. ✅ Combined stats summary passed to child components (total games, aggregate winrate, aggregate KDA)
9. ✅ When in individual account mode, Overview works exactly as today (no changes)

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

#### New Component: `OverviewAccountCards.vue` ✅ COMPLETE
Location: `client/src/components/overview/OverviewAccountCards.vue`

Renders a grid of account summary cards. Only shown in Overall mode. Displays up to 3 accounts per row with auto-fit responsive grid.

Props:
- `accounts: Array` — account summary objects from API
- `linkedAccounts: Array` — linked riot accounts for resolving rank/level/icon data
- `activeAccountPuuid: String` — current active account for highlighting

Events:
- `@select(accountId)` — user clicked a specific account card

Card Structure:
- **Top meta**: "Primary" chip if account is marked as primary
- **Avatar section**: 
  - Profile icon (or fallback avatar icon)
  - Level badge (top-right corner)
- **Account info**:
  - Game name + tag line (e.g., "FakerMain #EUW")
  - Flex rank: "Flex - Diamond II - 67 LP"
  - Solo rank: "Solo - Diamond II - 45 LP"

Features:
- Grid layout: `grid-template-columns: repeat(auto-fit, minmax(180px, 1fr))`
- Responsive: Adapts to screen size
- Interactive: Hover effect with border/shadow transition
- Active state: Primary account highlighted with left accent bar and enhanced shadow
- Icon error handling: Falls back to default avatar icon if profile icon fails to load
- Rank display: Automatically resolves tier/rank/LP from account or linked account data

#### Modified View: `OverviewPage.vue` ✅ COMPLETE
- ✅ Watches `authStore.isOverallMode` to conditionally render Overall vs individual layout
- ✅ In Overall mode: renders `OverviewAccountCards` in `#header` slot instead of `OverviewPlayerHeader`
- ✅ In Overall mode: passes combined heatmap data to `MatchActivityHeatmap` (same component, different data source)
- ✅ In Overall mode: passes `accountName` prop to `LastMatchCard` (shows account that played the match)
- ✅ Displays first 3 account cards (overflow handled via grid layout)
- ✅ `handleAccountSelect` handler links to `authStore.setActiveAccount()`
- ✅ `rankSnapshotLabel` computed property shows "Highest Rank" in Overall mode with queue type suffix
- ✅ Combined data flows to all child components via parent state

#### Modified Component: `LastMatchCard.vue` ✅ COMPLETE
- ✅ Added optional `accountName` prop (shown as small tag when in Overall mode)
- ✅ Tag displays account game name next to timestamp in Overall mode

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
- ✅ `OverviewAccountCards` renders correct number of account cards
- ✅ Clicking account card emits `select` event with account ID
- ✅ `OverviewAccountCards` highlights active account with primary indicator
- ✅ `OverviewAccountCards` displays rank data correctly (resolves from linked accounts if needed)
- ✅ `OverviewAccountCards` shows profile icon or fallback avatar
- ✅ `OverviewAccountCards` displays level badge
- ✅ `LastMatchCard` shows account tag when `accountName` prop provided
- ✅ `LastMatchCard` hides account tag when `accountName` not provided
- ✅ Overview page renders account cards in Overall mode
- ✅ Overview page renders `OverviewPlayerHeader` in non-Overall mode
- ✅ Account selection triggers data refresh via store watcher

### Integration Tests
- ✅ API call fetches combined data via `getOverview()`, `getMatchActivity()`, `getSoloDashboard()`
- ✅ Account card click updates `authStore.activeAccountPuuid` and refreshes data
- ✅ RankSnapshot displays "Highest Rank" label in Overall mode
- ✅ Match heatmap displays combined activity from all accounts

## Dependencies
### Internal Dependencies
- ✅ MA-02 (backend returns `accountSummaries[]` in Overview response)
- ✅ MA-03 (active account state drives `isOverallMode` flag and account switching logic)
- ✅ `authStore.setActiveAccount()` (switches active account; triggers data refresh)
- ✅ `authStore.isOverallMode` (conditional rendering flag)
- ✅ `authStore.riotAccounts` (linked account data for resolving icons/ranks)

### External Dependencies
- None

## Risks and Mitigations
| Risk | Impact | Probability | Mitigation | Status |
|------|--------|-------------|------------|--------|
| Too many account cards overflow the header | Low | Low | Grid layout displays first 3 cards, responsive grid handles rest | ✅ RESOLVED |
| RankSnapshot unclear in Overall mode (which rank?) | Medium | Medium | Label "Highest Rank" with queue type suffix shown in Overall mode | ✅ RESOLVED |
| Combined heatmap data makes individual activity hard to read | Low | Medium | Heatmap currently shows combined data; future enhancement: color-code by account | ⏳ FUTURE |
| Missing account data in lastMatch (which account played?) | Medium | High | Backend includes account reference; frontend shows as tag on card | ✅ RESOLVED |

## Implementation Notes

### What Was Built
- **OverviewAccountCards Component**: Displays all linked accounts in a responsive grid layout with rank, profile icon, and level badge. Up to 3 cards visible per row; auto-wraps on smaller screens.
- **Conditional Rendering**: Overview page switches between `OverviewPlayerHeader` (individual mode) and `OverviewAccountCards` (Overall mode) based on `authStore.isOverallMode`.
- **Account Selection**: Clicking any account card updates the active account and triggers automatic data refresh.
- **Rank Snapshot Label**: Changes to "Highest Rank (Queue Type)" in Overall mode to clarify which rank is shown.
- **LastMatchCard Enhancement**: Optional `accountName` prop displays account tag next to match info in Overall mode.
- **Data Integration**: All API responses flow through parent component; data refresh triggered by account changes.

### Deviations from Plan
- Account cards display **Flex and Solo ranks separately** (not just region) for clarity in multi-queue environment
- Cards limited to **first 3 displayed inline** with responsive grid (more mature than horizontal scroll)
- **No "games today" count** on cards currently (backend may not provide per-account daily stats in combined response)
- Account name tag on LastMatchCard appears as **small text chip** rather than prominent display

### Future Enhancements
- Per-account activity heatmap differentiation (color or opacity coding)
- Drag-to-reorder account cards to customize display order
- Account card click animation feedback
- Expanded stats comparison (side-by-side KDA, win rate per account)
