# API v2 Design - Pulse (pulse.gg)

**Status:** Design Phase (F1)  
**Priority:** P0 - Critical  
**Date:** January 9, 2026

---

## 1. Overview

This document defines the v2 API surface for Pulse, introducing:
- **Versioned routes** (`/api/v2/...`)
- **Optimized response shapes** for frontend dashboards
- **Consistent queue filtering** across all endpoints
- **Modular dashboard summaries** (solo, duo, team)
- **AI/goal recommendations** integration points

**Design Principle:** Response shapes should require **minimal client-side aggregation**. The backend returns dashboard-ready data.

---

## 2. Route Scheme & Structure

### Base Path
```
/api/v2/
```

### Route Organization by Feature

#### A. Solo Dashboard
```
GET  /api/v2/solo/summary/{userId}
GET  /api/v2/solo/performance/{userId}
GET  /api/v2/solo/champions/{userId}
GET  /api/v2/solo/matchups/{userId}
```

#### B. Duo Dashboard
```
GET  /api/v2/duo/summary/{userId1}/{userId2}
GET  /api/v2/duo/performance/{userId1}/{userId2}
GET  /api/v2/duo/synergy/{userId1}/{userId2}
GET  /api/v2/duo/kills/{userId1}/{userId2}
GET  /api/v2/duo/deaths/{userId1}/{userId2}
GET  /api/v2/duo/vs-enemy/{userId1}/{userId2}
```

#### C. Team Dashboard
```
GET  /api/v2/team/summary
GET  /api/v2/team/performance
GET  /api/v2/team/composition
GET  /api/v2/team/objectives
GET  /api/v2/team/synergy
```

#### D. AI & Goals (Future)
```
GET  /api/v2/goals/recommendations/{userId}
POST /api/v2/goals/{userId}
GET  /api/v2/goals/{userId}/progress
```

#### E. User
```
POST /api/v2/login                     # Authenticate and obtain session cookie
POST /api/v2/logout                    # Clear session
POST /api/v2/users                     # Create/register a user for dashboards
GET  /api/v2/users/{userId}            # Fetch user details for solo/duo/team views
GET  /api/v2/users/by-puuid/{puuid}    # Optional helper when PUUID is already known
```

#### F. WebSocket - Real-time Sync Progress
```
WS   /ws/sync                          # WebSocket endpoint for real-time match sync progress
```

---

## 3. Query Parameters - Queue Filtering

**Standard queue filter** (optional, defaults to `all`):

```
queueType=ranked_solo
queueType=ranked_flex
queueType=normal
queueType=aram
queueType=all  (default)
```

**Usage:**
```
GET /api/v2/solo/summary/{userId}?queueType=ranked_solo
GET /api/v2/duo/summary/{user1}/{user2}?queueType=ranked_flex
GET /api/v2/team/summary?queueType=aram
```

**Implementation:**
- Query param validation in endpoint handlers
- Passed to repository layer for SQL filtering
- Defaults to `all` if omitted

---

## 4. Response DTOs by Endpoint

### A. Solo Dashboard Endpoints

#### 4.A.1 `GET /api/v2/solo/summary/{userId}`
**Purpose:** Single-screen solo player overview.

```csharp
public record SoloSummaryResponse(
    int GamesPlayed,
    int Wins,
    double WinRate,                    // e.g., 52.5
    int KDA,                            // Avg kills + assists / deaths
    double AvgGameDurationMinutes,
    
    // Side statistics (new: win distribution)
    SideWinDistribution SideStats,      // Breaks down wins by side
    
    // Champion pool summary
    int UniqueChampsPlayedCount,
    ChampionSummary MainChampion,       // Most-played or highest WR
    
    // Recent trend
    TrendMetric Last10Games,
    TrendMetric Last20Games,
    
    // Queue type
    string QueueType
);

public record SideWinDistribution(
    int BlueWins,
    int RedWins,
    int BlueGames,
    int RedGames,
    int TotalGames,
    double BlueWinDistribution,         // % of total wins from blue
    double RedWinDistribution           // % of total wins from red
);

public record ChampionSummary(
    int ChampionId,
    string ChampionName,
    int Picks,
    double WinRate,
    double PickRate                     // % of total picks
);

public record TrendMetric(
    int Games,
    int Wins,
    double WinRate,
    double AvgKDA
);
```

#### 4.A.2 `GET /api/v2/solo/performance/{userId}`
**Purpose:** Time-based performance breakdown (early/mid/late game).

```csharp
public record SoloPerformanceResponse(
    PerformancePhase EarlyGame,         // 0-15 minutes
    PerformancePhase MidGame,           // 15-25 minutes
    PerformancePhase LateGame,          // 25+ minutes
    
    // Position-specific stats
    PositionStats[] PositionBreakdown   // By lane (TOP, JGL, MID, ADC, SUP)
);

public record PerformancePhase(
    int Games,
    int Wins,
    double WinRate,
    double AvgKDA,
    double AvgGoldPerMin,
    double AvgDamagePerMin,
    double DeathRate                    // Deaths per game
);

public record PositionStats(
    string Position,                    // TOP, JGL, MID, ADC, SUP
    int GamesPlayed,
    int Wins,
    double WinRate,
    double AvgKDA
);
```

#### 4.A.3 `GET /api/v2/solo/champions/{userId}`
**Purpose:** Detailed champion pool analysis.

```csharp
public record SoloChampionResponse(
    ChampionDetail[] Champions,
    int UniqueChampsPlayed,
    int MainChampionPoolSize             // Top 3 champs = main pool
);

public record ChampionDetail(
    int ChampionId,
    string ChampionName,
    int GamesPlayed,
    int Wins,
    double WinRate,
    double PickRate,                    // % of all games
    double AvgKDA,
    double AvgGameDurationMinutes,
    string[] CounterChampions,          // Worst matchups
    string[] FavorableChampions         // Best matchups
);
```

#### 4.A.4 `GET /api/v2/solo/matchups/{userId}`
**Purpose:** Champion matchup analysis (vs specific enemies).

```csharp
public record SoloMatchupsResponse(
    MatchupDetail[] TopMatchups,        // 20 most-faced enemies
    OverallMatchupStats Overall
);

public record MatchupDetail(
    string MyChampion,
    string EnemyChampion,
    int Encounters,
    int Wins,
    double WinRate,
    double AvgKDA
);

public record OverallMatchupStats(
    double FavorableFavorableCount,     // Matchups with WR > 60%
    double UnfavorableCount,            // Matchups with WR < 40%
    double BalancedCount                // Matchups 40-60% WR
);
```

---

### B. Duo Dashboard Endpoints

#### 4.B.1 `GET /api/v2/duo/summary/{userId1}/{userId2}`
**Purpose:** Duo partner overview.

```csharp
public record DuoSummaryResponse(
    string Player1Puuid,
    string Player2Puuid,
    string Player1Name,
    string Player2Name,
    
    int GamesPlayed,
    int Wins,
    double WinRate,
    
    // Lane-specific performance
    DuoLanePerformance BotLane,         // If they play bot lane together
    
    // Role-specific stats
    DuoRoleStats[] RoleBreakdown,       // ADC+SUP, JGL+MID, etc.
    
    // Recent trend
    TrendMetric Last10Together,
    
    string QueueType
);

public record DuoLanePerformance(
    int Games,
    int Wins,
    double WinRate,
    double AvgCombinedGoldAt10,         // Gold diff vs enemy duo at 10 min
    double AvgCombinedGoldAt15,
    double ComebackWinRate,             // % of wins from behind at 15 min
    double EarlyLeadConversionRate      // % of wins when ahead at 10 min
);

public record DuoRoleStats(
    string Role1,                       // ADC, JGL, MID, etc.
    string Role2,
    int GamesPlayed,
    int Wins,
    double WinRate
);
```

#### 4.B.2 `GET /api/v2/duo/performance/{userId1}/{userId2}`
**Purpose:** Time-phased duo performance.

```csharp
public record DuoPerformanceResponse(
    DuoPhasePerformance EarlyGame,      // 0-15 min
    DuoPhasePerformance MidGame,        // 15-25 min
    DuoPhasePerformance LateGame        // 25+ min
);

public record DuoPhasePerformance(
    int Games,
    int Wins,
    double WinRate,
    double AvgCombinedKDA,
    double AvgCombinedGold,
    double AvgCombinedDamage,
    KillDeltaStats KillDelta,           // Kills vs enemy duo
    GoldDeltaStats GoldDelta            // Gold vs enemy duo
);

public record KillDeltaStats(
    int AvgDifference,
    double WinRateWhenAhead,
    double WinRateWhenBehind
);

public record GoldDeltaStats(
    int AvgDifferenceAt10,
    int AvgDifferenceAt15,
    double WinRateWhenAhead,
    double WinRateWhenBehind
);
```

#### 4.B.3 `GET /api/v2/duo/synergy/{userId1}/{userId2}`
**Purpose:** Champion synergy & combo analysis.

```csharp
public record DuoSynergyResponse(
    DuoCombo[] TopCombos,               // Best duo champion pairs
    int UniqueDuoCombinations,
    SynergyScore OverallSynergy
);

public record DuoCombo(
    string Champ1,
    string Champ2,
    int Picks,
    int Wins,
    double WinRate,
    double SynergyScore,                // 0-100, how well they work together
    string StrengthsDescription         // e.g., "Early game synergy"
);

public record SynergyScore(
    double OffensiveScore,              // How much they amplify each other's damage
    double DefensiveScore,              // How well they protect each other
    double UtilityScore                 // How well their utility spells combo
);
```

#### 4.B.4 `GET /api/v2/duo/kills/{userId1}/{userId2}`
**Purpose:** Kill analysis (multi-kills, timing, participation).

```csharp
public record DuoKillsResponse(
    KillPhaseAnalysis EarlyKills,       // 0-15 min
    KillPhaseAnalysis MidKills,         // 15-25 min
    KillPhaseAnalysis LateKills,        // 25+ min
    
    double DoubleKillRate,              // % of games with >=2 kills together
    double TripleKillRate,              // % of games with >=3 kills together
    int AvgCombinedKillsPerGame,
    int AvgAssistsToEachOther
);

public record KillPhaseAnalysis(
    int Games,
    int TotalKillsDealt,
    int AssistsToTeam,
    double KillParticipationRate,
    int AvgFirstKillMinute
);
```

#### 4.B.5 `GET /api/v2/duo/deaths/{userId1}/{userId2}`
**Purpose:** Death analysis (when, where, patterns).

```csharp
public record DuoDeathsResponse(
    DeathPhaseAnalysis EarlyDeaths,     // 0-15 min
    DeathPhaseAnalysis MidDeaths,       // 15-25 min
    DeathPhaseAnalysis LateDeaths,      // 25+ min
    
    int TotalDeathsTogether,
    int AvgDeathsPerGame,
    double DeathRateImpact,             // How much deaths correlate with losses
    DeathByPosition[] DeathsByPosition  // Deaths at different map locations
);

public record DeathPhaseAnalysis(
    int Games,
    int TotalDeaths,
    double DeathRate,
    bool WinRateAfterDeaths,
    int AvgTimeToNextObjective
);

public record DeathByPosition(
    string Location,                    // BARON_PIT, DRAGON_PIT, LANE, etc.
    int Deaths,
    double ImpactOnWinRate
);
```

#### 4.B.6 `GET /api/v2/duo/vs-enemy/{userId1}/{userId2}`
**Purpose:** Duo vs enemy duo matchups.

```csharp
public record DuoVsEnemyResponse(
    EnemyDuoMatchup[] TopEnemyDuos,     // Most-faced enemy duos
    double FavorableMatchupRate,        // % with WR > 55%
    double UnfavorableMatchupRate       // % with WR < 45%
);

public record EnemyDuoMatchup(
    string EnemyPlayer1Name,
    string EnemyPlayer2Name,
    int Encounters,
    int WinsAgainst,
    double WinRate,
    string[] FrequentEnemyChamps        // What they typically pick
);
```

---

### C. Team Dashboard Endpoints

#### 4.C.1 `GET /api/v2/team/summary`
**Purpose:** 5-player team overview.

```csharp
public record TeamSummaryResponse(
    string[] PlayerNames,               // 5 players
    string[] Puuids,
    
    int GamesPlayed,
    int Wins,
    double WinRate,
    
    // Team cohesion
    double AvgTeamKDA,
    TeamCohesionScore Cohesion,
    
    // Side stats
    SideWinDistribution SideStats,
    
    // Member breakdown
    TeamMemberStat[] Members,           // Per-player KDA, role performance
    
    string QueueType
);

public record TeamCohesionScore(
    double Consistency,                 // How stable the winrate (low variance = high)
    double RoleFlexibility,             // Can members play multiple roles
    double ChampionPoolSize             // Total unique champs by team
);

public record TeamMemberStat(
    string PlayerName,
    string Role,
    int GamesPlayed,
    double AvgKDA,
    double WinRate,
    int MainChampionCount               // # of champs played
);
```

#### 4.C.2 `GET /api/v2/team/performance`
**Purpose:** Time-phased team performance.

```csharp
public record TeamPerformanceResponse(
    TeamPhasePerformance EarlyGame,
    TeamPhasePerformance MidGame,
    TeamPhasePerformance LateGame,
    
    double ObjectivePrioritization,     // How early they secure objectives
    double GroupingEfficiency           // % of fights with full team
);

public record TeamPhasePerformance(
    int Games,
    int Wins,
    double WinRate,
    double AvgTeamKDA,
    double AvgTeamGold,
    int AvgTeamKills,
    double AvgGoldPerTeamMember
);
```

#### 4.C.3 `GET /api/v2/team/composition`
**Purpose:** Team composition & role flexibility.

```csharp
public record TeamCompositionResponse(
    CompositionPattern[] TopCompositions,// Champ combinations
    RoleDistribution RoleMetrics,
    FlexibilityScore Flexibility
);

public record CompositionPattern(
    string[] ChampionNames,             // 5 champs
    int Picks,
    int Wins,
    double WinRate,
    string CompositionType              // SCALING, EARLY_AGGRESSION, POKE, etc.
);

public record RoleDistribution(
    string[] Roles,                     // [TOP, JGL, MID, ADC, SUP]
    double[] PlayRates,                 // How often each role is filled
    double[] WinRates
);

public record FlexibilityScore(
    double AverageRolesPerPlayer,       // Can they flex?
    double ChampionPoolPerPlayer,
    double SubstitutionReliability      // If someone can't play, how flexible is replacement?
);
```

#### 4.C.4 `GET /api/v2/team/objectives`
**Purpose:** Objective control (dragons, barons, towers).

```csharp
public record TeamObjectivesResponse(
    ObjectiveStats Dragons,
    ObjectiveStats Barons,
    ObjectiveStats Towers,
    ObjectiveStats Herald,
    
    double ObjectiveControlRate,        // % of objectives secured
    double ObjectivePacingScore,        // How quickly they take objectives
    ObjectiveByPhase[] TakesByPhase
);

public record ObjectiveStats(
    int Secured,
    int Contested,
    int Lost,
    double SecureRate,                  // % won when contested
    double AvgTimeMinute                // Avg game minute secured
);

public record ObjectiveByPhase(
    string Phase,                       // EARLY, MID, LATE
    int ObjectivesTaken,
    double WinRateWhenSecured
);
```

#### 4.C.5 `GET /api/v2/team/synergy`
**Purpose:** Team composition synergy analysis.

```csharp
public record TeamSynergyResponse(
    double OffensiveSynergy,            // How well ults combo
    double DefensiveSynergy,            // Peel/protect coverage
    double UtilitySynergy,              // CC chains, crowd control
    double InitiationSynergy,           // How well they start fights
    double RoleComplementarity,         // Do roles fill gaps?
    string[] StrengthAreas,             // ["Early game dominance", "Teamfight synergy"]
    string[] WeaknessAreas              // ["Late game scaling", "Pick vulnerability"]
);
```

---

### D. AI & Goals Endpoints (Future - Placeholder)

#### 4.D.1 `GET /api/v2/goals/recommendations/{userId}`
**Purpose:** AI-generated improvement recommendations.

```csharp
public record GoalRecommendationResponse(
    Goal[] RecommendedGoals,
    InsightCategory[] Insights
);

public record Goal(
    string GoalId,
    string Title,
    string Description,
    string Category,                    // MECHANICS, POSITIONING, MACRO, CHAMPION_POOL
    int Priority,                       // 1 (critical) to 5 (nice-to-have)
    double ImpactOnWinrate              // Projected % improvement
);

public record InsightCategory(
    string Category,
    string Description,
    string[] ActionItems
);
```

---

### E. User Endpoints

#### 4.E.1 `POST /api/v2/login`
**Purpose:** Authenticate with username and password, obtain an httpOnly session cookie for subsequent requests.

```csharp
public record LoginRequest(
    string Username,                    // User account username
    string Password                     // User password (validated server-side, hashed in DB)
);

public record LoginResponse(
    int UserId,
    string Username,
    string Message                      // "Login successful"
);
```

**Notes:**
- No auth required for this endpoint
- Response sets an httpOnly, secure, SameSite=Lax cookie automatically
- Requests without valid credentials return `401 Unauthorized`

#### 4.E.2 `POST /api/v2/logout`
**Purpose:** Clear the session cookie and sign out the user.

```csharp
// No request body
// Returns: { message: "Logged out successfully" }
```

#### 4.E.3 `POST /api/v2/users`
**Purpose:** Create/register a user and attach Riot identity so solo/duo/team dashboards can resolve PUUID.

```csharp
public record CreateUserRequest(
    string GameName,                    // Riot game name
    string TagLine,                     // #TAG line
    string Region,                      // e.g., euw1, na1, kr
    string Platform,                    // e.g., americas, europe, asia (for match routing)
    string? SummonerNameOverride        // Optional: override display if different from gameName
);

public record UserResponse(
    string UserId,
    string Puuid,
    string GameName,
    string TagLine,
    string Region,
    string Platform,
    string SummonerName,
    int ProfileIconId,
    DateTime CreatedAt,
    DateTime? LastSyncedAt
);
```

#### 4.E.4 `GET /api/v2/users/{userId}`
**Purpose:** Fetch a user for the solo view (or any dashboard) including PUUID and display info.

```csharp
// Returns UserResponse
```

#### 4.E.5 `GET /api/v2/users/by-puuid/{puuid}`
**Purpose:** Helper when the client already has the PUUID (e.g., deep link) and needs Pulse user metadata.

```csharp
// Returns UserResponse
```

---

### F. WebSocket Endpoints

#### 4.F.1 `WS /ws/sync`
**Purpose:** Real-time WebSocket endpoint for receiving match sync progress updates.

**Authentication:** Session cookie (same as HTTP endpoints). Unauthenticated connections are rejected with close code 4401.

**Client → Server Messages:**
```json
{ "type": "subscribe", "puuid": "abc123..." }
{ "type": "unsubscribe", "puuid": "abc123..." }
```

**Server → Client Messages:**
```csharp
// Progress update during sync
public record SyncProgressMessage(
    string Type = "sync_progress",
    string Puuid,
    string Status,                       // "syncing"
    int Progress,                        // Current match index
    int Total,                           // Total matches to sync
    string? MatchId                      // Current match being processed
);

// Sync completed
public record SyncCompleteMessage(
    string Type = "sync_complete",
    string Puuid,
    string Status = "completed",
    int TotalSynced                      // Total matches synced
);

// Sync error
public record SyncErrorMessage(
    string Type = "sync_error",
    string Puuid,
    string Status = "failed",
    string Error                         // Error message
);
```

**Connection Flow:**
1. Client connects to `/ws/sync` (session cookie included automatically)
2. Server validates session; rejects if unauthorized
3. Client sends `subscribe` message for each PUUID to track
4. Server broadcasts progress updates as matches are synced
5. On completion, server sends `sync_complete` message
6. On error, server sends `sync_error` message
7. Client can send `unsubscribe` to stop receiving updates

**Reconnection:** Clients should implement exponential backoff reconnection (recommended: 1s, 2s, 4s, 8s, up to 30s max).

---

## 5. Implementation Guidelines

### 5.1 Response Shapes
- **No nested array expansion:** Avoid `List<List<>>` responses
- **Dashboard-ready:** Frontend should consume response directly without heavy aggregation
- **Consistent naming:** Use camelCase in JSON (via `[JsonPropertyName]`)
- **Nullable fields:** Optional stats use `?` notation (e.g., `double?`)

### 5.2 Queue Filtering
1. Accept `queueType` query parameter
2. Validate against whitelist: `ranked_solo | ranked_flex | normal | aram | all`
3. Pass to repository layer for SQL filtering
4. Default to `all` if omitted

### 5.3 DTO Modules
Organize DTOs by feature:
```
DTOs/
├── Solo/
│   ├── SoloSummaryDto.cs
│   ├── SoloPerformanceDto.cs
│   ├── SoloChampionDto.cs
│   └── SoloMatchupDto.cs
├── Duo/
│   ├── DuoSummaryDto.cs
│   ├── DuoPerformanceDto.cs
│   ├── DuoSynergyDto.cs
│   ├── DuoKillsDto.cs
│   ├── DuoDeathsDto.cs
│   └── DuoVsEnemyDto.cs
├── User/
│   ├── CreateUserDto.cs
│   └── UserResponseDto.cs
├── Team/
│   ├── TeamSummaryDto.cs
│   ├── TeamPerformanceDto.cs
│   ├── TeamCompositionDto.cs
│   ├── TeamObjectivesDto.cs
│   └── TeamSynergyDto.cs
├── Shared/
│   ├── TrendMetricDto.cs
│   ├── SideWinDistributionDto.cs
│   └── ChampionDetailDto.cs
└── Goals/
    └── GoalRecommendationDto.cs (future)
```

### 5.4 Endpoint Registration
Update `RiotProxyApplication.cs`:
```csharp
var soloV2Endpoint = new SoloSummaryV2Endpoint(_basePath);
_endpoints.Add(soloV2Endpoint);

var duoV2Endpoint = new DuoSummaryV2Endpoint(_basePath);
_endpoints.Add(duoV2Endpoint);

// ... etc for team, goals
```

### 5.5 Authentication
- All v2 endpoints require cookie-based session authentication
- Clients must first authenticate via a login endpoint to obtain an auth cookie (httpOnly, secure, SameSite=Lax)
- Subsequent requests include the cookie automatically; server validates session on each request
- Missing/expired/invalid session → `401 Unauthorized`
- Authorization policies can further restrict access per endpoint as needed

---

## 6. Versioning Strategy

### 6.1 Coexistence (v1 + v2)
- **v1 endpoints:** `/api/v1/...` (existing, maintained for backwards compatibility)
- **v2 endpoints:** `/api/v2/...` (new, dashboard-optimized)
- **Deprecation path:** Announce v1 sunset in 6-12 months
- **Breaking changes:** Only in v3+ (if needed)

### 6.2 API Contract
- Response DTOs are **versioned** (e.g., `SoloSummaryResponseV2`)
- Never mutate existing v1 endpoints
- New features = v2 endpoints

---

## 7. Acceptance Criteria Checklist

- [x] **API v2 route scheme decided** (`/api/v2/solo|duo|team|goals|users`)
- [x] **Request/response models defined** for all dashboard endpoints (see Section 4)
- [x] **Response shapes optimized** (minimal client aggregation, dashboard-ready)
- [x] **Queue filtering standardized** (`?queueType=ranked_solo|ranked_flex|normal|aram|all`)
- [x] **Authentication required** (v2 endpoints assume bearer/cookie auth; unauthorized → 401)
- [ ] DTOs created & implemented (Next: F2)
- [ ] Endpoints implemented & tested (Next: F3)
- [ ] Frontend integration & validation (Next: F4)

---

## 8. Next Steps (F2, F3, F4)

### F2: Implement v2 DTOs
- Create DTO classes per Section 5.3 structure
- Add unit tests for serialization

### F3: Implement v2 Endpoints
- Register endpoints in `RiotProxyApplication.cs`
- Implement endpoint handlers (query params, repository calls)
- Add queue filtering logic

### F4: Frontend Integration
- Update API client (`client/src/api/solo.js`, etc.)
- Consume v2 endpoints
- Remove aggregation logic
- Validate response shapes in dashboard components

---

## 9. Design Rationale

### Why separate v2?
- **Backwards compatibility:** v1 stays unchanged, v1 clients unaffected
- **Clean slate:** v2 can optimize response shapes without retrofit constraints
- **Feature parity:** Both can coexist during transition

### Why optimize response shapes?
- **Performance:** Smaller payloads, fewer client transforms
- **UX:** Dashboards render faster (data already aggregated server-side)
- **Maintainability:** Logic lives in one place (backend), not duplicated across clients

### Why standardize queue filtering?
- **Consistency:** Users filter by queue across all views
- **Analytics:** Server can track query patterns
- **Future dashboards:** New features inherit filtering automatically

---

**Approval:** [Awaiting review]  
**Last Updated:** January 9, 2026
