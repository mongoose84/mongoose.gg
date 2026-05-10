# Feature: Damage Per Minute (DPM) Trend Graph

> Purpose: Add a single DPM trend graph to the Solo Dashboard using the same interaction model as existing trend graphs.

## Problem Statement

Players currently cannot see how their combat output changes over time from the Solo Dashboard trend section. Existing graphs cover outcomes and supporting metrics, but there is no direct damage output trend.

## Scope (Rewritten)

This feature is intentionally limited to one chart.

In scope:
- Add one DPM trend graph card in the same area and style as existing trend cards.
- Use existing dashboard filters (queue, time range, account).
- Show one DPM series per selected account (same overlay behavior as other trend graphs).
- Keep standard chart tooltip behavior used by existing graphs.

Out of scope:
- No efficiency cards.
- No extra KPI row.
- No chart mode toggle buttons.
- No win/loss split mode.
- No phase split (early/mid/late).
- No role benchmark cards.

## User Story

As a solo ladder climber, I want a simple DPM trend graph so I can quickly see whether my damage output is trending up or down over recent games.

## Requirements

### Functional Requirements

1. Render one DPM trend graph in Solo Dashboard Zone 3 after Vision Score, following existing trend-card structure.
2. Fetch trend data from a backend endpoint aligned with current trend API conventions.
3. Respect shared filters:
- `queueType`
- `timeRange`
- `accountId`
4. Show loading, empty, and error states consistent with other trend graphs.
5. Support account overlays only as already supported by current account filter behavior.

### Non-Functional Requirements

- Performance: endpoint returns within existing trend endpoint expectations for filtered datasets.
- Security: authenticated endpoint with ownership checks; no raw PUUID accepted from client.
- Accessibility: graph uses the same keyboard and ARIA behavior as existing trend charts.
- Consistency: visual style, legend, tooltip, and card chrome match existing trend components.

## Technical Approach

### Backend

- Endpoint location: `Application/Endpoints/Trends/`
- Route: `GET /api/v2/trends/damage-per-minute/{userId}`
- Query params:
- `queueType` (optional): `ranked_solo | ranked_flex | normal | aram | all`
- `timeRange` (optional): `1w | 1m | 3m | 6m | current_season | last_season | all`
- `accountId` (optional): omitted, `all`, or opaque `acc_*`
- `limit` (optional): integer, max 500

Data contract should match other trend endpoints:
- `dpmTrend` (array of points)
- `averageDamagePerMinute` (double)
- `overallAverage` (double)
- `trend` (string: `up`, `down`, `stable`)

Point shape (matching VisionScoreTrendPoint and CsPerMinuteTrendPoint patterns):
- `matchId`
- `gameIndex`
- `timestamp`
- `totalDamageDealt` (int)
- `damagePerMinute` (double)
- `gameDurationMinutes` (double)
- `championName` (string)
- `role` (string, optional)
- `accountGameName` (string, optional; included when overlaying account series)

Notes:
- DPM formula: `totalDamageDealt / gameDurationMinutes`
- Reuse `IQueryFilterBuilder` for queue type and time-range normalization to avoid one-off formats.
- Match response shape to existing trend endpoints (VisionScoreTrendResponse, CsPerMinuteTrendResponse).
- Keep orchestration in Application and reusable calculations in Core.
- Use parameterized SQL only.
- Sanitize logged values.

### Frontend

- Add one chart component in `client/src/components/Charts/` or reuse existing generic trend chart if available.
- Add one composable/service fetch path for DPM trend data using shared filter model.
- Integrate into `SoloStatsPage.vue` after Vision Score.
- Do not add any extra cards or controls beyond standard trend graph behavior.

## API Contract

### Endpoint

`GET /api/v2/trends/damage-per-minute/{userId}?queueType=ranked_solo&timeRange=3m&accountId=all`

### Response (200)

```json
{
  "dpmTrend": [
    {
      "matchId": "NA1_12345_67890",
      "gameIndex": 1,
      "timestamp": "2026-05-09T14:30:00Z",
      "totalDamageDealt": 18000,
      "damagePerMinute": 456.0,
      "gameDurationMinutes": 39.5,
      "championName": "Ahri",
      "role": "mid",
      "accountGameName": "Main"
    }
  ],
  "averageDamagePerMinute": 445.2,
  "overallAverage": 420.5,
  "trend": "up"
}
```

### Error Responses

- 400 Bad Request: invalid query params
- 401 Unauthorized: missing/invalid auth
- 403 Forbidden: user not allowed to access requested account scope
- 500 Internal Server Error: unexpected failure

## UI/UX Requirements

- Card title: `Damage Per Minute`
- Subtitle style follows existing trend graph subtitle pattern.
- Single line/series visualization style consistent with other trend cards.
- Tooltip shows at least:
- Game number
- DPM value
- Match timestamp

No additional UI sections are included below the graph.

## Testing Strategy

### Backend Tests

- `DpmTrendEndpointTests.cs`
- Auth required returns 401 when missing.
- Ownership/account scope enforcement returns 403 when invalid.
- Invalid `queueType` or `timeRange` returns 400.
- Valid request returns 200 with `trendData` and `gamesAnalyzed`.

- Service/repository tests:
- DPM calculation is correct.
- Filters (`queueType`, `timeRange`, `accountId`) are applied correctly.
- Empty result returns empty `trendData` without failure.

### Frontend Tests

- `DpmChart.spec.js` (or existing trend chart spec extension):
- Renders DPM graph with provided data.
- Shows loading state.
- Shows empty state for no data.
- Uses existing tooltip pattern.

- `useSoloDpmData.spec.js`:
- Calls API with shared filter params.
- Handles success and error responses.

### E2E

- Solo Dashboard displays DPM graph in expected position.
- Changing shared filters refreshes DPM graph.
- Graph remains chart-only (no extra cards/toggles rendered).

## Validation Criteria

Feature is complete when:

- DPM appears as a single trend graph card in Solo Dashboard.
- Endpoint and filter contract follow existing `/api/v2` trend conventions.
- No raw PUUID is accepted from client input.
- No extra UI blocks (cards, split toggles, benchmark sections) are present.
- Existing dashboard test suite passes without regressions.

## Files Affected Summary

Likely new/updated files:
- `server/Mongoose.Api/Application/Endpoints/Trends/DpmTrendEndpoint.cs`
- `server/Mongoose.Api/Application/Services/DpmTrendService.cs`
- `server/Mongoose.Api/Infrastructure/Repositories/DpmTrendRepository.cs`
- `server/Mongoose.Api.Tests/DpmTrendEndpointTests.cs`
- `client/src/components/Charts/DpmChart.vue` (or shared trend chart reuse)
- `client/src/composables/useSoloDpmData.js`
- `client/src/services/analyticsService.js`
- `client/src/views/SoloStatsPage.vue`
- `client/test/unit/DpmChart.spec.js`
- `client/test/unit/useSoloDpmData.spec.js`
- `client/e2e/solo-dashboard.spec.js`
