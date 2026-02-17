# Feature: Danger Zones Death Heatmap — Backend

## Problem Statement
The Solo Dashboard shows that deaths are "the most actionable improvement target" but only presents deaths as an aggregate trend line (deaths per game over time) and time-bucketed counts (pre-10, 10–20, etc.). Players cannot see **where** on the map they die most frequently. Spatial death patterns reveal specific positional mistakes (e.g., "I keep dying in river near dragon pit") that time-based data alone cannot surface.

The Riot API Timeline v5 provides x/y map coordinates for every `CHAMPION_KILL` event, but the current sync pipeline discards position data. This feature adds the backend infrastructure to store and serve death position data.

## Proposed Solution
1. Create a `participant_death_events` database table to store individual death events with x/y coordinates
2. Extend `RiotTimelineMapper` to extract death position data from timeline events
3. Extend the `MatchHistorySyncJob` to persist death events during match sync
4. Create a new API endpoint `GET /api/v2/solo/death-positions/{userId}` that returns aggregated death positions for heatmap rendering

## User Stories
### Primary User Story
As a solo player, I want to see where on the Summoner's Rift map I die most frequently so that I can identify dangerous areas and adjust my pathing and positioning.

### Additional User Stories
- As a solo player, I want to filter death positions by game phase (0–10, 10–20, 20–30, 30+ minutes) so that I can distinguish laning deaths from teamfight deaths
- As a solo player, I want to filter by queue type and time range so that I see recent and relevant data
- As a solo player, I want to see death positions from enough games (last 20+) so that meaningful patterns emerge

## Requirements

### Functional Requirements
1. Store individual death events with map coordinates (x, y), timestamp (minute mark), killer champion ID, and number of assisting enemies
2. Extract death position data from `CHAMPION_KILL` timeline events during match sync
3. Store death events for ALL 10 participants in each match (not just the syncing player) to support future Duo/Team features
4. Serve aggregated death positions for a player across their recent matches
5. Support filtering by game phase (early: 0–10, mid: 10–20, late: 20–30, very late: 30+), queue type, and time range
6. Return death count per match for density context
7. New matches should automatically have death events extracted — no backfill required initially (only applies to matches synced after this feature ships)

### Non-Functional Requirements
- **Performance**: Endpoint response under 300ms for up to 200 matches worth of death events (~1000 death points)
- **Security**: Requires authentication; user can only access own data
- **Storage**: ~5–10 death events per participant per match × 10 participants = 50–100 rows per match. Budget for ~500K rows per active user year.
- **Data integrity**: Death events must be linked to participants via foreign key with CASCADE delete

## Technical Approach

### Database Changes
**New table**: `participant_death_events`

```sql
CREATE TABLE IF NOT EXISTS participant_death_events (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    participant_id BIGINT UNSIGNED NOT NULL,
    minute_mark INT NOT NULL,
    position_x INT NOT NULL,
    position_y INT NOT NULL,
    killer_champion_id INT NULL,
    assist_count INT NOT NULL DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    KEY idx_participant_id (participant_id),
    KEY idx_minute_mark (minute_mark),
    CONSTRAINT fk_death_events_participant FOREIGN KEY (participant_id) REFERENCES participants(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

**Columns**:
| Column | Type | Description |
|--------|------|-------------|
| `id` | `BIGINT UNSIGNED AUTO_INCREMENT` | Primary key |
| `participant_id` | `BIGINT UNSIGNED NOT NULL` | FK to `participants.id` |
| `minute_mark` | `INT NOT NULL` | Game minute when death occurred (floor of `timestamp / 60000`) |
| `position_x` | `INT NOT NULL` | Riot API x coordinate (0–15000 range, origin at bottom-left blue fountain) |
| `position_y` | `INT NOT NULL` | Riot API y coordinate (0–15000 range) |
| `killer_champion_id` | `INT NULL` | Champion ID of the killer (null for fountain/execute deaths) |
| `assist_count` | `INT NOT NULL DEFAULT 0` | Number of enemy assistants (indicates gank vs 1v1 death) |
| `created_at` | `TIMESTAMP` | Row creation time |

**Indexes**: `idx_participant_id` for queries by participant, `idx_minute_mark` for phase filtering.

**Schema file**: Add to `server/schema.sql` after the `participant_objectives` table.

### Backend Changes
**Language**: C#

**Components**:
- [ ] Entity: `server/Core/Entities/ParticipantDeathEvent.cs`
- [ ] Repository interface: `server/Core/Interfaces/IParticipantDeathEventsRepository.cs`
- [ ] Repository implementation: `server/Infrastructure/Database/Repositories/ParticipantDeathEventsRepository.cs`
- [ ] DTO: `server/Application/DTOs/Solo/DeathPositionsDto.cs`
- [ ] API endpoint interface: `server/Core/Interfaces/IDeathPositionsRepository.cs` (read-side query repo)
- [ ] API endpoint repository: `server/Infrastructure/Database/Repositories/DeathPositionsRepository.cs`
- [ ] Endpoint: `server/Application/Endpoints/Solo/DeathPositionsEndpoint.cs`
- [ ] Timeline mapper extension: `server/Infrastructure/Riot/Mappers/RiotTimelineMapper.cs` (add `ExtractDeathPositions()`)
- [ ] Sync job extension: `server/Infrastructure/Jobs/MatchHistorySyncJob.cs` (add death event persistence step)
- [ ] DI registration: `server/Program.cs`
- [ ] Endpoint registration: `server/Application/MongooseApiApplication.cs`

### Entity

**File**: `server/Core/Entities/ParticipantDeathEvent.cs`
```csharp
namespace Mongoose.Api.Core.Entities;

public class ParticipantDeathEvent : EntityBase
{
    public long Id { get; set; }
    public long ParticipantId { get; set; }
    public int MinuteMark { get; set; }
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public int? KillerChampionId { get; set; }
    public int AssistCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### Timeline Mapper Extension

**File**: `server/Infrastructure/Riot/Mappers/RiotTimelineMapper.cs`

Add a new static method `ExtractDeathPositions()` following the same pattern as `ExtractDeathTimings()`:

```csharp
/// <summary>
/// Extracts death position data from timeline events.
/// Returns a dictionary mapping participantId to a list of death position events.
/// Each event includes x/y coordinates, minute mark, killer champion ID, and assist count.
/// </summary>
public static Dictionary<int, List<DeathPositionData>> ExtractDeathPositions(JsonElement timelineRoot)
```

**Data class** (nested in `RiotTimelineMapper`):
```csharp
public class DeathPositionData
{
    public int MinuteMark { get; set; }
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public int? KillerChampionId { get; set; }
    public int AssistCount { get; set; }
}
```

**Extraction logic** — iterate timeline frames → events, filter `type == "CHAMPION_KILL"`:
- `victimId` → used as dictionary key (maps to Riot participantId 1–10)
- `position.x`, `position.y` → death coordinates
- `timestamp` → convert to minute mark (`timestamp / 60000`)
- `killerId` → look up champion ID from participant mapping (or pass through as Riot participantId for later resolution)
- `assistingParticipantIds` → count for `assistCount`

**Important**: The existing `ExtractDeathTimings()` already iterates the same events. Consider whether to combine them into a single pass or keep separated for backwards compatibility. Recommended: **keep separate** to avoid breaking existing code, and let both iterate independently. The performance cost of double-iteration is negligible (<1ms for typical timelines).

### Sync Job Extension

**File**: `server/Infrastructure/Jobs/MatchHistorySyncJob.cs`

In the timeline processing section (after the existing checkpoints and objective participation blocks, around line 635), add:

```csharp
// Death position events (for danger zone heatmap)
var deathPositions = RiotTimelineMapper.ExtractDeathPositions(timelineRoot.Value);
foreach (var (riotPid, positions) in deathPositions)
{
    if (!participantIdMap.TryGetValue(riotPid, out var dbPid)) continue;
    foreach (var pos in positions)
    {
        await deathEventsRepo.InsertAsync(new ParticipantDeathEvent
        {
            ParticipantId = dbPid,
            MinuteMark = pos.MinuteMark,
            PositionX = pos.PositionX,
            PositionY = pos.PositionY,
            KillerChampionId = pos.KillerChampionId,
            AssistCount = pos.AssistCount,
            CreatedAt = DateTime.UtcNow
        });
    }
}
```

**Injection**: The `MatchHistorySyncJob` needs `IParticipantDeathEventsRepository` added to its constructor DI.

### Write Repository

**Interface**: `server/Core/Interfaces/IParticipantDeathEventsRepository.cs`
```csharp
public interface IParticipantDeathEventsRepository
{
    Task InsertAsync(ParticipantDeathEvent deathEvent);
    Task InsertBatchAsync(IEnumerable<ParticipantDeathEvent> deathEvents);
}
```

**Implementation**: `server/Infrastructure/Database/Repositories/ParticipantDeathEventsRepository.cs`
- `InsertAsync` — single INSERT
- `InsertBatchAsync` — batch INSERT for efficiency (all death events for one participant in a match)

### Read Repository (for API endpoint)

**Interface**: `server/Core/Interfaces/IDeathPositionsRepository.cs`
```csharp
public interface IDeathPositionsRepository
{
    Task<DeathPositionsResponse?> GetDeathPositionsAsync(
        string puuid, string? queueType = null, string? timeRange = null, string? phase = null);
}
```

**Implementation**: `server/Infrastructure/Database/Repositories/DeathPositionsRepository.cs`
- Extends `RepositoryBase`, injects `IQueryFilterBuilder`
- Joins `participant_death_events` → `participants` → `matches` for filtering
- Optional `phase` filter adds `AND pde.minute_mark < 10` / `BETWEEN 10 AND 19` / `BETWEEN 20 AND 29` / `>= 30`

### API Contract

#### `GET /api/v2/solo/death-positions/{userId}`

**Query Parameters**:
| Parameter | Type | Required | Default | Values |
|-----------|------|----------|---------|--------|
| `queueType` | string | No | `all` | `ranked_solo`, `ranked_flex`, `normal`, `aram`, `all` |
| `timeRange` | string | No | `null` (all time) | `1w`, `1m`, `3m`, `6m`, `current_season`, `last_season` |
| `phase` | string | No | `null` (all phases) | `early` (0–10), `mid` (10–20), `late` (20–30), `very_late` (30+) |

**Response (200)**:
```json
{
  "deaths": [
    {
      "x": 7234,
      "y": 8456,
      "minuteMark": 8,
      "phase": "early",
      "killerChampionId": 238,
      "assistCount": 1
    },
    {
      "x": 4561,
      "y": 6789,
      "minuteMark": 22,
      "phase": "late",
      "killerChampionId": 412,
      "assistCount": 3
    }
  ],
  "totalDeaths": 143,
  "matchesAnalyzed": 32,
  "phaseSummary": {
    "early": 28,
    "mid": 45,
    "late": 42,
    "veryLate": 28
  }
}
```

**DTO**:
```csharp
public static class DeathPositionsDto
{
    public record DeathPosition(
        [property: JsonPropertyName("x")] int X,
        [property: JsonPropertyName("y")] int Y,
        [property: JsonPropertyName("minuteMark")] int MinuteMark,
        [property: JsonPropertyName("phase")] string Phase,
        [property: JsonPropertyName("killerChampionId")] int? KillerChampionId,
        [property: JsonPropertyName("assistCount")] int AssistCount
    );

    public record PhaseSummary(
        [property: JsonPropertyName("early")] int Early,
        [property: JsonPropertyName("mid")] int Mid,
        [property: JsonPropertyName("late")] int Late,
        [property: JsonPropertyName("veryLate")] int VeryLate
    );

    public record DeathPositionsResponse(
        [property: JsonPropertyName("deaths")] DeathPosition[] Deaths,
        [property: JsonPropertyName("totalDeaths")] int TotalDeaths,
        [property: JsonPropertyName("matchesAnalyzed")] int MatchesAnalyzed,
        [property: JsonPropertyName("phaseSummary")] PhaseSummary PhaseSummary
    );
}
```

**Error Responses**:
| Status | Condition |
|--------|-----------|
| 401 | Not authenticated |
| 403 | UserId mismatch |
| 400 | Invalid userId format or invalid phase value |
| 404 | No linked Riot account |
| 200 | Empty `deaths` array if no death events stored yet |

### Summoner's Rift Coordinate Reference
The Riot API uses a coordinate system:
- **Origin (0, 0)**: Bottom-left corner (blue side fountain)
- **Max (~15000, 15000)**: Top-right corner (red side fountain)
- **Mid lane**: Diagonal from ~(2000, 2000) to ~(13000, 13000)
- Coordinates are stored as-is (raw Riot values). Frontend handles normalization to minimap pixel space.

### Implementation Pattern
Follow existing endpoint patterns:

1. **Endpoint** (`DeathPositionsEndpoint`): implements `IEndpoint`, sealed class
   - Route: `basePath + "/solo/death-positions/{userId}"`
   - Standard auth → parse → verify → resolve PUUID → validate phase param → call repository → return DTO
   - Validate `phase` param: must be one of `early`, `mid`, `late`, `very_late`, or null

2. **Read Repository** (`DeathPositionsRepository`): extends `RepositoryBase`
   - SQL query joining `participant_death_events pde` → `participants p` → `matches m`
   - WHERE `p.puuid = @puuid` + queue/time/phase filters
   - Phase filter mapping: `early` → `pde.minute_mark < 10`, `mid` → `BETWEEN 10 AND 19`, `late` → `BETWEEN 20 AND 29`, `very_late` → `>= 30`
   - Also compute `matchesAnalyzed` (COUNT DISTINCT p.match_id) and `phaseSummary` in the same query or a second query

3. **Write Repository** (`ParticipantDeathEventsRepository`): extends `RepositoryBase`
   - Simple INSERT statements, called from sync job

4. **Registration**:
   - `Program.cs`: `builder.Services.AddScoped<IParticipantDeathEventsRepository, ParticipantDeathEventsRepository>()` and `builder.Services.AddScoped<IDeathPositionsRepository, DeathPositionsRepository>()`
   - `MongooseApiApplication.cs`: instantiate and add `DeathPositionsEndpoint(basePath)`

### SQL Query Structure (Read)
```sql
SELECT
    pde.position_x,
    pde.position_y,
    pde.minute_mark,
    pde.killer_champion_id,
    pde.assist_count
FROM participant_death_events pde
INNER JOIN participants p ON p.id = pde.participant_id
INNER JOIN matches m ON m.match_id = p.match_id
WHERE p.puuid = @puuid
    {queueFilter}
    {timeFilter}
    {phaseFilter}
ORDER BY m.game_start_time DESC, pde.minute_mark ASC
```

Summary query:
```sql
SELECT
    COUNT(*) AS total_deaths,
    COUNT(DISTINCT p.match_id) AS matches_analyzed,
    SUM(CASE WHEN pde.minute_mark < 10 THEN 1 ELSE 0 END) AS early_deaths,
    SUM(CASE WHEN pde.minute_mark BETWEEN 10 AND 19 THEN 1 ELSE 0 END) AS mid_deaths,
    SUM(CASE WHEN pde.minute_mark BETWEEN 20 AND 29 THEN 1 ELSE 0 END) AS late_deaths,
    SUM(CASE WHEN pde.minute_mark >= 30 THEN 1 ELSE 0 END) AS very_late_deaths
FROM participant_death_events pde
INNER JOIN participants p ON p.id = pde.participant_id
INNER JOIN matches m ON m.match_id = p.match_id
WHERE p.puuid = @puuid
    {queueFilter}
    {timeFilter}
```

## Testing Strategy

### Integration Tests
**File**: `server/Mongoose.Api.Tests/DeathPositionsEndpointTests.cs`

- [ ] `GetDeathPositions_ReturnsData_WhenAuthenticated` — verify 200, deaths array, phaseSummary
- [ ] `GetDeathPositions_Returns401_WhenNotAuthenticated`
- [ ] `GetDeathPositions_Returns403_WhenAccessingOtherUsersData`
- [ ] `GetDeathPositions_Returns404_WhenNoRiotAccountLinked`
- [ ] `GetDeathPositions_ReturnsEmptyArray_WhenNoDeathEventsStored`
- [ ] `GetDeathPositions_SupportsPhaseFilter` — verify phase filtering returns correct subset
- [ ] `GetDeathPositions_SupportsQueueFilter`
- [ ] `GetDeathPositions_SupportsTimeRangeFilter`
- [ ] `GetDeathPositions_Returns400_ForInvalidPhase`

**Fake**: Add `FakeDeathPositionsRepository` to `TestWebApplicationFactory.cs`.

### Unit Tests
- [ ] `ExtractDeathPositions` — test with mock timeline JSON containing known death events, verify x/y/minute/killer extraction
- [ ] `ExtractDeathPositions` — test with no deaths (empty result)
- [ ] `ExtractDeathPositions` — test with execute death (no killer, killerId=0)
- [ ] Phase classification — verify minute-to-phase mapping

## Validation Criteria
Feature is considered complete when:
- [ ] `participant_death_events` table created in `schema.sql`
- [ ] Death events are extracted and stored during match sync for all participants
- [ ] API endpoint returns death positions with correct coordinates and phase info
- [ ] Phase filtering works correctly
- [ ] Queue and time range filters work
- [ ] Auth/error handling is correct
- [ ] Integration tests pass
- [ ] Sync job stores death events without errors for real Riot API timeline data

## Dependencies
### Internal Dependencies
- [ ] `RiotTimelineMapper` — extend with `ExtractDeathPositions()` (already exists)
- [ ] `MatchHistorySyncJob` — extend with death events persistence step (already exists)
- [ ] `IQueryFilterBuilder` — for queue/time range filtering (already exists)
- [ ] `RepositoryBase` — base class for DB access (already exists)

### External Dependencies
- None (Riot API Timeline v5 already fetched during sync)

## Risks and Mitigations
| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Large table size over time | Medium | Medium | CASCADE delete handles cleanup; `MatchCleanupJob` already deletes old matches (180-day retention) which cascades to death events |
| No backfill for existing matches | Low | Certain | Death events only for newly synced matches. Players naturally accumulate data as they play. Could add backfill job later if needed. |
| Execute deaths (killerId=0) have no position | Low | Low | Riot API still provides victim position for execute deaths. Killer ID is simply null. |

## Backfill Strategy (Future)
If needed, a one-time backfill job could re-fetch timelines for existing matches via `GET /lol/match/v5/matches/{matchId}/timeline` and extract death positions. This would be rate-limited by the Riot API (100 requests/2 minutes) and should run as a low-priority background task.

## References
- [Solo Page Graph Alternatives](../../../docs/solo-page-graph-alternatives.md) — Danger Zones section
- [Solo Page Feature Research](../../../docs/solo-page-feature-research.md) — Death Spatial Heatmap (Feature 2)
- [Win Prediction Metrics Research](../../../docs/win-prediction-metrics-research.md) — Death impact analysis
- [Architecture Spec](../architecture.spec.md) — Endpoint and repository patterns
- [Database Schema Spec](../database-schema.spec.md) — Existing table structures
