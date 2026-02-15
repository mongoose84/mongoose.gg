# Feature: Deaths Over Time Chart

## Problem Statement
Deaths are the single most actionable metric for player improvement according to academic research (win-prediction-metrics-research.md), yet players currently have no visibility into their death trends over time. Each death causes ~300 gold advantage to enemies, 20-60 seconds of map pressure loss, and potential objective losses, creating cascading negative effects that are entirely preventable through better decision-making.

## Proposed Solution
Add a "Deaths Over Time" chart to the Solo dashboard showing death trends with game-by-game data points. This chart will help players track their most impactful metric for improvement and identify patterns in their death frequency.

## User Stories
### Primary User Story
As a player looking to improve, I want to see my death trends over time so that I can track my progress on the most actionable metric for winning more games.

### Additional User Stories
- As a player, I want to see game-by-game deaths data so I can identify specific games where I died too frequently
- As a competitive player, I want to compare my recent death rate to my overall average to see if I'm improving
- As a player, I want to easily identify my target death reduction goal based on my current trends

## Requirements

### Functional Requirements
1. Display deaths per game as a line chart over time
2. Show rolling average trend line (e.g., 10-game rolling average)
3. Include target line based on rank-appropriate death rates
4. Default to last 20 games with expand option for full season
5. Color-code trend: green when improving (deaths decreasing), red when worsening
6. Show individual game data points with hover details
7. Highlight games with unusually high death counts

### Non-Functional Requirements
- **Performance**: Chart renders smoothly with up to 200+ games of data
- **Security**: Use existing authenticated match data
- **Accessibility**: Color-blind friendly colors, proper contrast ratios
- **Compatibility**: Responsive design for various screen sizes

## Technical Approach

### Backend Changes
**Language**: C#
**Components**:
- [ ] Data models: `server/Application/DTOs/Solo/DeathsTrendDto.cs`
- [ ] Business logic: `server/Application/Services/SoloAnalyticsService.cs` - add deaths trend calculation
- [ ] API endpoints: `server/Application/Endpoints/Solo/GetDeathsTrend.cs`
- [ ] Database queries: SQL query to extract deaths per match from participants table

### Frontend Changes
**Framework**: Vue 3

**Components**:
- [ ] UI components: `client/src/components/solo/DeathsChart.vue`
- [ ] Chart integration: Extends existing Chart.js patterns from WinrateChart
- [ ] State management: Add to existing SoloPage data fetching
- [ ] Styles: Follow TrendChartCard pattern for consistency

### Database Changes
**Database**: MySQL

**Schema Changes**:
- [ ] No new tables required
- [ ] Uses existing `match_participants.deaths` column
- [ ] May add index on `(summoner_id, match_timestamp)` if not exists for performance

### API Contracts
#### Get Deaths Trend
```
GET /api/solo/deaths-trend?queue={queue}&timeRange={range}
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
      "deaths": 3,
      "rollingAverage": 3.8,
      "championName": "Jinx",
      "gameLength": 1845
    }
  ],
  "summary": {
    "averageDeaths": 4.2,
    "overallAverage": 4.8,
    "trend": "improving",
    "targetDeaths": 3.5
  }
}
```

## Testing Strategy

### Unit Tests
**Frameworks**: xUnit (backend), Vitest (frontend)

- [ ] Test deaths trend calculation with various data sets
- [ ] Test rolling average computation accuracy
- [ ] Test chart rendering with different data ranges
- [ ] Test color-coding logic for trend determination

### Integration Tests
- [ ] Test API endpoint with different queue filters
- [ ] Test chart interaction with expand/collapse functionality
- [ ] Test error handling for missing match data

### Manual Testing Scenarios
1. **Improvement Trend**: Verify chart shows green trend when deaths decrease over time
2. **Worsening Trend**: Verify chart shows red trend when deaths increase
3. **Hover Details**: Verify tooltip shows match details, champion, deaths, date
4. **Expand Functionality**: Verify seamless transition from 20 games to full season
5. **Target Line**: Verify target line appears at appropriate level for player's rank

## Validation Criteria
Feature is considered complete when:
- [ ] Chart displays deaths per game over time with proper scaling
- [ ] Rolling average trend line accurately represents improvement/decline
- [ ] Color coding correctly reflects improving (green) vs worsening (red) trends
- [ ] Target line helps players understand their improvement goal
- [ ] Chart integrates seamlessly into existing Solo dashboard layout
- [ ] Performance is acceptable with large data sets
- [ ] All accessibility requirements met

## Design Notes
- Position in Zone 3 of AnalysisLayout, potentially replacing one LP chart when both Solo/Duo and Flex are available
- Use Chart.js Line chart matching existing WinrateChart patterns
- Color scheme: Green for improvement, red for regression, purple/neutral for stable
- Target line should be subtle (dotted) to avoid visual clutter
- Research shows average KDA across players is ~2.7, so target deaths should be contextual to rank/role