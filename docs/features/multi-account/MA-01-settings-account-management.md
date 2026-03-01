# Feature: MA-01 — Settings Page: Multi-Account Management UI

## Problem Statement
Users can currently link one Riot account on the Overview page via the empty-state CTA, but there is no dedicated UI to view, manage, or remove linked accounts. The Settings page has no Riot account section at all. Users with multiple Riot accounts (90% of the user base) cannot link additional accounts, set a primary, or see sync status per account.

## Proposed Solution
Add a "Linked Riot Accounts" section to the Settings page between the Account section and Security section. This section lists all linked Riot accounts with rank, region, sync status, and management actions (set primary, sync, remove). It includes the existing `LinkRiotAccountModal` for adding new accounts.

## User Stories
### Primary User Story
As a player with multiple Riot accounts, I want to link and manage all my accounts from one Settings page so that I can control which accounts Mongoose tracks.

### Additional User Stories
- As a player, I want to set which account is my primary so the app defaults to it
- As a player, I want to see the sync status and rank of each linked account so I know my data is current
- As a player, I want to remove an account I no longer use so it doesn't clutter my profile
- As a free-tier user, I want to see that linking more accounts requires Pro so I understand the upgrade value

## Requirements

### Functional Requirements
1. Settings page shows a "Linked Riot Accounts" section listing all linked accounts
2. Each account row shows: game name, tag line, region, rank (solo/flex), primary badge, last sync time
3. Users can set any linked account as primary via a "Set Primary" button (hidden on current primary)
4. Users can trigger sync per account via a "Sync" button
5. Users can remove any account via a "Remove" button (with confirmation)
6. Removing the primary account promotes the next oldest account to primary
7. "Link Another Account" button opens the existing `LinkRiotAccountModal`
8. Free users see an upgrade prompt after their first linked account instead of the link button
9. A "Set Primary" API endpoint must be exposed on the backend

### Non-Functional Requirements
- **Performance**: Account list loads with the existing user data (no extra API call needed — `riotAccounts` already in auth store)
- **Security**: Set-primary and remove actions require authentication, user can only modify own accounts
- **Accessibility**: All actions keyboard-navigable, remove has confirmation dialog, status changes announced via aria-live

## Technical Approach

### Backend Changes
**Language**: C#
**Components**:
- [ ] New endpoint: `PUT /api/v2/users/me/riot-accounts/{puuid}/primary` in `server/Application/Endpoints/Auth/RiotAccountsEndpoint.cs`
- [ ] Auto-promote logic: When unlinking primary account, promote next oldest. Add to existing unlink handler in `RiotAccountsEndpoint.cs`
- [ ] The repository method `SetPrimaryAsync` already exists and is fully implemented — just needs an HTTP endpoint

### Frontend Changes
**Framework**: Vue
**Components**:
- [ ] New component: `client/src/components/settings/LinkedAccountsSection.vue` — account list with actions
- [ ] New component: `client/src/components/settings/LinkedAccountRow.vue` — single account row
- [ ] Modified view: `client/src/views/UserSettingsPage.vue` — add the new section
- [ ] Modified store: `client/src/stores/authStore.js` — add `setPrimary()` action
- [ ] Modified service: `client/src/services/authApi.js` — add `setPrimaryRiotAccount()` API call

### Database Changes
None — schema already supports `is_primary` flag on `user_riot_accounts`.

### API Contracts
#### Set Primary Account
```
PUT /api/v2/users/me/riot-accounts/{puuid}/primary
```
**Request**: No body
**Response (200)**:
```json
{ "success": true }
```
**Errors**:
- `404` — `{ "error": "Account not linked", "code": "ACCOUNT_NOT_LINKED" }`
- `401` — Not authenticated

## UI/UX Requirements

### LinkedAccountsSection

**Layout**: New section on `UserSettingsPage` between "Account" and "Security" sections. Same card styling pattern as existing sections.

**Structure**:
```
Linked Riot Accounts (N linked — [Tier] tier)
┌─────────────────────────────────────────────────────────┐
│ 🎮 GameName#TAG    EUW    Diamond II 67LP   ★ Primary  │
│    Last synced: 2 hours ago          [Sync]  [Remove]   │
├─────────────────────────────────────────────────────────┤
│ 🎮 SmurfName#TAG   EUW    Platinum I 45LP              │
│    Last synced: 1 day ago   [Sync] [Set Primary] [Remove]│
├─────────────────────────────────────────────────────────┤
│ [+ Link Another Account]                                │
│  🔒 Upgrade to Pro to link more accounts  (free only)   │
└─────────────────────────────────────────────────────────┘
```

**Components**:
- Section header: `h2` text-lg font-semibold, with account count + tier label
- Account row: `bg-background-surface border border-border rounded-lg p-xl`
- Game name: `text-sm font-semibold text-text`
- Region/rank: `text-xs text-text-secondary`
- Primary badge: `text-xs px-2 py-0.5 rounded-sm bg-primary-soft text-primary font-semibold`
- Sync button: `BaseButton variant="ghost" size="sm"`
- Set Primary button: `BaseButton variant="ghost" size="sm"`
- Remove button: `BaseButton variant="ghost" size="sm"` with error color text
- Link button: `BaseButton variant="primary" size="md"`
- Upgrade prompt: `text-xs text-text-secondary` with link to upgrade

**Behavior**:
- Set Primary: Calls API, refreshes user data, shows updated primary badge
- Remove: Shows confirmation dialog ("Remove GameName#TAG?"), on confirm calls unlink API
- Sync: Calls existing sync trigger, shows sync progress inline
- Link: Opens existing `LinkRiotAccountModal`, on success refreshes account list

**Accessibility**:
- Each account row is a semantic list item within an `<ul>`
- Buttons have descriptive aria-labels: "Set SmurfName as primary account"
- Remove confirmation uses `BaseModal` (Headless UI Dialog) for built-in ARIA
- Status changes (sync started, primary changed) use `aria-live="polite"` region

## Testing Strategy

### Unit Tests (Vitest)
- [ ] `LinkedAccountsSection` renders account list correctly
- [ ] Shows primary badge on correct account
- [ ] Hides "Set Primary" on current primary account
- [ ] Shows upgrade prompt for free tier with 1 account
- [ ] Shows "Link Another Account" for Pro tier
- [ ] Remove confirmation flow works correctly

### Integration Tests (xUnit)
- [ ] `PUT /api/v2/users/me/riot-accounts/{puuid}/primary` — success case
- [ ] Set primary — account not linked → 404
- [ ] Set primary — unauthenticated → 401
- [ ] Unlink primary account → next account promoted

## Dependencies
### Internal Dependencies
- [ ] Existing `LinkRiotAccountModal` component
- [ ] Existing `authStore.linkRiotAccount()` / `unlinkRiotAccount()` actions
- [ ] Existing `SetPrimaryAsync` repository method

### External Dependencies
- None

## Risks and Mitigations
| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Removing primary with no other accounts | Medium | Low | Disable remove when only 1 account linked, or allow and show empty state |
| Race condition on set primary during sync | Low | Low | SetPrimary is a simple DB update, sync operates on PUUID not primary flag |

## Open Questions
- [ ] Should we prevent removing the last linked account, or allow it and show empty state?
