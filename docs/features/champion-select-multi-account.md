# Feature: Champion Select Multi-Account Support

## Problem Statement

The Champion Select page currently only queries data for the user's **primary** Riot account, even though:

1. The frontend already sends an `accountId` query parameter via `appendAccountParam()` (values: `"all"`, a specific `acc_*` ID, or default `"all"`).
2. Both backend endpoints (`ChampionSelectEndpoint`, `SoloMatchupsEndpoint`) call `ResolvePrimaryAccountAsync`, which ignores the `accountId` parameter entirely.
3. All 14 other analytics endpoints (Overview, SoloPerformance, RadarChart, all Trends, MatchList, DeathPositions, MatchActivity, etc.) already support multi-account via `ResolveRequestedAccountsAsync`.

Users with multiple linked Riot accounts see incomplete champion select recommendations because data from secondary accounts is excluded. The account switcher in the UI has no effect on this page.

## Proposed Solution

Align the two Champion Select backend endpoints with the established multi-account pattern used across the rest of the analytics API. Update the repository interfaces and implementations to accept a list of PUUIDs. No frontend changes are needed — the client already sends the correct parameter.

## User Stories

### Primary User Story
As a player with multiple Riot accounts, I want Champion Select recommendations to reflect data from all my linked accounts (or a specific selected account) so that my champion pool and matchup insights are complete.

### Additional User Stories
- As a player using the account switcher, I want Champion Select to respond to my account selection so the page behaves consistently with every other analytics page.

---

## Backend

### Affected Files

| Layer | File | Change |
|---|---|---|
| Endpoint | `server/Mongoose.Api/Application/Endpoints/ChampionSelect/ChampionSelectEndpoint.cs` | Add `accountId` param, switch to `ResolveRequestedAccountsAsync` |
| Endpoint | `server/Mongoose.Api/Application/Endpoints/Solo/SoloMatchupsEndpoint.cs` | Add `accountId` param, switch to `ResolveRequestedAccountsAsync` |
| Interface | `server/Mongoose.Api/Core/Interfaces/Analytics/IChampionSelectRepository.cs` | Change `string puuid` → `IReadOnlyList<string> puuids` |
| Interface | `server/Mongoose.Api/Core/Interfaces/Analytics/IMatchupRepository.cs` | Change `string puuid` → `IReadOnlyList<string> puuids` |
| Repository | `server/Mongoose.Api/Infrastructure/Database/Repositories/ChampionSelectRepository.cs` | Update SQL to use `IN` clause for puuids list |
| Repository | `server/Mongoose.Api/Infrastructure/Database/Repositories/MatchupRepository.cs` | Update SQL to use `IN` clause for puuids list |
| Tests | `server/Mongoose.Api.Tests/ChampionSelectEndpointTests.cs` | Add multi-account test cases, update mock interface |
| Test fakes | Any fake/mock implementing `IChampionSelectRepository` or `IMatchupRepository` | Update signature |

### Endpoint Changes

Both endpoints follow the same transformation. Reference `SoloPerformanceEndpoint` as the canonical example.

#### Before (both endpoints)

```csharp
var (accountError, resolvedAccount) = await puuidResolutionService.ResolvePrimaryAccountAsync(authorizedUser!.UserId);
if (accountError != null)
    return accountError;

var primaryPuuid = resolvedAccount!.Account.Puuid;
var data = await repo.GetDataAsync(primaryPuuid, queueType, timeRange);
```

#### After (both endpoints)

```csharp
[FromQuery] string? accountId,  // add parameter

var (accountError, resolvedAccounts) = await puuidResolutionService.ResolveRequestedAccountsAsync(authorizedUser!.UserId, accountId);
if (accountError != null)
    return accountError;

var puuids = resolvedAccounts!.Select(a => a.Account.Puuid).ToList();
var data = await repo.GetDataAsync(puuids, queueType, timeRange);
```

### Interface Changes

#### IChampionSelectRepository

```csharp
// Before
Task<ChampionSelectResponse?> GetChampionSelectDataAsync(string puuid, string? queueType = null, string? timeRange = null);

// After
Task<ChampionSelectResponse?> GetChampionSelectDataAsync(IReadOnlyList<string> puuids, string? queueType = null, string? timeRange = null);
```

#### IMatchupRepository

```csharp
// Before
Task<ChampionMatchupsResponse> GetChampionMatchupsAsync(string puuid, string? queueType = null, string? timeRange = null);

// After
Task<ChampionMatchupsResponse> GetChampionMatchupsAsync(IReadOnlyList<string> puuids, string? queueType = null, string? timeRange = null);
```

### Repository SQL Changes

Update SQL `WHERE` clauses from single-PUUID to multi-PUUID using parameterized `IN` lists. Follow the pattern already established in `SoloPerformanceRepository`, `MatchesRepository`, `DeathPositionsRepository`, etc.

#### Before

```sql
WHERE p.puuid = @puuid {queueFilter} {timeFilter}
```

#### After

```sql
WHERE p.puuid IN (@puuid0, @puuid1, ...) {queueFilter} {timeFilter}
```

Parameters built dynamically (same pattern as existing repos):

```csharp
var puuidParams = string.Join(", ", puuids.Select((_, i) => $"@puuid{i}"));
// ... later in AddWithValue loop:
for (int i = 0; i < puuids.Count; i++)
    cmd.Parameters.AddWithValue($"@puuid{i}", puuids[i]);
```

This applies to both queries in `ChampionSelectRepository` (`GetBasicStatsAsync` and `GetMainChampionsByRoleAsync`) and all queries in `MatchupRepository`.

### Testing

| Scenario | Endpoint | Expected |
|---|---|---|
| No `accountId` param | Both | Falls back to primary account (backwards compat) |
| `accountId=all` | Both | Aggregates data across all linked accounts |
| `accountId=acc_xxxx` (valid) | Both | Returns data for that specific account |
| `accountId=acc_xxxx` (not owned) | Both | 403 Forbidden |
| Single linked account + `accountId=all` | Both | Same result as primary-only |

---

## Frontend

### No Changes Required

The frontend already correctly handles multi-account selection for Champion Select:

| Layer | File | Status |
|---|---|---|
| API service | `client/src/services/soloApi.js` | Already calls `appendAccountParam(params)` for both `getChampionSelectData` (line 69) and `getChampionMatchups` (line 173) |
| Account context | `client/src/services/accountContext.js` | Shared helper already maps account switcher state to the `accountId` query param |
| Page component | `client/src/views/ChampionSelectPage.vue` | Already watches `authStore.activeAccountPuuid` and refetches on change (line 120) |

The `accountId` parameter is already being sent on every request — it is simply ignored by the backend today. Once the backend changes are deployed, the frontend will work correctly without modification.

### Verification

After backend deployment, verify:

- Account switcher dropdown on the Champion Select page triggers a refetch (already wired)
- "Overall" mode sends `accountId=all` and returns aggregated data from all accounts
- Selecting a specific account sends `accountId=acc_xxxx` and returns data for that account only
