# Feature: MA-04 — Sidebar Account Switcher

## Problem Statement
Users have no way to switch between their linked accounts or select "Overall" mode without navigating to Settings. The active account context is invisible — users can't tell which account they're currently viewing data for.

## Proposed Solution
Add an "Accounts" section to the bottom of `AppSidebar` (above the User nav item) that lists all linked accounts plus an "Overall" virtual entry. Clicking any entry switches the active account context. The active entry is visually highlighted. In collapsed mode, clicking shows a popover with the same list.

## User Stories
### Primary User Story
As a player with multiple accounts, I want to switch between them with a single click from any page so I don't have to navigate away to change context.

### Additional User Stories
- As a user, I want to always see which account I'm currently viewing data for
- As a user, I want quick access to link a new account from the sidebar
- As a user in collapsed sidebar mode, I want the switcher to still be accessible

## Requirements

### Functional Requirements
1. Sidebar shows an "Accounts" section below the nav items, separated by a divider
2. First entry: "Overall" with aggregate icon (shown only when 2+ accounts linked)
3. Subsequent entries: each linked account — game name, region tag, rank badge
4. Active entry has highlighted background (`bg-primary-soft`) and bold text
5. Clicking an entry calls `authStore.setActiveAccount(puuid)` (or `'overall'`)
6. "Link Account" button at bottom of account list opens `LinkRiotAccountModal`
7. Collapsed sidebar: account section shows only the active account's profile icon (or Σ icon for Overall). Click opens a popover/dropdown with the full list.
8. Account list updates reactively when accounts are linked/unlinked

### Non-Functional Requirements
- **Performance**: No additional API calls — reads from `authStore.riotAccounts`
- **Accessibility**: Account list is a `role="listbox"` with `role="option"` per entry. Active entry has `aria-selected="true"`. Keyboard navigable.
- **Animation**: Smooth transition on selection change. Popover uses dropdown animation pattern.

## Technical Approach

### Frontend Changes
**Framework**: Vue
**Components**:

#### New Component: `AccountSwitcher.vue`
Location: `client/src/components/sidebar/AccountSwitcher.vue`

Renders the account list section. Props:
- `collapsed: Boolean` — whether sidebar is in collapsed mode
- `accounts: Array` — from `authStore.riotAccounts`
- `activeAccountPuuid: String` — from `authStore.activeAccountPuuid`
- `showOverall: Boolean` — true when 2+ accounts (computed from accounts length)

Events:
- `@select(puuid)` — emits when user clicks an account
- `@link` — emits when user clicks "Link Account"

#### New Component: `AccountSwitcherPopover.vue`
Location: `client/src/components/sidebar/AccountSwitcherPopover.vue`

Used in collapsed mode. Renders as a popover positioned to the right of the sidebar, triggered by clicking the compact avatar. Uses Headless UI `Popover` for accessible positioning.

#### Modified Component: `AppSidebar.vue`
- Import and render `AccountSwitcher` below nav items
- Pass `isCollapsed` state to toggle between inline list and popover
- Wire `@select` to `authStore.setActiveAccount`
- Wire `@link` to open `LinkRiotAccountModal`

### Backend Changes
None.

### Database Changes
None.

## UI/UX Requirements

### Expanded Sidebar Account Section

**Structure**:
```
── Accounts ────────────────
● Overall                     ← highlighted when active
○ FakerMain · EUW  [D2]      ← rank badge, dimmed when not active
○ FakerSmurf · EUW [P1]
[+ Link Account]
── ─────────────────────────
⚙ User
```

**Components**:
- Section divider: `border-t border-border mt-auto pt-md` (pushed to bottom via flex)
- Section label: `text-3xs uppercase tracking-wider text-text-secondary px-md mb-xs`
- Account entry: `flex items-center gap-sm px-md py-xs rounded-md cursor-pointer transition-all duration-200`
- Active entry: `bg-primary-soft text-text font-semibold`
- Inactive entry: `text-text-secondary hover:bg-background-elevated hover:text-text`
- Rank badge: `text-3xs px-1.5 py-0.5 rounded-sm bg-background-elevated text-text-secondary`
- Overall icon: `📊` or a small chart SVG icon, `w-4 h-4`
- Profile icon per account: Riot profile icon, `w-5 h-5 rounded-full`
- Link button: `text-xs text-primary hover:text-primary-light cursor-pointer px-md`

### Collapsed Sidebar Account Section

**Structure**:
```
[Active icon]  ← small circle avatar or Σ, clickable
```

Click opens popover positioned to the right. Same content as expanded list.

**Popover**:
```css
background: var(--color-surface);
border: 1px solid var(--color-border);
border-radius: var(--radius-lg);
box-shadow: var(--shadow-lg);
padding: var(--spacing-sm);
min-width: 220px;
```

**Behavior**:
- Popover appears to the right of sidebar (left: 64px + 8px gap)
- Click outside closes
- After selecting an account, popover closes
- Focus trapped inside popover when open

**Accessibility**:
- Popover uses Headless UI `Popover` for focus management and escape key handling
- Each account entry has `role="option"` and `aria-selected`
- Overall entry has `aria-label="View all accounts combined"`

## Testing Strategy

### Unit Tests (Vitest)
- [ ] `AccountSwitcher` renders Overall + all accounts
- [ ] Overall entry hidden when only 1 account linked
- [ ] Active account is visually highlighted
- [ ] Clicking entry emits `select` event with correct PUUID
- [ ] Clicking "Link Account" emits `link` event
- [ ] Collapsed mode renders compact avatar only

### Manual Testing
1. Switch between accounts — all pages update
2. Collapse sidebar — popover appears on click
3. Link new account — appears in list immediately
4. Unlink active account — switches to Overall

## Dependencies
### Internal Dependencies
- [ ] MA-03 (active account state management must exist in store)
- [ ] MA-01 (account management in Settings for link/unlink)

### External Dependencies
- [ ] Headless UI `Popover` component (already in project)

## Risks and Mitigations
| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Sidebar becomes too tall with many accounts | Medium | Low | Add max-height + scroll to account section |
| Popover positioning conflicts with sidebar edge | Low | Medium | Use Headless UI auto-positioning; test at various viewport heights |
