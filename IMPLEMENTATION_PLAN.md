# Optional Enhancements Implementation Plan

**Date Created:** February 27, 2026  
**Status:** In Progress  
**Estimated Effort:** 40-50 hours total

---

## Phase 1: Endpoint Refactoring (16 remaining endpoints)

### Status: IN PROGRESS
- **Completed:** 4 endpoints (SoloPerformanceEndpoint, WinrateTrendEndpoint, OverviewEndpoint, MatchListEndpoint)
- **Remaining:** 16 endpoints
- **Estimated Effort:** 12-16 hours

### Endpoints to Refactor by Category

#### A. Solo Dashboard Endpoints (5 total)
- [ ] SoloMatchupsEndpoint
- [ ] RadarChartEndpoint
- [ ] MatchActivityEndpoint
- [ ] DeathPositionsEndpoint
- [ ] (Already done: SoloPerformanceEndpoint)

#### B. Trend Endpoints (6 total)
- [ ] VisionScoreTrendEndpoint
- [ ] GoldAt15TrendEndpoint
- [ ] DragonParticipationTrendEndpoint
- [ ] CsPerMinuteTrendEndpoint
- [ ] DeathsTrendEndpoint
- [ ] (Already done: WinrateTrendEndpoint)

#### C. Match Endpoints (2 total)
- [ ] MatchDetailsEndpoint
- [ ] MatchNarrativeEndpoint

#### D. Other Authenticated Endpoints (3 total)
- [ ] ChampionSelectEndpoint
- [ ] RiotAccountsEndpoint (auth required)
- [ ] UsersMeEndpoint
- [ ] DeleteAccountEndpoint

**Notes:**
- AnalyticsEndpoint (public with optional auth) - skip for now
- Auth endpoints (public) - skip
- PublicStatsEndpoint (public) - skip

---

## Phase 2: Query Builder Pattern

### Status: NOT STARTED
- **Estimated Effort:** 8-12 hours

### Scope:
1. Create fluent query builder for complex SQL queries
2. Focus on high-impact repositories:
   - TrendRepository
   - MatchupRepository
   - SoloPerformanceRepository
3. Create MatchQueryBuilder for match history filtering
4. Update 5-8 repository methods

---

## Phase 3: RBAC Infrastructure

### Status: NOT STARTED
- **Estimated Effort:** 3-4 hours

### Scope:
1. Add role field to User entity
2. Add role claim on login/token generation
3. Create authorization policies (ReadOnly, Admin, Support)
4. Create RoleBasedEndpoint base class
5. Document RBAC patterns

---

## Phase 4: Frontend Composables

### Status: NOT STARTED
- **Estimated Effort:** 6-8 hours

### Composables to Create:
1. [ ] useAsyncData - Generic async data fetching
2. [ ] useQueryFilters - Queue/timeRange filters with URL sync
3. [ ] useToast - Global toast notifications
4. [ ] useDebounce - Debounce with ref support
5. [ ] usePageView - Page analytics tracking

### Impact:
- Reduces frontend code by ~200+ lines
- Improves UI consistency
- Better separation of concerns

---

## Phase 5: Distributed Tracing with Application Insights

### Status: NOT STARTED
- **Estimated Effort:** 4-6 hours

### Scope:
1. Install Application Insights NuGet package
2. Configure in Program.cs
3. Add correlation IDs to requests
4. Instrument key operations:
   - API endpoints
   - Database queries
   - Riot API calls
   - Background jobs
5. Create telemetry wrapper for structured logging

---

## Implementation Order

### Week 1: Endpoint Refactoring
- Day 1-2: Refactor Solo & Trend endpoints (11 endpoints)
- Day 3: Refactor Match & Other endpoints (5 endpoints)
- Day 4-5: Create/update tests for all refactored endpoints

### Week 2: Query Builder & RBAC
- Day 1-2: Implement query builder pattern
- Day 3-4: Create RBAC infrastructure
- Day 5: Documentation & testing

### Week 3: Frontend & Telemetry
- Day 1-3: Create frontend composables
- Day 4-5: Implement distributed tracing

---

## Success Criteria

### Phase 1 (Endpoints)
- ✅ All 16 endpoints use AuthorizationHelper.ValidateAndGetUser()
- ✅ All 16 endpoints use PuuidResolutionService
- ✅ All existing tests pass
- ✅ New tests cover refactored code paths

### Phase 2 (Query Builder)
- ✅ QueryBuilder interfaces defined
- ✅ 5-8 repositories refactored
- ✅ SQL queries more readable
- ✅ No regression in query performance

### Phase 3 (RBAC)
- ✅ Role column added to users table
- ✅ Authorization policies configured
- ✅ Role claims added to authentication
- ✅ Documentation updated

### Phase 4 (Frontend)
- ✅ 5 composables created & tested
- ✅ 50+ lines of code reused in views
- ✅ Vitest coverage > 80%
- ✅ No breaking changes to existing components

### Phase 5 (Telemetry)
- ✅ Application Insights configured
- ✅ Custom telemetry events created
- ✅ Dashboard created for monitoring
- ✅ Alerts configured for errors

---

## Dependencies & Risks

### Dependencies:
- Phase 1 → Phase 2 (Query builder builds on refactored endpoints)
- Phase 1 → Phase 3 (RBAC uses refactored endpoints)
- Phase 4 (Frontend) is independent

### Risks:
- **Data-intensive endpoints** may need performance tuning (CsPerMinuteTrendEndpoint)
- **Query builder** may cause temporary regression (test thoroughly)
- **RBAC migration** needs careful database migration (add default roles)

---

## Notes for Implementation
- Always update tests alongside refactoring
- Keep git commits small and focused
- Create separate PR for each phase if possible
- Document breaking changes (if any) in migration guide
