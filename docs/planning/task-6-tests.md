# Task 6: Tests

> **Parent**: [match-details-kpi-redesign.md](match-details-kpi-redesign.md)
> **Type**: Testing
> **Dependencies**: Tasks 1–5

---

## Backend Tests

### MatchEndpointTests.cs

- Verify `dragonsParticipated` integer field is present in `GET /api/v2/matches/{matchId}/details` response
- Verify it defaults to 0 when no `participant_objectives` row exists
- Add `DragonsParticipated` to any existing `MatchDetailsItem` assertions

## Frontend Tests

### New: `WinPredictionStats.spec.js`

- Renders 6 KPI tiles with correct labels
- Applies `.positive` class when deaths < baseline − 1
- Applies `.negative` class when deaths > baseline + 1
- Gold@15 shows "N/A" when `goldDiffAt15` is null
- Gold@15 shows "Won lane" when ≥ +500
- Dragon participation shows "No dragons" (neutral) when `teamDragons === 0`
- Dragon participation shows percentage when `teamDragons > 0`
- No comparison lines render when `baseline` is null
- CS/min sentiment suppressed for support role

### Update: `MatchDetails.spec.js`

- `WinPredictionStats` renders before `TeamComparison` in DOM order
- `ImpactStats` is no longer rendered
- Verify `WinPredictionStats` receives `match` and `baseline` props

### Update: `MatchActions.spec.js`

- "View Analysis" button is NOT disabled
- Clicking "View Analysis" calls `router.push({ name: 'app-solo' })`
- "View Goal Impact" button remains disabled

### Update: `StatSnapshot.spec.js`

- CS/min and Vision Score are no longer in the stat list
- Dmg/Gold appears with correct value and sentiment
- Dmg/Death appears for non-support roles
- Vision/min appears for support roles
- Total stat count is 10

## Acceptance Criteria

- [ ] All existing tests still pass
- [ ] New WinPredictionStats component has full unit test coverage
- [ ] MatchDetails section order verified in test
- [ ] MatchActions navigation verified in test
- [ ] StatSnapshot revised list verified in test
