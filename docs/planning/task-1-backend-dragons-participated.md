# Task 1: Backend — Add dragonsParticipated to Match Details DTO

> **Parent**: [match-details-kpi-redesign.md](match-details-kpi-redesign.md)
> **Type**: Backend
> **Dependencies**: None
> **Blocked by**: Nothing

---

## Objective

Add `dragonsParticipated` to the match details API response so the frontend can compute dragon participation rate per match.

## Database Context

The data already exists:
- `participant_objectives.dragons_participated` — how many dragon fights the player joined
- `team_objectives.dragons_taken` — how many dragons the player's team took (already returned as `teamDragons`)

## Files to Change

### 1. `server/Mongoose.Api/Core/QueryModels/MatchQueryModels.cs`

Add to `MatchDetailsRawData`:
```csharp
int DragonsParticipated
```

Add to `MatchDetailsItem`:
```csharp
[property: JsonPropertyName("dragonsParticipated")] int DragonsParticipated
```

### 2. `server/Mongoose.Api/Infrastructure/Database/Repositories/MatchesRepository.cs`

In `GetMatchDetailsAsync`:

**Add to SELECT**:
```sql
COALESCE(po.dragons_participated, 0) as dragons_participated
```

**Add to JOINs** (after the existing `tobj_enemy` join):
```sql
LEFT JOIN participant_objectives po ON po.participant_id = p.id
```

**Add to the `MatchDetailsItem` construction** at the end of the method:
```csharp
DragonsParticipated: rawData.DragonsParticipated
```

### 3. Test stubs implementing `IMatchesRepository`

Update any `MatchDetailsItem` construction in:
- `server/Mongoose.Api.Tests/TestWebApplicationFactory.cs`
- `server/Mongoose.Api.Tests/MatchCleanupJobTests.cs`

Add `DragonsParticipated: 0` (or appropriate test value) to all `MatchDetailsItem` instantiations.

## Acceptance Criteria

- [ ] `GET /api/v2/matches/{matchId}/details` response includes `dragonsParticipated` integer field
- [ ] Value matches `participant_objectives.dragons_participated` for the correct participant
- [ ] Defaults to 0 when no `participant_objectives` row exists
- [ ] Existing tests still pass
