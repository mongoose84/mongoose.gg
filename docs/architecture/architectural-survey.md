# Architectural Survey Report

**Date**: January 2026  
**Surveyor**: Software Architect  
**Stack**: Vue 3 + Tailwind + Headless UI (Frontend) | .NET 9 Clean Architecture (Backend) | MySQL

---

## Executive Summary

The lol-app (Mongoose) codebase demonstrates **solid architectural foundations** with Clean Architecture on the backend and modern Vue 3 patterns on the frontend. The codebase is ready for showcase with some targeted improvements.

### Codebase Metrics

| Metric | Lines of Code | Test Lines | Test Ratio |
|--------|---------------|------------|------------|
| **Server** | 15,403 | 2,960 | 19.2% |
| **Client** | 11,497 | 2,340 | 20.3% |
| **Total** | 26,900 | 5,300 | 19.7% |

---

## 1. Reusability Assessment

### Grade: B+ (Good)

#### Frontend Strengths ✅
- **Base Component Library**: Well-designed primitives (`BaseButton`, `BaseCard`, `BaseModal`, `BaseInput`, `BaseQueueToggle`) with:
  - Consistent prop interfaces
  - Multiple variants and sizes
  - Composable slots for flexibility
  - Proper TypeScript-like prop validation
- **Composables Pattern**: Clean reactive logic extraction (`useSyncWebSocket`, `useWinRateColor`)
- **Utility Functions**: Centralized formatters (`formatKda`, `formatDuration`, `formatRelativeTime`, `formatRole`)
- **Feature Components**: Good domain organization (`components/matches/`, `components/overview/`)

#### Backend Strengths ✅
- **Repository Base Class**: `RepositoryBase` provides excellent method extraction:
  - `ExecuteScalarAsync`, `ExecuteSingleAsync`, `ExecuteListAsync`
  - `ExecuteTransactionAsync`, `ExecuteWithConnectionAsync`
  - Reduces boilerplate across all repositories
- **DTOs as Records**: Immutable, self-documenting response types
- **Endpoint Interface Pattern**: `IEndpoint` enables modular registration

#### Opportunities for Improvement 🔧
1. **Extract common SQL patterns**: Queue filtering and time range filtering are duplicated across repositories
2. **Create shared query builder**: Abstract filter construction into reusable methods
3. **Frontend composables**: Could extract more shared logic (e.g., `useQueueFilter`, `useTimeRangeFilter`)

---

## 2. Testability Assessment

### Grade: B (Good)

#### Strengths ✅
- **Test Factory Pattern**: `TestWebApplicationFactory` enables isolated integration tests
- **Composable Tests**: `useSyncWebSocket.spec.js` shows excellent mock isolation patterns
- **xUnit + FluentAssertions**: Clean, readable server tests

#### Current Coverage
- **Server**: 15 test files covering endpoints, services, and infrastructure
- **Client**: 17 test files covering components, composables, and views

#### Opportunities for Improvement 🔧
1. **Repository Testing Gap**: Large repositories like `SoloStatsRepository` (1,224 lines) lack dedicated unit tests
2. **Extract Complex Logic**: The LP calculation and trend analysis logic should be unit-testable outside SQL queries
3. **Integration Test Coverage**: Expand endpoint tests beyond authentication checks
4. **E2E Tests**: Consider expanding Playwright coverage for critical user flows

---

## 3. Lines of Code Analysis (Bloat Assessment)

### Grade: B (Good)

#### Files Requiring Attention 🔍

| File | Lines | Issue |
|------|-------|-------|
| `SoloStatsRepository.cs` | 1,224 | Too many responsibilities |
| `authApi.js` | 558 | Single API service doing too much |
| `useSyncWebSocket.js` | 331 | Acceptable but consider splitting |
| `OverviewEndpoint.cs` | 338 | Contains LP calculation logic that could be extracted |

#### Recommended Refactoring

**1. Split `SoloStatsRepository` (Estimated savings: 400+ lines)**

```
Current: SoloStatsRepository (1,224 lines)
   ├── GetSoloDashboardAsync
   ├── GetWinrateTrendAsync
   ├── GetLpTrendAsync
   ├── GetDailyMatchCountsAsync
   ├── GetChampionMatchupsAsync
   └── 20+ private helper methods

Proposed:
   ├── SoloDashboardRepository (~400 lines) - Dashboard aggregations
   ├── TrendRepository (~300 lines) - Winrate/LP trends
   ├── MatchActivityRepository (~200 lines) - Heatmap data
   └── QueryFilterBuilder (shared) - Queue/time filtering
```

**2. Split `authApi.js` (Estimated savings: 200+ lines)**
```
Current: authApi.js (558 lines) - Auth + User + Riot Account + Dashboard APIs

Proposed:
   ├── authApi.js (~150 lines) - Auth only (login, register, verify)
   ├── userApi.js (~100 lines) - User profile
   ├── riotAccountApi.js (~100 lines) - Account linking/sync
   └── dashboardApi.js (~200 lines) - Overview/Solo/Matches
```

**3. Extract LP Calculation Service**
The `CalculateAbsoluteLp`, `GetTierValue`, `GetDivisionValue` methods appear in both `OverviewEndpoint.cs` and `SoloStatsRepository.cs`. Extract to a shared `LpCalculationService`.

---

## 4. Clean Code Principles Assessment

### Grade: B+ (Good)

#### SOLID Compliance

| Principle | Grade | Assessment |
|-----------|-------|------------|
| **S**ingle Responsibility | B | Endpoints good, some repositories overloaded |
| **O**pen/Closed | A | Extension via IEndpoint pattern |
| **L**iskov Substitution | A | Interface-based repositories |
| **I**nterface Segregation | B+ | Good interfaces, could be more granular |
| **D**ependency Inversion | A | Full DI throughout |

#### DRY Violations Found 🔍

1. **Queue Filter Building** - Repeated in `SoloStatsRepository`, `OverviewStatsRepository`, `MatchesRepository`
   ```csharp
   // This pattern appears 5+ times:
   var queueFilter = queueType?.ToLowerInvariant() switch
   {
       "ranked_solo" => "AND m.queue_id = 420",
       "ranked_flex" => "AND m.queue_id = 440",
       // ...
   };
   ```

2. **Time Range Resolution** - Similar DateTime calculations in multiple repositories

3. **Parameter Binding** - Repeated pattern:
   ```csharp
   if (timeRangeStart.HasValue)
       cmd.Parameters.AddWithValue("@startTime", ...);
   if (!string.IsNullOrEmpty(seasonCode))
       cmd.Parameters.AddWithValue("@season", seasonCode);
   ```

4. **Data Dragon URL Building** - Duplicated in `OverviewEndpoint.cs` and client components

#### Recommended Refactoring

Create `QueryFilterExtensions.cs`:
```csharp
public static class QueryFilterExtensions
{
    public static string BuildQueueFilter(string? queueType);
    public static (DateTime? Start, string? Season, string TimeRange) ResolveTimeRange(string? timeRange);
    public static void AddTimeRangeParameters(this MySqlCommand cmd, DateTime? start, string? season);
}
```

---

## 5. Separation of Concerns Assessment

### Grade: A- (Very Good)

#### Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                        CLIENT (Vue 3)                        │
├─────────────────────────────────────────────────────────────┤
│  Views          │  Components      │  Composables           │
│  (Page-level)   │  (UI elements)   │  (Shared logic)        │
├─────────────────┴─────────────────┴────────────────────────┤
│  Stores (Pinia)         │  Services (API calls)            │
└─────────────────────────┴──────────────────────────────────┘
                              │ HTTP
┌─────────────────────────────▼──────────────────────────────┐
│                     SERVER (.NET 9)                         │
├─────────────────────────────────────────────────────────────┤
│  APPLICATION LAYER                                          │
│  ├── Endpoints (HTTP handlers)                              │
│  ├── DTOs (Request/Response shapes)                         │
│  └── Services (Business logic)                              │
├─────────────────────────────────────────────────────────────┤
│  CORE LAYER                                                 │
│  ├── Entities (Domain models)                               │
│  ├── Interfaces (Repository contracts)                      │
│  └── ValueObjects / QueryModels                             │
├─────────────────────────────────────────────────────────────┤
│  INFRASTRUCTURE LAYER                                       │
│  ├── Database (Repositories, MySqlConnector)                │
│  ├── Riot (API client, mappers)                             │
│  ├── Email (SMTP service)                                   │
│  └── Jobs (Background workers)                              │
└─────────────────────────────────────────────────────────────┘
```

#### Strengths ✅
- **Clean Architecture Boundaries**: Core has no infrastructure dependencies
- **Interface-based DI**: All infrastructure behind interfaces
- **Endpoint Modularity**: Each endpoint is self-contained
- **Layer Independence**: Infrastructure changes don't affect Core

#### Minor Concerns 🔍
1. **Endpoint Logic Creep**: `OverviewEndpoint.cs` contains LP calculation logic (Lines 138-310) that belongs in a service
2. **Repository SQL Complexity**: Some repositories mix data access with business logic (e.g., trend calculations)
3. **Client API Service**: `authApi.js` mixes authentication, user management, and dashboard concerns

---

## Recommendations Summary

### High Priority (Do First)
1. **Extract Query Filter Builder** - Eliminate 300+ lines of duplication
2. **Split `SoloStatsRepository`** - Improve maintainability and testability
3. **Extract LP Calculation Service** - Remove duplication between endpoint and repository

### Medium Priority
4. **Split `authApi.js`** - Better frontend separation of concerns
5. **Increase Repository Test Coverage** - Critical business logic needs direct testing
6. **Extract Endpoint Business Logic** - Move calculations to dedicated services

### Low Priority (Nice to Have)
7. **Create more frontend composables** - `useQueueFilter`, `useTimeRangeFilter`
8. **Expand E2E test coverage**
9. **Document Data Dragon version management**

---

## Conclusion

The Mongoose codebase demonstrates **modern, professional architecture** that is ready for showcase. The Clean Architecture implementation on the backend and Vue 3 Composition API patterns on the frontend follow industry best practices.

**Key Strengths:**
- Clean layer separation with proper dependency direction
- Modern tech stack (Vue 3, .NET 9, Tailwind CSS)
- Good base component library for UI consistency
- Solid repository pattern with useful base class abstractions

**Primary Focus Areas:**
- Repository refactoring to reduce size and improve testability
- Extract repeated query building patterns
- Move business logic from endpoints to services

With the recommended refactoring, the codebase would achieve **A-grade architecture** suitable for any technical audit.

