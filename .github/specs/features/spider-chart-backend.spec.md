# Feature: Performance Radar Chart — Backend

## Problem Statement
The Solo Dashboard currently offers 6 independent trend charts (winrate, deaths, dragon participation, vision score, gold@15, CS/min) but no holistic overview that shows a player's strengths and weaknesses at a glance. Players must mentally synthesize 6 separate charts to answer "what should I work on?" — a cognitive load problem that a single multi-axis visualization can solve.

The AnalysisLayout Zone 4 ("deep-analysis") is currently empty. This feature provides the backend data for a radar/spider chart that fills that gap.

## Proposed Solution
Create a new API endpoint `GET /api/v2/solo/radar-chart/{userId}` that aggregates 6 performance dimensions from existing database tables, normalizes each to a 0–100 scale, and returns a structured response suitable for rendering a radar chart on the frontend.

## User Stories
### Primary User Story
As a solo player, I want to see my performance profile across multiple dimensions so that I can identify which areas to focus on improving.

### Additional User Stories
- As a solo player, I want to filter my radar chart by queue type and time range so that I can see my performance profile in specific contexts
- As a solo player, I want to see both my normalized score and raw value for each axis so that I understand what the numbers mean

## Requirements

### Functional Requirements
1. Aggregate 6 performance axes from existing data: Laning (gold diff @15), Farming (CS/min), Combat (kill participation), Vision (vision/min), Objectives (objective participation %), and Survivability (inverse deaths/game)
2. Normalize each axis to a 0–100 scale using fixed competitive ranges (not player-relative)
3. Support standard queue filtering (`ranked_solo`, `ranked_flex`, `normal`, `aram`, `all`) and time range filtering (`1w`, `1m`, `3m`, `6m`, `current_season`, `last_season`)
4. Return both the normalized value (0–100) and the raw value with its unit for each axis
5. Return total games analyzed count for context
6. Return `404` when no match data exists for the player

### Non-Functional Requirements
- **Performance**: Response under 200ms for typical player data (< 500 matches)
- **Security**: Requires authentication; user can only access own data (userId ↔ authenticated user check)
- **Accessibility**: N/A (backend only)

## Technical Approach

### Backend Changes
**Language**: C#

**Components**:
- [ ] DTO record: `server/Application/DTOs/Solo/RadarChartDto.cs`
- [ ] Repository interface: `server/Core/Interfaces/IRadarChartRepository.cs`
- [ ] Repository implementation: `server/Infrastructure/Database/Repositories/RadarChartRepository.cs`
- [ ] Endpoint: `server/Application/Endpoints/Solo/RadarChartEndpoint.cs`
- [ ] DI registration: `server/Program.cs` (add `AddScoped<IRadarChartRepository, RadarChartRepository>`)
- [ ] Endpoint registration: `server/Application/MongooseApiApplication.cs`

### Database Changes
**None.** All required data already exists in:
- `participants` — deaths, creep_score, gold_earned
- `participant_metrics` — kill_participation_pct, damage_share_pct, vision_per_min, deaths_pre_10/10_20/20_30/30_plus
- `participant_checkpoints` — gold_diff_vs_lane at minute_mark 15
- `participant_objectives` — dragons/heralds/barons/towers_participated
- `team_objectives` — dragons/heralds/barons/towers_taken (denominators)
- `matches` — game_duration_sec, game_start_time, queue_id

### API Contract
#### `GET /api/v2/solo/radar-chart/{userId}`

**Query Parameters**:
| Parameter | Type | Required | Default | Values |
|-----------|------|----------|---------|--------|
| `queueType` | string | No | `all` | `ranked_solo`, `ranked_flex`, `normal`, `aram`, `all` |
| `timeRange` | string | No | `null` (all time) | `1w`, `1m`, `3m`, `6m`, `current_season`, `last_season` |

**Response (200)**:
```json
{
  "axes": [
    {
      "key": "laning",
      "label": "Laning",
      "value": 62.5,
      "rawValue": 500,
      "rawUnit": "gold diff @15"
    },
    {
      "key": "farming",
      "label": "Farming",
      "value": 58.0,
      "rawValue": 5.8,
      "rawUnit": "CS/min"
    },
    {
      "key": "combat",
      "label": "Combat",
      "value": 64.2,
      "rawValue": 64.2,
      "rawUnit": "% KP"
    },
    {
      "key": "vision",
      "label": "Vision",
      "value": 44.0,
      "rawValue": 1.1,
      "rawUnit": "VS/min"
    },
    {
      "key": "objectives",
      "label": "Objectives",
      "value": 55.3,
      "rawValue": 55.3,
      "rawUnit": "% obj"
    },
    {
      "key": "survivability",
      "label": "Survivability",
      "value": 56.0,
      "rawValue": 4.4,
      "rawUnit": "deaths/game"
    }
  ],
  "gamesAnalyzed": 87
}
```

**Error Responses**:
| Status | Body | Condition |
|--------|------|-----------|
| 401 | `{ "error": "Not authenticated", "code": "NOT_AUTHENTICATED" }` | No valid session |
| 403 | Forbid | UserId doesn't match authenticated user |
| 400 | `{ "error": "Invalid userId format" }` | Non-numeric userId |
| 404 | `{ "error": "No riot accounts found for this user" }` | No linked Riot account |
| 200 | `{ "axes": [], "gamesAnalyzed": 0 }` | Account linked but no match data |

### Normalization Ranges
Each axis is normalized from raw value to 0–100 using fixed competitive ranges rather than player-relative percentiles. This avoids needing a global stats table and provides a consistent frame of reference.

| Axis | Raw Min (→0) | Raw Max (→100) | Notes |
|------|-------------|----------------|-------|
| Laning | -2000 gold diff | +2000 gold diff | 0 diff = 50. Linear scale. |
| Farming | 0 CS/min | 10 CS/min | ~6 CS/min is average; 10 is near-perfect. |
| Combat | 0% KP | 100% KP | Already a 0–100 percentage. |
| Vision | 0 VS/min | 2.5 VS/min | ~1.0 average non-support; 2.0+ for supports. |
| Objectives | 0% participation | 100% participation | Combined dragon/herald/baron/tower participation rate. |
| Survivability | 10 deaths/game (→0) | 0 deaths/game (→100) | Inverted: fewer deaths = higher score. |

### Implementation Pattern
Follow the existing endpoint pattern established by `DeathsTrendEndpoint`:

1. **Endpoint class** (`RadarChartEndpoint`): implements `IEndpoint`, sealed class
   - Route: `basePath + "/solo/radar-chart/{userId}"`
   - Auth check → parse userId → verify ownership → resolve PUUID via `IUserRiotAccountsRepository` → call repository → return DTO
   - Log with `LogSanitizer.Sanitize()` for all user input

2. **Repository** (`RadarChartRepository`): extends `RepositoryBase`
   - Constructor injects `IDbConnectionFactory`, `ILogger<RadarChartRepository>`, `IQueryFilterBuilder`
   - Single SQL query joining `participants`, `participant_metrics`, `participant_checkpoints` (minute_mark=15), `participant_objectives`, `team_objectives`, and `matches`
   - Use `_filterBuilder.ValidateQueueType()`, `BuildQueueFilter()`, `ResolveTimeRangeAsync()`, `BuildTimeRangeFilter()`, `AddTimeRangeParameters()`
   - Compute averages in SQL, normalize in C# static methods
   - Return `RadarChartResponse` or `null` if no data

3. **DTO** (`RadarChartDto`): static class with `RadarAxis` and `RadarChartResponse` records
   - Use `[property: JsonPropertyName("camelCase")]` on all properties

4. **Registration**:
   - `Program.cs`: `builder.Services.AddScoped<IRadarChartRepository, RadarChartRepository>();`
   - `MongooseApiApplication.cs`: instantiate and add `RadarChartEndpoint(basePath)`

### SQL Query Structure
```sql
SELECT
    COUNT(*) AS games_analyzed,
    AVG(pc.gold_diff_vs_lane) AS avg_gold_diff_15,
    AVG(p.creep_score / (m.game_duration_sec / 60.0)) AS avg_cs_per_min,
    AVG(pm.kill_participation_pct) AS avg_kill_participation,
    AVG(pm.vision_per_min) AS avg_vision_per_min,
    AVG(
        CASE
            WHEN (COALESCE(tobj.dragons_taken,0) + COALESCE(tobj.heralds_taken,0) + COALESCE(tobj.barons_taken,0) + COALESCE(tobj.towers_taken,0)) > 0
            THEN (COALESCE(po.dragons_participated,0) + COALESCE(po.heralds_participated,0) + COALESCE(po.barons_participated,0) + COALESCE(po.towers_participated,0)) * 100.0
                / (COALESCE(tobj.dragons_taken,0) + COALESCE(tobj.heralds_taken,0) + COALESCE(tobj.barons_taken,0) + COALESCE(tobj.towers_taken,0))
            ELSE NULL
        END
    ) AS avg_objective_participation,
    AVG(p.deaths) AS avg_deaths,
    AVG(pm.damage_share_pct) AS avg_damage_share
FROM participants p
INNER JOIN matches m ON m.match_id = p.match_id
LEFT JOIN participant_metrics pm ON pm.participant_id = p.id
LEFT JOIN participant_checkpoints pc ON pc.participant_id = p.id AND pc.minute_mark = 15
LEFT JOIN participant_objectives po ON po.participant_id = p.id
LEFT JOIN team_objectives tobj ON tobj.match_id = p.match_id AND tobj.team_id = p.team_id
WHERE p.puuid = @puuid {queueFilter} {timeFilter}
```

## Testing Strategy

### Integration Tests
**File**: `server/Mongoose.Api.Tests/RadarChartEndpointTests.cs`

Must follow the existing test patterns using `TestWebApplicationFactory`:

- [ ] `GetRadarChart_ReturnsData_WhenAuthenticated` — verify 200, 6 axes, gamesAnalyzed > 0
- [ ] `GetRadarChart_Returns401_WhenNotAuthenticated` — verify 401
- [ ] `GetRadarChart_Returns403_WhenAccessingOtherUsersData` — verify 403
- [ ] `GetRadarChart_Returns404_WhenNoRiotAccountLinked` — verify 404
- [ ] `GetRadarChart_SupportsQueueFilter` — verify filtering works
- [ ] `GetRadarChart_SupportsTimeRangeFilter` — verify time range works
- [ ] `GetRadarChart_ReturnsNormalizedValues_InZeroToHundredRange` — all axes.value between 0–100

**Fake**: Add `FakeRadarChartRepository` to `TestWebApplicationFactory.cs` implementing `IRadarChartRepository`, following the `FakeTrendRepository` pattern (uses `ConcurrentDictionary<string, RadarChartResponse>` keyed by puuid). Wire into DI alongside other fakes.

### Unit Tests
- [ ] Test normalization functions (gold diff, CS/min, KP, vision, objectives, survivability) with edge cases (0, max, negative values)

## Validation Criteria
Feature is considered complete when:
- [ ] Endpoint returns correct 6-axis radar data for authenticated users
- [ ] All normalization values are within 0–100 range
- [ ] Queue and time range filters work correctly
- [ ] Auth checks (401, 403) and error handling (404, 400) are correct
- [ ] Integration tests pass
- [ ] Endpoint is registered and accessible at `/api/v2/solo/radar-chart/{userId}`

## Dependencies
### Internal Dependencies
- [ ] `IQueryFilterBuilder` — for queue/time range filtering (already exists)
- [ ] `IUserRiotAccountsRepository` — for PUUID resolution (already exists)
- [ ] `RepositoryBase` — base class for DB access (already exists)

### External Dependencies
- None

## References
- [Solo Page Graph Alternatives](../../../docs/solo-page-graph-alternatives.md) — Spider Chart section
- [Solo Page Feature Research](../../../docs/solo-page-feature-research.md) — Performance Radar Chart (Feature 1)
- [Win Prediction Metrics Research](../../../docs/win-prediction-metrics-research.md) — Academic backing for axis selection
- [Architecture Spec](../architecture.spec.md) — Endpoint patterns, DTOs, repositories
- [Database Schema Spec](../database-schema.spec.md) — Table structures for queried data
- [Testing Spec](../test-strategy.spec.md) — xUnit patterns and TestWebApplicationFactory
