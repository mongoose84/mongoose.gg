# Overview Redesign — Backend Tasks

Parent feature: [overview-glance-redesign.md](overview-glance-redesign.md)

## Execution Order

```
Task 1  (DTOs)
  ↓
Task 2  (Repository)
  ↓
Task 3  (Endpoint wiring + integration tests)
```

These three tasks are sequential but can be merged into a single PR.

---

## Task 1: Add `SessionStats` and `SurvivalStats` DTOs

**Scope**: Define the new record types (`SessionStats`, `SessionChampion`, `SurvivalStats`) in the DTOs area. Add `SessionStats?` and `SurvivalStats?` as optional fields on `OverviewResponse`.

**Files to create/modify**:
- New DTO records (location per project convention — likely alongside existing Overview DTOs)
- `OverviewResponse` record — add two new optional parameters

**DTO definitions** (from feature spec):
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

**Interface additions**:
```csharp
Task<SessionStatsData> GetSessionStatsAsync(IReadOnlyList<string> puuids, DateTime todayUtc);
Task<SurvivalStatsData> GetSurvivalStatsAsync(IReadOnlyList<string> puuids, int lastNGames = 20);
```

**Query notes**:
- **Session stats**: Filter `matches` by `game_start_time >= start of today (UTC)`, join `participants` on matched PUUIDs. Group for W/L/KDA. Best champion = highest `(wins / total)` with KDA tiebreaker. Also compute "this week" stats (last 7 days).
- **Survival stats**: Last 20 games across all PUUIDs, join `participant_metrics` for `deaths_pre_10`. Bucket games by death count (≤3 vs 5+), compute win rate per bucket.
- Both queries must use parameterized SQL only.

**Acceptance**:
- [ ] `GetSessionStatsAsync` returns correct aggregation across multiple PUUIDs
- [ ] `GetSurvivalStatsAsync` returns last-20-game death buckets with win rate correlation
- [ ] Repository integration tests pass for multi-PUUID aggregation

---

## Task 3: Wire new data into `OverviewEndpoint`

**Scope**: Call the new repository methods in `OverviewEndpoint.cs` when `accountId=all` (Overall mode). Populate `SessionStats` and `SurvivalStats` on `OverviewResponse`. Populate the currently-hardcoded `GamesToday` / `GamesThisWeek` fields on each `AccountSummary`.

**File to modify**:
- `server/Mongoose.Api/Application/Endpoints/Overview/OverviewEndpoint.cs`

**Changes**:
1. In the `isAllMode` block, call `GetSessionStatsAsync(selectedPuuids, DateTime.UtcNow)` and `GetSurvivalStatsAsync(selectedPuuids)`
2. Map repository results to `SessionStats` and `SurvivalStats` DTOs
3. Pass them into the `OverviewResponse` constructor
4. Replace the hardcoded `GamesToday: 0, GamesThisWeek: 0` on each `AccountSummary` with real values from the session query (per-account breakdown or aggregate — TBD based on query design in Task 2)
5. Individual mode: both fields remain `null` on `OverviewResponse`

**Tests** (`OverviewEndpointTests.cs`):
- [ ] Overall mode response includes `sessionStats` and `survivalStats` (non-null)
- [ ] Individual mode response does NOT include `sessionStats` / `survivalStats` (null)
- [ ] `accountSummaries[].gamesToday` and `gamesThisWeek` are populated (not 0)

**Acceptance**:
- [ ] No additional API round-trips — new data piggybacks on the existing overview fetch
- [ ] All existing `OverviewEndpointTests` still pass
- [ ] New integration tests pass
