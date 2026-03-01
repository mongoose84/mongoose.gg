# Feature: MA-09 — Settings: Display Preferences

## Problem Statement
Users need a way to configure their multi-account display behavior: which view (Overall vs specific account) loads by default, and how trend charts render multi-account data (merged vs per-account lines). Without this, the app makes opinionated defaults that may not match each user's preference.

## Proposed Solution
Add a "Display Preferences" section to the Settings page with two configurable options:
1. **Default View** — which account context loads on app startup (Overall, or a specific account)
2. **Chart Display Mode** — how trend charts show multi-account data (Merged, or Per-Account Lines)

Both settings are persisted to `localStorage` and read by the relevant composables.

## User Stories
### Primary User Story
As a player, I want to choose whether the app defaults to "Overall" or my main account so it shows what I care about first.

### Additional User Stories
- As a player, I want to choose how my charts display multi-account data so I can see the view that makes most sense to me
- As a player, I want my preferences to persist across sessions

## Requirements

### Functional Requirements
1. Settings page has a "Display Preferences" section between "Linked Riot Accounts" and "Security"
2. "Default View" dropdown with options: "Overall" + each linked account by name
3. "Chart Display Mode" dropdown with options: "Merged" (default), "Per-Account Lines"
4. Both preferences persisted to `localStorage`
5. On app load, `authStore` reads the default view preference and sets `activeAccountPuuid` accordingly
6. Chart components read chart mode from `useChartDisplayMode()` composable
7. If the default view is set to an account that gets unlinked, fall back to "Overall"
8. These settings are only shown when 2+ accounts are linked (meaningless with 1 account)

### Non-Functional Requirements
- **Performance**: No API calls — purely client-side settings stored in localStorage
- **UX**: Changes apply immediately (no save button needed) — use reactive composables
- **Accessibility**: Dropdowns are keyboard-navigable, labeled with visible labels

## Technical Approach

### Frontend Changes
**Framework**: Vue

#### New Section in `UserSettingsPage.vue`
- [ ] "Display Preferences" section conditionally rendered when `authStore.riotAccounts.length >= 2`
- [ ] Default View: `<select>` or `BaseTimeRangeSelect`-style dropdown
- [ ] Chart Display Mode: `<select>` or similar dropdown

#### Modified Composable: `useChartDisplayMode.js` (from MA-08)
Already handles `localStorage` read/write for `mongoose_chart_mode`. MA-09 adds the Settings UI to control it.

#### New Composable: `useDefaultView.js`
Location: `client/src/composables/useDefaultView.js`
```javascript
const DEFAULT_VIEW_KEY = 'mongoose_default_view'

export function useDefaultView() {
  const defaultView = ref(localStorage.getItem(DEFAULT_VIEW_KEY) || 'overall')
  
  function setDefaultView(value) {
    defaultView.value = value
    localStorage.setItem(DEFAULT_VIEW_KEY, value)
  }
  
  return { defaultView, setDefaultView }
}
```

#### Modified Store: `authStore.js`
- [ ] On `initialize()`, after loading user data, read default view preference and call `setActiveAccount(defaultView)` if no previous active selection exists
- [ ] Validation: if default view PUUID is no longer linked, reset to 'overall'

### Backend Changes
None — purely client-side preferences.

### Database Changes
None.

## UI/UX Requirements

### Display Preferences Section

**Layout**: New section on `UserSettingsPage` between "Linked Riot Accounts" (MA-01) and "Security". Same card styling pattern.

**Structure**:
```
Display Preferences
┌─────────────────────────────────────────────────────────┐
│ Default View                                            │
│ The account context shown when you open the app         │
│ [ Overall                    ▼ ]                        │
│   ○ Overall                                             │
│   ○ FakerMain (EUW)                                    │
│   ○ FakerSmurf (EUW)                                   │
│                                                         │
│ Chart Display Mode                                      │
│ How trend charts show data across multiple accounts     │
│ [ Merged (single line)       ▼ ]                        │
│   ○ Merged (single line)                                │
│   ○ Per-Account Lines                                   │
└─────────────────────────────────────────────────────────┘
```

**Components**:
- Section header: `h2` text-lg font-semibold text-text tracking-tight
- Card: `bg-background-surface border border-border rounded-lg p-xl`
- Field label: `text-sm font-medium text-text`
- Field description: `text-xs text-text-secondary mt-xs mb-sm`
- Dropdown: styled `<select>` or custom component matching existing BaseTimeRangeSelect pattern
- Each field separated by: `py-md border-b border-border` (last field no border)

**Behavior**:
- Changes apply immediately on selection (no save button)
- Default View change updates `localStorage` + if user is on Overview, re-fetches data
- Chart Mode change updates `localStorage` + any visible charts reactively re-render
- Section hidden when only 1 linked account

**Accessibility**:
- Each dropdown has an associated `<label>` with `for` attribute
- Dropdowns navigable by keyboard (arrow keys, Enter/Space to open)
- Description text linked to dropdown via `aria-describedby`

## Testing Strategy

### Unit Tests (Vitest)
- [ ] Display Preferences section renders when 2+ accounts linked
- [ ] Display Preferences section hidden when 1 account linked
- [ ] Default View dropdown lists "Overall" + all linked accounts
- [ ] Selecting a default view updates localStorage
- [ ] Chart Mode dropdown lists "Merged" and "Per-Account Lines"
- [ ] `useDefaultView` composable reads/writes localStorage correctly
- [ ] `useChartDisplayMode` composable reads/writes localStorage correctly

### Manual Testing
1. Set default view to "FakerMain" → close and reopen app → lands on FakerMain context
2. Set default view to "Overall" → close and reopen → lands on Overall
3. Unlink the account set as default view → default resets to Overall
4. Change chart mode to "Per-Account Lines" → Solo page charts update immediately
5. Settings hidden when only 1 account linked

## Dependencies
### Internal Dependencies
- [ ] MA-01 (Settings page account section must exist)
- [ ] MA-03 (active account state management)
- [ ] MA-08 (chart components must support chart mode prop)

### External Dependencies
- None

## Risks and Mitigations
| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| User expects server-side persistence | Low | Medium | localStorage is fine for display prefs; note in UI if needed |
| Preferences lost on browser clear | Low | Low | Graceful fallback to defaults; not critical data |
| Default view set to account that gets unlinked | Low | Medium | Validation on init; fallback to 'overall' |
