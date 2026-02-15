# Feature: CS Per Minute Chart

## Problem Statement
CS/min is identified as a Tier 2 improvement metric (high impact + moderate difficulty) in the research, directly contributing to gold income and mid-game power spikes. Players need visibility into their farming consistency and efficiency trends to improve their economic foundation, which feeds into the #1 win predictor (gold differential).

## Proposed Solution
Add a "CS Per Minute" chart showing farming efficiency over time with role and rank-appropriate benchmarks. This helps players track their economic foundation improvement, which directly contributes to gold leads that predict game outcomes.

## User Stories
### Primary User Story
As a player focused on mechanical improvement, I want to see my CS/min trends over time so I can track my farming consistency and efficiency improvements.

### Additional User Stories
- As a laner, I want to compare my CS/min to rank-appropriate benchmarks to understand if my farming is holding me back
- As a competitive player, I want to see CS/min trends across different champions to identify champion-specific farming strengths/weaknesses
- As an improving player, I want to track my progress toward +0.5 CS/min improvement targets from research

## Requirements

### Functional Requirements
1. Display CS per minute as a line chart over time
2. Show role and rank-appropriate benchmark lines (e.g., ADC targets higher than Support)
3. Include champion-specific context where relevant
4. Color-code performance: green for above-average, yellow for average, red for below
5. Default to last 20 games with expand option for full season
6. Show both CS/min and total CS for context
7. Filter out games shorter than 15 minutes for accuracy

### Non-Functional Requirements
- **Performance**: Smooth rendering with CS data from match participants
- **Security**: Use existing authenticated match data
- **Accessibility**: Clear benchmark lines and performance indicators
- **Compatibility**: Responsive design across screen sizes

## Technical Approach

### Backend Changes
**Language**: C#
**Components**:
- [ ] Data models: `server/Application/DTOs/Solo/CsTrendDto.cs`
- [ ] Business logic: `server/Application/Services/FarmingAnalyticsService.cs` - new service
- [ ] API endpoints: `server/Application/Endpoints/Solo/GetCsTrend.cs`
- [ ] Database queries: Calculate CS/min from participants table with game duration

### Frontend Changes
**Framework**: Vue 3

**Components**:
- [ ] UI components: `client/src/components/solo/CsChart.vue`
- [ ] Chart integration: Chart.js line chart with role-specific benchmarks
- [ ] State management: Integrate with existing SoloPage data flow
- [ ] Styles: Follow TrendChartCard patterns for consistency

### Database Changes
**Database**: MySQL

**Schema Changes**:
- [ ] Uses existing `match_participants.total_minions_killed` column
- [ ] Uses existing `matches.game_duration` for per-minute calculation
- [ ] May add computed column or index for CS/min if performance requires

### API Contracts
#### Get CS Per Minute Trend
```
GET /api/solo/cs-trend?queue={queue}&timeRange={range}
```
**Response**:
```json
{
  "success": true,
  "data": [
    {
      "matchId": "NA1_4567890123",
      "timestamp": "2026-02-14T20:30:00Z",
      "gameIndex": 1,
      "totalCs": 185,
      "csPerMinute": 6.8,
      "gameDuration": 1635,
      "role": "ADC",
      "championName": "Jinx",
      "lanePhaseCs": 142
    }
  ],
  "summary": {
    "averageCsPerMinute": 6.2,
    "roleTarget": 7.0,
    "rankAverage": 5.8,
    "improvementTarget": 6.7,
    "trend": "stable"
  }
}
```

## Testing Strategy

### Unit Tests
**Frameworks**: xUnit (backend), Vitest (frontend)

- [ ] Test CS per minute calculation accuracy
- [ ] Test role-specific benchmark calculation
- [ ] Test filtering of games shorter than 15 minutes
- [ ] Test chart rendering with various CS ranges

### Integration Tests
- [ ] Test API endpoint with different roles and champions
- [ ] Test chart performance with large datasets
- [ ] Test error handling for missing CS data

### Manual Testing Scenarios
1. **High CS Roles**: Verify ADC/Mid have higher targets than Support/Jungle
2. **Champion Context**: Verify farming-heavy vs roam-heavy champions show appropriately
3. **Short Games**: Verify games <15min are filtered out for accuracy
4. **Improvement Tracking**: Verify +0.5 CS/min improvements are clearly visible
5. **Rank Benchmarks**: Verify targets scale appropriately with rank

## Validation Criteria
Feature is considered complete when:
- [ ] Chart accurately displays CS per minute trends over time
- [ ] Role and rank-appropriate benchmarks provide meaningful targets
- [ ] Short games are appropriately filtered for accurate trending
- [ ] Color coding clearly indicates farming performance levels
- [ ] Champion context helps explain CS variations
- [ ] Chart integrates well into Solo dashboard without overcrowding
- [ ] Performance acceptable with full season of data

## Design Notes
- Position in Zone 3 of AnalysisLayout as a core mechanical skill metric
- Use CS/min scaling to normalize for different game lengths
- Role-specific targets: ADC/Mid higher than Support/Jungle
- Filter games <15 minutes as they skew CS/min calculations unfairly
- Research shows +0.5 CS/min as realistic improvement target - highlight this
- Color scheme should emphasize economic impact (green = strong economy)
- Consider showing relationship to gold at 15 metric in tooltip
- Include educational note that CS improvement directly feeds gold differential (79% win predictor)