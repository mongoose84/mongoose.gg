# Overview Redesign — Backend Tasks

> Source: [overview-page-redesign.md](overview-page-redesign.md) — Tasks 1–3

## Execution Order

```
Task 1  (DTOs)
  ↓
Task 2  (Repository)
  ↓
Task 3  (Endpoint wiring + backend tests)
```

Tasks 1–3 can be merged as one PR if preferred.

---

## Task 1: Add `SessionStats` and `SurvivalStats` DTOs

**Scope**: Define the new record types (`SessionStats`, `SessionChampion`, `SurvivalStats`) in the DTOs area. Add `SessionStats?` and `SurvivalStats?` as optional fields on `OverviewResponse`.

**Files to create/modify**:
- New DTO records (location per project convention — likely alongside existing Overview DTOs)
- `OverviewResponse` record — add two new optional parameters

**DTO definitions**:
```csharp
public record SessionStats(
    [property: JsonPropertyName("gamesToday")] int GamesToday,
    [property: JsonPropertyName("winsToday")] int WinsToday,
    [property: JsonPropertyName("lossesToday")] int LossesToday,
    [property: JsonPropertyName("avgKdaToday")] double? AvgKdaToday,
    [property: JsonPropertyName("bestChampionToday")] SessionChampion? BestChampionToday,
    [property: JsonPropertyName("gamesThisWeek")] int GamesThisWeek,
    [property: JsonPropertyName("winsThisWeek")] int WinsThisWeek,
    [property: JsonPropertyName("lossesThisWeek")] int LossesThisWeek,
    [property: JsonPropertyName("avgKdaThisWeek")] double? AvgKdaThisWeek
);

public record SessionChampion(
    [property: JsonPropertyName("championName")] string ChampionName,
    [property: JsonPropertyName("wins")] int Wins,
    [property: JsonPropertyName("losses")] int Losses,
    [property: JsonPropertyName("avgKda")] double AvgKda
);

public record SurvivalStats(
    [property: JsonPropertyName("avgDeathsPerGame")] double AvgDeathsPerGame,
    [property: JsonPropertyName("deathsBefore10Pct")] double DeathsBefore10Pct,
    [property: JsonPropertyName("winRateAtOrBelow3Deaths")] double? WinRateAtOrBelow3Deaths,
    [property: JsonPropertyName("winRateAbove5Deaths")] double? WinRateAbove5Deaths,
    [property: JsonPropertyName("gamesAtOrBelow3Deaths")] int GamesAtOrBelow3Deaths,
    [property: JsonPropertyName("gamesAbove5Deaths")] int GamesAbove5Deaths,
    [property: JsonPropertyName("totalGames")] int TotalGames
);
```

**`OverviewResponse` additions**:
```csharp
[property: JsonPropertyName("sessionStats")] SessionStats? SessionStats = null,
[property: JsonPropertyName("survivalStats")] SurvivalStats? SurvivalStats = null
```

**Acceptance**:
- [ ] New DTOs compile and follow existing `[JsonPropertyName("camelCase")]` convention
- [ ] `OverviewResponse` accepts the new optional fields without breaking existing consumers (default `null`)

---

## Task 2: Repository methods for session and survival data

**Scope**: Add `GetSessionStatsAsync` and `GetSurvivalStatsAsync` to `IOverviewStatsRepository` and implement in the MySQL repository. No schema changes — queries use existing `matches`, `participants`, and `participant_metrics` tables.

**Files to modify**:
- `IOverviewStatsRepository.cs` — add interface methods
- MySQL implementation (e.g. `OverviewStatsRepository.cs`) — add query implementations
- `OverviewQueryModels.cs` — add query model definitions

**Query model definitions** (add to `Core/QueryModels/OverviewQueryModels.cs`):
```csharp
/// <summary>
/// Per-PUUID session breakdown. The repository returns one entry per PUUID so the
/// endpoint can populate both the aggregate SessionStats DTO and per-account
/// AccountSummary.GamesToday / GamesThisWeek fields in a single query.
/// </summary>
public record PerAccountSessionData(
    string Puuid,
    int GamesToday,
    int WinsToday,
    int LossesToday,
    double? AvgKdaToday,
    string? BestChampionName,
    int BestChampionWins,
    int BestChampionLosses,
    double BestChampionAvgKda,
    int GamesThisWeek,
    int WinsThisWeek,
    int LossesThisWeek,
    double? AvgKdaThisWeek
);

/// <summary>
/// Aggregate session stats across all requested PUUIDs.
/// Built by the endpoint from the per-account breakdown.
/// </summary>
public record SessionStatsData(
    IReadOnlyList<PerAccountSessionData> PerAccount
);

/// <summary>
/// Survival analysis over the last N games.
/// </summary>
public record SurvivalStatsData(
    double AvgDeathsPerGame,
    double DeathsBefore10Pct,
    double? WinRateAtOrBelow3Deaths,
    double? WinRateAbove5Deaths,
    int GamesAtOrBelow3Deaths,
    int GamesAbove5Deaths,
    int TotalGames
);
```

**Interface additions**:
```csharp
Task<SessionStatsData> GetSessionStatsAsync(IReadOnlyList<string> puuids, DateTime todayUtc);
Task<SurvivalStatsData> GetSurvivalStatsAsync(IReadOnlyList<string> puuids, int lastNGames = 20);
```

**Query notes**:
- **Session stats**: Filter `matches` by `game_start_time >= start of today (UTC)`, join `participants` on matched PUUIDs. **Group by PUUID** to produce per-account breakdown (`PerAccountSessionData`). Aggregate W/L/KDA. Best champion = highest `(wins / total)` with KDA tiebreaker. Also compute "this week" stats (last 7 days). The endpoint aggregates the per-account rows into the `SessionStats` DTO and distributes per-PUUID counts into `AccountSummary.GamesToday` / `GamesThisWeek`.
- **Survival stats**: **Last 20 games across all PUUIDs sorted by `game_start_time` descending** (not 20 per PUUID). Join `participant_metrics` for `deaths_pre_10`. Bucket games by death count (≤3 vs 5+), compute win rate per bucket.
- Both queries must use parameterized SQL only.

**Acceptance**:
- [ ] `GetSessionStatsAsync` returns per-PUUID breakdown for correct aggregation across multiple PUUIDs
- [ ] `GetSurvivalStatsAsync` returns last-20-game death buckets (across all accounts) with win rate correlation
- [ ] Repository integration tests pass for multi-PUUID aggregation

---

## Task 3: Wire new data into `OverviewEndpoint`

**Scope**: Call the new repository methods in `OverviewEndpoint.cs` for every request. Populate `SessionStats` and `SurvivalStats` on `OverviewResponse`. When `accountId=all`, populate `GamesToday` / `GamesThisWeek` on each `AccountSummary` from the per-PUUID session breakdown. Also remove the now-unused `wlLast20`, `last20Wins`, and `last20Losses` fields from the `RankSnapshot` DTO and the query that computes them.

**Files to modify**:
- `server/Mongoose.Api/Application/Endpoints/Overview/OverviewEndpoint.cs`
- `server/Mongoose.Api/Application/DTOs/Overview/OverviewDto.cs` — remove `wlLast20`, `last20Wins`, `last20Losses` from `RankSnapshot`

**Changes**:
1. Call `GetSessionStatsAsync(selectedPuuids, DateTime.UtcNow)` and `GetSurvivalStatsAsync(selectedPuuids)` for every request (regardless of `accountId`)
2. **Parallelize** the new calls with existing independent calls using `Task.WhenAll`:
   ```
   GetPrimaryQueueAsync → GetLast20MatchesAsync → Task.WhenAll(
       GetLastMatchAsync,
       GetMostPlayedChampionAsync,
       GetSessionStatsAsync,      // new
       GetSurvivalStatsAsync       // new
   )
   ```
3. Map repository results to `SessionStats` and `SurvivalStats` DTOs. Aggregate the per-PUUID `PerAccountSessionData` rows into the top-level `SessionStats` (sum games/wins/losses, weighted-average KDA, pick best champion across all accounts).
4. Pass them into the `OverviewResponse` constructor
5. In the `isAllMode` block, replace the hardcoded `GamesToday: 0, GamesThisWeek: 0` on each `AccountSummary` with real values looked up from the per-PUUID breakdown by matching PUUID
6. **Remove `wlLast20`, `last20Wins`, `last20Losses`** from the `RankSnapshot` DTO — these fields are no longer rendered by any frontend component after this redesign. Remove the corresponding backend logic that computes them.

**Tests** (`OverviewEndpointTests.cs`):
- [ ] Response includes `sessionStats` and `survivalStats` (non-null) for both `accountId=all` and single-account requests
- [ ] `accountSummaries[].gamesToday` and `gamesThisWeek` are populated (not 0) when `accountId=all`
- [ ] `rankSnapshot` no longer includes `wlLast20`, `last20Wins`, or `last20Losses`

**Acceptance**:
- [ ] No additional API round-trips — new data piggybacks on the existing overview fetch
- [ ] New DB calls are parallelized via `Task.WhenAll` alongside existing independent calls
- [ ] All existing `OverviewEndpointTests` still pass (update assertions for removed `RankSnapshot` fields)
- [ ] New integration tests pass
