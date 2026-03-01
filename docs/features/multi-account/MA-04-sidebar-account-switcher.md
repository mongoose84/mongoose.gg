# Feature: MA-04 — Sidebar Account Switcher

## Problem Statement
Users have no way to switch between their linked accounts or select "Overall" mode without navigating to Settings. The active account context is invisible — users can't tell which account they're currently viewing data for.

## Proposed Solution
Add a single-row "Account Switcher" to the bottom of `AppSidebar` (above the User nav item) that displays **only the currently active account**. Clicking the row opens a dropdown listing all linked accounts plus an "Overall" virtual entry. Selecting an entry switches the active account context and closes the dropdown. This follows the standard "workspace switcher" pattern (Slack, GitHub, Figma) — one row regardless of how many accounts exist, keeping the sidebar compact. In collapsed mode, clicking the active account icon opens the same dropdown as a popover.

## User Stories
### Primary User Story
As a player with multiple accounts, I want to switch between them with a single click from any page so I don't have to navigate away to change context.

### Additional User Stories
- As a user, I want to always see which account I'm currently viewing data for
- As a user, I want quick access to link a new account from the sidebar
- As a user in collapsed sidebar mode, I want the switcher to still be accessible

## Requirements

### Functional Requirements
1. Sidebar shows a single-row "Account Switcher" below the nav items, separated by a divider
2. The row displays the active account's profile icon, game name, region tag, rank badge, and a chevron (▾) indicator
3. When active account is "Overall", the row shows an aggregate icon (Σ or chart icon) and the label "Overall"
4. Clicking the row opens a dropdown listing all accounts plus "Overall" (if 2+ accounts linked)
5. The active entry in the dropdown has a checkmark (✓) indicator
6. Selecting a dropdown entry calls `authStore.setActiveAccount(puuid)` (or `'overall'`) and closes the dropdown
7. "Link Account" button at bottom of dropdown opens `LinkRiotAccountModal`
8. Collapsed sidebar: shows only the active account's profile icon (or Σ icon for Overall). Click opens the dropdown as a popover positioned to the right.
9. Account list updates reactively when accounts are linked/unlinked
10. Dropdown closes on outside click, Escape key, or account selection

### Non-Functional Requirements
- **Performance**: No additional API calls — reads from `authStore.riotAccounts`
- **Accessibility**: Trigger row has `aria-haspopup="listbox"` and `aria-expanded`. Dropdown uses `role="listbox"` with `role="option"` per entry. Active entry has `aria-selected="true"`. Keyboard navigable (arrow keys to move, Enter to select, Escape to close).
- **Animation**: Dropdown uses slide-down + fade animation (150ms ease-out). Selection change in the trigger row transitions smoothly.

## Technical Approach

### Frontend Changes
**Framework**: Vue
**Components**:

#### New Component: `AccountSwitcher.vue`
Location: `client/src/components/sidebar/AccountSwitcher.vue`

Renders the single-row trigger and manages the dropdown. Handles both expanded and collapsed sidebar modes internally.

Props:
- `collapsed: Boolean` — whether sidebar is in collapsed mode
- `accounts: Array` — from `authStore.riotAccounts`
- `activeAccountPuuid: String` — from `authStore.activeAccountPuuid`
- `showOverall: Boolean` — true when 2+ accounts (computed from accounts length)

Events:
- `@select(puuid)` — emits when user selects an account from the dropdown
- `@link` — emits when user clicks "Link Account"

Internal state:
- `isOpen: Boolean` — controls dropdown visibility

Behavior:
- **Expanded mode**: Renders a clickable row showing the active account. Click toggles the dropdown below the row, anchored to the left edge of the switcher.
- **Collapsed mode**: Renders only the active account's profile icon. Click opens the dropdown as a popover positioned to the right of the sidebar (using Headless UI `Popover`).
- Dropdown uses Headless UI `Listbox` for keyboard navigation and accessibility.

#### Modified Component: `AppSidebar.vue`
- Import and render `AccountSwitcher` below nav items (above Feedback section)
- Pass `isCollapsed` state
- Wire `@select` to `authStore.setActiveAccount`
- Wire `@link` to open `LinkRiotAccountModal`

### Backend Changes
None.

### Database Changes
None.

## UI/UX Requirements

### Expanded Sidebar — Trigger Row

**Structure (single row, always visible)**:
```
── ─────────────────────────
[icon] FakerMain · EUW [D2] ▾    ← click opens dropdown
── ─────────────────────────
💬 Feedback
⚙ User
```

When "Overall" is active:
```
── ─────────────────────────
[Σ]  Overall               ▾    ← aggregate icon
── ─────────────────────────
```

**Trigger row styles**:
- Container: `border-t border-border py-sm`
- Row: `flex items-center gap-sm px-md py-xs mx-sm rounded-md cursor-pointer transition-all duration-200 hover:bg-background-elevated`
- Profile icon: Riot profile icon, `w-5 h-5 rounded-full` (or Σ icon `w-5 h-5` for Overall)
- Account name: `text-sm font-medium text-text truncate`
- Region + rank: `text-3xs text-text-secondary` inline after name
- Rank badge: `text-3xs px-1.5 py-0.5 rounded-sm bg-background-elevated text-text-secondary`
- Chevron: `w-4 h-4 text-text-secondary ml-auto shrink-0 transition-transform duration-200` (rotates 180° when open)

### Expanded Sidebar — Dropdown

Opens below the trigger row, anchored to the switcher's left edge, same width as the sidebar content area.

**Structure**:
```
┌─────────────────────────────┐
│ ✓ Overall                   │  ← checkmark on active
│   FakerMain · EUW  [D2]    │
│   FakerSmurf · EUW [P1]    │
│   SmurfThree · NA  [G1]    │
│ ─────────────────────────── │
│   + Link Account            │
└─────────────────────────────┘
```

**Dropdown styles**:
```css
background: var(--color-surface);
border: 1px solid var(--color-border);
border-radius: var(--radius-lg);
box-shadow: var(--shadow-lg);
padding: var(--spacing-xs) 0;
max-height: 280px;
overflow-y: auto;
```

**Dropdown entry styles**:
- Entry: `flex items-center gap-sm px-md py-xs cursor-pointer transition-colors duration-150`
- Active (selected): `text-text font-medium` with checkmark icon (`w-4 h-4 text-primary`) in leading position
- Inactive: `text-text-secondary hover:bg-background-elevated hover:text-text` with empty leading space (same width as checkmark for alignment)
- Profile icon per account: Riot profile icon, `w-5 h-5 rounded-full`
- Overall icon: Σ or chart SVG, `w-5 h-5`
- Rank badge: `text-3xs px-1.5 py-0.5 rounded-sm bg-background-elevated text-text-secondary`
- Divider before Link button: `border-t border-border my-xs`
- Link button: `flex items-center gap-sm px-md py-xs text-xs text-primary hover:text-primary-light cursor-pointer`

### Collapsed Sidebar

**Structure**:
```
[Active icon]  ← small circle avatar (w-8 h-8) or Σ icon, clickable
```

Click opens the dropdown as a popover positioned to the right of the sidebar.

**Popover styles**:
```css
background: var(--color-surface);
border: 1px solid var(--color-border);
border-radius: var(--radius-lg);
box-shadow: var(--shadow-lg);
padding: var(--spacing-xs) 0;
min-width: 220px;
max-height: 280px;
overflow-y: auto;
```

**Popover positioning**:
- Appears to the right of sidebar (`left: 64px + 8px` gap)
- Vertically aligned with the trigger icon
- Uses Headless UI auto-positioning to avoid viewport overflow

### Shared Behavior (Expanded + Collapsed)
- Click outside closes dropdown/popover
- Escape key closes dropdown/popover
- After selecting an account, dropdown/popover closes
- Focus trapped inside when open
- Arrow keys navigate entries, Enter selects, Home/End jump to first/last
- Dropdown/popover animation: fade-in + scale from 95% to 100% (150ms ease-out)

### Accessibility
- Trigger row: `role="button"`, `aria-haspopup="listbox"`, `aria-expanded="true|false"`
- Dropdown: `role="listbox"`, `aria-label="Switch account"`
- Each entry: `role="option"`, `aria-selected="true|false"`
- Overall entry: additional `aria-label="View all accounts combined"`
- Focus moves to first item on open, returns to trigger on close

## Testing Strategy

### Unit Tests (Vitest)
- [ ] Trigger row renders active account name, region, and rank
- [ ] Trigger row shows "Overall" with Σ icon when overall mode is active
- [ ] Clicking trigger row opens dropdown
- [ ] Dropdown lists all accounts + Overall (when 2+ accounts)
- [ ] Overall entry hidden in dropdown when only 1 account linked
- [ ] Active account in dropdown has checkmark indicator
- [ ] Selecting a dropdown entry emits `select` with correct PUUID
- [ ] Selecting a dropdown entry closes the dropdown
- [ ] Clicking "Link Account" in dropdown emits `link` event
- [ ] Escape key closes dropdown
- [ ] Collapsed mode renders only active account icon
- [ ] Collapsed mode click opens popover with full account list

### Manual Testing
1. Click trigger row — dropdown opens with all accounts listed
2. Select a different account — trigger row updates, all pages re-fetch data
3. Collapse sidebar — only icon visible; click opens popover to the right
4. Link new account — appears in dropdown immediately
5. Unlink active account — switches to Overall, trigger row updates
6. Keyboard navigation: Tab to trigger, Enter to open, arrow keys to navigate, Enter to select, Escape to close

## Dependencies
### Internal Dependencies
- [ ] MA-03 (active account state management must exist in store)
- [ ] MA-01 (account management in Settings for link/unlink)

### External Dependencies
- [ ] Headless UI `Popover` component (already in project)

## Risks and Mitigations
| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Dropdown overflows viewport on short screens | Low | Low | `max-height: 280px` with `overflow-y: auto`; Headless UI auto-positioning flips above trigger if needed |
| Popover positioning conflicts with sidebar edge (collapsed mode) | Low | Medium | Use Headless UI auto-positioning; test at various viewport heights |
| Long account names truncate in trigger row | Low | Medium | `truncate` class on name; full name visible in dropdown and on hover via `title` attribute |
