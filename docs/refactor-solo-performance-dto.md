# Refactor Plan: DTO & Endpoint Cleanup

## Overview

This document consolidates all DTO and endpoint issues identified during the comprehensive codebase review.
Issues are organized by severity and grouped into implementation phases.

---

## Problem Summary

### 🔴 Critical Issues

1. **Clean Architecture Violations** - 3 Core layer interfaces depend on Application layer DTOs
2. **God DTO** - `SoloPerformanceDto.cs` contains types used across multiple unrelated features

### 🟠 Medium Issues

3. **Misplaced Types** - `WinrateTrendPoint` and `LpTrendPoint` in wrong DTO file
4. **Over-Fetching** - `ChampionSelectEndpoint` fetches entire `SoloPerformanceResponse`
5. **Missing TrendDto** - Trend endpoints use inline anonymous response objects

### 🟡 Minor Issues

6. **Namespace Inconsistencies** - Some DTOs use subfolder in namespace, others don't
7. **Folder Organization** - `SoloMatchupsEndpoint.cs` in wrong folder
8. **Inline Anonymous Objects** - 3 endpoints return `new { }` instead of proper DTOs

---

## Current State Analysis

### Clean Architecture Violations

Core layer interfaces importing Application layer DTOs (backwards dependency):

```csharp
// server/Core/Interfaces/ISoloPerformanceRepository.cs
using static Mongoose.Api.Application.DTOs.SoloPerformanceDto;  // ❌

// server/Core/Interfaces/ITrendRepository.cs
using static Mongoose.Api.Application.DTOs.SoloPerformanceDto;  // ❌

// server/Core/Interfaces/IMatchupRepository.cs
using static Mongoose.Api.Application.DTOs.SoloMatchupsDto;     // ❌
```

### SoloPerformanceDto Analysis (148 lines)

## Current State Analysis

### File: `server/Application/DTOs/Solo/SoloPerformanceDto.cs` (148 lines)

Contains 10 record types:

| Type | Solo Page | Champion Select | Trends | Should Move To |
|------|-----------|-----------------|--------|----------------|
| `SoloPerformanceResponse` | ✅ | ❌ (over-fetches) | ❌ | Keep (Solo only) |
| `SideWinDistribution` | ✅ | ❌ | ❌ | Keep (Solo only) |
| `ChampionSummary` | ✅ | ❌ | ❌ | Keep (Solo only) |
| `TrendMetric` | ✅ | ❌ | ❌ | Keep (Solo only) |
| `PerformancePhase` | ✅ | ❌ | ❌ | Keep (Solo only) |
| `RolePerformance` | ✅ | ❌ | ❌ | Keep (Solo only) |
| `DeathEfficiency` | ✅ | ❌ | ❌ | Keep (Solo only) |
| `WinrateTrendPoint` | ✅ (embedded) | ❌ | ✅ | **Move to TrendDto.cs** |
| `LpTrendPoint` | ✅ (embedded) | ❌ | ✅ | **Move to TrendDto.cs** |
| `MainChampionRoleGroup` | ✅ | ✅ | ❌ | Already in MainChampionDto.cs |

### ChampionSelectEndpoint Over-Fetching

```csharp
// server/Application/Endpoints/ChampionSelect/ChampionSelectEndpoint.cs:73
var dashboard = await soloPerformanceRepo.GetSoloPerformanceAsync(primaryPuuid, queueType, timeRange);
return Results.Ok(dashboard);  // Returns ALL solo stats when only MainChampions needed
```

### Namespace Inconsistencies

| File | Current Namespace | Expected |
|------|------------------|----------|
| `FeedbackDto.cs` | `Mongoose.Api.Application.DTOs.Feedback` | `Mongoose.Api.Application.DTOs` |
| `MatchActivityDto.cs` | `Mongoose.Api.Application.DTOs.Solo` | `Mongoose.Api.Application.DTOs` |
| `MatchListDto.cs` | `Mongoose.Api.Application.DTOs.Matches` | `Mongoose.Api.Application.DTOs` |
| `MatchNarrativeDto.cs` | `Mongoose.Api.Application.DTOs.Matches` | `Mongoose.Api.Application.DTOs` |
| `OverviewDto.cs` | `Mongoose.Api.Application.DTOs.Overview` | `Mongoose.Api.Application.DTOs` |

### Inline Anonymous Response Objects

| Endpoint | Line | Current Code |
|----------|------|--------------|
| `LogoutEndpoint.cs` | 36 | `Results.Ok(new { message = "..." })` |
| `WinrateTrendEndpoint.cs` | 77 | `Results.Ok(new { winrateTrend })` |
| `LpTrendEndpoint.cs` | 82 | `Results.Ok(new { lpTrend })` |

### Folder Organization Issue

`SoloMatchupsEndpoint.cs` is in `ChampionSelect/` folder but route is `/solo/matchups/{userId}`

---

## Proposed Solution

### Phase 1: Extract Trend DTOs & Fix Trend Endpoints (Low Risk)

**Goal**: Move trend-related types to their own DTO file and add proper response DTOs.

**Step 1.1**: Create `server/Application/DTOs/Trends/TrendDto.cs`:
```csharp
namespace Mongoose.Api.Application.DTOs;

public static class TrendDto
{
    public record WinrateTrendPoint(
        [property: JsonPropertyName("gameIndex")] int GameIndex,
        [property: JsonPropertyName("winRate")] double WinRate,
        [property: JsonPropertyName("timestamp")] DateTime Timestamp
    );

    public record LpTrendPoint(
        [property: JsonPropertyName("gameIndex")] int GameIndex,
        [property: JsonPropertyName("lpGain")] int? LpGain,
        [property: JsonPropertyName("currentLp")] int CurrentLp,
        [property: JsonPropertyName("rank")] string Rank,
        [property: JsonPropertyName("timestamp")] DateTime Timestamp,
        [property: JsonPropertyName("isPromotion")] bool IsPromotion,
        [property: JsonPropertyName("isDemotion")] bool IsDemotion,
        [property: JsonPropertyName("win")] bool Win
    );

    // Response DTOs for trend endpoints (replaces inline anonymous objects)
    public record WinrateTrendResponse(
        [property: JsonPropertyName("winrateTrend")] WinrateTrendPoint[] WinrateTrend
    );

    public record LpTrendResponse(
        [property: JsonPropertyName("lpTrend")] LpTrendPoint[] LpTrend
    );
}
```

**Step 1.2**: Update imports in:
- `ITrendRepository.cs` → `using static Mongoose.Api.Application.DTOs.TrendDto;`
- `TrendRepository.cs` → same
- `SoloPerformanceRepository.cs` → add import for `LpTrendPoint`
- `SoloPerformanceDto.cs` → add import for `LpTrendPoint`

**Step 1.3**: Remove `WinrateTrendPoint` and `LpTrendPoint` from `SoloPerformanceDto.cs`

**Step 1.4**: Update trend endpoints to use proper response DTOs:
- `WinrateTrendEndpoint.cs`: `Results.Ok(new WinrateTrendResponse(winrateTrend))`
- `LpTrendEndpoint.cs`: `Results.Ok(new LpTrendResponse(lpTrend))`

---

### Phase 2: Create ChampionSelect-Specific Response (Medium Risk)

**Goal**: ChampionSelectEndpoint should return only what it needs.

**Step 2.1**: Create `server/Application/DTOs/ChampionSelect/ChampionSelectDto.cs`:
```csharp
namespace Mongoose.Api.Application.DTOs;

public static class ChampionSelectDto
{
    public record ChampionSelectResponse(
        [property: JsonPropertyName("mainChampions")] MainChampionRoleGroup[] MainChampions,
        [property: JsonPropertyName("gamesPlayed")] int GamesPlayed,
        [property: JsonPropertyName("winRate")] double WinRate
    );
}
```

**Step 2.2**: Create `IChampionSelectRepository` interface with focused query

**Step 2.3**: Create `ChampionSelectRepository` that only fetches champion data

**Step 2.4**: Update `ChampionSelectEndpoint` to use new repository

**Step 2.5**: Update frontend `getChampionSelectData()` to expect smaller response

---

### Phase 3: Fix Clean Architecture Violations (Medium Risk)

**Goal**: Core layer should not depend on Application layer.

**Approach**: Move shared types to `Core/QueryModels/` (simpler for this codebase size)

**Step 3.1**: Create `server/Core/QueryModels/TrendQueryModels.cs`:
```csharp
namespace Mongoose.Api.Core.QueryModels;

public record WinrateTrendData(int GameIndex, double WinRate, DateTime Timestamp);
public record LpTrendData(int GameIndex, int? LpGain, int CurrentLp, string Rank,
    DateTime Timestamp, bool IsPromotion, bool IsDemotion, bool Win);
```

**Step 3.2**: Create `server/Core/QueryModels/SoloQueryModels.cs`:
```csharp
namespace Mongoose.Api.Core.QueryModels;

public record SoloPerformanceData(...);  // Core layer version
```

**Step 3.3**: Create `server/Core/QueryModels/MatchupQueryModels.cs`:
```csharp
namespace Mongoose.Api.Core.QueryModels;

public record ChampionMatchupsData(...);  // Core layer version
```

**Step 3.4**: Update repository interfaces to use Core QueryModels

**Step 3.5**: Map Core QueryModels to Application DTOs in endpoints

---

### Phase 4: Namespace & Organization Cleanup (Low Risk)

**Goal**: Consistent namespace conventions and folder organization.

**Step 4.1**: Standardize namespaces to `Mongoose.Api.Application.DTOs`:
- `FeedbackDto.cs`: Change `DTOs.Feedback` → `DTOs`
- `MatchActivityDto.cs`: Change `DTOs.Solo` → `DTOs`
- `MatchListDto.cs`: Change `DTOs.Matches` → `DTOs`
- `MatchNarrativeDto.cs`: Change `DTOs.Matches` → `DTOs`
- `OverviewDto.cs`: Change `DTOs.Overview` → `DTOs`

**Step 4.2**: Move `SoloMatchupsEndpoint.cs` from `ChampionSelect/` to `Solo/` folder

**Step 4.3**: Add proper DTO for logout response:
```csharp
// In Auth/LogoutDto.cs or existing Auth DTO file
public record LogoutResponse([property: JsonPropertyName("message")] string Message);
```

---

## Implementation Order

```
Phase 1: Extract Trend DTOs & Fix Trend Endpoints (30 min)
├── Create server/Application/DTOs/Trends/TrendDto.cs
├── Update ITrendRepository imports
├── Update TrendRepository imports
├── Update SoloPerformanceRepository imports
├── Update SoloPerformanceDto imports
├── Remove WinrateTrendPoint/LpTrendPoint from SoloPerformanceDto
├── Update WinrateTrendEndpoint to use WinrateTrendResponse
├── Update LpTrendEndpoint to use LpTrendResponse
├── Run tests
└── Commit

Phase 2: ChampionSelect Separation (1-2 hrs)
├── Create server/Application/DTOs/ChampionSelect/ChampionSelectDto.cs
├── Create server/Core/Interfaces/IChampionSelectRepository.cs
├── Create server/Infrastructure/Database/Repositories/ChampionSelectRepository.cs
├── Update ChampionSelectEndpoint to use new repository
├── Register new repository in Program.cs
├── Update frontend getChampionSelectData()
├── Update frontend ChampionSelectPage.vue
├── Run tests
└── Commit

Phase 3: Clean Architecture Fix (1 hr)
├── Create server/Core/QueryModels/TrendQueryModels.cs
├── Create server/Core/QueryModels/SoloQueryModels.cs
├── Create server/Core/QueryModels/MatchupQueryModels.cs
├── Update ISoloPerformanceRepository to use Core types
├── Update ITrendRepository to use Core types
├── Update IMatchupRepository to use Core types
├── Update repository implementations
├── Map Core types to DTOs in endpoints
├── Run tests
└── Commit

Phase 4: Namespace & Organization Cleanup (30 min)
├── Standardize DTO namespaces to Mongoose.Api.Application.DTOs
├── Move SoloMatchupsEndpoint.cs to Solo/ folder
├── Add LogoutResponse DTO
├── Update LogoutEndpoint to use LogoutResponse
├── Run tests
└── Commit
```

---

## Risk Assessment

| Phase | Risk | Effort | Mitigation |
|-------|------|--------|------------|
| Phase 1 | Low | 30 min | Pure refactor, no behavior change |
| Phase 2 | Medium | 1-2 hrs | Frontend API contract changes - coordinate with frontend |
| Phase 3 | Medium | 1 hr | Many file changes, but mechanical |
| Phase 4 | Low | 30 min | Namespace changes only, no behavior change |

---

## Estimated Effort

| Phase | Time |
|-------|------|
| Phase 1: Extract Trend DTOs | 30 minutes |
| Phase 2: ChampionSelect Separation | 1-2 hours |
| Phase 3: Clean Architecture Fix | 1 hour |
| Phase 4: Namespace Cleanup | 30 minutes |
| **Total** | **3-4 hours** |

---

## Success Criteria

### Phase 1
- [ ] `TrendDto.cs` exists with `WinrateTrendPoint`, `LpTrendPoint`, `WinrateTrendResponse`, `LpTrendResponse`
- [ ] `SoloPerformanceDto.cs` no longer contains trend types
- [ ] Trend endpoints return proper response DTOs (not anonymous objects)
- [ ] All backend tests pass

### Phase 2
- [ ] `ChampionSelectDto.cs` exists with focused response type
- [ ] `ChampionSelectEndpoint` returns only needed data
- [ ] Frontend handles new response shape
- [ ] All frontend and backend tests pass

### Phase 3
- [ ] Core layer has no `using` statements referencing Application layer
- [ ] All repository interfaces use Core QueryModels
- [ ] DTOs are mapped in endpoints only
- [ ] All tests pass

### Phase 4
- [ ] All DTOs use consistent namespace `Mongoose.Api.Application.DTOs`
- [ ] `SoloMatchupsEndpoint.cs` is in `Solo/` folder
- [ ] No inline anonymous response objects remain
- [ ] All tests pass

---

## Files to Create

### Phase 1
- `server/Application/DTOs/Trends/TrendDto.cs`

### Phase 2
- `server/Application/DTOs/ChampionSelect/ChampionSelectDto.cs`
- `server/Core/Interfaces/IChampionSelectRepository.cs`
- `server/Infrastructure/Database/Repositories/ChampionSelectRepository.cs`

### Phase 3
- `server/Core/QueryModels/TrendQueryModels.cs`
- `server/Core/QueryModels/SoloQueryModels.cs`
- `server/Core/QueryModels/MatchupQueryModels.cs`

### Phase 4
- `server/Application/DTOs/Auth/LogoutDto.cs` (or add to existing Auth DTO)

---

## Files to Modify

### Phase 1
- `server/Application/DTOs/Solo/SoloPerformanceDto.cs` - Remove trend types, add import
- `server/Core/Interfaces/ITrendRepository.cs` - Update import
- `server/Infrastructure/Database/Repositories/TrendRepository.cs` - Update import
- `server/Infrastructure/Database/Repositories/SoloPerformanceRepository.cs` - Update import
- `server/Application/Endpoints/Trends/WinrateTrendEndpoint.cs` - Use response DTO
- `server/Application/Endpoints/Trends/LpTrendEndpoint.cs` - Use response DTO

### Phase 2
- `server/Application/Endpoints/ChampionSelect/ChampionSelectEndpoint.cs` - Use new repository
- `server/Program.cs` - Register new repository
- `client/src/services/authApi.js` - Update response handling
- `client/src/views/ChampionSelectPage.vue` - Update data access

### Phase 3
- `server/Core/Interfaces/ISoloPerformanceRepository.cs` - Use Core types
- `server/Core/Interfaces/ITrendRepository.cs` - Use Core types
- `server/Core/Interfaces/IMatchupRepository.cs` - Use Core types
- `server/Infrastructure/Database/Repositories/SoloPerformanceRepository.cs` - Return Core types
- `server/Infrastructure/Database/Repositories/TrendRepository.cs` - Return Core types
- `server/Infrastructure/Database/Repositories/MatchupRepository.cs` - Return Core types
- `server/Application/Endpoints/Solo/SoloPerformanceEndpoint.cs` - Map to DTOs
- `server/Application/Endpoints/ChampionSelect/SoloMatchupsEndpoint.cs` - Map to DTOs

### Phase 4
- `server/Application/DTOs/Feedback/FeedbackDto.cs` - Fix namespace
- `server/Application/DTOs/Solo/MatchActivityDto.cs` - Fix namespace
- `server/Application/DTOs/Matches/MatchListDto.cs` - Fix namespace
- `server/Application/DTOs/Matches/MatchNarrativeDto.cs` - Fix namespace
- `server/Application/DTOs/Overview/OverviewDto.cs` - Fix namespace
- `server/Application/Endpoints/Auth/LogoutEndpoint.cs` - Use response DTO
- Move `server/Application/Endpoints/ChampionSelect/SoloMatchupsEndpoint.cs` → `server/Application/Endpoints/Solo/`

---

## ✅ DTOs in Good State (No Changes Needed)

These DTOs were reviewed and found to be well-organized:

| DTO File | Lines | Status |
|----------|-------|--------|
| `Analytics/AnalyticsDto.cs` | 40 | ✅ Clean, focused |
| `Auth/LoginDto.cs` | 22 | ✅ Clean, focused |
| `Auth/RegisterDto.cs` | 22 | ✅ Clean, focused |
| `Auth/DeleteAccountDto.cs` | 23 | ✅ Clean, focused |
| `Solo/MainChampionDto.cs` | 27 | ✅ Clean, properly separated |

---

## ✅ Endpoints in Good State (No Changes Needed)

These endpoints were reviewed and found to be well-organized:

| Endpoint | Status |
|----------|--------|
| `AnalyticsEndpoint.cs` | ✅ Uses proper DTOs |
| `LoginEndpoint.cs` | ✅ Uses proper DTOs |
| `MatchListEndpoint.cs` | ✅ Uses proper DTOs + Core QueryModels |
| `MatchDetailsEndpoint.cs` | ✅ Uses proper DTOs |
| `MatchNarrativeEndpoint.cs` | ✅ Uses proper DTOs |
| `OverviewEndpoint.cs` | ✅ Uses proper DTOs |
| `SoloPerformanceEndpoint.cs` | ✅ Uses proper DTOs |
| `MatchActivityEndpoint.cs` | ✅ Uses proper DTOs |
| `FeedbackEndpoint.cs` | ✅ Uses proper DTOs |

