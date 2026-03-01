# Feature: MA-07 — Match History: Overall Mode (Interleaved)

## Problem Statement
The Match History page currently shows matches from a single account. When a user has multiple accounts and is in "Overall" mode, they want to see all their recent games from all accounts in one chronological timeline — making it easy to review their entire session regardless of which account they played on.

## Proposed Solution
When the active account is "Overall", the Match History page fetches matches from all linked accounts via `?account=all`. The match list is sorted by game time (most recent first) and each match row includes a small account tag showing which account the game was played on.

## User Stories
### Primary User Story
As a player who switches between accounts during a session, I want to see all my recent games in one list so I can review my full session without switching accounts.

### Additional User Stories
- As a player, I want to quickly identify which account each game was played on
- As a player, I want to click a match and see its full details regardless of which account it's from
- As a player, I want to filter the interleaved list by queue type and it applies across all accounts

## Requirements

### Functional Requirements
1. When `activeAccountPuuid === 'overall'`, match list API call uses `?account=all`
2. Matches from all accounts returned interleaved by `game_start_time DESC`
3. Each match in the response includes `accountGameName` and `accountRegion` fields (from MA-02)
4. `MatchRow` component shows an account tag when `accountGameName` is present
5. Clicking a match row expands details as normal — `MatchDetails` works with any PUUID's match
6. Queue filter and pagination work across all accounts (applied server-side)
7. When viewing a specific account, match history works exactly as today (no account tags)

### Non-Functional Requirements
- **Performance**: Single API call returns interleaved results — no client-side merging/sorting
- **Layout**: Account tag must not break the existing `MatchRow` layout; it's an additive element
- **Consistency**: Match details expanded from Overall mode must work correctly (the match endpoint needs to accept matches from any linked PUUID)

## Technical Approach

### Backend Changes
Already handled by MA-02. The match list endpoint with `?account=all` returns interleaved matches tagged with account info.

Additionally:
- [ ] `MatchDetailsEndpoint` must accept matches from any linked PUUID (not just primary). Uses `VerifyPuuidOwnershipAsync` — already implemented.

### Frontend Changes
**Framework**: Vue

#### Modified Component: `MatchRow.vue`
- [ ] Add optional `accountGameName` prop
- [ ] Add optional `accountRegion` prop  
- [ ] When present, render a subtle account tag inline with existing match info

#### Modified View: `MatchesPage.vue`
- [ ] API call includes `?account=` parameter (from MA-03 state)
- [ ] Pass `accountGameName` / `accountRegion` through to `MatchRow` when in Overall mode
- [ ] Watch `activeAccountPuuid` to re-fetch match list on account switch

### Database Changes
None.

## UI/UX Requirements

### Match Row with Account Tag

**Individual account mode** (no change):
```
🟢 Win  [Jinx icon]  Jinx   12/3/8   3.3 KDA   Ranked Solo   2h ago
```

**Overall mode** (with account tag):
```
🟢 Win  [Jinx icon]  Jinx   12/3/8   3.3 KDA   Ranked Solo   2h ago  [FakerMain · EUW]
```

**Account tag styling**:
- Position: Right-aligned, after the timestamp, before the expand chevron
- Style: `text-3xs px-1.5 py-0.5 rounded-sm bg-background-elevated text-text-secondary`
- Font: `font-medium tracking-tight`
- On row hover: tag remains at same opacity (doesn't compete with row hover effect)

**Behavior**:
- Account tag is purely informational — not clickable (don't want accidental account switches when trying to expand a match)
- Account tag hidden on narrow viewports if space is tight (responsive breakpoint)

### Match Details in Overall Mode
No changes needed — `MatchDetails` already works by match ID. The backend resolves the match participant from the PUUID, and `VerifyPuuidOwnershipAsync` validates ownership.

## Testing Strategy

### Unit Tests (Vitest)
- [ ] `MatchRow` renders account tag when `accountGameName` prop provided
- [ ] `MatchRow` hides account tag when `accountGameName` not provided
- [ ] Account tag displays correct text format "GameName · Region"

### Integration Tests (xUnit)
- [ ] Match list with `?account=all` returns matches from multiple PUUIDs
- [ ] Matches are sorted by `game_start_time DESC` across PUUIDs
- [ ] Each match includes `accountGameName` and `accountRegion`
- [ ] Match details accessible for matches from any linked PUUID

### Manual Testing
1. Overall mode — verify interleaved match list with tags from different accounts
2. Queue filter in Overall mode — filters apply across all accounts
3. Switch to specific account — tags disappear, only that account's matches shown
4. Expand match details from Overall list — detail panel works correctly

## Dependencies
### Internal Dependencies
- [ ] MA-02 (backend returns interleaved matches with account info)
- [ ] MA-03 (active account state drives the `?account=` param)

### External Dependencies
- None

## Risks and Mitigations
| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Interleaved list is confusing with many accounts | Low | Low | Account tags provide clear attribution; limit is practical (most users have 2-3) |
| Match details fail for non-primary account's match | Medium | Low | Already handled by `VerifyPuuidOwnershipAsync` |
| Account tag overflows on small viewports | Low | Medium | Hide tag below a breakpoint or truncate game name |
