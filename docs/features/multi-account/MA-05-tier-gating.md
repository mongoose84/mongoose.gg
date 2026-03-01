# Feature: MA-05 — Tier-Based Account Linking Limits

## Problem Statement
Multi-account support is a premium feature that creates a natural upgrade incentive. Free users should be limited to 1 linked account, while Pro users can link unlimited accounts. The "Overall" view (which aggregates across accounts) is explicitly a Pro-only feature — it requires both Pro tier and 2+ linked accounts.

When a Pro user downgrades to Free, their non-primary linked accounts become unavailable (links are preserved in the database but hidden from the UI and excluded from queries). The user sees only their primary account's data, matching today's single-account experience.

## Proposed Solution
Enforce account linking limits on the backend based on user tier. On downgrade to Free, hide non-primary accounts rather than deleting them (so re-upgrading to Pro restores full access). Show clear upgrade prompts on the frontend when free users attempt to link additional accounts. The "Overall" entry in the sidebar requires both Pro tier and 2+ visible accounts.

## User Stories
### Primary User Story
As a free-tier user, I want to understand that linking more accounts is a Pro feature so I can decide whether to upgrade.

### Additional User Stories
- As a Pro user, I want to link as many accounts as I need without restriction
- As a free user, I should never see broken UI from accessing a feature I don't have
- As a product owner, I want account limits to drive Pro conversions

## Requirements

### Functional Requirements
1. Backend enforces linking limit: free = 1 account, pro = unlimited
2. Backend returns `ACCOUNT_LIMIT_REACHED` error code with tier info when limit exceeded
3. Frontend shows upgrade prompt instead of "Link Account" button when limit reached
4. Sidebar "Overall" entry only visible when `authStore.tier === 'pro'` AND `riotAccounts.length >= 2`
5. On Pro → Free downgrade: non-primary linked accounts become unavailable — they are hidden from the UI and excluded from all data queries. Only the primary account remains active. Links are preserved in the database so re-upgrading to Pro restores them.
6. Backend enforces tier check on `?account=all`: free-tier users requesting `?account=all` receive only their primary account's data (same as omitting the parameter). This prevents free-tier users from accessing aggregate data even if they have preserved non-primary links.
7. `authStore.riotAccounts` getter filters to only visible/available accounts based on tier (free = primary only, pro = all linked)
8. Display preferences that reference "Overall" are hidden/disabled for single-account or free-tier users

### Non-Functional Requirements
- **Security**: Limit enforced server-side — frontend gating is UX only, not a security boundary
- **UX**: Upgrade prompts should feel encouraging, not punitive. Use language like "Link unlimited accounts with Pro" not "You can't do this"

## Technical Approach

### Backend Changes
**Language**: C#
**Components**:

#### RiotAccountsEndpoint.cs — Link Handler
- [ ] Before linking, check user tier and current link count
- [ ] Free tier: if `linkCount >= 1`, return `400` with `ACCOUNT_LIMIT_REACHED`
- [ ] Pro tier: no limit check

```csharp
// In link handler, before calling LinkAsync:
var linkCount = await userRiotAccountsRepo.GetLinkCountForUserAsync(userId);
var userTier = user.Tier; // from auth claims or user lookup

if (userTier == "free" && linkCount >= 1)
{
    return Results.BadRequest(new { 
        error = "Free tier is limited to 1 linked account. Upgrade to Pro for unlimited accounts.",
        code = "ACCOUNT_LIMIT_REACHED",
        currentLimit = 1,
        tier = "free"
    });
}
```

#### IUserRiotAccountsRepository — New Method
- [ ] `GetLinkCountForUserAsync(long userId)` → returns count of linked accounts for a user
- [ ] (Distinct from existing `GetLinkCountAsync(string puuid)` which counts users linked to a PUUID)

#### PuuidResolutionService — Tier-Aware Resolution
- [ ] `ResolveRequestedAccountsAsync` (from MA-02) must check user tier when `accountParam` is `"all"`
- [ ] If user is free tier: ignore the `"all"` request and resolve to primary PUUID only (same as omitting `?account=`)
- [ ] If user is pro tier: resolve all linked PUUIDs as normal
- [ ] This is the **server-side security boundary** — frontend gating is UX only

#### Downgrade Handling
- [ ] When a user's tier changes from Pro → Free, no database changes occur (links are preserved)
- [ ] `GetByUserIdAsync` / `GetVisibleAccountsAsync` gains a tier-aware variant that filters to primary-only for free users
- [ ] All data endpoints implicitly use only visible accounts (via `ResolveRequestedAccountsAsync` tier check)
- [ ] Re-upgrading to Pro immediately restores access to all preserved links

### Frontend Changes
**Framework**: Vue

#### LinkedAccountsSection.vue (from MA-01)
- [ ] When `authStore.tier === 'free'` and `riotAccounts.length >= 1`, replace "Link Account" button with upgrade prompt
- [ ] Upgrade prompt: card with lock icon, text, and CTA button → navigates to upgrade page or shows upgrade modal

#### AccountSwitcher.vue (from MA-04)
- [ ] "Overall" entry only rendered when `authStore.tier === 'pro'` AND `visibleAccounts.length >= 2`
- [ ] Free-tier users with preserved non-primary links do NOT see those accounts in the switcher
- [ ] "Link Account" button shows upgrade prompt for free tier at limit

#### LinkRiotAccountModal.vue
- [ ] Add pre-check: if at limit, show inline message instead of the form
- [ ] Or: prevent modal from opening at all (handled by parent)

#### Error Handling in authStore.linkRiotAccount()
- [ ] Handle `ACCOUNT_LIMIT_REACHED` error code
- [ ] Surface tier info to the UI

### Database Changes
None.

### API Contracts
#### Link Account Error Response (limit reached)
```
POST /api/v2/users/me/riot-accounts
```
**Response (400)**:
```json
{
  "error": "Free tier is limited to 1 linked account. Upgrade to Pro for unlimited accounts.",
  "code": "ACCOUNT_LIMIT_REACHED",
  "currentLimit": 1,
  "tier": "free"
}
```

## UI/UX Requirements

### Upgrade Prompt (Settings Page)

**Structure**:
```
┌─────────────────────────────────────────────────┐
│ 🔒  Link Unlimited Accounts                    │
│                                                 │
│ Free tier supports 1 linked account.            │
│ Upgrade to Pro to link all your accounts and    │
│ view combined stats across them.                │
│                                                 │
│ [Upgrade to Pro]                                │
└─────────────────────────────────────────────────┘
```

**Styling**:
- Container: `bg-background-surface border border-border rounded-lg p-xl`
- Lock icon: Heroicons `LockClosedIcon` `w-5 h-5 text-text-secondary`
- Title: `text-sm font-semibold text-text`
- Description: `text-xs text-text-secondary mt-xs`
- CTA: `BaseButton variant="primary" size="sm" mt-md`

### Upgrade Prompt (Sidebar — Compact)
```
[🔒 + Link]  ← text-xs text-primary, opens upgrade flow
```

**Behavior**:
- Clicking upgrade CTA navigates to `/app/user` (Settings) with an upgrade section or external payment link
- If payment/upgrade flow isn't built yet, link to a placeholder or show a "Coming Soon" toast

## Testing Strategy

### Unit Tests
- [ ] Backend: Link account with free tier at limit → 400
- [ ] Backend: Link account with pro tier at any count → success
- [ ] Backend: Free-tier user with `?account=all` → resolves to primary PUUID only
- [ ] Backend: Pro-tier user with `?account=all` → resolves to all linked PUUIDs
- [ ] Backend: Free-tier user with preserved non-primary links → only primary returned by visible accounts query
- [ ] Frontend: Upgrade prompt shown for free tier at limit
- [ ] Frontend: "Link Account" visible for pro tier
- [ ] Frontend: "Overall" hidden in sidebar for free tier (even with preserved links)
- [ ] Frontend: "Overall" hidden in sidebar when only 1 account (even Pro tier)
- [ ] Frontend: `riotAccounts` getter returns only primary for free tier

### Integration Tests (xUnit)
- [ ] Free user with 1 account → link second → 400 ACCOUNT_LIMIT_REACHED
- [ ] Pro user with 5 accounts → link sixth → 200 success
- [ ] Free user with 0 accounts → link first → 200 success
- [ ] Pro user with 3 accounts downgrades to Free → API returns only primary account data
- [ ] Downgraded free user with preserved links → `?account=all` returns primary-only data
- [ ] Downgraded free user re-upgrades to Pro → all preserved accounts become visible again

## Dependencies
### Internal Dependencies
- [ ] MA-01 (Settings account management section)
- [ ] MA-04 (Sidebar account switcher)

### External Dependencies
- [ ] Existing tier/subscription system

## Risks and Mitigations
| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Pro users downgrade with many accounts | Low | Medium | Non-primary links are preserved but hidden; only primary account is active for free tier. Re-upgrading restores all links. |
| Free users bypass frontend limit check | Low | Low | Backend enforcement is the security boundary |
| No upgrade/payment flow built yet | Medium | Medium | Link to Settings page; add TODO for payment integration |
