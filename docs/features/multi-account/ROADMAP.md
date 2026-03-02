# Multi-Account Feature — Implementation Roadmap

> **Epic**: Multi-Account Support  
> **Goal**: Allow users to link multiple Riot accounts, switch between them, and view aggregated "Overall" analytics across all accounts.  
> **Tier gating**: Free = 1 account, Pro = unlimited  
> **Decision date**: March 1, 2026

---

## Implementation Priority

Features are ordered by **dependency chain** and **incremental value delivery**. Each feature is a single branch and can be shipped independently (though later features depend on earlier ones being merged).

| Priority | Feature | Branch | Est. Effort | Depends On | Ships Value? |
|----------|---------|--------|-------------|------------|--------------|
| **1** | [MA-01: Settings Account Management](features/multi-account/MA-01-settings-account-management.md) | `feature/ma-01-settings-accounts` | S–M | — | ✅ Users can manage accounts |
| **2** | [MA-05: Tier-Based Account Limits](features/multi-account/MA-05-tier-gating.md) | `feature/ma-05-tier-gating` | S | MA-01 | ✅ Upgrade incentive active |
| **3** | [MA-02: Backend Multi-PUUID Resolution](features/multi-account/MA-02-backend-multi-puuid-resolution.md) | `feature/ma-02-multi-puuid-backend` | L | MA-01 | ⚙️ Enables all frontend features |
| **4** | [MA-03: Active Account State Management](features/multi-account/MA-03-active-account-state.md) | `feature/ma-03-active-account-state` | S | MA-02 | ⚙️ Enables switcher + pages |
| **5** | [MA-04: Sidebar Account Switcher](features/multi-account/MA-04-sidebar-account-switcher.md) | `feature/ma-04-sidebar-switcher` | M | MA-03 | ✅ Users can switch accounts |
| **6** | [MA-06: Overview Overall Mode](features/multi-account/MA-06-overview-overall-mode.md) | `feature/ma-06-overview-overall` | M | MA-03 | ✅ Cross-account overview |
| **7** | [MA-07: Match History Overall Mode](features/multi-account/MA-07-match-history-overall.md) | `feature/ma-07-matches-overall` | S–M | MA-03 | ✅ Interleaved match history |
| **8** | [MA-08: Solo Page Overall Mode](features/multi-account/MA-08-solo-page-overall.md) | `feature/ma-08-solo-overall` | M–L | MA-03 | ✅ Aggregated Solo analytics |
| **9** | [MA-09: Display Preferences](features/multi-account/MA-09-display-preferences.md) | `feature/ma-09-display-preferences` | S | MA-08 | ✅ User customization |

**Effort scale**: S = 1–2 days, M = 3–5 days, L = 5–8 days

---

## Dependency Graph

```
MA-01 (Settings Accounts) Done
  │
  ├── MA-05 (Tier Gating) Done
  │
  └── MA-02 (Backend Multi-PUUID)
        │
        └── MA-03 (Active Account State)
              │
              ├── MA-04 (Sidebar Switcher)
              │
              ├── MA-06 (Overview Overall)
              │
              ├── MA-07 (Matches Overall)
              │
              └── MA-08 (Solo Overall)
                    │
                    └── MA-09 (Display Preferences)
```

---

## Phased Delivery

### Phase 1: Foundation (MA-01 + MA-05)
**What ships**: Users can link/manage multiple accounts in Settings. Free tier limit enforced. Pro users can link unlimited.

**User-visible value**: Account management exists. Upgrade prompt visible. Data on the platform.

**Risk**: Low — builds on existing infrastructure (schema, repository methods already support multi-account).

### Phase 2: Backend Infrastructure (MA-02)
**What ships**: All data endpoints accept `?account=` parameter. Multi-PUUID queries work.

**User-visible value**: None directly — this is backend plumbing. But it unblocks all Phase 3 features.

**Risk**: Medium — largest single feature. Touches many endpoints and repositories. Needs thorough integration testing.

### Phase 3: Account Switching (MA-03 + MA-04)
**What ships**: Sidebar shows account switcher. Users can switch between accounts. All pages re-fetch for the selected account.

**User-visible value**: High — the core multi-account UX is now live. Users can view data for any linked account.

**Risk**: Low–Medium — store changes are straightforward. Sidebar changes are contained.

### Phase 4: Overall Mode (MA-06 + MA-07 + MA-08)
**What ships**: "Overall" view works on Overview, Matches, and Solo pages. Aggregated stats, interleaved match history, combined trend charts.

**User-visible value**: High — the differentiating feature. Cross-account analytics arrive.

**Risk**: Medium — chart rendering with multi-account data needs careful UX testing.

**Note**: MA-06, MA-07, and MA-08 can be implemented in parallel by different developers since they modify different pages. They all depend only on MA-03.

### Phase 5: Polish (MA-09)
**What ships**: Users can customize their default view and chart display mode in Settings.

**User-visible value**: Moderate — personalization. Converts power users.

**Risk**: Low — purely client-side localStorage preferences.

---

## Rollout Strategy

1. **Feature flag**: Consider a `multi_account_enabled` flag to gate the entire feature during development
2. **Backwards compatibility**: `?account=` parameter is optional — existing API calls work without it
3. **Data migration**: None needed — schema already supports M:N user↔riot_account relationships
4. **Sync**: When a new account is linked, sync triggers automatically (existing behavior). All linked accounts sync when user clicks "Sync" (new behavior from MA-01)

---

## Success Metrics
- **Adoption**: % of Pro users with 2+ linked accounts (target: 60% within 30 days)
- **Engagement**: Sessions using Overall mode vs specific account mode
- **Conversion**: Free→Pro upgrades attributed to account limit prompt
- **Retention**: Session frequency change for users who link 2+ accounts

---

## Total Estimated Effort
**Optimistic**: 3–4 weeks (one developer)  
**Realistic**: 5–6 weeks (one developer, accounting for testing + edge cases)  
**Parallel team**: 3–4 weeks (Phase 4 features parallelized across 2–3 developers)
