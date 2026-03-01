# Feature: MA-02 — Backend: Multi-PUUID Data Resolution

## Problem Statement
All data endpoints currently resolve a single primary PUUID via `PuuidResolutionService.ResolvePrimaryPuuidAsync()` and query data for that one account. To support the "Overall" view (aggregated data across all linked accounts) and per-account switching, endpoints need to accept a PUUID parameter and optionally query across multiple PUUIDs.

## Proposed Solution
Extend data endpoints to accept an optional `?account=` query parameter. When set to `all`, query all linked PUUIDs and aggregate. When set to a specific PUUID, query that account's data. When omitted, default to the user's primary PUUID (preserving current single-account behavior). The frontend explicitly sends `?account=all` to opt into the aggregate "Overall" view. Extend `PuuidResolutionService` with a method that resolves PUUIDs based on this parameter.

## User Stories
### Primary User Story
As the frontend, I need to request data for a specific linked account or all linked accounts so that the user can view per-account or aggregated analytics.

### Additional User Stories
- As a user in Overall mode, I want combined stats from all my accounts calculated correctly
- As a user viewing a specific account, I want only that account's data returned

## Requirements

### Functional Requirements
1. All data endpoints accept an optional `?account=` query parameter with three distinct behaviors:
   - **Omitted** → resolves to the user's primary PUUID (backwards compatible with existing single-account behavior)
   - **`account=all`** → resolves to all linked PUUIDs for the user (aggregate/Overall mode)
   - **`account={puuid}`** → resolves to that specific PUUID (must be linked to the user)
2. `PuuidResolutionService` gains a `ResolveRequestedAccountsAsync(userId, accountParam)` method
3. Repositories that query by single PUUID gain multi-PUUID variants (WHERE puuid IN (...))
4. Aggregation logic for stats (win rate, KDA, games played) computed server-side across PUUIDs
5. Overview endpoint returns per-account summary cards when `account=all`
6. Match list endpoint returns interleaved matches from all PUUIDs when `account=all`, each tagged with account info

### Non-Functional Requirements
- **Performance**: Multi-PUUID queries must use `IN (...)` clauses, not N+1 queries. Index on `puuid` already exists.
- **Security**: `VerifyPuuidOwnershipAsync` must validate that the requested PUUID belongs to the authenticated user. Cannot request another user's PUUID.
- **Backwards compatibility**: Omitting `?account=` parameter defaults to the user's primary PUUID, preserving current single-account behavior. The frontend must explicitly send `?account=all` for aggregated data.

## Technical Approach

### Backend Changes
**Language**: C#
**Components**:

#### PuuidResolutionService (new method)
- [ ] `ResolveRequestedAccountsAsync(long userId, string? accountParam)` → returns `List<ResolvedAccount>`
  - `null` → calls `ResolvePrimaryPuuidAsync(userId)` and returns single primary account (backwards compatible default)
  - `"all"` → calls `ResolveAllAccountsAsync(userId)` and returns all linked accounts
  - specific PUUID → calls `VerifyPuuidOwnershipAsync(userId, puuid)` + returns single account
  - invalid/unlinked PUUID → returns 403 error

#### Affected Endpoints
Each endpoint below needs to:
1. Read `?account=` query param
2. Call `ResolveRequestedAccountsAsync` to get PUUID list
3. Pass PUUID list to repository queries

- [ ] `server/Application/Endpoints/Overview/OverviewEndpoint.cs`
- [ ] `server/Application/Endpoints/Solo/SoloPerformanceEndpoint.cs`
- [ ] `server/Application/Endpoints/Solo/WinrateTrendEndpoint.cs`
- [ ] `server/Application/Endpoints/Solo/DeathsTrendEndpoint.cs`
- [ ] `server/Application/Endpoints/Solo/DragonParticipationTrendEndpoint.cs`
- [ ] `server/Application/Endpoints/Solo/VisionScoreTrendEndpoint.cs`
- [ ] `server/Application/Endpoints/Solo/GoldAt15TrendEndpoint.cs`
- [ ] `server/Application/Endpoints/Solo/CsPerMinuteTrendEndpoint.cs`
- [ ] `server/Application/Endpoints/Solo/RadarChartEndpoint.cs`
- [ ] `server/Application/Endpoints/Solo/DeathPositionsEndpoint.cs`
- [ ] `server/Application/Endpoints/Matches/MatchListEndpoint.cs`
- [ ] `server/Application/Endpoints/Matches/MatchActivityEndpoint.cs`

#### Repository Changes
- [ ] All repositories taking a single `puuid` parameter gain overloads accepting `IReadOnlyList<string> puuids`
- [ ] SQL queries use `WHERE puuid IN (@p0, @p1, ...)` parameterized pattern
- [ ] A shared helper builds parameterized IN clauses safely (no string concatenation)

#### Aggregation
- [ ] Win rate: total wins / total games across all PUUIDs
- [ ] KDA: summed (kills + assists) / summed deaths across all PUUIDs
- [ ] Games played: sum across all PUUIDs
- [ ] Trend data: interleave by `game_start_time`, tag each point with source PUUID
- [ ] Match list: interleave by `game_start_time DESC`, include `accountGameName` and `accountRegion` fields per match

### Database Changes
None — existing indexes on `puuid` support IN queries efficiently.

### API Contracts
#### Query Parameter
All data endpoints gain:
```
(omitted)             → primary PUUID only (backwards compatible default)
?account=all          → aggregate all linked accounts
?account={puuid}      → specific account data only
```

#### Overview Response Extension (account=all)
```json
{
  "playerHeader": { ... },
  "accountSummaries": [
    {
      "gameName": "FakerMain",
      "tagLine": "EUW",
      "region": "euw1",
      "puuid": "...",
      "rank": "Diamond II",
      "lp": 67,
      "gamesToday": 5,
      "gamesThisWeek": 23
    },
    { ... }
  ],
  "combinedStats": {
    "totalGames": 152,
    "winRate": 54.6,
    "avgKda": 3.2
  },
  "rankSnapshot": { ... },
  "lastMatch": { ... },
  "mostPlayedChampion": { ... }
}
```

#### Match List Response Extension (account=all)
Each match object gains:
```json
{
  "matchId": "...",
  "accountGameName": "FakerMain",
  "accountTagLine": "EUW",
  "accountRegion": "euw1",
  ...existing fields...
}
```

#### Solo Dashboard Response (account=all)
Stats are aggregated. Response shape is identical, values are computed across all PUUIDs.

## Testing Strategy

### Unit Tests (xUnit)
- [ ] `ResolveRequestedAccountsAsync` with `null` → returns primary account only
- [ ] `ResolveRequestedAccountsAsync` with `"all"` → returns all linked accounts
- [ ] `ResolveRequestedAccountsAsync` with valid PUUID → returns single account
- [ ] `ResolveRequestedAccountsAsync` with unlinked PUUID → returns 403

### Integration Tests (xUnit)
- [ ] Overview endpoint with `?account=all` returns aggregated data
- [ ] Overview endpoint with `?account={puuid}` returns single account data
- [ ] Solo endpoint with `?account=all` returns aggregated stats
- [ ] Match list with `?account=all` interleaves matches from multiple PUUIDs
- [ ] Requesting another user's PUUID → 403

## Dependencies
### Internal Dependencies
- [ ] MA-01 (Set Primary endpoint must exist for full account management)

### External Dependencies
- None

## Risks and Mitigations
| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Performance regression with multi-PUUID IN queries | Medium | Low | Existing indexes handle IN clauses well; monitor query times |
| Aggregation edge cases (0 games on one account) | Low | Medium | Guard against division by zero; exclude accounts with 0 games from averages |
| Breaking existing API consumers | High | Low | `?account=` is optional; omitting it defaults to primary PUUID, preserving current behavior |

## Open Questions
- [x] ~During rollout, should omitting `?account=` default to primary or all?~ **Resolved**: Omitting `?account=` defaults to primary PUUID. Frontend sends `?account=all` explicitly for aggregate data.
