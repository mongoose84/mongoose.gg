# Task 5: `SurvivalCheckCard.vue` component

> Source: [overview-redesign-frontend.md](overview-redesign-frontend.md) — extracted from Task 5

**Scope**: Build `SurvivalCheckCard.vue` in `client/src/components/overview/`. Single-insight card answering one question: *"Does dying less actually win me games?"* — with rank-adaptive death thresholds.

**File to create**:
- `client/src/components/overview/SurvivalCheckCard.vue`

---

## Design Principles

This `SurvivalCheckCard` design applies two UX principles from the [UI/UX spec](../../.github/specs/ui-ux.spec.md):

1. **"Every insight answers one question and implies one action"** — the old spec answered three questions (how much do I die? when do I die? how do deaths affect my wins?) and implied no clear action.
2. **"Overview is orientation, not work — 5–15 seconds"** — parsing avg deaths, a percentage bar, AND two bucket rows with game counts required focused reading, not a glance.

The redesign presents a **single pre-computed verdict** with a **single implied action** (play safer). Inspired by fitness app patterns (e.g. Apple Health's "Your resting heart rate is lower than last month") — one contextualized finding, not a dashboard.

---

## Rank-Adaptive Death Thresholds

Death thresholds are meaningless when hardcoded. A Bronze player averaging 7.5 deaths/game sees "72% WR when you die ≤3 times" based on maybe 1 game out of 20 — that's noise, not insight. The thresholds must reflect what "good" and "bad" look like **at the player's rank**.

| Rank Tier | Low Deaths (good game) | High Deaths (bad game) | Approx Avg |
|-----------|------------------------|------------------------|------------|
| Iron | ≤ 6 | 9+ | ~8.5 |
| Bronze | ≤ 5 | 8+ | ~7.5 |
| Silver | ≤ 5 | 7+ | ~6.5 |
| Gold | ≤ 4 | 6+ | ~5.5 |
| Platinum | ≤ 4 | 6+ | ~5.0 |
| Emerald | ≤ 3 | 5+ | ~4.5 |
| Diamond+ | ≤ 3 | 5+ | ~4.0 |
| Unranked / null | ≤ 4 | 7+ | Generous default |

The gap between "low" and "high" is intentional — a neutral zone avoids false precision and keeps both buckets large enough to produce meaningful win rate samples.

**Threshold resolution lives server-side.** The backend returns `lowDeathThreshold` and `highDeathThreshold` in the `SurvivalStats` response so the frontend never duplicates rank→threshold mapping. The card just displays "≤{lowDeathThreshold} deaths → {winRateLowDeaths}% WR".

**Edge case — rank changes mid-window**: Use the player's **current** rank. The insight is forward-looking ("play safer in your next game"), so current rank is the right anchor.

---

## Backend Tasks

> These tasks supersede the `SurvivalStats` portions of [overview-redesign-backend.md](overview-redesign-backend.md) Tasks 1–3. The `SessionStats` portions of those tasks remain unchanged.

### Execution Order

```
Task B1  (Core — DeathThresholds + DTO)
  ↓
Task B2  (Repository — parameterized survival query)
  ↓
Task B3  (Endpoint wiring + tests)
```

---

### Task B1: Core — `DeathThresholds` static lookup + `SurvivalStats` DTO

**Scope**: Add the rank→threshold mapping in Core and define the updated `SurvivalStats` DTO.

**Files to create/modify**:
- `server/Mongoose.Api/Core/DeathThresholds.cs` — new file
- `server/Mongoose.Api/Application/DTOs/Overview/OverviewDto.cs` — replace old `SurvivalStats` record

**`DeathThresholds.cs`** — pure domain logic, no dependencies:

```csharp
public static class DeathThresholds
{
    public static (int Low, int High) ForRank(string? rankTier) => rankTier?.ToUpperInvariant() switch
    {
        "IRON"        => (6, 9),
        "BRONZE"      => (5, 8),
        "SILVER"      => (5, 7),
        "GOLD"        => (4, 6),
        "PLATINUM"    => (4, 6),
        "EMERALD"     => (3, 5),
        "DIAMOND"     => (3, 5),
        "MASTER"      => (3, 5),
        "GRANDMASTER" => (3, 5),
        "CHALLENGER"  => (3, 5),
        _             => (4, 7) // Unranked / unknown
    };
}
```

**`SurvivalStats` DTO** — replaces the old version with hardcoded bucket names:

| Old field | New field |
|-----------|-----------|
| `winRateAtOrBelow3Deaths` | `winRateLowDeaths` |
| `winRateAbove5Deaths` | `winRateHighDeaths` |
| `gamesAtOrBelow3Deaths` | `gamesLowDeaths` |
| `gamesAbove5Deaths` | `gamesHighDeaths` |
| *(removed)* `deathsBefore10Pct` | — moved to Solo page scope |
| *(new)* | `lowDeathThreshold` (int) |
| *(new)* | `highDeathThreshold` (int) |

```csharp
public record SurvivalStats(
    [property: JsonPropertyName("avgDeathsPerGame")] double AvgDeathsPerGame,
    [property: JsonPropertyName("winRateLowDeaths")] double? WinRateLowDeaths,
    [property: JsonPropertyName("winRateHighDeaths")] double? WinRateHighDeaths,
    [property: JsonPropertyName("gamesLowDeaths")] int GamesLowDeaths,
    [property: JsonPropertyName("gamesHighDeaths")] int GamesHighDeaths,
    [property: JsonPropertyName("lowDeathThreshold")] int LowDeathThreshold,
    [property: JsonPropertyName("highDeathThreshold")] int HighDeathThreshold,
    [property: JsonPropertyName("totalGames")] int TotalGames
);
```

**Acceptance**:
- [ ] `DeathThresholds.ForRank("BRONZE")` returns `(5, 8)`, `ForRank(null)` returns `(4, 7)`, etc.
- [ ] `SurvivalStats` compiles, no `deathsBefore10Pct` field
- [ ] `OverviewResponse` still accepts `SurvivalStats?` (shape changed, optionality unchanged)

---

### Task B2: Repository — parameterized survival query

**Scope**: Update `GetSurvivalStatsAsync` to accept low/high thresholds instead of using hardcoded ≤3 / 5+ in SQL. Update the query model to match.

**Files to modify**:
- `IOverviewStatsRepository.cs` — update interface signature
- MySQL implementation (e.g. `OverviewStatsRepository.cs`) — parameterize SQL
- `Core/QueryModels/OverviewQueryModels.cs` — update `SurvivalStatsData`

**Updated query model**:

```csharp
/// <summary>
/// Survival analysis over the last N games with rank-adaptive death thresholds.
/// </summary>
public record SurvivalStatsData(
    double AvgDeathsPerGame,
    double? WinRateLowDeaths,
    double? WinRateHighDeaths,
    int GamesLowDeaths,
    int GamesHighDeaths,
    int TotalGames
);
```

**Updated interface**:

```csharp
Task<SurvivalStatsData> GetSurvivalStatsAsync(
    IReadOnlyList<string> puuids,
    int lowDeathThreshold,
    int highDeathThreshold,
    int lastNGames = 20);
```

**SQL changes** — replace hardcoded buckets with parameters:

```sql
-- Death bucket aggregation (within the last-20-games CTE)
SUM(CASE WHEN p.deaths <= @lowThreshold THEN 1 ELSE 0 END) AS games_low_deaths,
SUM(CASE WHEN p.deaths <= @lowThreshold AND p.win = 1 THEN 1 ELSE 0 END) AS wins_low_deaths,
SUM(CASE WHEN p.deaths >= @highThreshold THEN 1 ELSE 0 END) AS games_high_deaths,
SUM(CASE WHEN p.deaths >= @highThreshold AND p.win = 1 THEN 1 ELSE 0 END) AS wins_high_deaths
```

**Removed from query**: `deaths_pre_10` aggregation (`deathsBefore10Pct`). This metric moves to the Solo page and will be served by a different query when needed.

**Query notes**:
- **Last 20 games across all PUUIDs** sorted by `game_start_time` descending (unchanged from original spec)
- Win rate per bucket = `wins_low_deaths / games_low_deaths` (null if 0 games in bucket)
- Both `@lowThreshold` and `@highThreshold` are parameterized — no SQL concatenation
- Average deaths still computed as `AVG(p.deaths)` across all 20 games

**Acceptance**:
- [ ] Passing `(5, 8)` produces different bucket counts than passing `(3, 5)` for the same data
- [ ] Win rates are null (not 0) when a bucket has 0 games
- [ ] Repository integration tests cover at least two different threshold pairs
- [ ] `deathsBefore10Pct` is no longer computed or returned

---

### Task B3: Wire survival data into `OverviewEndpoint`

**Scope**: Resolve rank tier → death thresholds in the endpoint, pass them to the repository, and include them in the `SurvivalStats` DTO response.

**Files to modify**:
- `server/Mongoose.Api/Application/Endpoints/Overview/OverviewEndpoint.cs`

**Changes**:
1. After `GetPrimaryQueueAsync` resolves the player's rank, extract the rank tier string (e.g. `"GOLD"`)
2. Call `DeathThresholds.ForRank(rankTier)` to get `(lowThreshold, highThreshold)`
3. Pass thresholds to `GetSurvivalStatsAsync(selectedPuuids, lowThreshold, highThreshold)`
4. Map result to `SurvivalStats` DTO, including `LowDeathThreshold` and `HighDeathThreshold` in the response
5. **Parallelization** — `GetSurvivalStatsAsync` depends on rank tier from `GetPrimaryQueueAsync`, so it cannot run in parallel with the calls that don't need rank. Updated call graph:
   ```
   GetPrimaryQueueAsync
     ↓
   resolve DeathThresholds.ForRank(tier)
     ↓
   Task.WhenAll(
       GetLastMatchAsync,
       GetMostPlayedChampionAsync,
       GetSessionStatsAsync,
       GetSurvivalStatsAsync(puuids, low, high)   // now needs thresholds
   )
   ```

**Tests** (`OverviewEndpointTests.cs`):
- [ ] Response `survivalStats` includes `lowDeathThreshold` and `highDeathThreshold`
- [ ] Thresholds match the player's current rank (e.g. Gold player gets `lowDeathThreshold: 4, highDeathThreshold: 6`)
- [ ] Unranked player gets default thresholds `(4, 7)`
- [ ] `survivalStats` no longer includes `deathsBefore10Pct`
- [ ] `winRateLowDeaths` / `winRateHighDeaths` field names are correct in JSON response

**Acceptance**:
- [ ] `DeathThresholds.ForRank` is called once per request, not per PUUID
- [ ] Thresholds flow from endpoint → repository (SQL params) → DTO (response fields)
- [ ] All existing `OverviewEndpointTests` still pass (update assertions for renamed/removed fields)
- [ ] New integration tests pass

---

## Props

- `survivalStats` — the `SurvivalStats` object from the API (nullable), containing:
  - `winRateLowDeaths` (`number | null`) — WR when deaths ≤ `lowDeathThreshold`
  - `winRateHighDeaths` (`number | null`) — WR when deaths ≥ `highDeathThreshold`
  - `avgDeathsPerGame` (`number`)
  - `lowDeathThreshold` (`number`) — rank-adaptive "good" boundary
  - `highDeathThreshold` (`number`) — rank-adaptive "bad" boundary
  - `totalGames` (`number`)
- `loading` — boolean for skeleton state

---

## Visual Structure

```
┌─ [3px left border: success/error/default] ────────────────────┐
│                                                                │
│  DEATH INSIGHT                                                 │
│                                                                │
│  72%  win rate when you die ≤5 times                           │
│  (40px, green)   (14px, text-secondary)                        │
│                                                                │
│  vs  41%  when you die 8+ times                                │
│  (14px, muted)  (18px, red)  (14px, text-secondary)            │
│                                                                │
│  ──────────────────────────────────────                        │
│  Your avg: 6.8 deaths/game  ·  15 games                        │
│  (12px, text-secondary)                                        │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

(Example shown for a Bronze player — thresholds ≤5 / 8+ come from backend.)

---

## Styling

- Card base: `background: var(--color-surface)`, `border-radius: var(--radius-lg)`, `backdrop-filter: blur(10px)`
- Section label: `text-xs uppercase tracking-wide text-text-secondary`
- Hero WR number: `font-size-2xl` (40px), `font-weight-bold`, color-coded via `useWinRateColor()`
- Hero qualifier text: `font-size-sm`, `text-text-secondary`, inline after the number
- Contrast "vs" line: `font-size-lg` (18px) for the WR number (color-coded via `useWinRateColor()`), `font-size-sm text-text-secondary` for surrounding text
- Divider: `1px solid var(--color-border)`, `margin: var(--spacing-sm) 0`
- Footer context: `font-size-xs`, `text-text-secondary`
- Hover: `transform: translateY(-2px)` + `box-shadow: var(--shadow-md)` + `transition: all 0.2s ease`

### Performance-tinted left border

| Condition | Border color | Token |
|-----------|-------------|-------|
| Avg deaths ≤ `lowDeathThreshold` | Green | `var(--color-success-border)` |
| Avg deaths ≥ `highDeathThreshold` | Red | `var(--color-error-border)` |
| Between | Default purple | `var(--color-border)` |

Remaining three borders: `1px solid var(--color-border)`.

---

## Adaptive Headline Logic

The card picks the **single most impactful framing** from the data rather than dumping all numbers.

| Condition | Headline | Tone |
|-----------|----------|------|
| `winRateLowDeaths` ≥ 55% AND gap ≥ 15pp | **"{WR}% win rate when you die ≤{low} times"** | Motivational (success border) |
| `winRateHighDeaths` ≤ 45% AND gap ≥ 15pp | **"You lose {WR}% when dying {high}+ times"** | Warning (error border) |
| Gap < 15pp or insufficient data | **"Avg {N} deaths/game across {total} games"** | Neutral (default border) |
| `survivalStats` is null | **"Play a few games to unlock death insights"** | Empty state |

Where:
- `gap` = `winRateLowDeaths - winRateHighDeaths`
- `{low}` = `survivalStats.lowDeathThreshold`
- `{high}` = `survivalStats.highDeathThreshold`
- "insufficient data" = either bucket has 0 games

When in motivational mode, the hero number is `winRateLowDeaths` (green). When in warning mode, the hero number is `winRateHighDeaths` (red) and the contrast line shows the low-death WR. When neutral, the hero number is `avgDeathsPerGame` with no contrast line.

---

## Height Alignment with TodaySessionCard

`DeathInsightCard` (right column) must match the height of `TodaySessionCard` (left column) in the "At a glance" row. Since the two cards can render different amounts of content, height must be enforced at the layout level — not by setting a fixed pixel height on either card.

**Required**: The `section-row` in `OverviewLayout.vue` already uses `align-items: stretch` on desktop. Both cards must use `height: 100%` so they fill the row height set by whichever card is taller.

**Implementation checklist**:
- [ ] `TodaySessionCard` root element: `height: 100%`
- [ ] `DeathInsightCard` root element: `height: 100%`
- [ ] Both cards use `display: flex; flex-direction: column` internally so content distributes within the full height
- [ ] `OverviewPage.vue` — the `.glance-right-fill` wrapper (if present) must also propagate height: `display: flex; flex-direction: column; height: 100%`
- [ ] Verify alignment holds in all four card states: loading skeleton, empty, motivational, and neutral/warning

---

## Consistency with TodaySessionCard

Both `#glance-left` and `#glance-right` follow the same card pattern — natural left-to-right read: **"How am I doing?" → "What should I focus on?"**

| Property | TodaySessionCard | DeathInsightCard |
|----------|-----------------|------------------|
| Hero element | Win rate (40px, color-coded) | Win rate (40px, color-coded) |
| Supporting detail | W/L, KDA (14px, secondary) | Contrast WR (18px) + condition text |
| Footer/context | W/L strip | Avg deaths + sample size |
| Left border | Performance-tinted | Death-rate-tinted |
| Sentiment | How you're doing | What to do about it |

---

## What Moves to Solo Page

Detail removed from Overview is relocated, not lost:

- `deathsBefore10Pct` (early death %) → Solo page deep analysis, alongside other timeline metrics
- Per-bucket game counts → Solo page, where sample size scrutiny is appropriate
- Full death breakdown table → potential Solo Zone 4 widget

This respects [Design Constraint #3](../../.github/specs/ui-ux.spec.md): *"No duplicated deep analysis across pages."*

---

## Accessibility

- `aria-label="Death insight: win rate correlation with deaths"` on card section
- Win rate values: numeric label always present (not color-only)
- Left border color is decorative — text carries meaning independently
- Contrast ratio: all text meets 4.5:1 against `var(--color-surface)`

---

## Tests (`test/unit/DeathInsightCard.spec.js`)

- [ ] Renders motivational headline when low-death WR ≥ 55% and gap ≥ 15pp
- [ ] Renders warning headline when high-death WR ≤ 45% and gap ≥ 15pp
- [ ] Falls back to neutral baseline when gap < 15pp
- [ ] Shows empty state when `survivalStats` is null
- [ ] Shows loading skeleton when `loading` is true
- [ ] Footer shows avg deaths and total games
- [ ] Displays rank-adaptive thresholds from `lowDeathThreshold` / `highDeathThreshold` in headline text
