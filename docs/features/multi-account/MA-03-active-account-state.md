# Feature: MA-03 — Frontend: Active Account State Management

## Problem Statement
The frontend currently has no concept of an "active account" — all API calls resolve to the primary PUUID server-side. To support the sidebar account switcher and the "Overall" aggregated view, the frontend needs a reactive state layer that tracks which account (or "overall") is currently selected, persists it across sessions, and pipes it into all API calls.

## Proposed Solution
Extend `authStore` with an `activeAccountPuuid` ref (values: `'overall'` or a specific PUUID string). Add a computed `activeAccount` getter. Persist the selection to `localStorage`. Update all API service functions to include the `?account=` parameter based on this state.

## User Stories
### Primary User Story
As a user, I want my selected account context (Overall or specific account) to persist when I navigate between pages or refresh the browser so I don't have to re-select every time.

### Additional User Stories
- As a user, I want all pages to instantly reflect my account selection without reloading
- As a developer, I want a single source of truth for the active account that all API calls reference

## Requirements

### Functional Requirements
1. `authStore` gains `activeAccountPuuid` state — values: `'overall'` | `'{puuid}'`
2. Default value: `'overall'` (configurable later in MA-09 Display Preferences)
3. `activeAccountPuuid` is persisted to `localStorage` key `mongoose_active_account`
4. On login: restore from `localStorage` if the stored PUUID is still linked; otherwise reset to `'overall'`
5. On account unlink: if the unlinked account was active, reset to `'overall'`
6. On account link (first account for free user): set active to that account's PUUID
7. Computed `activeAccount` getter returns the full `RiotAccount` object for the active PUUID, or `null` for `'overall'`
8. Computed `isOverallMode` getter returns `true` when `activeAccountPuuid === 'overall'`
9. `setActiveAccount(puuid)` action validates the PUUID is linked before setting
10. All API calls in `authApi.js` include `?account=` parameter, mapped from `activeAccountPuuid` via `getAccountParam()`: `'overall'` → `'all'`, PUUID string → passed through verbatim

### Non-Functional Requirements
- **Performance**: Switching accounts should not cause full page reload — only data re-fetches
- **Reactivity**: Pages watching the active account must re-fetch data when it changes (via `watch`)

## Technical Approach

### Frontend Changes
**Framework**: Vue

#### authStore.js Changes
```javascript
// New state
const activeAccountPuuid = ref(localStorage.getItem('mongoose_active_account') || 'overall')

// New computed
const activeAccount = computed(() => {
  if (activeAccountPuuid.value === 'overall') return null
  return riotAccounts.value.find(a => a.puuid === activeAccountPuuid.value) ?? null
})

const isOverallMode = computed(() => activeAccountPuuid.value === 'overall')

// New action
function setActiveAccount(puuid) {
  if (puuid !== 'overall') {
    const isLinked = riotAccounts.value.some(a => a.puuid === puuid)
    if (!isLinked) return
  }
  activeAccountPuuid.value = puuid
  localStorage.setItem('mongoose_active_account', puuid)
}

// API parameter mapping: frontend sentinel 'overall' → backend value 'all'
// This is the single translation point between frontend state and backend contract (MA-02)
function getAccountParam() {
  const puuid = activeAccountPuuid.value
  return puuid === 'overall' ? 'all' : puuid
}

// Validation on init: ensure stored account is still linked
function validateActiveAccount() {
  if (activeAccountPuuid.value === 'overall') return
  const isLinked = riotAccounts.value.some(a => a.puuid === activeAccountPuuid.value)
  if (!isLinked) {
    setActiveAccount('overall')
  }
}
```

#### authApi.js Changes
- [ ] Add helper `getAccountParam()` that reads from `authStore.activeAccountPuuid` and maps it to the backend query parameter value: `'overall'` → `'all'`, PUUID string → passed through verbatim. This is the **single boundary** between the frontend sentinel and the backend contract (MA-02 expects `?account=all`, not `?account=overall`)
- [ ] All data-fetching functions (`getOverview`, `getSoloDashboard`, `getWinrateTrend`, `getMatchList`, etc.) append `?account={getAccountParam()}` to their requests
- [ ] Auth-only functions (login, register, link, unlink) are NOT affected

#### Page Changes (watchers)
- [ ] `OverviewPage.vue` — watch `activeAccountPuuid`, re-call `fetchData()`
- [ ] `SoloStatsPage.vue` — watch `activeAccountPuuid`, re-call dashboard + trend fetches
- [ ] `MatchesPage.vue` — watch `activeAccountPuuid`, re-fetch match list
- [ ] `ChampionSelectPage.vue` — watch `activeAccountPuuid`, re-fetch matchup data

### Backend Changes
None — this feature is frontend-only. It depends on MA-02 for the backend to accept `?account=`.

### Database Changes
None.

## Testing Strategy

### Unit Tests (Vitest)
- [ ] `setActiveAccount('overall')` sets value and localStorage
- [ ] `setActiveAccount(validPuuid)` sets value when PUUID is linked
- [ ] `setActiveAccount(invalidPuuid)` does not change value
- [ ] `activeAccount` returns correct RiotAccount object
- [ ] `isOverallMode` is true for 'overall', false for PUUID
- [ ] `getAccountParam()` returns `'all'` when `activeAccountPuuid` is `'overall'`
- [ ] `getAccountParam()` returns the PUUID verbatim when `activeAccountPuuid` is a PUUID string
- [ ] `validateActiveAccount` resets to 'overall' when stored PUUID is no longer linked
- [ ] On unlink of active account, resets to 'overall'

### Integration Tests
- [ ] Page re-fetches data when active account changes (component test with mocked API)

## Dependencies
### Internal Dependencies
- [ ] MA-02 (backend must accept `?account=` parameter)
- [ ] Existing `authStore` structure

### External Dependencies
- None

## Risks and Mitigations
| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Stale localStorage referencing deleted PUUID | Low | Medium | `validateActiveAccount()` on init and after unlink |
| All pages re-fetching simultaneously on switch | Medium | Medium | Each page checks if it's the active route before re-fetching |
| Race condition between switch and in-flight API calls | Low | Low | Use abort controllers or ignore stale responses |

## Open Questions
- [ ] Should switching accounts show a brief loading state on the current page, or optimistically keep old data until new data arrives? Recommend: keep old data with subtle loading indicator.
